namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Turns a C# kernel into the artifacts the backends load: it lowers the source to <see cref="KernelProgram"/> and writes
/// the file each backend reads into a directory.
/// </summary>
/// <remarks>
/// <para>
/// This is the build step of the kernel pipeline, and it needs nothing installed: the translation, the SPIR-V binary and
/// the PTX text are all produced here, in C#. Run it once per kernel while building, and the runtime only reads files.
/// </para>
/// <para>
/// One translation of a kernel becomes the artifact each backend loads: <c>hlsl</c> for D3D12, which the system's own
/// compiler turns into bytecode when the pipeline is created, <c>spv</c> for Vulkan, <c>comp</c> for OpenGL, <c>metal</c>
/// for Metal and <c>ptx</c> for CUDA, which the platform turns into a <c>metallib</c> or the driver compiles for the card.
/// </para>
/// </remarks>
public static class KernelCompiler
{
    /// <summary>
    /// The extension of the source D3D12 compiles.
    /// </summary>
    public const string HLSL = ".hlsl";

    /// <summary>
    /// The extension of the SPIR-V binary Vulkan loads.
    /// </summary>
    public const string SPIRV = ".spv";

    /// <summary>
    /// The extension of the source OpenGL compiles.
    /// </summary>
    public const string GLSL = ".comp";

    /// <summary>
    /// The extension of the source Metal compiles.
    /// </summary>
    public const string METAL = ".metal";

    /// <summary>
    /// The extension of the text CUDA compiles.
    /// </summary>
    public const string PTX = ".ptx";

    /// <summary>
    /// Compiles one C# kernel into a directory of artifacts.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <param name="outputDirectory">The directory the artifacts are written to.</param>
    /// <param name="kernelName">The kernel name, which is the file name of every artifact.</param>
    /// <param name="entryName">The kernel to compile, or <see langword="null"/> to take the only one there is.</param>
    /// <returns>The artifacts that were written, in the order they were produced.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the kernel is outside the supported subset.</exception>
    public static IReadOnlyList<string> Compile(string source, string outputDirectory, string kernelName, string? entryName = null)
        => Compile(KernelTranspiler.Transpile(source, entryName), outputDirectory, kernelName);

    /// <summary>
    /// Writes the artifacts of one program into a directory.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <param name="outputDirectory">The directory the artifacts are written to.</param>
    /// <param name="kernelName">The kernel name, which is the file name of every artifact.</param>
    /// <returns>The artifacts that were written, in the order they were produced.</returns>
    public static IReadOnlyList<string> Compile(KernelProgram program, string outputDirectory, string kernelName)
    {
        if (program is null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        }

        if (string.IsNullOrEmpty(kernelName))
        {
            throw new ArgumentException("A kernel name is required.", nameof(kernelName));
        }

        Directory.CreateDirectory(outputDirectory);

        var artifacts = new List<string>();

        foreach (var artifact in new[]
                 {
                     Write(outputDirectory, kernelName + HLSL, new KernelHlsl().Emit(program)),
                     Write(outputDirectory, kernelName + GLSL, new KernelGlsl().Emit(program)),
                     Write(outputDirectory, kernelName + METAL, new KernelMetal().Emit(program)),
                     Write(outputDirectory, kernelName + SPIRV, new KernelSpirV().Emit(program)),
                     Write(outputDirectory, kernelName + PTX, new KernelPtx().Emit(program))
                 })
        {
            if (artifact is not null)
            {
                artifacts.Add(artifact);
            }
        }

        return artifacts;
    }

    /// <summary>
    /// Writes one artifact, if it holds something.
    /// </summary>
    /// <param name="directory">The directory to write it into.</param>
    /// <param name="name">The file name.</param>
    /// <param name="content">The bytes of the artifact.</param>
    /// <returns>The artifact, or <see langword="null"/> when it was not written.</returns>
    private static string? Write(string directory, string name, byte[] content)
    {
        if (content.Length == 0)
        {
            return null;
        }

        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, content);

        return path;
    }

    /// <summary>
    /// Writes one artifact from its text.
    /// </summary>
    /// <param name="directory">The directory to write it into.</param>
    /// <param name="name">The file name.</param>
    /// <param name="content">The text of the artifact.</param>
    /// <returns>The artifact, or <see langword="null"/> when it was not written.</returns>
    private static string? Write(string directory, string name, string content)
        => Write(directory, name, System.Text.Encoding.UTF8.GetBytes(content));
}
