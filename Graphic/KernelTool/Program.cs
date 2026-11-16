namespace Gubbins.Graphic.Kernels.Tool;

/// <summary>
/// The command line the build step calls: it compiles the kernels a project declares into the artifacts the backends load.
/// </summary>
/// <remarks>
/// A kernel file is named after the kernel it holds, with the <c>.Kernel</c> marker the build step recognises before the
/// extension, so <c>AddOne.Kernel.cs</c> becomes <c>AddOne.dxil</c> and the rest of the set.
/// </remarks>
internal static class Program
{
    /// <summary>
    /// The suffix a kernel file carries before its extension.
    /// </summary>
    private const string MARKER = ".Kernel";

    /// <summary>
    /// The switch that names the directory the artifacts are written to.
    /// </summary>
    private const string OUTPUT = "--output";

    /// <summary>
    /// The switch that names the Slang compiler, overriding the one on the path.
    /// </summary>
    private const string COMPILER = "--slangc";

    /// <summary>
    /// Compiles the kernels named on the command line.
    /// </summary>
    /// <param name="arguments">The switches and the kernel files.</param>
    /// <returns><see langword="0"/> when every kernel was written.</returns>
    private static int Main(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            Console.Error.WriteLine($"usage: --output <directory> [{COMPILER} <path>] <kernel file>...");
            return 2;
        }

        var output = string.Empty;
        var compiler = default(string);
        var sources = new List<string>();

        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case OUTPUT when index + 1 < arguments.Length:
                    output = arguments[++index];
                    break;

                case COMPILER when index + 1 < arguments.Length:
                    compiler = arguments[++index];
                    break;

                default:
                    sources.Add(arguments[index]);
                    break;
            }
        }

        if (output.Length == 0 || sources.Count == 0)
        {
            Console.Error.WriteLine($"usage: --output <directory> [{COMPILER} <path>] <kernel file>...");
            return 2;
        }

        var failures = 0;

        foreach (var source in sources)
        {
            var name = Path.GetFileNameWithoutExtension(source);

            if (name.EndsWith(MARKER, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - MARKER.Length);
            }

            try
            {
                foreach (var artifact in KernelCompiler.Compile(File.ReadAllText(source), output, name, compiler))
                {
                    Console.WriteLine($"{name}: {Path.GetFileName(artifact)}");
                }
            }
            catch (Exception exception) when (exception is KernelTranslationException or InvalidOperationException)
            {
                Console.Error.WriteLine($"{name}: {exception.Message}");
                failures++;
            }
        }

        return failures == 0 ? 0 : 1;
    }
}