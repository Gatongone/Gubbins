using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The subset of D3D12 this bridge needs, reached through COM vtables because the project has no COM bindings.
/// </summary>
/// <remarks>
/// <para>
/// Each interface is a pointer to a vtable, so a method call is an index into that table. The indices below are the ABI of
/// the interfaces D3D12 ships, which makes them as load bearing as the structure layouts: the tests assert the layouts, and
/// the harness is what proves the indices, because a wrong index calls an unrelated method and crashes instead of returning
/// an error.
/// </para>
/// <para>
/// The bridge never creates the device: the engine passes an <c>ID3D12Device</c>, a copy queue and the allocator it wants
/// used, exactly like the Vulkan bridge borrows a <c>VkDevice</c>.
/// </para>
/// </remarks>
internal static unsafe partial class D3D12Api
{
    /// <summary>
    /// The IID of <c>ID3D12Device</c>.
    /// </summary>
    internal static readonly Guid DeviceId = new("189819f1-1db6-4b57-be54-1821339b85f7");

    /// <summary>
    /// The IID of <c>ID3D12Heap</c>.
    /// </summary>
    internal static readonly Guid HeapId = new("6b3b2502-6e51-45b3-90ee-9884265e8df3");

    /// <summary>
    /// The IID of <c>ID3D12Resource</c>.
    /// </summary>
    internal static readonly Guid ResourceId = new("696442be-a72e-4059-bc79-5b5c98040fad");

    /// <summary>
    /// The IID of <c>ID3D12DescriptorHeap</c>.
    /// </summary>
    internal static readonly Guid DescriptorHeapId = new("8efb471d-616c-4f49-90f7-127bb763fa51");

    /// <summary>
    /// The IID of <c>ID3D12CommandQueue</c>.
    /// </summary>
    internal static readonly Guid CommandQueueId = new("0ec870a6-5d7e-4c22-8cfc-5baae07616ed");

    /// <summary>
    /// The IID of <c>ID3D12CommandAllocator</c>.
    /// </summary>
    internal static readonly Guid CommandAllocatorId = new("6102dee4-af59-4b09-b999-b44d73f09b24");

    /// <summary>
    /// The IID of <c>ID3D12GraphicsCommandList</c>.
    /// </summary>
    internal static readonly Guid GraphicsCommandListId = new("5b160d0f-ac1b-4185-8ba8-b3ae42a5a455");

    /// <summary>
    /// The IID of <c>ID3D12Fence</c>.
    /// </summary>
    internal static readonly Guid FenceId = new("0a753dcf-c4d8-4b91-adf6-be5a60d95a76");

    /// <summary>
    /// <c>D3D_FEATURE_LEVEL_11_0</c>.
    /// </summary>
    internal const int FEATURE_LEVEL11 = 0xB000;

    /// <summary>
    /// <c>D3D12_HEAP_TYPE_DEFAULT</c>: device local, not CPU addressable.
    /// </summary>
    internal const int HEAP_TYPE_DEFAULT = 1;

    /// <summary>
    /// <c>D3D12_HEAP_TYPE_UPLOAD</c>: CPU writable, GPU readable, write-combined.
    /// </summary>
    internal const int HEAP_TYPE_UPLOAD = 2;

    /// <summary>
    /// <c>D3D12_HEAP_TYPE_READBACK</c>: GPU writable, CPU readable.
    /// </summary>
    internal const int HEAP_TYPE_READBACK = 3;

    /// <summary>
    /// <c>D3D12_HEAP_TYPE_CUSTOM</c>: the type whose page properties decide where the memory really lives.
    /// </summary>
    internal const int HEAP_TYPE_CUSTOM = 4;

    /// <summary>
    /// <c>D3D12_CPU_PAGE_PROPERTY_UNKNOWN</c>: what a standard heap type requires, because only a custom heap may choose.
    /// </summary>
    internal const int CPU_PAGE_UNKNOWN = 0;

    /// <summary>
    /// <c>D3D12_CPU_PAGE_PROPERTY_WRITE_BACK</c>: cached CPU access, which is what a read-write heap needs.
    /// </summary>
    internal const int CPU_PAGE_WRITE_BACK = 3;

