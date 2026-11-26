using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The compute part of the D3D12 bootstrap: enough of the pipeline, root signature and dispatch surface to run a shader
/// over a block of unified memory.
/// </summary>
/// <remarks>
/// <para>
/// The root signature is deliberately the smallest one that can do the job: two root descriptors, a shader resource view
/// at <c>t0</c> and an unordered access view at <c>u0</c>. Root descriptors carry a GPU virtual address instead of a
/// descriptor heap slot, which is exactly what a <see cref="UnifiedMemoryBlock.DeviceAddress"/> is, so the shader reads the
/// block the allocator handed out with no descriptor and no copy in between.
/// </para>
/// <para>
/// Every slot index and every structure layout in this file was read out of the Windows SDK headers with a C probe
/// (<c>offsetof</c> over <c>ID3D12GraphicsCommandListVtbl</c> and friends), because a wrong slot here faults rather than
/// returning an error.
/// </para>
/// </remarks>
internal static unsafe partial class D3D12Api
{
    /// <summary>
    /// <c>D3D12_ROOT_PARAMETER_TYPE_SRV</c>: a root descriptor read as a shader resource view.
    /// </summary>
    internal const int ROOT_PARAMETER_TYPE_SRV = 3;

    /// <summary>
    /// <c>D3D12_ROOT_PARAMETER_TYPE_UAV</c>: a root descriptor written as an unordered access view.
    /// </summary>
    internal const int ROOT_PARAMETER_TYPE_UAV = 4;

    /// <summary>
    /// <c>D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE</c>: a table of descriptors.
    /// </summary>
    internal const int ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE = 0;

    /// <summary>
    /// <c>D3D12_DESCRIPTOR_RANGE_TYPE_SRV</c>.
    /// </summary>
    internal const int DESCRIPTOR_RANGE_TYPE_SRV = 0;

    /// <summary>
    /// <c>D3D12_DESCRIPTOR_RANGE_TYPE_SAMPLER</c>.
    /// </summary>
    internal const int DESCRIPTOR_RANGE_TYPE_SAMPLER = 3;

    /// <summary>
    /// <c>D3D12_SHADER_VISIBILITY_ALL</c>.
    /// </summary>
    internal const int SHADER_VISIBILITY_ALL = 0;

    /// <summary>
    /// <c>D3D_ROOT_SIGNATURE_VERSION_1</c>, the version <c>D3D12SerializeRootSignature</c> takes.
    /// </summary>
    internal const int ROOT_SIGNATURE_VERSION1 = 1;

    /// <summary>
    /// <c>D3D12_ROOT_SIGNATURE_FLAG_NONE</c>: a root signature that only compute uses needs no input layout.
    /// </summary>
    internal const int ROOT_SIGNATURE_FLAGS_NONE = 0;

    /// <summary>
    /// <c>D3D12_PIPELINE_STATE_FLAG_NONE</c>.
    /// </summary>
    internal const int PIPELINE_STATE_FLAG_NONE = 0;

    /// <summary>
    /// <c>D3D12_RESOURCE_STATE_UNORDERED_ACCESS</c>.
    /// </summary>
    internal const int RESOURCE_STATE_UNORDERED_ACCESS = 0x00000008;

    /// <summary>
    /// <c>D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE</c>: the read state a compute shader resource view needs.
    /// </summary>
    internal const int RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE = 0x00000040;

    /// <summary>
    /// <c>D3D12_RESOURCE_STATE_COPY_SOURCE</c>.
    /// </summary>
    internal const int RESOURCE_STATE_COPY_SOURCE = 0x00000800;

    /// <summary>
    /// <c>D3D12_RESOURCE_BARRIER_TYPE_TRANSITION</c>.
    /// </summary>
    internal const int RESOURCE_BARRIER_TYPE_TRANSITION = 0;

    /// <summary>
    /// <c>D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES</c>.
    /// </summary>
    internal const int RESOURCE_BARRIER_ALL_SUBRESOURCES = -1;

    /// <summary>
    /// <c>D3D12_COMMAND_LIST_TYPE_COMPUTE</c>.
    /// </summary>
    internal const int COMMAND_LIST_TYPE_COMPUTE = 2;

    /// <summary>
    /// The IID of <c>ID3D12RootSignature</c>.
    /// </summary>
    internal static readonly Guid RootSignatureId = new("c54a6b66-72df-4ee8-8be5-a946a1429214");

    /// <summary>
    /// The IID of <c>ID3D12PipelineState</c>.
    /// </summary>
    internal static readonly Guid PipelineStateId = new("765a30f3-f624-4c6f-a828-ace948622445");

    private static delegate* unmanaged[Cdecl]<D3D12RootSignatureDesc*, int, nint*, nint*, int> s_SerializeRootSignature;

