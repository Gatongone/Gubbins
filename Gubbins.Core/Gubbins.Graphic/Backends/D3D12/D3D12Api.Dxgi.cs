#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The DXGI part of the D3D12 bootstrap: the video memory budgets the bridge reports as a working set and as the largest
/// segment.
/// </summary>
/// <remarks>
/// D3D12 itself has no budget query, so the numbers come from DXGI through the adapter the device was created on. The
/// budgets fluctuate while the application runs, so they are sampled once when the bridge is built, which is the right
/// thing for a limit that is meant to stay put.
/// </remarks>
internal static unsafe partial class D3D12Api
{
    /// <summary>
    /// The IID of <c>IDXGIFactory1</c>, which is the only interface <c>CreateDXGIFactory1</c> promises.
    /// </summary>
    internal static readonly Guid Factory1Id = new("770aae78-f26f-4dba-a829-253c83d1b387");

    /// <summary>
    /// The IID of <c>IDXGIFactory4</c>, the first factory that can find an adapter by LUID.
    /// </summary>
    internal static readonly Guid Factory4Id = new("1bc6ea02-ef36-464f-bf0c-21ca39e5168a");

    /// <summary>
    /// The IID of <c>IDXGIAdapter3</c>, the first adapter with a video memory budget query.
    /// </summary>
    internal static readonly Guid Adapter3Id = new("645967a4-1392-4310-a798-8053ce3e93fd");

    /// <summary>
    /// <c>DXGI_MEMORY_SEGMENT_GROUP_LOCAL</c>: the memory the device prefers, which is VRAM on a discrete GPU.
    /// </summary>
    internal const int MEMORY_SEGMENT_GROUP_LOCAL = 0;

    /// <summary>
    /// <c>DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL</c>: system memory the device can still reach.
    /// </summary>
    internal const int MEMORY_SEGMENT_GROUP_NON_LOCAL = 1;

    /// <summary>
    /// The vtable slot of <c>ID3D12Device::GetAdapterLuid</c>: three <c>IUnknown</c> slots, four <c>ID3D12Object</c>
    /// slots, then thirty-six <c>ID3D12Device</c> slots of which this is the last.
    /// </summary>
    internal const int GET_ADAPTER_LUID_SLOT = 43;

    /// <summary>
    /// The vtable slot of <c>IDXGIFactory4::EnumAdapterByLuid</c>.
    /// </summary>
    /// <remarks>
    /// Seven <c>IUnknown</c> and <c>IDXGIObject</c> slots, five <c>IDXGIFactory</c> slots, two <c>IDXGIFactory1</c>
    /// slots, eleven <c>IDXGIFactory2</c> slots, one <c>IDXGIFactory3</c> slot, then this one, which leaves
    /// <c>EnumWarpAdapter</c> at <c>27</c>.
    /// </remarks>
    internal const int ENUM_ADAPTER_BY_LUID_SLOT = 26;

    /// <summary>
    /// The vtable slot of <c>IDXGIAdapter3::QueryVideoMemoryInfo</c>.
    /// </summary>
    internal const int QUERY_VIDEO_MEMORY_INFO_SLOT = 14;

    private static readonly nint dxgiLibrary = LoadDxgiLibrary();

    private static readonly delegate* unmanaged<uint, Guid*, nint*, int> createFactory2 = CreateFactory2Pointer();

    private static readonly delegate* unmanaged<Guid*, nint, nint*, int> createFactory1 = CreateFactory1Pointer();

    /// <summary>
    /// Queries the video memory budget of the adapter behind a device.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="localBudget">When the method returns, the budget of the local memory segment in bytes.</param>
    /// <param name="nonLocalBudget">When the method returns, the budget of the non-local memory segment in bytes.</param>
    /// <returns><see langword="true"/> if the budgets were read; otherwise, <see langword="false"/>.</returns>
    internal static bool TryQueryBudgets(nint device, out ulong localBudget, out ulong nonLocalBudget)
    {
        localBudget = 0;
        nonLocalBudget = 0;

        nint factory = 0;
        nint adapter = 0;

        try
        {
            if (!TryCreateFactory(&factory))
            {
                return false;
            }

            var luid = default(Luid);
            if (!TryGetAdapterLuid(device, &luid))
            {
                return false;
            }

            var adapterId = Adapter3Id;
            var enumerate = (delegate* unmanaged<nint, Luid, Guid*, nint*, int>)Method(factory, ENUM_ADAPTER_BY_LUID_SLOT);
            if (enumerate(factory, luid, &adapterId, &adapter) < 0 || adapter == 0)
            {
                return false;
            }

            var query = (delegate* unmanaged<nint, uint, int, DxgiQueryVideoMemoryInfo*, int>)Method(adapter, QUERY_VIDEO_MEMORY_INFO_SLOT);

            var local = default(DxgiQueryVideoMemoryInfo);
            if (query(adapter, 0, MEMORY_SEGMENT_GROUP_LOCAL, &local) < 0)
            {
                return false;
            }

            localBudget = local.Budget;

            // The non-local budget is a courtesy: the local budget on its own already describes what the adapter can hold,
            // so a driver that reports nothing for the other segment does not make the query fail.
            var nonLocal = default(DxgiQueryVideoMemoryInfo);
            if (query(adapter, 0, MEMORY_SEGMENT_GROUP_NON_LOCAL, &nonLocal) >= 0)
            {
                nonLocalBudget = nonLocal.Budget;
            }

            return true;
        }
        finally
        {
            Release(adapter);
            Release(factory);
        }
    }

