using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Unsafe;

/// <summary>
/// Defines a type that can supply an <see cref="Allocator"/> instance used to allocate memory for it.
/// </summary>
public interface IAllocatable
{
    /// <summary>
    /// Gets an <see cref="Allocator"/> instance that can be used to allocate memory for the implementing type.
    /// </summary>
    /// <returns>An <see cref="Allocator"/> instance that can be used to allocate memory for the implementing type.</returns>
    Allocator GetAllocator();
}

/// <summary>
/// Represents a memory allocator that provides methods for allocating, resizing, freeing, and remapping memory blocks.
/// </summary>
/// <param name="context">An opaque pointer passed as the first argument to every allocator method, used to hold the allocator's state.</param>
/// <param name="methods">The virtual table that implements the allocator operations.</param>
public readonly unsafe struct Allocator(void* context, Allocator.VTable methods)
{
    /// <summary>
    /// The context pointer that is passed to the allocator methods. This pointer is used to maintain state and context for the allocator.
    /// </summary>
    public readonly void* Context = context;

    /// <summary>
    /// The virtual table (vtable) containing function pointers to the allocator methods. This vtable allows for dynamic dispatch of the
    /// allocator methods based on the specific implementation of the allocator.
    /// </summary>
    public readonly VTable Methods = methods;

    /// <summary>
    /// Holds the managed function pointers that implement the allocator operations.
    /// Every function receives the allocator's <see cref="Allocator.Context"/> as its first argument.
    /// </summary>
    public readonly struct VTable
    {
        /// <summary>
        /// Resets the allocator's initial state,freeing all allocated memory blocks.
        /// </summary>
        public readonly delegate* managed<void*, void> Reset;

        /// <summary>
        /// Allocates a block of memory of the specified length and alignment. This will allocate memory for a specific type or data structure.
        /// </summary>
        public readonly delegate* managed<void*, nuint, byte, void*> Alloc;

        /// <summary>
        /// Resizes a previously allocated block of memory. This will change the size of an existing memory block, preserving its contents if possible.
        /// </summary>
        public readonly delegate* managed<void*, void*, nuint, nuint, byte, bool> Resize;

        /// <summary>
        /// Frees a previously allocated block of memory that is no longer needed, allowing it to be reused for future allocations.
        /// </summary>
        public readonly delegate* managed<void*, void*, nuint, byte, void> Free;

        /// <summary>
        /// Remaps a previously allocated block of memory to a new size, while preserving its contents.
        /// This will change the size of an existing memory block, potentially moving it to a new location in memory if necessary.
        /// If there is enough free space, it successfully returns a new pointer. If the buffer is full, it returns null.
        /// </summary>
        public readonly delegate* managed<void*, void*, nuint, nuint, byte, void*> Remap;

        /// <summary>
        /// Initializes a new virtual table with the given allocator method implementations.
        /// </summary>
        /// <param name="reset">The function pointer that resets the allocator.</param>
        /// <param name="alloc">The function pointer that allocates a memory block.</param>
        /// <param name="resize">The function pointer that resizes a memory block in place.</param>
        /// <param name="free">The function pointer that frees a memory block.</param>
        /// <param name="remap">The function pointer that remaps a memory block, potentially moving it.</param>
        public VTable(
            delegate*<void*, void> reset,
            delegate*<void*, nuint, byte, void*> alloc,
            delegate*<void*, void*, nuint, nuint, byte, bool> resize,
            delegate*<void*, void*, nuint, byte, void> free,
            delegate*<void*, void*, nuint, nuint, byte, void*> remap)
        {
            Reset = reset;
            Alloc = alloc;
            Resize = resize;
            Free = free;
            Remap = remap;
        }
    }


    /// <summary>
    /// Allocates a block of memory of the specified length and alignment. This will allocate memory for a specific type or data structure.
    /// </summary>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* Alloc(nuint len, byte alignment) => Methods.Alloc(Context, len, alignment);

    /// <summary>
    /// Resizes a previously allocated block of memory. This will change the size of an existing memory block, preserving its contents if possible.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block to be resized.</param>
    /// <param name="oldLen">The current size of the memory block in bytes.</param>
    /// <param name="newLen"> The desired new size of the memory block in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Resize(void* ptr, nuint oldLen, nuint newLen, byte alignment) => Methods.Resize(Context, ptr, oldLen, newLen, alignment);

    /// <summary>
    /// Frees a previously allocated block of memory that is no longer needed, allowing it to be reused for future allocations.
    /// </summary>
    /// <param name="ptr">The pointer to the memory block to be freed.</param>
    /// <param name="len">The size of the memory block to be freed in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free(void* ptr, nuint len, byte alignment) => Methods.Free(Context, ptr, len, alignment);

    /// <summary>
    /// Remaps a previously allocated block of memory to a new size, while preserving its contents.
    /// </summary>
    /// <param name="ptr">The pointer to the original memory block.</param>
    /// <param name="oldLen">The size of the original memory block in bytes.</param>
    /// <param name="newLen"> The desired new size of the memory block in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the new memory block (in bytes).</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* Remap(void* ptr, nuint oldLen, nuint newLen, byte alignment) => Methods.Remap(Context, ptr, oldLen, newLen, alignment);

    /// <summary>
    /// Resets the allocator's initial state, freeing all allocated memory blocks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Methods.Reset(Context);
}

