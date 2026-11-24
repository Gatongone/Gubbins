using System.Reflection;
using Gubbins.Graphic.Kernels;

namespace Gubbins.Graphic.Artifacts;

/// <summary>
/// The build step that turns the kernels a program declares into the artifacts the backends load.
/// </summary>
/// <remarks>
/// <para>
/// The source generator translated every kernel while the program compiled and left the program, the name and the group
/// size in a constant of the assembly. This tool reads those constants from the assembly without running it, writes the
/// Slang beside the artifacts, and runs <c>slangc</c> once per target, so the runtime only reads files.
/// </para>
/// <para>
/// The assembly is read with a metadata context rather than loaded: the program being built is not one this tool can
/// execute, and a kernel artifact must not depend on the program being runnable here.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>
    /// The switch that names the assembly the kernels were compiled into.
    /// </summary>
    private const string ASSEMBLY = "--assembly";

    /// <summary>
    /// The switch that names the directory the artifacts are written to.
    /// </summary>
    private const string OUTPUT = "--output";

    /// <summary>
    /// The switch that names the Slang compiler, overriding the one on the path.
    /// </summary>
    private const string COMPILER = "--slangc";

    /// <summary>
    /// The type the generator puts the constants of every kernel in.
    /// </summary>
    private const string REGISTRY = "Gubbins.Graphic.Generated.__GubbinsKernels";

    /// <summary>
    /// Reads the kernels of an assembly and compiles them.
    /// </summary>
    /// <param name="arguments">The switches and their values.</param>
    /// <returns><see langword="0"/> when every kernel that could be compiled was.</returns>
    private static int Main(string[] arguments)
    {
        string? assembly = null;
        string? output = null;
        string? slangc = null;

        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case ASSEMBLY when index + 1 < arguments.Length:
                    assembly = arguments[++index];
                    break;

                case OUTPUT when index + 1 < arguments.Length:
                    output = arguments[++index];
                    break;

                case COMPILER when index + 1 < arguments.Length:
                    slangc = arguments[++index];
                    break;

                default:
                    return Usage($"'{arguments[index]}' is not a switch this tool knows.");
            }
        }

        if (assembly is null || output is null)
        {
            return Usage("An assembly and an output directory are required.");
        }

        if (!File.Exists(assembly))
        {
            Console.Error.WriteLine($"The assembly '{assembly}' was not found.");
            return 2;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(assembly))!;
        var paths = new List<string> { Path.GetFullPath(assembly) };

        paths.AddRange(((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator));
        paths.AddRange(Directory.GetFiles(directory, "*.dll"));

        using var context = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct(StringComparer.OrdinalIgnoreCase)));
        var image = context.LoadFromAssemblyPath(Path.GetFullPath(assembly));
        var registry = image.GetType(REGISTRY);

        if (registry is null)
        {
            Console.WriteLine("No kernel was declared by this assembly.");
            return 0;
        }

        var failures = 0;

        foreach (var kernel in registry.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            var name = Constant<string>(kernel, "Name");
            var slang = Constant<string>(kernel, "Slang");

            if (name is null || slang is null)
            {
                failures++;
                continue;
            }

            try
            {
                foreach (var artifact in KernelCompiler.CompileSlang(slang, output, name, slangc))
                {
                    Console.WriteLine($"{name}: {Path.GetFileName(artifact)}");
                }
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("slangc was not found", StringComparison.Ordinal))
            {
                // A machine without the compiler still builds; it just cannot produce the artifacts, and says so rather
                // than failing a build that may be running on a machine that never runs a kernel.
                Console.WriteLine($"{name}: skipped, {exception.Message}");
                return 0;
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine($"{name}: {exception.Message}");
                failures++;
            }
        }

        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// Reads a constant of a generated kernel entry.
    /// </summary>
    /// <typeparam name="T">The type of the constant.</typeparam>
    /// <param name="kernel">The generated entry.</param>
    /// <param name="name">The name of the field.</param>
    /// <returns>The value, or the default when the field is not there.</returns>
    private static T? Constant<T>(Type kernel, string name) where T : class
        => kernel.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue() as T;

    /// <summary>
    /// Writes the command line and returns the code for a usage error.
    /// </summary>
    /// <param name="message">What was wrong.</param>
    /// <returns>The exit code.</returns>
    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine($"usage: {ASSEMBLY} <path> {OUTPUT} <directory> [{COMPILER} <path>]");

        return 2;
    }
}