    /// <summary>
    /// Creates a root signature from a serialized description.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="data">The serialized root signature.</param>
    /// <param name="size">The size of the serialized root signature in bytes.</param>
    /// <param name="signature">When the method returns, the <c>ID3D12RootSignature</c>.</param>
    /// <returns>The <c>HRESULT</c> of the call.</returns>
    internal static int CreateRootSignature(nint device, void* data, nuint size, out nint signature)
    {
        var create = (delegate* unmanaged[Cdecl]<nint, uint, void*, nuint, Guid*, nint*, int>)Method(device, 16);
        var iid = RootSignatureId;
        nint created;
        var result = create(device, 0, data, size, &iid, &created);
        signature = result >= 0 ? created : 0;
        return result;
    }

    /// <summary>
    /// Serializes a root signature description into the blob <c>CreateRootSignature</c> takes.
    /// </summary>
    /// <param name="description">The description to serialize.</param>
    /// <param name="blob">When the method returns, the blob holding the serialized signature.</param>
    /// <param name="error">When the method returns, the blob holding the compiler's complaint, if any.</param>
    /// <returns>
    /// The <c>HRESULT</c> of the call. When the runtime does not export the serializer the result is <c>E_FAIL</c>, because
    /// a root signature cannot be built without it.
    /// </returns>
    internal static int SerializeRootSignature(D3D12RootSignatureDesc* description, out nint blob, out nint error)
    {
        blob = 0;
        error = 0;

        var serialize = Serializer();

        if (serialize == null)
        {
            return unchecked((int)0x80004005);
        }

        nint serialized;
        nint complaint;
        var result = serialize(description, ROOT_SIGNATURE_VERSION1, &serialized, &complaint);

        blob = result >= 0 ? serialized : 0;
        error = complaint;
        return result;
    }

    /// <summary>
    /// Reads the bytes out of an <c>ID3DBlob</c>.
    /// </summary>
    /// <param name="blob">The blob.</param>
    /// <returns>The address of its first byte.</returns>
    internal static void* BlobData(nint blob)
    {
        var get = (delegate* unmanaged[Cdecl]<nint, void*>)Method(blob, 3);
        return get(blob);
    }

    /// <summary>
    /// Reads the byte count out of an <c>ID3DBlob</c>.
    /// </summary>
    /// <param name="blob">The blob.</param>
    /// <returns>The number of bytes it holds.</returns>
    internal static nuint BlobSize(nint blob)
    {
        var get = (delegate* unmanaged[Cdecl]<nint, nuint>)Method(blob, 4);
        return get(blob);
    }