    /// <summary>
    /// <c>D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE</c>: uncached CPU access.
    /// </summary>
    internal const int CPU_PAGE_WRITE_COMBINE = 2;

    /// <summary>
    /// <c>D3D12_CPU_PAGE_PROPERTY_NOT_AVAILABLE</c>: the CPU cannot map the memory.
    /// </summary>
    internal const int CPU_PAGE_NOT_AVAILABLE = 1;

    /// <summary>
    /// <c>D3D12_MEMORY_POOL_UNKNOWN</c>: what a standard heap type requires.
    /// </summary>
    internal const int MEMORY_POOL_UNKNOWN = 0;

    /// <summary>
    /// <c>D3D12_MEMORY_POOL_L0</c>: system memory, the pool that makes a custom heap mappable.
    /// </summary>
    internal const int MEMORY_POOL_L0 = 1;

    /// <summary>
    /// <c>D3D12_MEMORY_POOL_L1</c>: device local video memory.
    /// </summary>
    internal const int MEMORY_POOL_L1 = 2;

    /// <summary>
    /// <c>D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS</c>.
    /// </summary>
    internal const int HEAP_FLAG_ALLOW_ONLY_BUFFERS = 0x00000400;

    /// <summary>
    /// <c>D3D12_RESOURCE_DIMENSION_BUFFER</c>.
    /// </summary>
    internal const int RESOURCE_DIMENSION_BUFFER = 1;

    /// <summary>
    /// <c>D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS</c>, so a block can be bound as a shader resource without lying about
    /// its usage at creation time.
    /// </summary>
    internal const int RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS = 0x00000004;

    /// <summary>
    /// <c>D3D12_TEXTURE_LAYOUT_ROW_MAJOR</c>, which a buffer resource description has to carry.
    /// </summary>
    internal const int TEXTURE_LAYOUT_ROW_MAJOR = 1;

    /// <summary>
    /// <c>D3D12_RESOURCE_STATE_COMMON</c>, which is what a device-only buffer starts in.
    /// </summary>
    internal const int RESOURCE_STATE_COMMON = 0;

    /// <summary>
    /// <c>D3D12_RESOURCE_STATE_COPY_DEST</c>, which a readback heap resource has to be created in.
    /// </summary>
    internal const int RESOURCE_STATE_COPY_DESTINATION = 0x00000400;

    /// <summary>
    /// <c>D3D12_RESOURCE_STATE_GENERIC_READ</c>, which an upload heap resource has to be created in.
    /// </summary>
    internal const int RESOURCE_STATE_GENERIC_READ = 0x000002C3;

    /// <summary>
    /// <c>D3D12_COMMAND_LIST_TYPE_COPY</c>.
    /// </summary>
    internal const int COMMAND_LIST_TYPE_COPY = 3;

    /// <summary>
    /// <c>D3D12_FEATURE_ARCHITECTURE</c>.
    /// </summary>
    internal const int FEATURE_ARCHITECTURE = 1;

    /// <summary>
    /// <c>D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT</c>: the alignment a placed resource requires.
    /// </summary>
    internal const int RESOURCE_PLACEMENT_ALIGNMENT = 65536;

    /// <summary>
    /// <c>INFINITE</c>.
    /// </summary>
    private const uint INFINITE = 0xFFFFFFFF;

    private static readonly nint library = LoadLibrary();

    private static readonly delegate* unmanaged[Cdecl]<nint, int, Guid*, nint*, int> createDevice =
        (delegate* unmanaged[Cdecl]<nint, int, Guid*, nint*, int>)Export("D3D12CreateDevice");

    /// <summary>
    /// Gets a value indicating whether the D3D12 runtime is present on this machine.
    /// </summary>
    internal static bool IsAvailable => library != 0;

