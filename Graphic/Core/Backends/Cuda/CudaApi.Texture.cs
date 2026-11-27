using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The part of the CUDA driver API that creates and fills a texture: the array the texels live in, the object that samples
/// it and the 2D copy that moves texels between the array and a pinned host buffer.
/// </summary>
/// <remarks>
/// <para>
/// The layouts mirror <c>cuda.h</c> on a 64-bit build, where a handle and a <c>size_t</c> are eight bytes and a
/// <c>CUdeviceptr</c> is an unsigned 64 bit value. A wrong size or offset is answered by the driver with an error code, a
/// crash, or silently wrong texels rather than a managed exception, so <c>CudaInterop.test.cs</c> pins every number here.
/// </para>
/// <para>
/// Only the array variant of the <see cref="CUDA_RESOURCE_DESC"/> union is laid out, because a texture of this layer is
/// always a two dimensional array. The union's reserved member still sets the size the driver reads
/// <see cref="CUDA_RESOURCE_DESC.Flags"/> at.
/// </para>
/// </remarks>
internal static unsafe partial class CudaApi
{
    /// <summary>
    /// <c>CUDA_ERROR_INVALID_VALUE</c>, the code a refused descriptor comes back as.
    /// </summary>
    internal const int ERROR_INVALID_VALUE = 1;

    /// <summary>
    /// <c>CU_RESOURCE_TYPE_ARRAY</c>.
    /// </summary>
    internal const int RESOURCE_TYPE_ARRAY = 0x00;

    /// <summary>
    /// <c>CU_MEMORYTYPE_HOST</c>, the pinned buffer a transfer is staged through.
    /// </summary>
    internal const int MEMORYTYPE_HOST = 0x01;

    /// <summary>
    /// <c>CU_MEMORYTYPE_ARRAY</c>.
    /// </summary>
    internal const int MEMORYTYPE_ARRAY = 0x03;

    /// <summary>
    /// <c>CU_AD_FORMAT_UNSIGNED_INT8</c>, the channel format four normalized 8 bit channels are stored as.
    /// </summary>
    internal const int AD_FORMAT_UNSIGNED_INT8 = 0x01;

    /// <summary>
    /// <c>CU_AD_FORMAT_UNSIGNED_INT32</c>, the channel format one unsigned 32 bit channel is stored as.
    /// </summary>
    internal const int AD_FORMAT_UNSIGNED_INT32 = 0x03;

    /// <summary>
    /// <c>CU_AD_FORMAT_FLOAT</c>, the channel format one 32 bit float channel is stored as.
    /// </summary>
    internal const int AD_FORMAT_FLOAT = 0x20;

    /// <summary>
    /// <c>CU_RES_VIEW_FORMAT_UINT_1X8</c>, the view of one unsigned 8 bit channel.
    /// </summary>
    internal const int RES_VIEW_FORMAT_UINT_1X8 = 0x01;

    /// <summary>
    /// <c>CU_RES_VIEW_FORMAT_UINT_4X8</c>, the view of four unsigned 8 bit channels.
    /// </summary>
    internal const int RES_VIEW_FORMAT_UINT_4X8 = 0x03;

    /// <summary>
    /// <c>CU_RES_VIEW_FORMAT_UINT_1X32</c>, the view of one unsigned 32 bit channel.
    /// </summary>
    internal const int RES_VIEW_FORMAT_UINT_1X32 = 0x0D;

    /// <summary>
    /// <c>CU_RES_VIEW_FORMAT_FLOAT_1X32</c>, the view of one 32 bit float channel.
    /// </summary>
    internal const int RES_VIEW_FORMAT_FLOAT_1X32 = 0x16;

    /// <summary>
    /// <c>CU_TR_ADDRESS_MODE_WRAP</c>, which tiles the image.
    /// </summary>
    internal const int ADDRESS_MODE_WRAP = 0;

    /// <summary>
    /// <c>CU_TR_ADDRESS_MODE_CLAMP</c>, which repeats the edge pixel.
    /// </summary>
    internal const int ADDRESS_MODE_CLAMP = 1;

    /// <summary>
    /// <c>CU_TR_ADDRESS_MODE_MIRROR</c>, which tiles the image and mirrors every other tile.
    /// </summary>
    internal const int ADDRESS_MODE_MIRROR = 2;

    /// <summary>
    /// <c>CU_TR_FILTER_MODE_POINT</c>.
    /// </summary>
    internal const int FILTER_MODE_POINT = 0;

    /// <summary>
    /// <c>CU_TR_FILTER_MODE_LINEAR</c>.
    /// </summary>
    internal const int FILTER_MODE_LINEAR = 1;

