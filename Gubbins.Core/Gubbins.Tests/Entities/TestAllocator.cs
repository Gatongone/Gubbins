using Gubbins.Unsafe;

namespace Gubbins.Entities.Tests;

/// <summary>
/// Supplies an explicitly passed allocator for the entity tests.
/// </summary>
internal static class TestAllocator
{
    /// <summary>
    /// Keeps the allocator instance alive at a stable address for the lifetime of the test process.
    /// </summary>
    private static UnmanagedAllocator s_Impl;

    /// <summary>
    /// The allocator passed explicitly to repositories and chunks under test.
    /// </summary>
    public static readonly Allocator Shared = s_Impl.GetAllocator();
}
