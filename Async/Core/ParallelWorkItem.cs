using System.Runtime.CompilerServices;

namespace Gubbins.Async;

/// <summary>
/// A reusable thread pool work item that executes one worker range of a <see cref="PooledValueTaskSource"/>.
/// </summary>
/// <remarks>
/// On .NET 8+ the item implements <see cref="IThreadPoolWorkItem"/> so it can be queued without any wrapper
/// allocation. On older targets the cached <c>WaitCallback</c> in <see cref="PooledValueTaskSource"/> passes
/// the item as its state object, which also avoids boxing.
/// </remarks>
internal sealed class ParallelWorkItem
#if NET8_0_OR_GREATER
    : IThreadPoolWorkItem
#endif
{
    /// <summary>
    /// The operation currently owning this item.
    /// </summary>
    internal PooledValueTaskSource Owner = null!;

    /// <summary>
    /// The worker index this item always executes.
    /// </summary>
    internal readonly int Index;

    /// <param name="index">The worker index this item always executes.</param>
    public ParallelWorkItem(int index) => Index = index;

    /// <summary>
    /// Executes the owning operation's range for this worker index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute() => Owner.ExecuteWorker(Index);
}