    /// <summary>
    /// Reads the LUID that ties a device to the DXGI adapter it was created on.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="luid">When the method returns, the adapter LUID.</param>
    /// <returns><see langword="true"/> if the LUID was read; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The interface declares <c>GetAdapterLuid</c> as returning a <c>LUID</c> by value, but the slot is called the midl
    /// way for a struct return: the caller passes the address of the return slot and the method hands that same address
    /// back. Calling it as if the value came back in a register is what makes this slot fault, so the header's
    /// declaration is not the shape to call it with.
    /// </remarks>
    private static bool TryGetAdapterLuid(nint device, Luid* luid)
    {
        var getLuid = (delegate* unmanaged<nint, Luid*, Luid*>)Method(device, GET_ADAPTER_LUID_SLOT);

        return getLuid(device, luid) != null;
    }

    /// <summary>
    /// Creates a factory that can find an adapter by LUID.
    /// </summary>
    /// <param name="factory">When the method returns, the <c>IDXGIFactory4</c>, or <see langword="0"/>.</param>
    /// <returns><see langword="true"/> if the factory was created; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <c>CreateDXGIFactory2</c> is asked for <c>IDXGIFactory4</c> directly. When only the older entry point is available
    /// the factory it returns is queried for the newer interface, because <c>CreateDXGIFactory1</c> only promises
    /// <c>IDXGIFactory1</c> and fails with <c>E_POINTER</c> for any other interface, even one the runtime could produce.
    /// </remarks>
    private static bool TryCreateFactory(nint* factory)
    {
        *factory = 0;

        var factory4Id = Factory4Id;

        if (createFactory2 != null && createFactory2(0, &factory4Id, factory) >= 0 && *factory != 0)
        {
            return true;
        }

        if (createFactory1 == null)
        {
            return false;
        }

        var factory1Id = Factory1Id;
        nint basic = 0;
        if (createFactory1(&factory1Id, 0, &basic) < 0 || basic == 0)
        {
            return false;
        }

        // IUnknown::QueryInterface is at index zero.
        var queryInterface = (delegate* unmanaged<nint, Guid*, nint*, int>)Method(basic, 0);
        var result = queryInterface(basic, &factory4Id, factory);

        Release(basic);

        return result >= 0 && *factory != 0;
    }

    /// <summary>
    /// Finds the DXGI runtime.
    /// </summary>
    /// <returns>The library handle, or <see langword="0"/> when DXGI is not available.</returns>
    private static nint LoadDxgiLibrary() => NativeLibrary.TryLoad("dxgi.dll", out var handle) ? handle : 0;

    /// <summary>
    /// Resolves <c>CreateDXGIFactory2</c>, which is allowed to be missing because the budgets are optional.
    /// </summary>
    /// <returns>The entry point, or <see langword="null"/> when it cannot be resolved.</returns>
    private static delegate* unmanaged<uint, Guid*, nint*, int> CreateFactory2Pointer()
    {
        if (dxgiLibrary != 0 && NativeLibrary.TryGetExport(dxgiLibrary, "CreateDXGIFactory2", out var address))
        {
            return (delegate* unmanaged<uint, Guid*, nint*, int>)address;
        }

        return null;
    }

    /// <summary>
    /// Resolves <c>CreateDXGIFactory1</c>, the fallback when the newer entry point is missing.
    /// </summary>
    /// <returns>The entry point, or <see langword="null"/> when it cannot be resolved.</returns>
    private static delegate* unmanaged<Guid*, nint, nint*, int> CreateFactory1Pointer()
    {
        if (dxgiLibrary != 0 && NativeLibrary.TryGetExport(dxgiLibrary, "CreateDXGIFactory1", out var address))
        {
            return (delegate* unmanaged<Guid*, nint, nint*, int>)address;
        }

        return null;
    }
}

/// <summary>
/// <c>LUID</c>, 8 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Luid
{
    public uint LowPart;
    public int HighPart;
}

/// <summary>
/// <c>DXGI_QUERY_VIDEO_MEMORY_INFO</c>: four 64-bit counters, 32 bytes.
/// </summary>
/// <remarks>
/// The last two fields are about reservations rather than about what is in use, which is why the bridge only reads the
/// first two: the budget is the ceiling, and the current usage is what the adapter believes this process already holds.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct DxgiQueryVideoMemoryInfo
{
    public ulong Budget;
    public ulong CurrentUsage;
    public ulong AvailableForReservation;
    public ulong CurrentReservation;
}

#endif