/// <summary>
/// Represents a fixed-size memory allocator that allocates memory from a pre-allocated buffer.
/// This allocator does not support freeing or resizing memory blocks, and it is intended for
/// scenarios where memory usage is predictable and bounded.
/// </summary>
/// <param name="buffer">Fixed buffer</param>
public unsafe ref struct FixedAllocator(Span<byte> buffer) : IAllocatable
{
    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable s_Methods = new(&Reset, &Alloc, &Resize, &Free, &Remap);

    /// <summary>
    /// The fixed buffer from which memory is allocated. This buffer is provided by the user and must remain valid for the lifetime of the allocator.
    /// </summary>
    private readonly Span<byte> m_Buffer = buffer;

    /// <summary>
    /// The end index of the allocated memory within the fixed buffer. This index indicates the position in the buffer where the next allocation will occur.
    /// </summary>
    private int m_EndIndex;

    /// <summary>
    /// Gets the length of the allocated memory in bytes. This property returns the number of bytes that have been allocated from the fixed buffer.
    /// </summary>
    public int Length => m_EndIndex - 1;

    /// <summary>
    /// Gets the total capacity of the fixed buffer in bytes. This property returns the total number of bytes available in the fixed buffer for allocation.
    /// </summary>
    public int Capacity => m_Buffer.Length;

    /// <summary>
    /// Gets a value indicating whether the fixed buffer is full. This property returns true if the allocated memory length is greater than or equal to the total capacity of the fixed buffer; otherwise, it returns false.
    /// </summary>
    public bool Full => Length >= m_Buffer.Length;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), s_Methods);
#else
        fixed (FixedAllocator* pThis = &this)
        {
            return new Allocator(pThis, s_Methods);
        }