    /// <summary>
    /// Creates an <c>ID3D12Device</c>.
    /// </summary>
    /// <param name="adapter">The adapter to use, or <see langword="0"/> for the default one.</param>
    /// <param name="device">When the method returns, the device pointer.</param>
    /// <param name="hr">When the method returns, the HRESULT the call returned.</param>
    /// <returns><see langword="true"/> if the device was created; otherwise, <see langword="false"/>.</returns>
    internal static bool TryCreateDevice(nint adapter, out nint device, out int hr)
    {
        var iid = DeviceId;
        nint created;
        hr = createDevice(adapter, FEATURE_LEVEL11, &iid, &created);
        device = hr >= 0 ? created : 0;
        return device != 0;
    }

    /// <summary>
    /// Finds the D3D12 runtime.
    /// </summary>
    /// <returns>The library handle, or <see langword="0"/> when the runtime is not installed.</returns>
    private static nint LoadLibrary()
    {
        // The core D3D12 runtime ships as d3d12.dll; d3d12core.dll is only the new distribution model.
        return NativeInterop.TryLoad("d3d12.dll", out var handle) ? handle : 0;
    }

    /// <summary>
    /// Resolves an exported entry point.
    /// </summary>
    /// <param name="name">The export name.</param>
    /// <returns>The entry point address.</returns>
    /// <exception cref="EntryPointNotFoundException">Thrown when the runtime does not export the function.</exception>
    private static nint Export(string name)
    {
        if (NativeInterop.TryGetExport(library, name, out var address))
        {
            return address;
        }

        throw new EntryPointNotFoundException($"d3d12.dll does not export {name}.");
    }

    /// <summary>
    /// Reads a method from the vtable of a COM interface.
    /// </summary>
    /// <param name="instance">The interface pointer.</param>
    /// <param name="index">The vtable index of the method, counting <c>IUnknown</c>.</param>
    /// <returns>The method pointer.</returns>
    internal static void* Method(nint instance, int index) => ((void**)*(void**)instance)[index];

