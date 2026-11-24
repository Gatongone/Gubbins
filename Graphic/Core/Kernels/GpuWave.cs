namespace Gubbins.Graphic;

/// <summary>
/// The calls a kernel makes over the lanes of a wave, written as ordinary methods so that a kernel is still C# the
/// compiler accepts.
/// </summary>
/// <remarks>
/// <para>
/// A wave is the set of threads the device issues together, and the calls below reduce over it: what one lane reads is a
/// value the other lanes contributed. C# has no such set, so like the group calls of <see cref="GpuIntrinsics"/> these are
/// declared to let a kernel compile and are refused when the central processor reaches one, instead of answering with the
/// value of the calling lane and making a kernel that looks like it ran.
/// </para>
/// <para>
/// A kernel that uses them is exercised on the device. A kernel that does not can be run on the central processor as the
/// reference the device result is compared against, which is the point of the two sharing one declaration.
/// </para>
/// </remarks>
public static class GpuWave
{
    /// <summary>
    /// Sums a value across the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The sum, on every active lane.</returns>
    public static int Sum(int value) => throw Unavailable("GpuWave.Sum");

    /// <summary>
    /// Sums a value across the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The sum, on every active lane.</returns>
    public static uint Sum(uint value) => throw Unavailable("GpuWave.Sum");

    /// <summary>
    /// Sums a value across the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The sum, on every active lane.</returns>
    public static float Sum(float value) => throw Unavailable("GpuWave.Sum");

    /// <summary>
    /// Takes the smallest value of the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The smallest value, on every active lane.</returns>
    public static int Min(int value) => throw Unavailable("GpuWave.Min");

    /// <summary>
    /// Takes the smallest value of the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The smallest value, on every active lane.</returns>
    public static uint Min(uint value) => throw Unavailable("GpuWave.Min");

    /// <summary>
    /// Takes the smallest value of the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The smallest value, on every active lane.</returns>
    public static float Min(float value) => throw Unavailable("GpuWave.Min");

    /// <summary>
    /// Takes the largest value of the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The largest value, on every active lane.</returns>
    public static int Max(int value) => throw Unavailable("GpuWave.Max");

    /// <summary>
    /// Takes the largest value of the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The largest value, on every active lane.</returns>
    public static uint Max(uint value) => throw Unavailable("GpuWave.Max");

    /// <summary>
    /// Takes the largest value of the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The largest value, on every active lane.</returns>
    public static float Max(float value) => throw Unavailable("GpuWave.Max");

    /// <summary>
    /// Multiplies a value across the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The product, on every active lane.</returns>
    public static int Product(int value) => throw Unavailable("GpuWave.Product");

    /// <summary>
    /// Multiplies a value across the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The product, on every active lane.</returns>
    public static uint Product(uint value) => throw Unavailable("GpuWave.Product");

    /// <summary>
    /// Multiplies a value across the active lanes of the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The product, on every active lane.</returns>
    public static float Product(float value) => throw Unavailable("GpuWave.Product");

    /// <summary>
    /// Sums the values of the lanes below the calling one in the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The sum of the lanes before this one, which is zero for the first lane.</returns>
    public static int PrefixSum(int value) => throw Unavailable("GpuWave.PrefixSum");

    /// <summary>
    /// Sums the values of the lanes below the calling one in the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The sum of the lanes before this one, which is zero for the first lane.</returns>
    public static uint PrefixSum(uint value) => throw Unavailable("GpuWave.PrefixSum");

    /// <summary>
    /// Sums the values of the lanes below the calling one in the wave.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns>The sum of the lanes before this one, which is zero for the first lane.</returns>
    public static float PrefixSum(float value) => throw Unavailable("GpuWave.PrefixSum");

    /// <summary>
    /// Reports whether any active lane of the wave holds a true value.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns><see langword="true"/> when a lane does; otherwise, <see langword="false"/>.</returns>
    public static bool Any(bool value) => throw Unavailable("GpuWave.Any");

    /// <summary>
    /// Reports whether every active lane of the wave holds a true value.
    /// </summary>
    /// <param name="value">The value of the calling lane.</param>
    /// <returns><see langword="true"/> when every lane does; otherwise, <see langword="false"/>.</returns>
    public static bool All(bool value) => throw Unavailable("GpuWave.All");

    /// <summary>
    /// Builds the exception a call on the central processor is refused with.
    /// </summary>
    /// <param name="name">The call that was reached.</param>
    /// <returns>The exception to throw.</returns>
    private static NotSupportedException Unavailable(string name)
        => new($"{name} reduces over the lanes of a wave and has no meaning on the central processor; run the kernel on the device, or keep the kernel free of wave operations so that it can run on both.");
}