#endif
    }

    /// <summary>
    /// Resets the allocator's initial state, freeing all allocated memory blocks. This method sets the end index
    /// of the allocated memory back to zero, effectively marking all previously allocated memory as free.
    /// </summary>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    private static void Reset(void* ctx)
    {
        var self = (FixedAllocator*)ctx;
        self->m_EndIndex = 0;
    }

    /// <summary>
    /// Allocates a block of memory of the specified length and alignment from the fixed buffer.
    /// </summary>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    /// <returns> A pointer to the allocated memory block.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when the requested length exceeds the available space in the fixed buffer.</exception>
    private static void* Alloc(void* ctx, nuint len, byte alignment)
    {
        var self = (FixedAllocator*)ctx;
        if ((nuint)self->m_EndIndex + len - 1 > (nuint)self->m_Buffer.Length)
        {
            throw new OutOfMemoryException("Out of memory.");
        }

        var ptr = Native.AsPointer(self->m_Buffer.Slice(self->m_EndIndex, (int)len));
        self->m_EndIndex += (int)len;
        return ptr;
    }

    /// <summary>
    /// Resizes the memory block pointed to by <paramref name="ptr"/> to a new size of <paramref name="newLen"/> bytes.
    /// </summary>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be resized.</param>
    /// <param name="oldLen">The current size of the memory block in bytes.</param>
    /// <param name="newLen"> The desired new size of the memory block in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>
    /// If the <paramref name="newLen"/> is larger than the <paramref name="oldLen"/>, it returns true.
    /// Space is only reclaimed if the memory block you request to expand is exactly the one allocated last
    /// and there is still free space in the fixed buffer; otherwise, the function always returns false.
    /// </returns>
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (FixedAllocator*)ctx;
        if (oldLen > newLen)
        {
            return true;
        }

        var endIndex = (uint)ptr - (uint)Native.AsPointer(self->m_Buffer) + oldLen;
        if (endIndex != (uint)self->m_EndIndex || endIndex - 1 > (uint)self->m_Buffer.Length)
        {
            return false;
        }

        self->m_EndIndex = (int)endIndex;
        return true;
    }

    /// <summary>
    /// Frees the memory block pointed to by <paramref name="ptr"/>. If the pointer is not the last allocated block, it does nothing.
    /// </summary>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be freed.</param>
    /// <param name="len">The size of the memory block to be freed in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
    {
        var self = (FixedAllocator*)ctx;
        var endIndex = (uint)ptr - (uint)Native.AsPointer(self->m_Buffer) + len;

        if (endIndex != (uint)self->m_EndIndex)
        {
            return;
        }

        if (endIndex > (uint)self->m_EndIndex)
        {
            return;
        }

        self->m_EndIndex -= (int)len;
    }

    /// <summary>
    /// Remap the memory block pointed to by <paramref name="ptr"/> to a new size of <paramref name="newLen"/> bytes,
    /// while preserving the contents of the original block. 
    /// </summary>
    /// <remarks>
    /// Since old memory cannot be truly reclaimed, this operation significantly accelerates the depletion of buffer space.
    /// </remarks>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    /// <param name="ptr">The pointer to the original memory block (not used in this implementation, but required for the signature).</param>
    /// <param name="oldLen">The size of the original memory block in bytes (not used in this implementation, but required for the signature).</param>
    /// <param name="newLen"> The desired new size of the memory block in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the new memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>If there is enough free space, it successfully returns a new pointer. If the buffer is full, it returns null.</returns>
    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (FixedAllocator*)ctx;
        var remaining = self->m_Buffer[self->m_EndIndex..];
        return remaining.Length > (int)newLen ? Native.AsPointer(remaining) : null;
    }
}

/// <summary>
/// Represents an allocator that serves allocations from pinned managed byte arrays.
/// Memory is therefore managed by the garbage collector rather than the native heap, at the cost of
/// pinning the backing arrays for as long as their blocks are alive.
/// </summary>
public unsafe struct ManagedAllocator() : IAllocatable
{
    /// <summary>
    /// Maps the address of each live allocation to the <see cref="GCHandle"/> that keeps its backing array pinned,
    /// so that the array can be released later.
    /// </summary>
    private readonly ConcurrentDictionary<IntPtr, GCHandle> m_PinnedMaps = new();

    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable s_Methods = new(&Reset, &Alloc, &Resize, &Free, &Remap);

    /// <inheritdoc/>
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), s_Methods);
#else
        fixed (ManagedAllocator* pThis = &this)
        {
            return new Allocator(pThis, s_Methods);
        }
