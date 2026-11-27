using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The Objective-C side of the Metal backend: the runtime lookups, the message sends and the small number of constants the
/// bridge needs.
/// </summary>
/// <remarks>
/// <para>
/// Metal has no C API. Everything is an Objective-C message, so the bridge talks to <c>libobjc</c> directly:
/// <c>objc_getClass</c> and <c>sel_registerName</c> to turn strings into a class and a selector, and <c>objc_msgSend</c>
/// cast to the prototype of each call. Every method on a Metal object returns either nothing, a pointer, a
/// <c>NSUInteger</c>, or a <c>BOOL</c>, none of which is a structure, so the plain <c>objc_msgSend</c> entry point is the
/// right one to use on both x86-64 and arm64 - the <c>objc_msgSend_stret</c> variant is only needed for structure returns.
/// </para>
/// <para>
/// Autoreleased objects - a command buffer and a blit encoder are both autoreleased - are only reclaimed by an autorelease
/// pool, and a .NET thread has none, so every operation that creates one runs inside
/// <c>objc_autoreleasePoolPush</c> and <c>objc_autoreleasePoolPop</c>.
/// </para>
/// <para>
/// The constants below are the ones the bridge passes to Metal. The values come from the Metal headers as they are
/// published in the .NET for macOS bindings: <c>MTLStorageModeShared</c> is 0, <c>Managed</c> is 1 and <c>Private</c> is
/// 2, and <c>MTLResourceStorageModePrivate</c> is the storage mode shifted into the resource options (<c>2 &lt;&lt; 4</c>),
/// with <c>MTLResourceCPUCacheModeWriteCombined</c> as its low bit.
/// </para>
/// </remarks>
internal static unsafe class MetalApi
{
    /// <summary>
    /// <c>MTLResourceStorageModeShared</c>.
    /// </summary>
    internal const int RESOURCE_STORAGE_MODE_SHARED = 0;

    /// <summary>
    /// <c>MTLResourceStorageModePrivate</c>.
    /// </summary>
    internal const int RESOURCE_STORAGE_MODE_PRIVATE = MetalStoragePolicy.STORAGE_MODE_PRIVATE << 4;

    /// <summary>
    /// <c>MTLResourceCPUCacheModeWriteCombined</c>.
    /// </summary>
    internal const int RESOURCE_CPU_CACHE_MODE_WRITE_COMBINED = MetalStoragePolicy.CPU_CACHE_MODE_WRITE_COMBINED;

    /// <summary>
    /// The alignment a Metal buffer address is reported to have. Metal does not document a buffer base alignment, and the
    /// addresses the driver hands out are page aligned in practice, so the bridge asks for the arena alignment.
    /// </summary>
    internal const nuint SEGMENT_ALIGNMENT = 256;

    /// <summary>
    /// The largest size a Metal blit copy can move with one call is unbounded, but its offsets and its size have to be
    /// multiples of this.
    /// </summary>
    internal const nuint COPY_ALIGNMENT = 4;

    private static readonly nint objC = LoadObjC();

    private static readonly nint metal = LoadMetal();

    private static readonly delegate* unmanaged[Cdecl]<byte*, nint> getClass =
        (delegate* unmanaged[Cdecl]<byte*, nint>)Export(objC, "objc_getClass");

    private static readonly delegate* unmanaged[Cdecl]<byte*, nint> registerSelector =
        (delegate* unmanaged[Cdecl]<byte*, nint>)Export(objC, "sel_registerName");

    private static readonly delegate* unmanaged[Cdecl]<nint> pushPool =
        (delegate* unmanaged[Cdecl]<nint>)Export(objC, "objc_autoreleasePoolPush");

    private static readonly delegate* unmanaged[Cdecl]<nint, void> popPool =
        (delegate* unmanaged[Cdecl]<nint, void>)Export(objC, "objc_autoreleasePoolPop");

    private static readonly nint send = Export(objC, "objc_msgSend");

    private static readonly delegate* unmanaged[Cdecl]<nint> createSystemDefaultDevice =
        (delegate* unmanaged[Cdecl]<nint>)Export(metal, "MTLCreateSystemDefaultDevice");

    private static readonly Dictionary<string, nint> selectors = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether this machine has the Objective-C runtime and the Metal framework.
    /// </summary>
    internal static bool IsAvailable => objC != 0 && metal != 0 && send != 0 && createSystemDefaultDevice != null;

