using NUnitLite;

namespace Gubbins.Entities.TestRunner;

/// <summary>
/// Runs the entity tests inside this process, which is how they run on a machine where the test platform's host cannot
/// start.
/// </summary>
/// <remarks>
/// The host the <c>dotnet test</c> command uses opens a handle to its parent process to notice when that process goes
/// away, and an environment that refuses that handle aborts the run before a single test starts. NUnitLite discovers the
/// same fixtures and reports the same counts without a host: <c>dotnet run --project Entities/TestRunner --
/// --where "class=~EnableDisableTests"</c>.
/// </remarks>
internal static class Program
{
    /// <summary>
    /// Runs the tests of the assembly the fixtures are in.
    /// </summary>
    /// <param name="arguments">The arguments NUnitLite understands, such as <c>--where</c> or <c>--labels</c>.</param>
    /// <returns>The number of tests that failed.</returns>
    private static int Main(string[] arguments) =>
        new AutoRun(typeof(Tests.ChunkBehaviorTests).Assembly).Execute(arguments);
}