    /// <summary>
    /// Calls <c>ID3D12Device::CreateComputePipelineState</c>.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="description">The pipeline description.</param>
    /// <param name="state">When the method returns, the <c>ID3D12PipelineState</c>.</param>
    /// <returns>The <c>HRESULT</c> of the call.</returns>
    internal static int CreateComputePipelineState(nint device, D3D12ComputePipelineStateDesc* description, out nint state)
    {
        var create = (delegate* unmanaged[Cdecl]<nint, D3D12ComputePipelineStateDesc*, Guid*, nint*, int>)Method(device, 11);
        var iid = PipelineStateId;
        nint created;
        var result = create(device, description, &iid, &created);
        state = result >= 0 ? created : 0;
        return result;
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::ResourceBarrier</c> for a single transition.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="barrier">The transition to record.</param>
    internal static void ResourceBarrier(nint list, D3D12ResourceBarrier* barrier)
    {
        var record = (delegate* unmanaged[Cdecl]<nint, uint, D3D12ResourceBarrier*, void>)Method(list, 26);
        record(list, 1, barrier);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::SetPipelineState</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="state">The pipeline state.</param>
    internal static void SetPipelineState(nint list, nint state)
    {
        var set = (delegate* unmanaged[Cdecl]<nint, nint, void>)Method(list, 25);
        set(list, state);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::SetComputeRootSignature</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="signature">The root signature.</param>
    internal static void SetComputeRootSignature(nint list, nint signature)
    {
        var set = (delegate* unmanaged[Cdecl]<nint, nint, void>)Method(list, 29);
        set(list, signature);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::SetComputeRootShaderResourceView</c>, which takes a GPU virtual address because
    /// the parameter is a root descriptor.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="index">The root parameter index.</param>
    /// <param name="address">The GPU virtual address of the buffer.</param>
    internal static void SetComputeRootShaderResourceView(nint list, uint index, ulong address)
    {
        var set = (delegate* unmanaged[Cdecl]<nint, uint, ulong, void>)Method(list, 39);
        set(list, index, address);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::SetComputeRootUnorderedAccessView</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="index">The root parameter index.</param>
    /// <param name="address">The GPU virtual address of the buffer.</param>
    internal static void SetComputeRootUnorderedAccessView(nint list, uint index, ulong address)
    {
        var set = (delegate* unmanaged[Cdecl]<nint, uint, ulong, void>)Method(list, 41);
        set(list, index, address);
    }

    /// <summary>
    /// Calls <c>ID3D12GraphicsCommandList::Dispatch</c>.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="x">The thread group count along x.</param>
    /// <param name="y">The thread group count along y.</param>
    /// <param name="z">The thread group count along z.</param>
    internal static void Dispatch(nint list, uint x, uint y, uint z)
    {
        var dispatch = (delegate* unmanaged[Cdecl]<nint, uint, uint, uint, void>)Method(list, 14);
        dispatch(list, x, y, z);
    }

    /// <summary>
    /// Resolves <c>D3D12SerializeRootSignature</c>, the runtime's own root signature compiler.
    /// </summary>
    /// <returns>The entry point, or <see langword="null"/> when the runtime does not export it.</returns>
    private static delegate* unmanaged[Cdecl]<D3D12RootSignatureDesc*, int, nint*, nint*, int> Serializer()
    {
        if (s_SerializeRootSignature != null)
        {
            return s_SerializeRootSignature;
        }

        if (library != 0 && NativeInterop.TryGetExport(library, "D3D12SerializeRootSignature", out var address))
        {
            s_SerializeRootSignature = (delegate* unmanaged[Cdecl]<D3D12RootSignatureDesc*, int, nint*, nint*, int>)address;
        }

        return s_SerializeRootSignature;
    }
}

/// <summary>
/// <c>D3D12_SHADER_BYTECODE</c>: 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12ShaderBytecode
{
    public byte* Data;
    public nuint Size;
}

/// <summary>
/// <c>D3D12_COMPUTE_PIPELINE_STATE_DESC</c>: 56 bytes. The embedded pipeline state blobs are a
/// <see cref="D3D12ShaderBytecode"/> each, empty for a shader compiled up front.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12ComputePipelineStateDesc
{
    public nint RootSignature;
    public D3D12ShaderBytecode Shader;
    public uint NodeMask;
    public D3D12ShaderBytecode CachedPipelineState;
    public int Flags;
}

/// <summary>
/// <c>D3D12_ROOT_SIGNATURE_DESC</c>: 40 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12RootSignatureDesc
{
    public uint ParameterCount;
    public D3D12RootParameter* Parameters;
    public uint StaticSamplerCount;
    public void* StaticSamplers;
    public int Flags;
}

/// <summary>
/// <c>D3D12_ROOT_PARAMETER</c>: 32 bytes, with the descriptor union at offset 8 and the visibility - which is declared after
/// the union, not before it - at offset 24.
/// </summary>
/// <remarks>
/// The offsets are explicit because the union is as wide as a descriptor table, which is 16 bytes. Laying the fields out
/// sequentially would put the visibility at offset 16 and shrink the structure to 20 bytes, and the root signature compiler
/// would then read a garbage visibility for every parameter after the first.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct D3D12RootParameter
{
    [FieldOffset(0)] public int Type;

    /// <summary>
    /// The register the root descriptor binds, which is the first field of the union.
    /// </summary>
    [FieldOffset(8)] public uint ShaderRegister;

    /// <summary>
    /// The register space of the root descriptor, which follows the register.
    /// </summary>
    [FieldOffset(12)] public uint RegisterSpace;

    /// <summary>
    /// The constant count of a <c>D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS</c> parameter, which shares the same union.
    /// </summary>
    [FieldOffset(16)] public uint Num32BitValues;

    /// <summary>
    /// The range array of a descriptor table, which is the union the table parameter uses.
    /// </summary>
    [FieldOffset(8)] public nint DescriptorTable;

    /// <summary>
    /// The number of ranges of the descriptor table.
    /// </summary>
    [FieldOffset(16)] public uint NumDescriptorRanges;

    [FieldOffset(24)] public int ShaderVisibility;
}

/// <summary>
/// <c>D3D12_DESCRIPTOR_RANGE</c>, 20 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12DescriptorRange
{
    public int RangeType;
    public uint NumDescriptors;
    public uint BaseShaderRegister;
    public uint RegisterSpace;
    public uint OffsetInDescriptorsFromTableStart;
}

/// <summary>
/// <c>D3D12_RESOURCE_BARRIER</c>: 32 bytes, with the transition union at offset 8.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12ResourceBarrier
{
    public int Type;
    public int Flags;
    public nint Resource;
    public uint Subresource;
    public int StateBefore;
    public int StateAfter;
}