#endif
    }

    /// <summary>
    /// Releases every pinned array and clears the address map, returning the allocator to its initial state.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    private static void Reset(void* ctx)
    {
        var self = (ManagedAllocator*)ctx;
        foreach (var kv in self->m_PinnedMaps)
        {
            kv.Value.Free();
        }
        self->m_PinnedMaps.Clear();
    }

    /// <summary>
    /// Allocates a pinned managed byte array of <paramref name="len"/> bytes and returns a pointer to its first byte.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    private static void* Alloc(void* ctx, nuint len, byte alignment)
    {
        var self = (ManagedAllocator*)ctx;
        var handle = GCHandle.Alloc(new byte[(int)len], GCHandleType.Pinned);
        var ptr = handle.AddrOfPinnedObject();
        self->m_PinnedMaps[ptr] = handle;
        return ptr.ToPointer();
    }

    /// <summary>
    /// Resizing pinned managed arrays in place is not supported.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be resized.</param>
    /// <param name="oldLen">The current size of the memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>Always <see langword="false"/>; use <see cref="Remap"/> instead.</returns>
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        return false;
    }

    /// <summary>
    /// Unpins the array backing the block at <paramref name="ptr"/>, allowing the garbage collector to reclaim it.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be freed.</param>
    /// <param name="len">The size of the memory block to be freed in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="ptr"/> was not allocated by this allocator.</exception>
    private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
    {
        var self = (ManagedAllocator*)ctx;
        if (!self->m_PinnedMaps.TryGetValue(new IntPtr(ptr), out var handle))
        {
            throw new InvalidOperationException("Not allocated address");
        }

        handle.Free();
    }

    /// <summary>
    /// Resizes the block at <paramref name="ptr"/> by resizing its backing array and re-pinning it,
    /// returning the address of the new pinned array. Existing contents are preserved up to <paramref name="oldLen"/> bytes.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    /// <param name="ptr">The pointer to the original memory block.</param>
    /// <param name="oldLen">The size of the original memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the new memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>A pointer to the new memory block.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="ptr"/> was not allocated by this allocator.</exception>
    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (ManagedAllocator*)ctx;
        var safePtr = new IntPtr(ptr);
        if (!self->m_PinnedMaps.TryRemove(safePtr, out var handle))
        {
            throw new InvalidOperationException("Not allocated address");
        }

        var array = (byte[])handle.Target!;
        Array.Resize(ref array, (int)newLen);
        handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        safePtr = handle.AddrOfPinnedObject();
        self->m_PinnedMaps[safePtr] = handle;
        return safePtr.ToPointer();
    }
}

/// <summary>
/// Represents an allocator that serves allocations from the native heap using
/// <see cref="NativeMemory"/> (or <see cref="Marshal"/> on older target frameworks).
/// </summary>
public unsafe struct UnmanagedAllocator : IAllocatable
{
    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable s_Methods = new(&Reset, &Alloc, &Resize, &Free, &Remap);

    /// <inheritdoc/>
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), s_Methods);
#else
        fixed (UnmanagedAllocator* pThis = &this)
        {
            return new Allocator(pThis, s_Methods);
        }
#endif
    }

    /// <summary>
    /// Does nothing: each block is allocated independently on the native heap, so there is no allocator-wide state to reset.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
    private static void Reset(void* ctx) { }

    /// <summary>
    /// Allocates a block of memory of <paramref name="len"/> bytes from the native heap with the requested alignment.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    private static void* Alloc(void* ctx, nuint len, byte alignment)
    {
#if NET6_0_OR_GREATER
        return NativeMemory.AlignedAlloc(len, alignment);
#else
        return Marshal.AllocHGlobal((nint)len).ToPointer();
#endif
    }

    /// <summary>
    /// Resizing blocks in place is not supported.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be resized.</param>
    /// <param name="oldLen">The current size of the memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>Always <see langword="false"/>; use <see cref="Remap"/> instead.</returns>
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        return false;
    }

    /// <summary>
    /// Releases the block at <paramref name="ptr"/> back to the native heap.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be freed.</param>
    /// <param name="len">The size of the memory block to be freed in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
    private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
    {
#if NET6_0_OR_GREATER
        NativeMemory.AlignedFree(ptr);
#else
        Marshal.FreeHGlobal(new IntPtr(ptr));
#endif
    }

    /// <summary>
    /// Reallocates the block at <paramref name="ptr"/> to <paramref name="newLen"/> bytes, preserving its contents.
    /// The block may be moved to a new address.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
    /// <param name="ptr">The pointer to the original memory block.</param>
    /// <param name="oldLen">The size of the original memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the new memory block (in bytes).</param>
    /// <returns>A pointer to the remapped memory block.</returns>
    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