    /// <summary>
    /// Asks Metal for the system's default device.
    /// </summary>
    /// <returns>The <c>MTLDevice</c>, or <see langword="0"/> when there is none.</returns>
    internal static nint CreateSystemDefaultDevice()
    {
        if (!IsAvailable)
        {
            return 0;
        }

        return createSystemDefaultDevice();
    }

    /// <summary>
    /// Starts an autorelease pool, which is what reclaims the objects Metal returns autoreleased.
    /// </summary>
    /// <returns>The pool token to hand back to <see cref="PopAutoreleasePool"/>.</returns>
    internal static nint PushAutoreleasePool() => pushPool != null ? pushPool() : 0;

    /// <summary>
    /// Ends an autorelease pool.
    /// </summary>
    /// <param name="token">The token <see cref="PushAutoreleasePool"/> returned.</param>
    internal static void PopAutoreleasePool(nint token)
    {
        if (token != 0 && popPool != null)
        {
            popPool(token);
        }
    }

    /// <summary>
    /// Asks an object whether it answers <paramref name="selector"/>, which is how the bridge avoids sending a message that
    /// only exists on a newer macOS.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector to look for.</param>
    /// <returns><see langword="true"/> when the object answers it.</returns>
    internal static bool RespondsToSelector(nint receiver, string selector)
        => receiver != 0
            && ((delegate* unmanaged[Cdecl]<nint, nint, nint, byte>)send)(receiver, Selector("respondsToSelector:"), Selector(selector)) != 0;