    /// <summary>
    /// <c>CU_TRSF_READ_AS_INTEGER</c>: reads an integer array as its elements instead of normalized floats.
    /// </summary>
    internal const uint TRSF_READ_AS_INTEGER = 0x01;

    /// <summary>
    /// <c>CU_TRSF_NORMALIZED_COORDINATES</c>: samples with coordinates in [0, 1), which is also the only way the driver
    /// honours an address mode other than clamp.
    /// </summary>
    internal const uint TRSF_NORMALIZED_COORDINATES = 0x02;

    /// <summary>
    /// <c>CUDA_ARRAY_DESCRIPTOR</c>: the shape and the channel format of a two dimensional array.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CUDA_ARRAY_DESCRIPTOR
    {
        /// <summary>
        /// The width, in elements.
        /// </summary>
        public nuint Width;

        /// <summary>
        /// The height, in elements.
        /// </summary>
        public nuint Height;

        /// <summary>
        /// The format of one channel, as a <c>CUarray_format</c> value.
        /// </summary>
        public int Format;

        /// <summary>
        /// The channels in one element, which is 1, 2 or 4.
        /// </summary>
        public uint NumChannels;
    }

    /// <summary>
    /// <c>CUDA_RESOURCE_DESC</c>: which resource a texture object samples, laid out for the array variant only.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    internal struct CUDA_RESOURCE_DESC
    {
        /// <summary>
        /// The resource type, as a <c>CUresourcetype</c> value; <see cref="RESOURCE_TYPE_ARRAY"/> for a texture of this
        /// layer.
        /// </summary>
        [FieldOffset(0)]
        public int ResType;

        /// <summary>
        /// The <c>CUarray</c> the texture object samples, which is the union's array member.
        /// </summary>
        [FieldOffset(8)]
        public nint HArray;

        /// <summary>
        /// The flags, which the driver requires to be zero.
        /// </summary>
        [FieldOffset(136)]
        public uint Flags;
    }

    /// <summary>
    /// <c>CUDA_TEXTURE_DESC</c>: how a texture object samples its resource, which is where CUDA keeps the sampler.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CUDA_TEXTURE_DESC
    {
        /// <summary>
        /// The address mode of x, y and z.
        /// </summary>
        public fixed int AddressMode[3];

        /// <summary>
        /// The filter mode, as a <c>CUfilter_mode</c> value.
        /// </summary>
        public int FilterMode;

        /// <summary>
        /// The flags, a combination of the <c>CU_TRSF_*</c> values.
        /// </summary>
        public uint Flags;

        /// <summary>
        /// The maximum anisotropy ratio, clamped by the driver to [1, 16].
        /// </summary>
        public uint MaxAnisotropy;

        /// <summary>
        /// The filter mode between two mipmap levels.
        /// </summary>
        public int MipmapFilterMode;

        /// <summary>
        /// The offset applied to the calculated mipmap level.
        /// </summary>
        public float MipmapLevelBias;

        /// <summary>
        /// The lower end of the mipmap level range.
        /// </summary>
        public float MinMipmapLevelClamp;

        /// <summary>
        /// The upper end of the mipmap level range.
        /// </summary>
        public float MaxMipmapLevelClamp;

        /// <summary>
        /// The border colour, which only the border address mode reads.
        /// </summary>
        public fixed float BorderColor[4];

        /// <summary>
        /// The reserved fields, which the driver requires to be zero.
        /// </summary>
        public fixed int Reserved[12];
    }

    /// <summary>
    /// <c>CUDA_RESOURCE_VIEW_DESC</c>: the format a texture object reads an array through.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CUDA_RESOURCE_VIEW_DESC
    {
        /// <summary>
        /// The format the array is read as, as a <c>CUresourceViewFormat</c> value.
        /// </summary>
        public int Format;

        /// <summary>
        /// The width of the view, in elements, which has to be the width of the array.
        /// </summary>
        public nuint Width;

        /// <summary>
        /// The height of the view, in elements, which has to be the height of the array.
        /// </summary>
        public nuint Height;

        /// <summary>
        /// The depth of the view, which has to be the depth of the array; a two dimensional array has one.
        /// </summary>
        public nuint Depth;

        /// <summary>
        /// The most detailed mipmap level, which is zero for an array with no mipmaps.
        /// </summary>
        public uint FirstMipmapLevel;

        /// <summary>
        /// The least detailed mipmap level, which is zero for an array with no mipmaps.
        /// </summary>
        public uint LastMipmapLevel;

        /// <summary>
        /// The first layer, which is zero for an array that is not layered.
        /// </summary>
        public uint FirstLayer;

        /// <summary>
        /// The last layer, which is zero for an array that is not layered.
        /// </summary>
        public uint LastLayer;

        /// <summary>
        /// The reserved fields, which the driver requires to be zero.
        /// </summary>
        public fixed uint Reserved[16];
    }