#if NET6_0_OR_GREATER
        return NativeMemory.AlignedRealloc(ptr, newLen, alignment);
#else
        return Marshal.ReAllocHGlobal(new IntPtr(ptr), (nint)newLen).ToPointer();
#endif
    }
}

/// <summary>
/// Provides extension methods for the <see cref="Allocator"/> struct, allowing for convenient allocation and management of memory blocks for specific types.
/// </summary>
public static unsafe class AllocatorExtensions
{
    /// <param name="allocator">The <see cref="Allocator"/> instance to extend.</param>
    extension(ref Allocator allocator)
    {
        /// <summary>
        /// Allocates a block of memory for a specified number of elements of type <typeparamref name="T"/> with the default alignment for that type.
        /// </summary>
        /// <param name="count">The number of elements of type <typeparamref name="T"/> to allocate.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of elements to allocate memory for.</typeparam>
        /// <returns> A <see cref="Span{T}"/> representing the allocated memory block.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Alloc<T>(nuint count, byte alignment)
        {
            var size = count * Native.GetStackSize<T>();
            return new Span<T>(allocator.Alloc(size, alignment), (int)count);
        }

        /// <summary>
        /// Allocates a block of memory for a specified number of elements of type <typeparamref name="T"/> with the default alignment for that type.
        /// </summary>
        /// <param name="count">The number of elements of type <typeparamref name="T"/> to allocate.</param>
        /// <typeparam name="T">The type of elements to allocate memory for.</typeparam>
        /// <returns> A <see cref="Span{T}"/> representing the allocated memory block.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Alloc<T>(nuint count)
        {
            return allocator.Alloc<T>(count, Reflection.Type<T>.Info.Alignment);
        }

        /// <summary>
        /// Allocates a new instance of type <typeparamref name="T"/> with the specified alignment, and returns a reference to it.
        /// </summary>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of the object to allocate.</typeparam>
        /// <returns> A reference to the newly allocated instance of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T New<T>(byte alignment)
        {
            var typeInfo = Reflection.Type<T>.Info;
            if (typeInfo.IsClass)
            {
                var buffer = (nint*)allocator.Alloc(typeInfo.FixedSize, alignment);
                buffer[0] = IntPtr.Zero; // Optional
                buffer[1] = typeof(T).TypeHandle.Value; // MethodTable
                var ptr = (nint)buffer + 1;
                return ref Native.Cast<T>(ptr);
            }
            else
            {
                var ptr = (nint)allocator.Alloc(typeInfo.Size, alignment);
                return ref Native.Cast<T>(ptr);
            }
        }

        /// <summary>
        /// Allocates a new instance of type <typeparamref name="T"/> with the default alignment for that type, and returns a reference to it.
        /// </summary>
        /// <typeparam name="T">The type of the object to allocate.</typeparam>
        /// <returns> A reference to the newly allocated instance of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T New<T>() => ref allocator.New<T>(Reflection.Type<T>.Info.Alignment);

        /// <summary>
        /// Frees the memory block associated with the specified reference of type <typeparamref name="T"/> with the specified alignment.
        /// </summary>
        /// <param name="src">The reference to the object of type <typeparamref name="T"/> whose memory block is to be freed.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of the object whose memory block is to be freed.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free<T>(ref T src, byte alignment) => allocator.Free(Native.AsPointer(ref src), Reflection.Type<T>.Info.FixedSize, alignment);

        /// <summary>
        /// Frees the memory block associated with the specified reference of type <typeparamref name="T"/> with the default alignment for that type.
        /// </summary>
        /// <param name="src">The reference to the object of type <typeparamref name="T"/> whose memory block is to be freed.</param>
        /// <typeparam name="T">The type of the object whose memory block is to be freed.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free<T>(ref T src) => allocator.Free(ref src, Reflection.Type<T>.Info.Alignment);