    /// <summary>
    /// Calls <c>IUnknown::Release</c> on an interface pointer.
    /// </summary>
    /// <param name="instance">The interface pointer, which may be <see langword="0"/>.</param>
    internal static void Release(nint instance)
    {
        if (instance != 0)
        {
            var release = (delegate* unmanaged[Cdecl]<nint, uint>)Method(instance, 2);
            release(instance);
        }
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateCommandQueue</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="desc">The queue description.</param>
    /// <param name="queue">When the method returns, the queue.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateCommandQueue(nint device, D3D12CommandQueueDesc* desc, out nint queue)
    {
        var iid = CommandQueueId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, D3D12CommandQueueDesc*, Guid*, nint*, int>)Method(device, 8);
        var hr = create(device, desc, &iid, &created);
        queue = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateCommandAllocator</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="type">The command list type.</param>
    /// <param name="allocator">When the method returns, the allocator.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateCommandAllocator(nint device, int type, out nint allocator)
    {
        var iid = CommandAllocatorId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, int, Guid*, nint*, int>)Method(device, 9);
        var hr = create(device, type, &iid, &created);
        allocator = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateCommandList</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="allocator">The allocator the list records into.</param>
    /// <param name="type">The command list type.</param>
    /// <param name="list">When the method returns, the command list.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateCommandList(nint device, nint allocator, int type, out nint list)
    {
        var iid = GraphicsCommandListId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, uint, int, nint, nint, Guid*, nint*, int>)Method(device, 12);
        var hr = create(device, 0, type, allocator, 0, &iid, &created);
        list = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CheckFeatureSupport</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="feature">The feature to query.</param>
    /// <param name="data">The structure that receives the answer.</param>
    /// <param name="size">The size of that structure.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CheckFeatureSupport(nint device, int feature, void* data, uint size)
    {
        var check = (delegate* unmanaged[Cdecl]<nint, int, void*, uint, int>)Method(device, 13);
        return check(device, feature, data, size);
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateCommittedResource</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="properties">The heap properties.</param>
    /// <param name="flags">The heap flags.</param>
    /// <param name="desc">The resource description.</param>
    /// <param name="state">The initial resource state.</param>
    /// <param name="resource">When the method returns, the resource.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateCommittedResource(nint device, D3D12HeapProperties* properties, int flags, D3D12ResourceDesc* desc, int state, out nint resource)
    {
        var iid = ResourceId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, D3D12HeapProperties*, int, D3D12ResourceDesc*, int, void*, Guid*, nint*, int>)Method(device, 27);
        var hr = create(device, properties, flags, desc, state, null, &iid, &created);
        resource = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateDescriptorHeap</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="desc">The heap description.</param>
    /// <param name="heap">When the method returns, the heap.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateDescriptorHeap(nint device, D3D12DescriptorHeapDesc* desc, out nint heap)
    {
        var iid = DescriptorHeapId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, D3D12DescriptorHeapDesc*, Guid*, nint*, int>)Method(device, 14);
        var hr = create(device, desc, &iid, &created);
        heap = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::GetDescriptorHandleIncrementSize</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="type">The descriptor heap type.</param>
    /// <returns>The increment.</returns>
    internal static uint GetDescriptorHandleIncrementSize(nint device, int type)
        => ((delegate* unmanaged[Cdecl]<nint, int, uint>)Method(device, 15))(device, type);

    /// <summary>
    /// Calls <c>ID3D12Device::CopyDescriptorsSimple</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="count">The number of descriptors to copy.</param>
    /// <param name="destination">The first descriptor to copy into.</param>
    /// <param name="source">The first descriptor to copy from.</param>
    /// <param name="type">The heap type the descriptors belong to, which has to be the same for both sides.</param>
    internal static void CopyDescriptorsSimple(nint device, uint count, D3D12CpuDescriptorHandle destination, D3D12CpuDescriptorHandle source, int type)
        => ((delegate* unmanaged[Cdecl]<nint, uint, D3D12CpuDescriptorHandle, D3D12CpuDescriptorHandle, int, void>)Method(device, 24))(device, count, destination, source, type);

    /// <summary>
    /// Calls <c>ID3D12Device::CreateShaderResourceView</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="resource">The resource.</param>
    /// <param name="desc">The view description.</param>
    /// <param name="handle">The destination descriptor.</param>
    internal static void CreateShaderResourceView(nint device, nint resource, D3D12ShaderResourceViewDesc* desc, D3D12CpuDescriptorHandle handle)
        => ((delegate* unmanaged[Cdecl]<nint, nint, D3D12ShaderResourceViewDesc*, D3D12CpuDescriptorHandle, void>)Method(device, 18))(device, resource, desc, handle);

    /// <summary>
    /// Calls <c>ID3D12Device::CreateSampler</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="desc">The sampler description.</param>
    /// <param name="handle">The destination descriptor.</param>
    internal static void CreateSampler(nint device, D3D12SamplerDesc* desc, D3D12CpuDescriptorHandle handle)
        => ((delegate* unmanaged[Cdecl]<nint, D3D12SamplerDesc*, D3D12CpuDescriptorHandle, void>)Method(device, 22))(device, desc, handle);

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::CopyTextureRegion</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="destinationX">The destination x.</param>
    /// <param name="destinationY">The destination y.</param>
    /// <param name="destinationZ">The destination z.</param>
    /// <param name="source">The source.</param>
    /// <param name="box">The source box, or <see langword="null"/> for the whole footprint.</param>
    internal static void CopyTextureRegion(nint list, D3D12TextureCopyLocation* destination, uint destinationX, uint destinationY, uint destinationZ, D3D12TextureCopyLocation* source, D3D12Box* box)
        => ((delegate* unmanaged[Cdecl]<nint, D3D12TextureCopyLocation*, uint, uint, uint, D3D12TextureCopyLocation*, D3D12Box*, void>)Method(list, 16))(list, destination, destinationX, destinationY, destinationZ, source, box);

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::SetDescriptorHeaps</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="count">The number of heaps.</param>
    /// <param name="heaps">The heaps.</param>
    internal static void SetDescriptorHeaps(nint list, uint count, nint* heaps)
        => ((delegate* unmanaged[Cdecl]<nint, uint, nint*, void>)Method(list, 28))(list, count, heaps);

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::SetComputeRootDescriptorTable</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="index">The root parameter index.</param>
    /// <param name="handle">The GPU descriptor handle.</param>
    internal static void SetComputeRootDescriptorTable(nint list, uint index, D3D12GpuDescriptorHandle handle)
        => ((delegate* unmanaged[Cdecl]<nint, uint, D3D12GpuDescriptorHandle, void>)Method(list, 31))(list, index, handle);

    /// <summary>
    /// Calls <c>ID3D12DescriptorHeap::GetCPUDescriptorHandleForHeapStart</c>.
    /// </summary>
    /// <param name="heap">The descriptor heap.</param>
    /// <returns>The handle.</returns>
    internal static D3D12CpuDescriptorHandle GetCpuDescriptorHandle(nint heap)
    {
        D3D12CpuDescriptorHandle handle;
        ((delegate* unmanaged[Cdecl]<nint, D3D12CpuDescriptorHandle*, void>)Method(heap, 9))(heap, &handle);
        return handle;
    }

    /// <summary>
    /// Calls <c>ID3D12DescriptorHeap::GetGPUDescriptorHandleForHeapStart</c>.
    /// </summary>
    /// <param name="heap">The descriptor heap.</param>
    /// <returns>The handle.</returns>
    internal static D3D12GpuDescriptorHandle GetGpuDescriptorHandle(nint heap)
    {
        D3D12GpuDescriptorHandle handle;
        ((delegate* unmanaged[Cdecl]<nint, D3D12GpuDescriptorHandle*, void>)Method(heap, 10))(heap, &handle);
        return handle;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateHeap</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="desc">The heap description.</param>
    /// <param name="heap">When the method returns, the heap.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateHeap(nint device, D3D12HeapDesc* desc, out nint heap)
    {
        var iid = HeapId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, D3D12HeapDesc*, Guid*, nint*, int>)Method(device, 28);
        var hr = create(device, desc, &iid, &created);
        heap = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreatePlacedResource</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="heap">The heap the resource is placed in.</param>
    /// <param name="offset">The offset inside the heap.</param>
    /// <param name="desc">The resource description.</param>
    /// <param name="state">The initial resource state.</param>
    /// <param name="resource">When the method returns, the resource.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreatePlacedResource(nint device, nint heap, ulong offset, D3D12ResourceDesc* desc, int state, out nint resource)
    {
        var iid = ResourceId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, nint, ulong, D3D12ResourceDesc*, int, nint, Guid*, nint*, int>)Method(device, 29);
        var hr = create(device, heap, offset, desc, state, 0, &iid, &created);
        resource = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateFence</c>.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="fence">When the method returns, the fence.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CreateFence(nint device, out nint fence)
    {
        var iid = FenceId;
        nint created;
        var create = (delegate* unmanaged[Cdecl]<nint, ulong, int, Guid*, nint*, int>)Method(device, 36);
        var hr = create(device, 0, 0, &iid, &created);
        fence = hr >= 0 ? created : 0;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Resource::Map</c>.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <param name="readRange">The range the CPU intends to read, or <see langword="null"/> for none.</param>
    /// <param name="pointer">When the method returns, the CPU address.</param>
    /// <returns>The HRESULT.</returns>
    internal static int Map(nint resource, D3D12Range* readRange, out void* pointer)
    {
        void* mapped;
        var map = (delegate* unmanaged[Cdecl]<nint, uint, D3D12Range*, void**, int>)Method(resource, 8);
        var hr = map(resource, 0, readRange, &mapped);
        pointer = hr >= 0 ? mapped : null;
        return hr;
    }

    /// <summary>
    /// Calls <c>ID3D12Resource::Unmap</c>.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <param name="writtenRange">The range the CPU wrote, or <see langword="null"/> when it only read.</param>
    internal static void Unmap(nint resource, D3D12Range* writtenRange)
    {
        var unmap = (delegate* unmanaged[Cdecl]<nint, uint, D3D12Range*, void>)Method(resource, 9);
        unmap(resource, 0, writtenRange);
    }

    /// <summary>
    /// Calls <c>ID3D12Resource::GetGPUVirtualAddress</c>.
    /// </summary>
    /// <param name="resource">The resource.</param>
    /// <returns>The GPU virtual address of the resource.</returns>
    internal static ulong GetVirtualAddress(nint resource)
    {
        var get = (delegate* unmanaged[Cdecl]<nint, ulong>)Method(resource, 11);
        return get(resource);
    }

    /// <summary>
    /// Calls <c>ID3D12CommandAllocator::Reset</c>.
    /// </summary>
    /// <param name="allocator">The allocator.</param>
    /// <returns>The HRESULT.</returns>
    internal static int ResetCommandAllocator(nint allocator)
    {
        var reset = (delegate* unmanaged[Cdecl]<nint, int>)Method(allocator, 8);
        return reset(allocator);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::Reset</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="allocator">The allocator to record into.</param>
    /// <returns>The HRESULT.</returns>
    internal static int ResetCommandList(nint list, nint allocator)
    {
        var reset = (delegate* unmanaged[Cdecl]<nint, nint, nint, int>)Method(list, 10);
        return reset(list, allocator, 0);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::Close</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <returns>The HRESULT.</returns>
    internal static int CloseCommandList(nint list)
    {
        var close = (delegate* unmanaged[Cdecl]<nint, int>)Method(list, 9);
        return close(list);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::CopyBufferRegion</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="destination">The resource to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination.</param>
    /// <param name="source">The resource to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source.</param>
    /// <param name="size">The number of bytes to copy.</param>
    internal static void CopyBufferRegion(nint list, nint destination, ulong destinationOffset, nint source, ulong sourceOffset, ulong size)
    {
        var copy = (delegate* unmanaged[Cdecl]<nint, nint, ulong, nint, ulong, ulong, void>)Method(list, 15);
        copy(list, destination, destinationOffset, source, sourceOffset, size);
    }

    /// <summary>
    /// Calls <c>ID3D12CommandQueue::ExecuteCommandLists</c>.
    /// </summary>
    /// <param name="queue">The queue.</param>
    /// <param name="list">The single command list to execute.</param>
    internal static void ExecuteCommandLists(nint queue, nint list)
    {
        var execute = (delegate* unmanaged[Cdecl]<nint, uint, nint*, void>)Method(queue, 10);
        execute(queue, 1, &list);
    }

    /// <summary>
    /// Calls <c>ID3D12CommandQueue::Signal</c>.
    /// </summary>
    /// <param name="queue">The queue.</param>
    /// <param name="fence">The fence to signal.</param>
    /// <param name="value">The value to reach.</param>
    /// <returns>The HRESULT.</returns>
    internal static int Signal(nint queue, nint fence, ulong value)
    {
        var signal = (delegate* unmanaged[Cdecl]<nint, nint, ulong, int>)Method(queue, 14);
        return signal(queue, fence, value);
    }

    /// <summary>
    /// Calls <c>ID3D12Fence::SetEventOnCompletion</c> and waits for it.
    /// </summary>
    /// <param name="fence">The fence to wait on.</param>
    /// <param name="value">The value to reach.</param>
    /// <returns><see langword="true"/> when the fence reached the value.</returns>
    internal static bool WaitForFence(nint fence, ulong value)
    {
        var handle = CreateEvent(0, 0, 0, 0);
        if (handle == 0)
        {
            return false;
        }

        var setEvent = (delegate* unmanaged[Cdecl]<nint, ulong, nint, int>)Method(fence, 9);
        var hr = setEvent(fence, value, handle);
        var reached = hr >= 0 && WaitForSingleObject(handle, INFINITE) == 0;
        CloseHandle(handle);
        return reached;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateEventW", SetLastError = true)]
    private static extern nint CreateEvent(nint attributes, int manualReset, int initialState, nint name);

    [DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static extern int WaitForSingleObject(nint handle, uint milliseconds);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    private static extern int CloseHandle(nint handle);
}

/// <summary>
/// <c>D3D12_HEAP_PROPERTIES</c>, 20 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12HeapProperties
{
    public int Type;
    public int CpuPageProperty;
    public int MemoryPoolPreference;
    public uint CreationNodeMask;
    public uint VisibleNodeMask;
}

/// <summary>
/// <c>D3D12_HEAP_DESC</c>, 48 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12HeapDesc
{
    public ulong SizeInBytes;
    public D3D12HeapProperties Properties;
    public uint PropertiesPadding;
    public ulong Alignment;
    public int Flags;
    public uint FlagsPadding;
}

/// <summary>
/// <c>D3D12_RESOURCE_DESC</c>, 56 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12ResourceDesc
{
    public int Dimension;
    public uint DimensionPadding;
    public ulong Alignment;
    public ulong Width;
    public uint Height;
    public ushort DepthOrArraySize;
    public ushort MipLevels;
    public uint Format;
    public uint SampleCount;
    public uint SampleQuality;
    public int Layout;
    public int Flags;
}

/// <summary>
/// <c>D3D12_DESCRIPTOR_HEAP_DESC</c>, 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12DescriptorHeapDesc
{
    public int Type;
    public uint NumDescriptors;
    public int Flags;
    public uint NodeMask;
}

/// <summary>
/// <c>D3D12_CPU_DESCRIPTOR_HANDLE</c>, 8 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12CpuDescriptorHandle
{
    public nuint Pointer;
}

/// <summary>
/// <c>D3D12_GPU_DESCRIPTOR_HANDLE</c>, 8 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12GpuDescriptorHandle
{
    public ulong Pointer;
}

/// <summary>
/// <c>D3D12_SHADER_RESOURCE_VIEW_DESC</c> for a two dimensional texture, 32 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12ShaderResourceViewDesc
{
    public uint Format;
    public int ViewDimension;
    public uint Shader4ComponentMapping;
    public uint UnionPadding;
    public uint MostDetailedMip;
    public uint MipLevels;
    public uint PlaneSlice;
    public float ResourceMinLodClamp;
    public uint Padding0;
    public uint Padding1;
}

/// <summary>
/// <c>D3D12_SAMPLER_DESC</c>, 52 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12SamplerDesc
{
    public uint Filter;
    public uint AddressU;
    public uint AddressV;
    public uint AddressW;
    public float MipLodBias;
    public uint MaxAnisotropy;
    public uint ComparisonFunc;
    public float BorderRed;
    public float BorderGreen;
    public float BorderBlue;
    public float BorderAlpha;
    public float MinLod;
    public float MaxLod;
}

/// <summary>
/// <c>D3D12_SUBRESOURCE_FOOTPRINT</c>, 20 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12SubresourceFootprint
{
    public uint Format;
    public uint Width;
    public uint Height;
    public uint Depth;
    public uint RowPitch;
}

/// <summary>
/// <c>D3D12_TEXTURE_COPY_LOCATION</c>, 48 bytes.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct D3D12TextureCopyLocation
{
    [FieldOffset(0)]
    public nint Resource;

    [FieldOffset(8)]
    public uint Type;

    [FieldOffset(12)]
    public uint Padding;

    [FieldOffset(16)]
    public ulong Offset;

    [FieldOffset(24)]
    public D3D12SubresourceFootprint Footprint;

    [FieldOffset(16)]
    public uint SubresourceIndex;
}

/// <summary>
/// <c>D3D12_BOX</c>, 24 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12Box
{
    public uint Left;
    public uint Top;
    public uint Front;
    public uint Right;
    public uint Bottom;
    public uint Back;
}


[StructLayout(LayoutKind.Sequential)]
internal struct D3D12Range
{
    public nuint Begin;
    public nuint End;
}

/// <summary>
/// <c>D3D12_COMMAND_QUEUE_DESC</c>, 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12CommandQueueDesc
{
    public int Type;
    public int Priority;
    public int Flags;
    public uint NodeMask;
}

/// <summary>
/// <c>D3D12_FEATURE_DATA_ARCHITECTURE</c>, 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12FeatureDataArchitecture
{
    public uint NodeIndex;
    public int TileBasedRenderer;
    public int UnifiedMemory;
    public int CacheCoherentUnifiedMemory;
}