    /// <summary>
    /// <c>CUDA_MEMCPY2D</c>: one rectangular copy between host memory, a device pointer or an array.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CUDA_MEMCPY2D
    {
        /// <summary>
        /// The first byte of the source row.
        /// </summary>
        public nuint SourceXInBytes;

        /// <summary>
        /// The first source row.
        /// </summary>
        public nuint SourceY;

        /// <summary>
        /// The kind of memory the source is in, as a <c>CUmemorytype</c> value.
        /// </summary>
        public int SourceMemoryType;

        /// <summary>
        /// The host pointer the source is read from, when the source is host memory.
        /// </summary>
        public nint SourceHost;

        /// <summary>
        /// The device pointer the source is read from, when the source is device memory.
        /// </summary>
        public ulong SourceDevice;

        /// <summary>
        /// The array the source is read from, when the source is an array.
        /// </summary>
        public nint SourceArray;

        /// <summary>
        /// The source row pitch, in bytes, which the driver ignores when the source is an array.
        /// </summary>
        public nuint SourcePitch;

        /// <summary>
        /// The first byte of the destination row.
        /// </summary>
        public nuint DestinationXInBytes;

        /// <summary>
        /// The first destination row.
        /// </summary>
        public nuint DestinationY;

        /// <summary>
        /// The kind of memory the destination is in, as a <c>CUmemorytype</c> value.
        /// </summary>
        public int DestinationMemoryType;

        /// <summary>
        /// The host pointer the destination is written to, when the destination is host memory.
        /// </summary>
        public nint DestinationHost;

        /// <summary>
        /// The device pointer the destination is written to, when the destination is device memory.
        /// </summary>
        public ulong DestinationDevice;

        /// <summary>
        /// The array the destination is written to, when the destination is an array.
        /// </summary>
        public nint DestinationArray;

        /// <summary>
        /// The destination row pitch, in bytes, which the driver ignores when the destination is an array.
        /// </summary>
        public nuint DestinationPitch;

        /// <summary>
        /// The width of the copy, in bytes.
        /// </summary>
        public nuint WidthInBytes;

        /// <summary>
        /// The height of the copy, in rows.
        /// </summary>
        public nuint Height;
    }

    /// <summary>
    /// Allocates a two dimensional array with the descriptor's shape and channel format.
    /// </summary>
    /// <param name="array">When the method returns, the <c>CUarray</c>.</param>
    /// <param name="descriptor">The shape and channel format.</param>
    /// <returns>The driver's result code.</returns>
    [DllImport(Library, EntryPoint = "cuArrayCreate_v2")]
    internal static extern int cuArrayCreate(out nint array, CUDA_ARRAY_DESCRIPTOR* descriptor);

    /// <summary>
    /// Releases an array.
    /// </summary>
    /// <param name="array">The <c>CUarray</c>.</param>
    /// <returns>The driver's result code.</returns>
    [DllImport(Library)]
    internal static extern int cuArrayDestroy(nint array);

    /// <summary>
    /// Creates the texture object that samples a resource.
    /// </summary>
    /// <param name="textureObject">When the method returns, the <c>CUtexObject</c>.</param>
    /// <param name="resource">The resource to sample.</param>
    /// <param name="texture">How to sample it.</param>
    /// <param name="view">The format to read it through, or <see langword="null"/> for the resource's own format.</param>
    /// <returns>The driver's result code.</returns>
    [DllImport(Library)]
    internal static extern int cuTexObjectCreate(out nint textureObject, CUDA_RESOURCE_DESC* resource, CUDA_TEXTURE_DESC* texture, CUDA_RESOURCE_VIEW_DESC* view);

    /// <summary>
    /// Releases a texture object.
    /// </summary>
    /// <param name="textureObject">The <c>CUtexObject</c>.</param>
    /// <returns>The driver's result code.</returns>
    [DllImport(Library)]
    internal static extern int cuTexObjectDestroy(nint textureObject);

    /// <summary>
    /// Copies a rectangle between host memory, a device pointer or an array.
    /// </summary>
    /// <param name="copy">The copy to make.</param>
    /// <returns>The driver's result code.</returns>
    [DllImport(Library, EntryPoint = "cuMemcpy2D_v2")]
    internal static extern int cuMemcpy2D(CUDA_MEMCPY2D* copy);
}