        /// <summary>
        /// Frees the memory block associated with the specified <see cref="Span{T}"/> of type <typeparamref name="T"/> with the specified alignment.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> representing the memory block to be freed.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/> whose memory block is to be freed.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free<T>(Span<T> span, byte alignment)
        {
            var typeInfo = Reflection.Type<T>.Info;
            allocator.Free(allocator.Context, (nuint)span.Length * typeInfo.Size, alignment);
        }

        /// <summary>
        /// Frees the memory block associated with the specified <see cref="Span{T}"/> of type <typeparamref name="T"/> with the default alignment for that type.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> representing the memory block to be freed.</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/> whose memory block is to be freed.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free<T>(Span<T> span) => allocator.Free(span, Reflection.Type<T>.Info.Alignment);

        /// <summary>
        /// Resizes the specified <see cref="Span{T}"/> to a new size of <paramref name="newSize"/> elements, while preserving its contents.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> to be resized.</param>
        /// <param name="newSize">The desired new size of the <see cref="Span{T}"/> in number of elements.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <param name="newSpan">When the method returns, contains the resized <see cref="Span{T}"/> if the operation was successful; otherwise, it contains an empty <see cref="Span{T}"/>.</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/> to be resized.</typeparam>
        /// <returns> true if the resize operation was successful; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Resize<T>(Span<T> span, int newSize, byte alignment, out Span<T> newSpan)
        {
            var typeInfo = Reflection.Type<T>.Info;
            var oldSize = (nuint)span.Length * typeInfo.Size;
            var newSizeBytes = (nuint)newSize * typeInfo.Size;
            var ptr = Native.AsPointer(span);
            var succeed = allocator.Resize(ptr, oldSize, newSizeBytes, alignment);
            if (succeed)
            {
                newSpan = new Span<T>(ptr, newSize);
                return true;
            }

            newSpan = Span<T>.Empty;
            return false;
        }

        /// <summary>
        /// Resizes the specified <see cref="Span{T}"/> to a new size of <paramref name="newSize"/> elements, while preserving its contents.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> to be resized.</param>
        /// <param name="newSize">The desired new size of the <see cref="Span{T}"/> in number of elements.</param>
        /// <param name="newSpan">When the method returns, contains the resized <see cref="Span{T}"/> if the operation was successful; otherwise, it contains an empty <see cref="Span{T}"/>.</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/> to be resized.</typeparam>
        /// <returns> true if the resize operation was successful; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Resize<T>(Span<T> span, int newSize, out Span<T> newSpan) => allocator.Resize(span, newSize, Reflection.Type<T>.Info.Alignment, out newSpan);

        /// <summary>
        /// Remaps the specified <see cref="Span{T}"/> to a new size of <paramref name="newCount"/> elements, while preserving its contents.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> to be remapped.</param>
        /// <param name="newCount">The desired new size of the <see cref="Span{T}"/> in number of elements.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/> to be remapped.</typeparam>
        /// <returns> A <see cref="Span{T}"/> representing the remapped memory block if the operation was successful; otherwise, it returns an empty <see cref="Span{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Remap<T>(Span<T> span, nuint newCount, byte alignment)
        {
            var typeInfo = Reflection.Type<T>.Info;
            var oldSize = (nuint)span.Length * typeInfo.Size;
            var newSize = newCount * typeInfo.Size;
            var ptr = allocator.Remap(Native.AsPointer(span), oldSize, newSize, alignment);
            return new Span<T>(ptr, (int)newCount);
        }

        /// <summary>
        /// Remaps the specified <see cref="Span{T}"/> to a new size of <paramref name="newCount"/> elements, while preserving its contents.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> to be remapped.</param>
        /// <param name="newCount">The desired new size of the <see cref="Span{T}"/> in number of elements.</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/> to be remapped.</typeparam>
        /// <returns> A <see cref="Span{T}"/> representing the remapped memory block if the operation was successful; otherwise, it returns an empty <see cref="Span{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Remap<T>(Span<T> span, nuint newCount) => allocator.Remap(span, newCount, Reflection.Type<T>.Info.Alignment);
    }
}