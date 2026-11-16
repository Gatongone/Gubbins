#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The residency part of the D3D12 bootstrap: making the memory behind a segment node resident or evictable.
/// </summary>
/// <remarks>
/// D3D12 is the one API of the two with an explicit residency control. <c>Evict</c> tells the driver the memory may be
/// paged out - the contents are preserved, and the pages come back on the next access or on <c>MakeResident</c> - which is
/// exactly what a <see cref="UnifiedMemoryResidency.Discardable"/> block promises. It is the only lever a caller has to
/// stay inside the working set the DXGI budgets report.
/// </remarks>
internal static unsafe partial class D3D12Api
{
    /// <summary>
    /// The vtable slot of <c>ID3D12Device::MakeResident</c>.
    /// </summary>
    internal const int MAKE_RESIDENT_SLOT = 34;

    /// <summary>
    /// The vtable slot of <c>ID3D12Device::Evict</c>.
    /// </summary>
    internal const int EVICT_SLOT = 35;

    /// <summary>
    /// Tells the driver that the memory behind these pageable objects may be paged out.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="objects">The <c>ID3D12Pageable</c> objects to evict.</param>
    /// <param name="count">The number of objects.</param>
    /// <returns>The <c>HRESULT</c> of the call.</returns>
    internal static int Evict(nint device, nint* objects, uint count)
    {
        var evict = (delegate* unmanaged[Cdecl]<nint, uint, nint*, int>)Method(device, EVICT_SLOT);

        return evict(device, count, objects);
    }

    /// <summary>
    /// Tells the driver that the memory behind these pageable objects has to be resident again.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="objects">The <c>ID3D12Pageable</c> objects to make resident.</param>
    /// <param name="count">The number of objects.</param>
    /// <returns>The <c>HRESULT</c> of the call.</returns>
    internal static int MakeResident(nint device, nint* objects, uint count)
    {
        var makeResident = (delegate* unmanaged[Cdecl]<nint, uint, nint*, int>)Method(device, MAKE_RESIDENT_SLOT);

        return makeResident(device, count, objects);
    }
}

