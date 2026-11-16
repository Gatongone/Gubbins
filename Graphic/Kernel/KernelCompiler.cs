using System.Diagnostics;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Turns a C# kernel into the artifacts the backends load: it translates the source to Slang and runs <c>slangc</c> once
/// per target, writing the files <c>GpuKernel.LoadFromDirectory</c> expects into a directory.
/// </summary>
/// <remarks>
/// <para>
/// This is the build step of the kernel pipeline. Run it once per kernel while building, and the runtime needs neither the
/// translator nor the compiler: it only reads the artifacts.
/// </para>
/// <para>
/// One translation of a kernel becomes the artifact each backend loads: <c>dxil</c> for D3D12, <c>spv</c> for Vulkan,
/// <c>comp</c> for OpenGL, <c>metal</c> for Metal and <c>ptx</c> for CUDA, which the platform turns into a <c>metallib</c>
/// or the driver compiles for the card when it has no such step. The PTX is not the shape a CUDA kernel is usually
/// launched with: the compiler passes the buffers in a constant of the module rather than as arguments, which the runtime
/// in this layer fills before the launch.
/// </para>
/// </remarks>
public static class KernelCompiler
{
    /// <summary>
    /// The name of the environment variable that holds the path of the Slang compiler.
    /// </summary>
    public const string SLANG_PATH_VARIABLE = "SLANGC";

    /// <summary>
    /// The profile every target is compiled with.
    /// </summary>
    private const string PROFILE = "cs_6_0";

    /// <summary>
    /// The target OpenGL loads.
    /// </summary>
    private const string GLSL = "glsl";

    /// <summary>
    /// The scoped barrier the GLSL compiler emits for a group synchronization.
    /// </summary>
    private const string SCOPED_BARRIER = "controlBarrier(gl_ScopeWorkgroup, gl_ScopeWorkgroup, gl_StorageSemanticsShared, gl_SemanticsAcquireRelease)";

    /// <summary>
    /// The barrier every OpenGL implementation has.
    /// </summary>
    private const string PLAIN_BARRIER = "barrier()";

    /// <summary>
    /// The line that requires the extension the scoped barrier belongs to.
    /// </summary>
    private const string EXTENSION_LINE = "#extension GL_KHR_memory_scope_semantics : require";

    /// <summary>
    /// What the compiler says when the machine it runs on has no downstream compiler for a target.
    /// </summary>
    private static readonly string[] MISSING_TOOLCHAIN =
    [
        "failed to load downstream compiler",
        "dxil library not found",
        "pass-through compiler not found",
    ];

    /// <summary>
    /// The name of the environment variable that holds the directory of a Metal toolchain.
    /// </summary>
    public const string METAL_PATH_VARIABLE = "METAL";

    /// <summary>
    /// The tool that runs a platform tool on macOS.
    /// </summary>
    private const string XCRUN = "xcrun";

    /// <summary>
    /// The argument that names the platform the tool runs against.
    /// </summary>
    private const string SDK = "-sdk";

    /// <summary>
    /// The platform the Metal toolchain belongs to.
    /// </summary>
    private const string PLATFORM = "macosx";

