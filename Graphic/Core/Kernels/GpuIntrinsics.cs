namespace Gubbins.Graphic;

/// <summary>
/// The calls a kernel makes that only mean something on the device, written as ordinary methods so that a kernel is
/// still C# the compiler accepts.
/// </summary>
/// <remarks>
/// <para>
/// A kernel is a normal method that is also translated for the device, so the two operations the shading language has and
/// C# does not, the group shared array and the group barrier, have to exist as C# declarations for the kernel to compile.
/// They do not have a central processor meaning: shared memory is the memory of a group of threads that run at the same
/// time, and a barrier is what makes those threads wait for each other, so calling one from the central processor cannot
/// mean what it means on the device.
/// </para>
/// <para>
/// The call is therefore refused at run time when the central processor reaches it, with a message that names the
/// operation, rather than quietly returning storage nothing synchronizes. A kernel that uses these calls is a device
/// kernel and is exercised by the device; a kernel that does not is the one the central processor can run as a reference.
/// </para>
/// </remarks>
public static class GpuIntrinsics
{
    /// <summary>
    /// The number of threads a group has when the kernel declares no size.
    /// </summary>
    /// <remarks>
    /// This is the same number the translator dispatches with and the callers divide an element count by, which is why it
    /// is a constant here rather than a literal in three places.
    /// </remarks>
    public const int DefaultThreadsPerGroup = 64;

    /// <summary>
    /// Declares the array the threads of a group address together.
    /// </summary>
    /// <typeparam name="T">The element type, which is one the device has.</typeparam>
    /// <param name="count">The number of elements.</param>
    /// <returns>The array, which is the memory of the group once the kernel runs on the device.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the central processor calls it: the memory belongs to a group of threads, and the central processor
    /// runs a kernel one thread at a time.
    /// </exception>
    public static Span<T> Shared<T>(int count) where T : unmanaged
        => throw new NotSupportedException(
            "Shared<T> is the memory of a group of threads and has no meaning on the central processor; run the kernel on the device, or keep the kernel free of group shared memory so that it can run on both.");

    /// <summary>
    /// Makes the threads of a group wait for each other, so that a write to shared memory is visible to the group.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when the central processor calls it: there are no other threads of the group to wait for.
    /// </exception>
    public static void Barrier()
        => throw new NotSupportedException(
            "Barrier() synchronizes the threads of a group and has no meaning on the central processor; run the kernel on the device, or keep the kernel free of group shared memory so that it can run on both.");
}
