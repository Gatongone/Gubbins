
using System.Runtime.InteropServices;

namespace Gubbins.Graphic;

/// <summary>
/// The CUDA driver entry points the bridge calls. No toolkit is needed: the driver compiles the PTX a kernel is loaded
/// from, so the runtime that ships with the display driver is enough.
/// </summary>
internal static unsafe partial class CudaApi
{
    private const string Library = "nvcuda.dll";

    /// <summary>
    /// The flag that makes a managed allocation reachable from every device and the CPU.
    /// </summary>
    internal const uint AttachGlobal = 1;

    /// <summary>
    /// The flag that gives host memory a device address, so that both processors address the same bytes.
    /// </summary>
    internal const uint HostDeviceMap = 2;

    /// <summary>
    /// The flag that makes host memory addressable from every context.
    /// </summary>
    internal const uint HostPortable = 1;

    [DllImport(Library)] internal static extern int cuInit(uint flags);

    [DllImport(Library)] internal static extern int cuDeviceGet(out int device, int ordinal);

    [DllImport(Library)] internal static extern int cuCtxCreate_v2(out nint context, uint flags, int device);

    [DllImport(Library)] internal static extern int cuCtxDestroy_v2(nint context);

    [DllImport(Library)] internal static extern int cuCtxSynchronize();

    [DllImport(Library)] internal static extern int cuMemAllocManaged(out ulong pointer, nuint size, uint flags);

    [DllImport(Library)] internal static extern int cuMemAlloc_v2(out ulong pointer, nuint size);

    [DllImport(Library)] internal static extern int cuMemHostAlloc(out nint pointer, nuint size, uint flags);

    [DllImport(Library)] internal static extern int cuMemHostGetDevicePointer_v2(out ulong device, void* pointer, uint flags);

    [DllImport(Library)] internal static extern int cuMemFreeHost(nint pointer);

    [DllImport(Library)] internal static extern int cuMemcpyDtoH_v2(void* destination, ulong source, nuint size);

    [DllImport(Library)] internal static extern int cuMemcpyDtoD_v2(ulong destination, ulong source, nuint size);

    [DllImport(Library)] internal static extern int cuMemFree_v2(ulong pointer);

    [DllImport(Library)] internal static extern int cuModuleLoadData(out nint module, void* image);

    [DllImport(Library)] internal static extern int cuModuleUnload(nint module);

    [DllImport(Library)] internal static extern int cuModuleGetFunction(out nint function, nint module, string name);

    [DllImport(Library)] internal static extern int cuModuleGetGlobal_v2(out ulong pointer, out nuint size, nint module, string name);

    [DllImport(Library)] internal static extern int cuMemcpyHtoD_v2(ulong destination, void* source, nuint size);

    [DllImport(Library)] internal static extern int cuLaunchKernel(nint function, uint gridX, uint gridY, uint gridZ, uint blockX, uint blockY, uint blockZ, uint sharedMemory, nint stream, void** parameters, void** extra);

    [DllImport(Library)] internal static extern int cuGetErrorName(int error, out nint name);

    /// <summary>
    /// Reports a failing driver call.
    /// </summary>
    /// <param name="result">The result code.</param>
    /// <param name="what">The call that failed.</param>
    /// <exception cref="InvalidOperationException">Thrown with the driver's own name for the code.</exception>
    internal static void Check(int result, string what)
    {
        if (result == 0)
        {
            return;
        }

        cuGetErrorName(result, out var name);

        throw new InvalidOperationException($"{what} failed: {result} ({Marshal.PtrToStringAnsi(name)}).");
    }
}

