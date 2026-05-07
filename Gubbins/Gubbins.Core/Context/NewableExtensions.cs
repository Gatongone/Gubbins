using Gubbins.Enhance;

namespace Gubbins.Context;

/// <summary>
/// Newable extensions for ISpawner.
/// </summary>
public static class NewableExtensions
{
    /// <summary>
    /// Converts the provided INewable object into an ISpawner object.
    /// </summary>
    internal static ISpawner ToSpawner(this INewable newable) => new NewableSpawner(newable);

    /// <summary>
    /// Implementation of ISpawner for INewable objects.
    /// </summary>
    private class NewableSpawner(INewable newable) : ISpawner
    {
        /// <inheritdoc />
        public object Spawn() => newable.New();
    }
}