    /// <summary>
    /// Sends a message that returns an object pointer.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector, for example <c>commandBuffer</c>.</param>
    /// <returns>The returned object pointer, or <see langword="0"/>.</returns>
    internal static nint Send(nint receiver, string selector)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint>)send)(receiver, Selector(selector));

    /// <summary>
    /// Sends a message that returns nothing.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector.</param>
    internal static void SendVoid(nint receiver, string selector)
        => ((delegate* unmanaged[Cdecl]<nint, nint, void>)send)(receiver, Selector(selector));

    /// <summary>
    /// Sends a message that returns a <c>BOOL</c>, read as a byte because that is what the Objective-C runtime returns.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector.</param>
    /// <returns><see langword="true"/> when the byte is not zero.</returns>
    internal static bool SendBool(nint receiver, string selector)
        => ((delegate* unmanaged[Cdecl]<nint, nint, byte>)send)(receiver, Selector(selector)) != 0;

    /// <summary>
    /// Sends a message that returns an <c>NSUInteger</c>, which is 64 bits wide on every Mac this bridge supports.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector.</param>
    /// <returns>The value, or <see langword="0"/>.</returns>
    internal static ulong SendUInt64(nint receiver, string selector)
        => ((delegate* unmanaged[Cdecl]<nint, nint, ulong>)send)(receiver, Selector(selector));

    /// <summary>
    /// Sends a message that takes one object and returns an object.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="argument">The object argument.</param>
    /// <returns>The returned object, or <see langword="0"/>.</returns>
    internal static nint SendObject(nint receiver, string selector, nint argument)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint>)send)(receiver, Selector(selector), argument);

    /// <summary>
    /// Sends a message that takes one unsigned integer and returns nothing.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="argument">The value.</param>
    internal static void SendUInt(nint receiver, string selector, nuint argument)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nuint, void>)send)(receiver, Selector(selector), argument);

    /// <summary>
    /// Sends a message that takes one float and returns nothing.
    /// </summary>
    /// <param name="receiver">The object.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="argument">The value.</param>
    internal static void SendFloat(nint receiver, string selector, float argument)
        => ((delegate* unmanaged[Cdecl]<nint, nint, float, void>)send)(receiver, Selector(selector), argument);

    /// <summary>
    /// Creates a buffer with <c>newBufferWithLength:options:</c>.
    /// </summary>
    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="length">The length of the buffer, in bytes.</param>
    /// <param name="options">The <c>MTLResourceOptions</c> to create it with.</param>
    /// <returns>The buffer, or <see langword="0"/> when Metal refused.</returns>
    internal static nint NewBuffer(nint device, nuint length, int options)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nuint, nuint, nint>)send)(
            device,
            Selector("newBufferWithLength:options:"),
            length,
            (nuint)options);

    /// <summary>
    /// Creates a two dimensional texture with <c>newTextureWithDescriptor:</c>.
    /// </summary>
    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="format">The <c>MTLPixelFormat</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="usage">The <c>MTLTextureUsage</c> flags.</param>
    /// <returns>The texture, or <see langword="0"/> when Metal refused.</returns>
    internal static nint NewTexture(nint device, uint format, nuint width, nuint height, nuint usage)
    {
        var descriptor = ((delegate* unmanaged[Cdecl]<nint, nint, nuint, nuint, nuint, byte, nint>)send)(
            Class("MTLTextureDescriptor"),
            Selector("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            format,
            width,
            height,
            0);

        if (descriptor == 0)
        {
            return 0;
        }

        SendUInt(descriptor, "setUsage:", usage);

        return SendObject(device, "newTextureWithDescriptor:", descriptor);
    }

    /// <summary>
    /// Writes one region of a texture with <c>replaceRegion:mipmapLevel:withBytes:bytesPerRow:</c>.
    /// </summary>
    /// <param name="texture">The <c>MTLTexture</c>.</param>
    /// <param name="region">The region.</param>
    /// <param name="level">The mip level.</param>
    /// <param name="bytes">The pixels.</param>
    /// <param name="bytesPerRow">The number of bytes in one row.</param>
    internal static void ReplaceRegion(nint texture, MetalRegion region, nuint level, void* bytes, nuint bytesPerRow)
        => ((delegate* unmanaged[Cdecl]<nint, nint, MetalRegion, nuint, void*, nuint, void>)send)(
            texture,
            Selector("replaceRegion:mipmapLevel:withBytes:bytesPerRow:"),
            region,
            level,
            bytes,
            bytesPerRow);

    /// <summary>
    /// Reads one region of a texture with <c>getBytes:bytesPerRow:fromRegion:mipmapLevel:</c>.
    /// </summary>
    /// <param name="texture">The <c>MTLTexture</c>.</param>
    /// <param name="bytes">The destination.</param>
    /// <param name="bytesPerRow">The number of bytes in one row.</param>
    /// <param name="region">The region.</param>
    /// <param name="level">The mip level.</param>
    internal static void GetBytes(nint texture, void* bytes, nuint bytesPerRow, MetalRegion region, nuint level)
        => ((delegate* unmanaged[Cdecl]<nint, nint, void*, nuint, MetalRegion, nuint, void>)send)(
            texture,
            Selector("getBytes:bytesPerRow:fromRegion:mipmapLevel:"),
            bytes,
            bytesPerRow,
            region,
            level);

    /// <summary>
    /// Creates a sampler state with <c>newSamplerStateWithDescriptor:</c>.
    /// </summary>
    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="minFilter">The <c>MTLSamplerMinMagFilter</c>.</param>
    /// <param name="magFilter">The <c>MTLSamplerMinMagFilter</c>.</param>
    /// <param name="address">The <c>MTLSamplerAddressMode</c>.</param>
    /// <returns>The sampler state, or <see langword="0"/> when Metal refused.</returns>
    internal static nint NewSamplerState(nint device, nuint minFilter, nuint magFilter, nuint address, float minLod, float maxLod, float lodBias, nuint maxAnisotropy)
    {
        var descriptor = Send(Class("MTLSamplerDescriptor"), "new");

        if (descriptor == 0)
        {
            return 0;
        }

        try
        {
            SendUInt(descriptor, "setMinFilter:", minFilter);
            SendUInt(descriptor, "setMagFilter:", magFilter);
            SendUInt(descriptor, "setSAddressMode:", address);
            SendUInt(descriptor, "setTAddressMode:", address);
            SendFloat(descriptor, "setMinLOD:", minLod);
            SendFloat(descriptor, "setMaxLOD:", maxLod);
            SendFloat(descriptor, "setLodBias:", lodBias);
            SendUInt(descriptor, "setMaxAnisotropy:", maxAnisotropy);

            return SendObject(device, "newSamplerStateWithDescriptor:", descriptor);
        }
        finally
        {
            Release(descriptor);
        }
    }

    /// <summary>
    /// Records <c>copyFromBuffer:sourceOffset:toBuffer:destinationOffset:size:</c> on a blit encoder.
    /// </summary>
    /// <param name="encoder">The <c>MTLBlitCommandEncoder</c>.</param>
    /// <param name="source">The source <c>MTLBuffer</c>.</param>
    /// <param name="sourceOffset">The offset inside the source, in bytes.</param>
    /// <param name="destination">The destination <c>MTLBuffer</c>.</param>
    /// <param name="destinationOffset">The offset inside the destination, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    internal static void CopyBuffer(nint encoder, nint source, nuint sourceOffset, nint destination, nuint destinationOffset, nuint size)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint, nuint, nint, nuint, nuint, void>)send)(
            encoder,
            Selector("copyFromBuffer:sourceOffset:toBuffer:destinationOffset:size:"),
            source,
            sourceOffset,
            destination,
            destinationOffset,
            size);

    /// <summary>
    /// Creates an <c>NSString</c> from a managed string.
    /// </summary>
    /// <remarks>
    /// The object is autoreleased, so the caller has to be inside an autorelease pool that it pops <em>after</em> the string
    /// has been handed to Metal: popping a pool of its own here would release the string before it is used, and a dangling
    /// Objective-C object shows up as a crash or a hang rather than as an error.
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <returns>The autoreleased string object.</returns>
    internal static nint NewString(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\0");

        fixed (byte* utf8 = bytes)
        {
            return ((delegate* unmanaged[Cdecl]<nint, nint, byte*, nint>)send)(
                Class("NSString"),
                Selector("stringWithUTF8String:"),
                utf8);
        }
    }

    /// <summary>
    /// Compiles Metal Shading Language source into a library with <c>newLibraryWithSource:options:error:</c>.
    /// </summary>
    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="source">The shader source.</param>
    /// <param name="error">When the method returns, the text of the <c>NSError</c> describing the failure, or an empty string.</param>
    /// <returns>The library, or <see langword="0"/> when the source does not compile.</returns>
    /// <remarks>
    /// The <c>NSError</c> an out-parameter returns is autoreleased, so its description is copied to a managed string before
    /// this method returns.
    /// </remarks>
    internal static nint NewLibrary(nint device, string source, out string error)
    {
        var pool = PushAutoreleasePool();
        nint failure = 0;

        try
        {
            var text = NewString(source);
            var library = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint*, nint>)send)(
                device,
                Selector("newLibraryWithSource:options:error:"),
                text,
                0,
                &failure);

            error = DescribeError(failure);
            return library;
        }
        finally
        {
            PopAutoreleasePool(pool);
        }
    }

    /// <summary>
    /// Loads a precompiled <c>.metallib</c> with <c>newLibraryWithFile:error:</c>.
    /// </summary>
    /// <remarks>
    /// A <c>metallib</c> loads without the runtime compiler behind <c>newLibraryWithSource:</c>, so a build that has
    /// <c>xcrun metal</c> can ship one. <c>newLibraryWithData:</c> is not an alternative here: it takes a
    /// <c>dispatch_data_t</c>, not an <c>NSData</c>, and handing it the wrong object crashes the process.
    /// </remarks>
    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="path">The path of the <c>.metallib</c> file.</param>
    /// <param name="error">When the method returns, the text of the <c>NSError</c> describing the failure, or an empty string.</param>
    /// <returns>The library, or <see langword="0"/> when Metal refused the file.</returns>
    internal static nint NewLibraryWithFile(nint device, string path, out string error)
    {
        var pool = PushAutoreleasePool();
        nint failure = 0;

        try
        {
            var library = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint*, nint>)send)(
                device,
                Selector("newLibraryWithFile:error:"),
                NewString(path),
                &failure);

            error = DescribeError(failure);
            return library;
        }
        finally
        {
            PopAutoreleasePool(pool);
        }
    }

    /// <summary>
    /// Looks a kernel up in a library with <c>newFunctionWithName:</c>.
    /// </summary>
    /// <param name="library">The <c>MTLLibrary</c>.</param>
    /// <param name="name">The function name.</param>
    /// <returns>The function, or <see langword="0"/> when it is not there.</returns>
    internal static nint NewFunction(nint library, string name)
    {
        var pool = PushAutoreleasePool();

        try
        {
            return ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint>)send)(
                library,
                Selector("newFunctionWithName:"),
                NewString(name));
        }
        finally
        {
            PopAutoreleasePool(pool);
        }
    }

    /// <summary>
    /// Builds a compute pipeline state with <c>newComputePipelineStateWithFunction:error:</c>.
    /// </summary>
    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="function">The kernel function.</param>
    /// <param name="error">When the method returns, the text of the <c>NSError</c> describing the failure, or an empty string.</param>
    /// <returns>The pipeline state, or <see langword="0"/> when Metal refused.</returns>
    internal static nint NewComputePipelineState(nint device, nint function, out string error)
    {
        var pool = PushAutoreleasePool();
        nint failure = 0;

        try
        {
            var state = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint*, nint>)send)(
                device,
                Selector("newComputePipelineStateWithFunction:error:"),
                function,
                &failure);

            error = DescribeError(failure);
            return state;
        }
        finally
        {
            PopAutoreleasePool(pool);
        }
    }

    /// <summary>
    /// Opens a compute encoder on a command buffer with <c>computeCommandEncoder</c>.
    /// </summary>
    /// <param name="commandBuffer">The <c>MTLCommandBuffer</c>.</param>
    /// <returns>The encoder, or <see langword="0"/> when Metal refused.</returns>
    internal static nint ComputeCommandEncoder(nint commandBuffer)
        => Send(commandBuffer, "computeCommandEncoder");

    /// <summary>
    /// Calls <c>setComputePipelineState:</c>.
    /// </summary>
    /// <param name="encoder">The <c>MTLComputeCommandEncoder</c>.</param>
    /// <param name="state">The pipeline state.</param>
    internal static void SetComputePipelineState(nint encoder, nint state)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint, void>)send)(encoder, Selector("setComputePipelineState:"), state);

    /// <summary>
    /// Calls <c>setBuffer:offset:atIndex:</c>.
    /// </summary>
    /// <param name="encoder">The <c>MTLComputeCommandEncoder</c>.</param>
    /// <param name="buffer">The <c>MTLBuffer</c>.</param>
    /// <param name="offset">The offset inside the buffer, in bytes.</param>
    /// <param name="index">The argument table index the kernel reads or writes the buffer at.</param>
    internal static void SetBuffer(nint encoder, nint buffer, nuint offset, nuint index)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint, nuint, nuint, void>)send)(
            encoder,
            Selector("setBuffer:offset:atIndex:"),
            buffer,
            offset,
            index);

    /// <summary>
    /// Binds a texture with <c>setTexture:atIndex:</c>.
    /// </summary>
    /// <param name="encoder">The <c>MTLComputeCommandEncoder</c>.</param>
    /// <param name="texture">The <c>MTLTexture</c>.</param>
    /// <param name="index">The texture index.</param>
    internal static void SetTexture(nint encoder, nint texture, nuint index)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint, nuint, void>)send)(
            encoder,
            Selector("setTexture:atIndex:"),
            texture,
            index);

    /// <summary>
    /// Binds a sampler state with <c>setSamplerState:atIndex:</c>.
    /// </summary>
    /// <param name="encoder">The <c>MTLComputeCommandEncoder</c>.</param>
    /// <param name="sampler">The <c>MTLSamplerState</c>.</param>
    /// <param name="index">The sampler index.</param>
    internal static void SetSamplerState(nint encoder, nint sampler, nuint index)
        => ((delegate* unmanaged[Cdecl]<nint, nint, nint, nuint, void>)send)(
            encoder,
            Selector("setSamplerState:atIndex:"),
            sampler,
            index);

    /// <summary>
    /// Calls <c>dispatchThreadgroups:threadsPerThreadgroup:</c>. Both arguments are <c>MTLSize</c>, which is three 64-bit
    /// fields, so the ABI hands them over by memory rather than in registers.
    /// </summary>
    /// <param name="encoder">The <c>MTLComputeCommandEncoder</c>.</param>
    /// <param name="groups">The thread groups per grid.</param>
    /// <param name="threads">The threads per group, which has to match the kernel's <c>threads_per_threadgroup</c>.</param>
    internal static void DispatchThreadgroups(nint encoder, MetalSize groups, MetalSize threads)
        => ((delegate* unmanaged[Cdecl]<nint, nint, MetalSize, MetalSize, void>)send)(
            encoder,
            Selector("dispatchThreadgroups:threadsPerThreadgroup:"),
            groups,
            threads);

    /// <summary>
    /// Reads the text of an <c>NSError</c>.
    /// </summary>
    /// <param name="error">The error, which may be <see langword="0"/>.</param>
    /// <returns>The localized description, or an empty string when there is none.</returns>
    internal static string DescribeError(nint error)
        => error == 0 ? string.Empty : Describe(Send(error, "localizedDescription"));

    /// <summary>
    /// Reads the text out of an <c>NSString</c> or of an <c>NSError</c>'s description.
    /// </summary>
    /// <param name="instance">The object, which may be <see langword="0"/>.</param>
    /// <returns>The text, or an empty string when there is none.</returns>
    internal static string Describe(nint instance)
    {
        if (instance == 0)
        {
            return string.Empty;
        }

        var text = Send(instance, "UTF8String");

        return text == 0 ? string.Empty : Marshal.PtrToStringUTF8(text) ?? string.Empty;
    }

    /// <summary>
    /// Looks up an Objective-C class.
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <returns>The class, or <see langword="0"/> when it is not there.</returns>
    private static nint Class(string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name + "\0");

        fixed (byte* text = bytes)
        {
            return getClass(text);
        }
    }

    /// <summary>
    /// Retains an Objective-C object so that it outlives the autorelease pool it was created in.
    /// </summary>
    /// <param name="instance">The object, which may be <see langword="0"/>.</param>
    internal static void Retain(nint instance)
    {
        if (instance != 0)
        {
            SendVoid(instance, "retain");
        }
    }

    /// <summary>
    /// Releases an Objective-C object this bridge owns.
    /// </summary>
    /// <param name="instance">The object, which may be <see langword="0"/>.</param>
    internal static void Release(nint instance)
    {
        if (instance != 0)
        {
            SendVoid(instance, "release");
        }
    }

    /// <summary>
    /// Looks up a selector, registering it the first time it is asked for.
    /// </summary>
    /// <param name="name">The selector name.</param>
    /// <returns>The registered selector.</returns>
    private static nint Selector(string name)
    {
        if (selectors.TryGetValue(name, out var cached))
        {
            return cached;
        }

        // The runtime copies the name, so the buffer only has to live for the duration of the call.
        var bytes = Encoding.UTF8.GetBytes(name + "\0");
        nint selector;

        fixed (byte* text = bytes)
        {
            selector = registerSelector(text);
        }

        selectors[name] = selector;
        return selector;
    }

    /// <summary>
    /// Finds the Objective-C runtime.
    /// </summary>
    /// <returns>The library handle, or <see langword="0"/> when this is not a Mac.</returns>
    private static nint LoadObjC() => NativeInterop.TryLoad("libobjc.A.dylib", out var handle) ? handle : 0;

    /// <summary>
    /// Finds the Metal framework.
    /// </summary>
    /// <returns>The library handle, or <see langword="0"/> when Metal is not available.</returns>
    private static nint LoadMetal() => NativeInterop.TryLoad("/System/Library/Frameworks/Metal.framework/Metal", out var handle) ? handle : 0;

    /// <summary>
    /// Resolves an exported name, allowing it to be missing so that the type can be loaded on a machine without Metal.
    /// </summary>
    /// <param name="library">The library handle, which may be <see langword="0"/>.</param>
    /// <param name="name">The exported name.</param>
    /// <returns>The address, or <see langword="0"/> when it is not there.</returns>
    private static nint Export(nint library, string name)
    {
        if (library != 0 && NativeInterop.TryGetExport(library, name, out var address))
        {
            return address;
        }

        return 0;
    }
}

/// <summary>
/// <c>MTLSize</c>: three 64-bit counts, 24 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MetalSize
{
    /// <summary>
    /// The count along x.
    /// </summary>
    public nuint Width;

    /// <summary>
    /// The count along y.
    /// </summary>
    public nuint Height;

    /// <summary>
    /// The count along z.
    /// </summary>
    public nuint Depth;
}

/// <summary>
/// <c>MTLRegion</c>: an origin and a size, each three 64-bit counts, 48 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MetalRegion
{
    /// <summary>
    /// The origin along x.
    /// </summary>
    public nuint X;

    /// <summary>
    /// The origin along y.
    /// </summary>
    public nuint Y;

    /// <summary>
    /// The origin along z.
    /// </summary>
    public nuint Z;

    /// <summary>
    /// The size along x.
    /// </summary>
    public nuint Width;

    /// <summary>
    /// The size along y.
    /// </summary>
    public nuint Height;

    /// <summary>
    /// The size along z.
    /// </summary>
    public nuint Depth;
}