    /// <summary>
    /// Compiles one C# kernel into a directory of artifacts.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <param name="outputDirectory">The directory the artifacts are written to.</param>
    /// <param name="kernelName">The kernel name, which is the file name of every artifact.</param>
    /// <param name="slangc">The path of <c>slangc</c>, or <see langword="null"/> to look it up.</param>
    /// <returns>The artifacts that were written, in the order they were produced.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the kernel is outside the supported subset.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the compiler is missing or refuses the program.</exception>
    public static IReadOnlyList<string> Compile(string source, string outputDirectory, string kernelName, string? slangc = null)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        }

        if (string.IsNullOrEmpty(kernelName))
        {
            throw new ArgumentException("A kernel name is required.", nameof(kernelName));
        }

        var compiler = slangc ?? FindSlangC();
        Directory.CreateDirectory(outputDirectory);

        // The Slang program is written beside the artifacts it produces: it is the answer to what a kernel became, and it
        // saves a temporary directory nothing else would clean up.
        var program = Path.Combine(outputDirectory, kernelName + ".slang");
        File.WriteAllText(program, KernelTranspiler.Transpile(source));

        var targets = new (string Target, string Extension)[]
        {
            ("dxil", ".dxil"),
            ("spirv", ".spv"),
            ("glsl", ".comp"),
            ("metal", ".metal"),
            ("ptx", ".ptx")
        };

        var produced = new List<string> { program };

        foreach (var (target, extension) in targets)
        {
            var artifact = Path.Combine(outputDirectory, kernelName + extension);

            // A target whose downstream compiler is not on this machine is skipped: the artifacts a build writes are the
            // ones the machine it runs on can produce, and a D3D12 kernel comes from a Windows or a Linux build.
            if (Run(compiler, program, target, artifact))
            {
                if (target == GLSL)
                {
                    ReplaceScopedBarrier(artifact);
                }

                produced.Add(artifact);
            }
        }

        var metal = Path.Combine(outputDirectory, kernelName + ".metal");

        if (CompileMetal(metal, outputDirectory, kernelName))
        {
            produced.Add(Path.Combine(outputDirectory, kernelName + ".metallib"));
        }

        return produced;
    }

    /// <summary>
    /// Replaces the scoped barrier OpenGL implementations may not have with the plain one they all have.
    /// </summary>
    /// <param name="path">The GLSL program to rewrite.</param>
    /// <remarks>
    /// The barrier a group synchronizes with comes out of the compiler as the scoped form of <c>GL_KHR_memory_scope_semantics</c>,
    /// which an implementation is free not to support, and the drivers do not: a kernel that synchronizes fails to compile
    /// there while the rest of it is valid. The plain <c>barrier()</c> the language has ordered group memory since 4.3
    /// means the same thing for this layer, which only ever synchronizes the shared memory of a group.
    /// </remarks>
    private static void ReplaceScopedBarrier(string path)
    {
        var text = File.ReadAllText(path);

        if (!text.Contains(SCOPED_BARRIER, StringComparison.Ordinal))
        {
            return;
        }

        text = text.Replace(SCOPED_BARRIER, PLAIN_BARRIER);
        text = text.Replace(EXTENSION_LINE, string.Empty);

        File.WriteAllText(path, text);
    }

    /// <summary>
    /// Builds the library Metal devices load from the shading language source, when the machine has the toolchain.
    /// </summary>
    /// <param name="source">The shading language source the Slang compiler wrote.</param>
    /// <param name="outputDirectory">The directory the library is written to.</param>
    /// <param name="kernelName">The kernel name, which is the file name of the library.</param>
    /// <returns><see langword="true"/> when a library was built.</returns>
    /// <remarks>
    /// Metal is the one backend whose artifact no cross platform compiler can produce: a <c>metallib</c> comes from the
    /// platform's own toolchain. A machine that has it gets the library here; a machine that does not keeps the source,
    /// which a Metal device compiles when it creates the pipeline.
    /// </remarks>
    private static bool CompileMetal(string source, string outputDirectory, string kernelName)
    {
        if (!File.Exists(source))
        {
            return false;
        }

        var toolchain = FindMetalToolchain();

        if (toolchain is null)
        {
            return false;
        }

        var assembler = Path.Combine(outputDirectory, kernelName + ".air");
        var library = Path.Combine(outputDirectory, kernelName + ".metallib");

        if (!RunTool(toolchain.Value.Compiler, new[] { "-c", source, "-o", assembler }))
        {
            return false;
        }

        if (!RunTool(toolchain.Value.Linker, new[] { assembler, "-o", library }))
        {
            return false;
        }

        File.Delete(assembler);
        return File.Exists(library);
    }

    /// <summary>
    /// Finds the tools that turn shading language source into a library, when they are installed.
    /// </summary>
    /// <returns>
    /// The compiler and the linker with the arguments that name them, or <see langword="null"/> when the machine has
    /// neither.
    /// </returns>
    private static (string[] Compiler, string[] Linker)? FindMetalToolchain()
    {
        var directory = Environment.GetEnvironmentVariable(METAL_PATH_VARIABLE);

        if (!string.IsNullOrEmpty(directory))
        {
            var extension = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
            var compiler = Path.Combine(directory, "metal" + extension);
            var linker = Path.Combine(directory, "metallib" + extension);

            if (File.Exists(compiler) && File.Exists(linker))
            {
                return (new[] { compiler }, new[] { linker });
            }
        }

        return OperatingSystem.IsMacOS()
            ? (new[] { XCRUN, SDK, PLATFORM, "metal" }, new[] { XCRUN, SDK, PLATFORM, "metallib" })
            : null;
    }

    /// <summary>
    /// Runs a tool and reports whether it succeeded.
    /// </summary>
    /// <param name="command">The executable and the arguments that name the tool.</param>
    /// <param name="arguments">The arguments of this run.</param>
    /// <returns><see langword="true"/> when the tool reported success.</returns>
    private static bool RunTool(string[] command, string[] arguments)
    {
        var start = new ProcessStartInfo(command[0])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        for (var index = 1; index < command.Length; index++)
        {
            start.ArgumentList.Add(command[index]);
        }

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"{command[0]} could not be started.");

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode == 0;
    }
    /// <summary>
    /// Finds the Slang compiler.
    /// </summary>
    /// <returns>The path of <c>slangc</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the compiler cannot be found.</exception>
    private static string FindSlangC()
    {
        var configured = Environment.GetEnvironmentVariable(SLANG_PATH_VARIABLE);

        if (!string.IsNullOrEmpty(configured) && File.Exists(configured))
        {
            return configured;
        }

        var name = OperatingSystem.IsWindows() ? "slangc.exe" : "slangc";

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
            {
                continue;
            }

            var candidate = Path.Combine(directory, name);

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"slangc was not found; set {SLANG_PATH_VARIABLE} to its path.");
    }

    /// <summary>
    /// Runs the compiler for one target.
    /// </summary>
    /// <param name="compiler">The path of <c>slangc</c>.</param>
    /// <param name="program">The Slang file to compile.</param>
    /// <param name="target">The target to emit.</param>
    /// <param name="artifact">The file to write.</param>
    /// <returns><see langword="true"/> when the artifact was written; <see langword="false"/> when this machine has no compiler for the target.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the compiler refuses the program.</exception>
    private static bool Run(string compiler, string program, string target, string artifact)
    {
        var start = new ProcessStartInfo(compiler)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        start.ArgumentList.Add(program);
        start.ArgumentList.Add("-target");
        start.ArgumentList.Add(target);
        start.ArgumentList.Add("-profile");
        start.ArgumentList.Add(PROFILE);
        start.ArgumentList.Add("-entry");
        start.ArgumentList.Add(KernelTranspiler.ENTRY_POINT);
        start.ArgumentList.Add("-o");
        start.ArgumentList.Add(artifact);


        using var process = Process.Start(start)
                            ?? throw new InvalidOperationException($"slangc could not be started from '{compiler}'.");

        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0 && File.Exists(artifact))
        {
            return true;
        }

        if (MISSING_TOOLCHAIN.Any(output.Contains))
        {
            return false;
        }

        var command = $"{compiler} {string.Join(' ', start.ArgumentList)}";

        throw new InvalidOperationException($"slangc failed for target '{target}':{Environment.NewLine}{command}{Environment.NewLine}{output}");
    }
}
