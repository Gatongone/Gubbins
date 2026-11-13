using System.Buffers;
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
    /// <remarks>
    /// The returned <see cref="Allocator"/> captures the address of the implementing instance as its
    /// <see cref="Allocator.Context"/>, so that instance must stay at a stable address for as long as the
    /// allocator is in use.
    /// </remarks>
    /// <returns>An <see cref="Allocator"/> instance that can be used to allocate memory for the implementing type.</returns>
    Allocator GetAllocator();
}

/// <summary>
/// Represents a memory allocator that provides methods for allocating, resizing, freeing, and remapping memory blocks.
/// </summary>
/// <param name="context">An opaque pointer passed as the first argument to every allocator method, used to hold the allocator's state.</param>
/// <param name="methods">The virtual table that implements the allocator operations.</param>
public readonly unsafe struct Allocator(void* context, Allocator.VTable methods) : IDisposable
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
        /// Freeing all allocated memory blocks. This method is used to reclaim memory in bulk and return the allocator to its initial state.
        /// </summary>
        public readonly delegate* managed<void*, void> Dispose;

        /// <summary>
        /// Resets the allocator's initial state, it may not free all allocated memory blocks.
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
        /// <param name="dispose">The function pointer that disposes of the allocator and frees all allocated memory blocks.</param>
        /// <param name="reset">The function pointer that resets the allocator.</param>
        /// <param name="alloc">The function pointer that allocates a memory block.</param>
        /// <param name="resize">The function pointer that resizes a memory block in place.</param>
        /// <param name="free">The function pointer that frees a memory block.</param>
        /// <param name="remap">The function pointer that remaps a memory block, potentially moving it.</param>
        public VTable(
            delegate*<void*, void> dispose,
            delegate*<void*, void> reset,
            delegate*<void*, nuint, byte, void*> alloc,
            delegate*<void*, void*, nuint, nuint, byte, bool> resize,
            delegate*<void*, void*, nuint, byte, void> free,
            delegate*<void*, void*, nuint, nuint, byte, void*> remap)
        {
            Dispose = dispose;
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
    /// Resets the allocator's initial state, it may not free all allocated memory blocks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        // A default-initialized allocator has no vtable; resetting it is a no-op.
        if (Methods.Reset != null)
        {
            Methods.Reset(Context);
        }
    }

    /// <summary>
    /// Freeing all allocated memory blocks. This method is used to reclaim memory in bulk and return the allocator to its initial state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // A default-initialized allocator has no vtable; disposing it is a no-op.
        if (Methods.Dispose != null)
        {
            Methods.Dispose(Context);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this instance wraps a usable virtual table.
    /// A default-initialized <see cref="Allocator"/> (as produced by <see langword="default"/> or an uninitialized struct field)
    /// has no vtable and must not be used to allocate or free memory.
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Methods.Alloc != null;
    }

    /// <summary>
    /// Casts the allocator to a specific type that implements <see cref="IAllocatable"/>. This allows for type-safe access to the allocator's methods and properties.
    /// </summary>
    /// <typeparam name="T">The type to which the allocator is being cast. This type must implement <see cref="IAllocatable"/>.</typeparam>
    /// <returns> A reference to the allocator cast to the specified type <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Cast<T>() where T : IAllocatable => ref Native.Cast<T>(Context);
}

/// <summary>
/// Owns an <see cref="IAllocatable"/> implementation in unmanaged storage and exposes a stable <see cref="Allocator"/>
/// whose <see cref="Allocator.Context"/> points at that storage.
/// </summary>
/// <remarks>
/// <para>
/// Use this when you want a single object to keep an allocator implementation and the <see cref="Allocator"/> returned by
/// <see cref="IAllocatable.GetAllocator"/> alive together. The implementation is copied into unmanaged memory, so it never
/// moves, and <see cref="Dispose"/> both disposes the allocator and releases the storage.
/// </para>
/// <para>
/// Only unmanaged allocator implementations can be stored this way (<see cref="ArenaAllocator"/>, <see cref="VirtualAllocator"/>,
/// <see cref="UnmanagedAllocator"/>, …). <see cref="FixedAllocator"/> is a <see langword="ref struct"/> over a caller-provided
/// buffer, and <see cref="ManagedAllocator"/> contains managed state, so those must keep their own lifetime discipline.
/// </para>
/// <para>
/// Allocators that chain to a base allocator (for example <see cref="ArenaAllocator"/>) still require the base allocator's
/// implementation to outlive the handle.
/// </para>
/// </remarks>
public sealed unsafe class AllocatorHandle : IDisposable
{
    /// <summary>
    /// The unmanaged storage holding the allocator implementation.
    /// </summary>
    private void* m_Storage;

    /// <summary>
    /// Gets the allocator backed by this handle. It is valid until <see cref="Dispose"/> is called.
    /// </summary>
    public Allocator Allocator { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this handle still owns an allocator.
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Storage != null;
    }

    /// <summary>
    /// Initializes a handle over unmanaged storage.
    /// </summary>
    /// <param name="storage">The unmanaged storage holding the implementation.</param>
    /// <param name="allocator">The allocator whose context points at <paramref name="storage"/>.</param>
    private AllocatorHandle(void* storage, Allocator allocator)
    {
        m_Storage = storage;
        Allocator = allocator;
    }

    /// <summary>
    /// Creates a handle that owns a copy of <paramref name="implementation"/> and the allocator it supplies.
    /// </summary>
    /// <typeparam name="T">The unmanaged allocator implementation type.</typeparam>
    /// <param name="implementation">The allocator implementation to copy into unmanaged storage.</param>
    /// <returns>A handle owning both the implementation and the allocator.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when the storage for the implementation cannot be allocated.</exception>
    public static AllocatorHandle Create<T>(in T implementation) where T : unmanaged, IAllocatable
    {
        var size = Reflection.Type<T>.Info.Size;

#if NET6_0_OR_GREATER
        var storage = NativeMemory.Alloc(size);
#else
        var storage = Marshal.AllocHGlobal((nint)size).ToPointer();
#endif

        if (storage == null)
        {
            throw new OutOfMemoryException($"Unable to allocate {size} bytes for allocator storage.");
        }

        *(T*)storage = implementation;
        var a = ((T*)storage)->GetAllocator();
        return new AllocatorHandle(storage, a);
    }

    /// <summary>
    /// Disposes the allocator (releasing all of its memory) and frees the storage that held the implementation.
    /// </summary>
    public void Dispose()
    {
        if (m_Storage == null)
        {
            return;
        }

        Allocator.Dispose();

#if NET6_0_OR_GREATER
        NativeMemory.Free(m_Storage);
#else
        Marshal.FreeHGlobal((IntPtr)m_Storage);
#endif
        Allocator = default;
        m_Storage = null;
    }

    /// <summary>
    /// Resets the allocator without releasing the storage that holds the implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Allocator.Reset();
}

/// <summary>
/// Represents a fixed-size memory allocator that serves aligned blocks from a pre-allocated buffer.
/// It is a bump allocator: only the most recently allocated block can be resized, freed or remapped,
/// and calling the reset method reclaims the whole buffer. It is intended for scenarios where memory
/// usage is predictable and bounded.
/// </summary>
/// <param name="buffer">Fixed buffer</param>
public unsafe ref struct FixedAllocator(Span<byte> buffer) : IAllocatable
{
    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable methods = new(&Dispose, &Reset, &Alloc, &Resize, &Free, &Remap);

    /// <summary>
    /// The fixed buffer from which memory is allocated. This buffer is provided by the user and must remain valid for the lifetime of the allocator.
    /// </summary>
    private readonly Span<byte> m_Buffer = buffer;

    /// <summary>
    /// The end index of the allocated memory within the fixed buffer. This index indicates the position in the buffer where the next allocation will occur.
    /// </summary>
    private int m_EndIndex;

    /// <summary>
    /// Gets the number of bytes that have been allocated from the fixed buffer.
    /// </summary>
    public int Length => m_EndIndex;

    /// <summary>
    /// Gets the total capacity of the fixed buffer in bytes. This property returns the total number of bytes available in the fixed buffer for allocation.
    /// </summary>
    public int Capacity => m_Buffer.Length;

    /// <summary>
    /// Gets a value indicating whether the fixed buffer is full. This property returns true if the allocated memory length is greater than or equal to the total capacity of the fixed buffer; otherwise, it returns false.
    /// </summary>
    public bool Full => m_EndIndex >= m_Buffer.Length;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), methods);
#else
        fixed (FixedAllocator* pThis = &this)
        {
            return new Allocator(pThis, methods);
        }
#endif
    }

    /// <summary>
    /// Fixed allocator can't actually dispose of the buffer, but only resets the end index to zero, effectively marking all previously allocated memory as free.
    /// </summary>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    private static void Dispose(void* ctx) => Reset(ctx);

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
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <returns> A pointer to the allocated memory block.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when the requested length (including alignment padding) exceeds the available space in the fixed buffer.</exception>
    private static void* Alloc(void* ctx, nuint len, byte alignment)
    {
        var self = (FixedAllocator*)ctx;

        // The buffer itself is not guaranteed to be aligned, so align the absolute address, not just the offset.
        // The padding between the previous block and this one becomes part of the used length.
        var bufferStart = (nuint)Native.AsPointer(self->m_Buffer);
        var alignedAddress = Native.AlignForward(bufferStart + (nuint)self->m_EndIndex, alignment);
        var start = alignedAddress - bufferStart;
        var length = (nuint)self->m_Buffer.Length;

        // Subtraction avoids overflowing when len is close to nuint.MaxValue.
        if (start > length || len > length - start)
        {
            throw new OutOfMemoryException("Out of memory.");
        }

        var ptr = Native.AsPointer(self->m_Buffer.Slice((int)start, (int)len));
        self->m_EndIndex = (int)(start + len);
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
    /// <see langword="true"/> if the block was resized; otherwise, <see langword="false"/>.
    /// Because the allocator is a bump allocator, only the most recently allocated block can be resized,
    /// and the new end must still fit inside the buffer.
    /// </returns>
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (FixedAllocator*)ctx;
        var offset = (nuint)ptr - (nuint)Native.AsPointer(self->m_Buffer);

        // Reject foreign pointers and blocks that are not the most recent allocation.
        if (offset > (nuint)self->m_EndIndex || offset + oldLen != (nuint)self->m_EndIndex)
        {
            return false;
        }

        var newEnd = offset + newLen;
        if (newEnd < offset || newEnd > (nuint)self->m_Buffer.Length)
        {
            return false;
        }

        // Both growing and shrinking update the end marker; shrinking returns the tail to the allocator.
        self->m_EndIndex = (int)newEnd;
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
        var offset = (nuint)ptr - (nuint)Native.AsPointer(self->m_Buffer);

        // Only the most recent allocation can be reclaimed. Foreign pointers must not move the marker.
        if (offset > (nuint)self->m_EndIndex || offset + len != (nuint)self->m_EndIndex)
        {
            return;
        }

        self->m_EndIndex = (int)offset;
    }

    /// <summary>
    /// Remaps the memory block pointed to by <paramref name="ptr"/> to a new size of <paramref name="newLen"/> bytes,
    /// while preserving the contents of the original block.
    /// </summary>
    /// <remarks>
    /// The block can never move, so this either grows the most recent allocation in place (returning the original
    /// pointer) or fails. Shrinking always succeeds and returns the original pointer.
    /// </remarks>
    /// <param name="ctx">The context pointer to the FixedAllocator instance.</param>
    /// <param name="ptr">The pointer to the original memory block.</param>
    /// <param name="oldLen">The size of the original memory block in bytes.</param>
    /// <param name="newLen"> The desired new size of the memory block in bytes.</param>
    /// <param name="alignment"> The alignment requirement for the new memory block (not used in this implementation, but required for the signature).</param>
    /// <returns>The original pointer on success; otherwise, <see langword="null"/> if the block cannot be grown in place.</returns>
    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        // A shrinking remap preserves the address without moving the end marker, which is safe.
        if (newLen <= oldLen)
        {
            return ptr;
        }

        return Resize(ctx, ptr, oldLen, newLen, alignment) ? ptr : null;
    }
}

/// <summary>
/// Represents an arena (bump) allocator that acquires geometrically growing blocks from a backing allocator.
/// </summary>
/// <param name="baseAllocator">The allocator used to acquire and release the underlying blocks.</param>
public unsafe struct ArenaAllocator(Allocator baseAllocator) : IAllocatable
{
    /// <summary>
    /// The largest length a managed array (and therefore a block payload) can have.
    /// </summary>
    private const int MAX_ARRAY_LENGTH = 0x7FFFFFC7;

    /// <summary>
    /// The minimum length of a block payload. This is the smallest size that can be requested from the backing allocator.
    /// </summary>
    private readonly Allocator m_BaseAllocator = baseAllocator;

    /// <summary>
    /// The absolute address of the first unused byte of the active (head) block.
    /// </summary>
    private nuint m_End;

    /// <summary>
    /// The most recently acquired block. Older blocks are chained through <see cref="Payload.Next"/>.
    /// </summary>
    private Payload* m_Head;

    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable methods = new(&Dispose, &Reset, &Alloc, &Resize, &Free, &Remap);

    /// <inheritdoc/>
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), methods);
#else
        fixed (ArenaAllocator* pThis = &this)
        {
            return new Allocator(pThis, methods);
        }
#endif
    }

    private static void Dispose(void* ctx)
    {
        var self = (ArenaAllocator*)ctx;

        if (self->m_Head == null)
        {
            self->m_End = 0;
            return;
        }

        var block = self->m_Head;

        while (block != null)
        {
            var next = block->Next;
            self->m_BaseAllocator.Free(block, (nuint)sizeof(Payload) + (nuint)block->Length, block->Alignment);
            block = next;
        }

        self->m_Head = null;
        self->m_End = 0;
    }


    private static void Reset(void* ctx)
    {
        var self = (ArenaAllocator*)ctx;


        if (self->m_Head == null)
        {
            self->m_End = 0;
            return;
        }

        // When keeping the active block, only the retired blocks that follow it need to be released.
        var block = self->m_Head->Next;

        while (block != null)
        {
            var next = block->Next;
            self->m_BaseAllocator.Free(block, (nuint)sizeof(Payload) + (nuint)block->Length, block->Alignment);
            block = next;
        }

        self->m_Head->Next = null;
        self->m_End = (nuint)self->m_Head->Buffer;
    }

    private static void* Alloc(void* ctx, nuint len, byte alignment)
    {
        var self = (ArenaAllocator*)ctx;

        while (true)
        {
            var block = self->m_Head;

            // Fast path: serve the request from the active block if it fits after alignment padding.
            if (block != null)
            {
                var bufferStart = (nuint)block->Buffer;
                var bufferEnd = bufferStart + (nuint)block->Length;
                var alignedEnd = Native.AlignForward(self->m_End, alignment);
                var newEnd = alignedEnd + len;

                if (newEnd >= alignedEnd && newEnd <= bufferEnd)
                {
                    self->m_End = newEnd;
                    return (void*)alignedEnd;
                }

                // Ask the backing allocator whether the active block can be extended in place.
                var newLength = newEnd - bufferStart;
                if (newLength <= MAX_ARRAY_LENGTH)
                {
                    var oldSize = (nuint)sizeof(Payload) + (nuint)block->Length;
                    var newSize = (nuint)sizeof(Payload) + newLength;
                    if (self->m_BaseAllocator.Resize(block, oldSize, newSize, alignment))
                    {
                        block->Length = (int)newLength;
                        self->m_End = newEnd;
                        return (void*)alignedEnd;
                    }
                }
            }

            // Slow path: acquire a new block large enough for this request and retry.
            // Include a worst-case alignment padding so the retry cannot fail.
            var required = len + alignment;
            if (required < len || required > MAX_ARRAY_LENGTH)
            {
                throw new OutOfMemoryException($"The arena cannot satisfy an allocation of {len} bytes.");
            }

            var capacity = GetNewCapacity(block, required);
            var newBlock = (Payload*)self->m_BaseAllocator.Alloc((nuint)sizeof(Payload) + (nuint)capacity, alignment);
            newBlock->Next = block;
            newBlock->Buffer = (byte*)(newBlock + 1);
            newBlock->Length = capacity;
            newBlock->Alignment = alignment;
            self->m_Head = newBlock;
            self->m_End = (nuint)newBlock->Buffer;
        }
    }

    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (ArenaAllocator*)ctx;
        var block = self->m_Head;
        if (block == null)
        {
            return false;
        }

        var bufferStart = (nuint)block->Buffer;
        var ptrAddress = (nuint)ptr;

        // Only the most recent allocation in the active block can be resized in place.
        if (ptrAddress < bufferStart || ptrAddress + oldLen != self->m_End)
        {
            return false;
        }

        var newEnd = ptrAddress + newLen;
        if (newEnd < ptrAddress || newEnd > bufferStart + (nuint)block->Length)
        {
            return false;
        }

        self->m_End = newEnd;
        return true;
    }

    private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
    {
        var self = (ArenaAllocator*)ctx;
        var block = self->m_Head;
        if (block == null)
        {
            return;
        }

        var ptrAddress = (nuint)ptr;

        // Only the most recent allocation can be reclaimed; earlier blocks are freed by Reset.
        if (ptrAddress < (nuint)block->Buffer || ptrAddress + len != self->m_End)
        {
            return;
        }

        self->m_End = ptrAddress;
    }

    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        if (newLen <= oldLen)
        {
            return ptr;
        }

        return Resize(ctx, ptr, oldLen, newLen, alignment) ? ptr : null;
    }

    /// <summary>
    /// Computes the capacity of the next block. Blocks start at one page and double from there.
    /// </summary>
    /// <param name="current">The active block, or <see langword="null"/> when the arena is empty.</param>
    /// <param name="required">The minimum capacity that must be returned.</param>
    /// <returns>The new payload capacity in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNewCapacity(Payload* current, nuint required)
    {
        var capacity = current == null ? 4096UL : (ulong)current->Length << 1;
        if (capacity < required)
        {
            capacity = required;
        }

        return capacity > MAX_ARRAY_LENGTH ? MAX_ARRAY_LENGTH : (int)capacity;
    }

    /// <summary>
    /// Header of a single arena block. The payload bytes immediately follow the header, and the header is
    /// described with plain unmanaged fields so that it can be stored directly in unmanaged memory.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Payload
    {
        /// <summary>
        /// The previously acquired block, or <see langword="null"/> for the oldest block.
        /// </summary>
        public Payload* Next;

        /// <summary>
        /// The first payload byte, immediately after this header.
        /// </summary>
        public byte* Buffer;

        /// <summary>
        /// The number of payload bytes available at <see cref="Buffer"/>.
        /// </summary>
        public int Length;

        /// <summary>
        /// The alignment that was requested when the block was allocated, reused when releasing the block.
        /// </summary>
        public byte Alignment;
    }
}

/// <summary>
/// Represents a bump allocator that reserves a fixed virtual address range up front and commits its pages on demand.
/// </summary>
/// <remarks>
/// <para>
/// The whole range is reserved on first use (<c>VirtualAlloc(MEM_RESERVE)</c> on Windows, <c>mmap(PROT_NONE)</c> on
/// Linux/macOS), so pointers returned by this allocator stay stable for the lifetime of the reservation; only the
/// physical backing (the commit) grows. This keeps large, sparsely-used arenas cheap in terms of committed memory.
/// </para>
/// <para>
/// Only the most recent allocation can be freed or resized, because the region is handed out linearly.
/// This allocator is not thread-safe.
/// </para>
/// </remarks>
/// <param name="maxReservedBytes">The size, in bytes, of the virtual address range to reserve.</param>
public unsafe struct VirtualAllocator(uint maxReservedBytes) : IAllocatable
{
    private readonly nuint m_MaxReservedBytes = maxReservedBytes;

    /// <summary>
    /// The base address of the reserved virtual range, or <see langword="null"/> until the first allocation.
    /// </summary>
    private void* m_Reserved;

    /// <summary>
    /// The number of committed bytes, rounded up to a whole number of pages.
    /// </summary>
    private nuint m_CommittedBytes;

    /// <summary>
    /// The first unused byte of the reserved range, relative to <see cref="m_Reserved"/>.
    /// </summary>
    private nuint m_End;

    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable methods = new(&Dispose, &Reset, &Alloc, &Resize, &Free, &Remap);

    /// <summary>
    /// Whether the current platform exposes the Win32 virtual memory API.
    /// </summary>
    private static readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Whether the current platform exposes the POSIX <c>mmap</c> family of calls.
    /// </summary>
    private static readonly bool isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Whether the current Unix is macOS, which uses different <c>MAP_ANONYMOUS</c> and <c>MADV_*</c> values.
    /// </summary>
    private static readonly bool isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// The platform-specific <c>MAP_ANONYMOUS</c> flag value.
    /// </summary>
    private static readonly int mapAnonymous = isMacOs ? Unix.MAP_ANONYMOUS_MACOS : Unix.MAP_ANONYMOUS_LINUX;

    /// <summary>
    /// The size of a memory page on the current platform, in bytes. This value is used to round up committed memory to whole pages.
    /// </summary>
    private static readonly nuint pageSize = (nuint)Environment.SystemPageSize;

    /// <inheritdoc/>
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), methods);
#else
        fixed (VirtualAllocator* pThis = &this)
        {
            return new Allocator(pThis, methods);
        }
#endif
    }

    /// <summary>
    /// Releases the whole reserved range back to the operating system and returns the allocator to its initial state.
    /// </summary>
    /// <param name="ctx">The context pointer to the VirtualAllocator instance.</param>
    private static void Dispose(void* ctx)
    {
        var self = (VirtualAllocator*)ctx;
        if (self->m_Reserved != null)
        {
            Release(self->m_Reserved, self->m_MaxReservedBytes);
            self->m_Reserved = null;
        }

        self->m_End = 0;
        self->m_CommittedBytes = 0;
    }

    /// <summary>
    /// Decommits every committed page while keeping the reserved address range for reuse.
    /// </summary>
    /// <param name="ctx">The context pointer to the VirtualAllocator instance.</param>
    private static void Reset(void* ctx)
    {
        var self = (VirtualAllocator*)ctx;
        if (self->m_Reserved != null)
        {
            Rewind(self, 0);
            return;
        }

        self->m_End = 0;
        self->m_CommittedBytes = 0;
    }

    /// <summary>
    /// Reserves the virtual range on first use and bumps the allocation cursor forward, committing any new pages.
    /// </summary>
    /// <param name="ctx">The context pointer to the VirtualAllocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the platform is neither Windows nor a supported Unix (Linux/macOS).</exception>
    /// <exception cref="OutOfMemoryException">Thrown when the reservation or the commit fails, or when the allocation does not fit.</exception>
    private static void* Alloc(void* ctx, nuint len, byte alignment)
    {
        var self = (VirtualAllocator*)ctx;

        if (self->m_Reserved == null)
        {
            self->m_Reserved = Reserve(self->m_MaxReservedBytes);
            if (self->m_Reserved == null)
            {
                throw new OutOfMemoryException(
                    $"Unable to reserve {self->m_MaxReservedBytes} bytes of virtual address space (OS error {Marshal.GetLastWin32Error()}).");
            }
        }

        // Align against the absolute address so the result is correct regardless of the reservation's base alignment.
        var aligned = AlignOffset(self->m_Reserved, self->m_End, alignment);
        var newEnd = aligned + len;
        if (newEnd < aligned || newEnd > self->m_MaxReservedBytes)
        {
            throw new OutOfMemoryException($"Exceeded the reserved virtual address space of {self->m_MaxReservedBytes} bytes.");
        }

        if (!EnsureCommitted(self, newEnd))
        {
            throw new OutOfMemoryException($"Unable to commit virtual memory (OS error {Marshal.GetLastWin32Error()}).");
        }

        self->m_End = newEnd;
        return (byte*)self->m_Reserved + aligned;
    }

    /// <summary>
    /// Resizes the most recent allocation in place, committing additional pages when it grows.
    /// </summary>
    /// <param name="ctx">The context pointer to the VirtualAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be resized.</param>
    /// <param name="oldLen">The current size of the memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used, the block cannot move).</param>
    /// <returns><see langword="true"/> if the block was resized in place; otherwise, <see langword="false"/>.</returns>
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (VirtualAllocator*)ctx;
        if (self->m_Reserved == null)
        {
            return false;
        }

        var offset = (nuint)ptr - (nuint)self->m_Reserved;

        // Reject foreign pointers and blocks that are not the most recent allocation.
        if (offset > self->m_End || offset + oldLen != self->m_End)
        {
            return false;
        }

        var newEnd = offset + newLen;
        if (newEnd < offset || newEnd > self->m_MaxReservedBytes)
        {
            return false;
        }

        if (!EnsureCommitted(self, newEnd))
        {
            return false;
        }

        Rewind(self, newEnd);
        return true;
    }

    /// <summary>
    /// Rewinds the allocation cursor when <paramref name="ptr"/> is the most recent allocation and decommits the
    /// trailing pages that are no longer used. Freeing an older block is a no-op.
    /// </summary>
    /// <param name="ctx">The context pointer to the VirtualAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be freed.</param>
    /// <param name="len">The size of the memory block to be freed in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used).</param>
    private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
    {
        var self = (VirtualAllocator*)ctx;
        if (self->m_Reserved == null)
        {
            return;
        }

        var offset = (nuint)ptr - (nuint)self->m_Reserved;
        if (offset > self->m_End || offset + len != self->m_End)
        {
            return;
        }

        Rewind(self, offset);
    }

    /// <summary>
    /// Resizes the block in place, since the virtual reservation never moves its contents.
    /// </summary>
    /// <param name="ctx">The context pointer to the VirtualAllocator instance.</param>
    /// <param name="ptr">The pointer to the original memory block.</param>
    /// <param name="oldLen">The size of the original memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the new memory block (not used, the block cannot move).</param>
    /// <returns>The original pointer on success; otherwise, <see langword="null"/>.</returns>
    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        // Shrinking keeps the address unconditionally; growing requires the block to be the most recent one.
        if (newLen <= oldLen)
        {
            return ptr;
        }

        return Resize(ctx, ptr, oldLen, newLen, alignment) ? ptr : null;
    }

    /// <summary>
    /// Moves the allocation cursor back to <paramref name="newEnd"/> and decommits every fully-unused page beyond it.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="newEnd">The new end of the used range, relative to the reservation base.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Rewind(VirtualAllocator* self, nuint newEnd)
    {
        self->m_End = newEnd;

        var committed = AlignUp(newEnd, pageSize);
        if (committed >= self->m_CommittedBytes)
        {
            return;
        }

        // Only whole pages that lie completely beyond the new end can be returned to the OS.
        if (Decommit((byte*)self->m_Reserved + committed, self->m_CommittedBytes - committed))
        {
            self->m_CommittedBytes = committed;
        }
    }

    /// <summary>
    /// Ensures that every byte below <paramref name="requiredEnd"/> is backed by committed physical storage.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="requiredEnd">The end of the range that must be committed, relative to the reservation base.</param>
    /// <returns><see langword="true"/> if the range is committed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EnsureCommitted(VirtualAllocator* self, nuint requiredEnd)
    {
        if (requiredEnd <= self->m_CommittedBytes)
        {
            return true;
        }

        var committed = AlignUp(requiredEnd, pageSize);
        if (committed < requiredEnd)
        {
            return false;
        }

        if (!Commit((byte*)self->m_Reserved + self->m_CommittedBytes, committed - self->m_CommittedBytes))
        {
            return false;
        }

        self->m_CommittedBytes = committed;
        return true;
    }

    /// <summary>
    /// Rounds <paramref name="value"/> up to the next multiple of <paramref name="alignment"/>.
    /// </summary>
    /// <param name="value">The value to round up.</param>
    /// <param name="alignment">A non-zero power of two, normally the OS page size.</param>
    /// <returns>The rounded value, or <see langword="0"/> if the rounding would overflow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint AlignUp(nuint value, nuint alignment)
    {
        var mask = alignment - 1;

        // `~mask` is the largest value that can be rounded up without overflowing.
        return value > ~mask ? 0 : (value + mask) & ~mask;
    }

    /// <summary>
    /// Returns the offset of the first aligned address at or after <paramref name="offset"/>, accounting for the
    /// alignment of the absolute base address.
    /// </summary>
    /// <param name="baseAddress">The base address of the reserved range.</param>
    /// <param name="offset">The unaligned offset into the reserved range.</param>
    /// <param name="alignment">The alignment in bytes (must be a non-zero power of 2).</param>
    /// <returns>The aligned offset relative to <paramref name="baseAddress"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint AlignOffset(void* baseAddress, nuint offset, byte alignment)
    {
        var baseValue = (nuint)baseAddress;
        return Native.AlignForward(baseValue + offset, alignment) - baseValue;
    }

    /// <summary>
    /// Reserves a virtual address range without committing physical storage.
    /// </summary>
    /// <param name="size">The number of bytes to reserve.</param>
    /// <returns>The base address of the reservation, or <see langword="null"/> on failure.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the platform is neither Windows nor a supported Unix.</exception>
    private static void* Reserve(nuint size)
    {
        if (isWindows)
        {
            return VirtualAlloc(null, size, Win32.MEM_RESERVE, Win32.PAGE_NOACCESS);
        }

        if (isUnix)
        {
            var result = mmap(null, size, Unix.PROT_NONE, Unix.MAP_PRIVATE | mapAnonymous, -1, 0);

            // mmap reports failure with MAP_FAILED (-1), not null.
            return (nint)result == -1 ? null : result;
        }

        throw new PlatformNotSupportedException("VirtualAllocator requires Windows VirtualAlloc or a Unix mmap implementation.");
    }

    /// <summary>
    /// Commits physical storage for a previously reserved range.
    /// </summary>
    /// <param name="address">The page-aligned start of the range.</param>
    /// <param name="size">The number of bytes to commit (a multiple of the page size).</param>
    /// <returns><see langword="true"/> on success; otherwise, <see langword="false"/>.</returns>
    private static bool Commit(void* address, nuint size)
    {
        if (isWindows)
        {
            return VirtualAlloc(address, size, Win32.MEM_COMMIT, Win32.PAGE_READWRITE) != null;
        }

        return mprotect(address, size, Unix.PROT_READ | Unix.PROT_WRITE) == 0;
    }

    /// <summary>
    /// Releases the physical storage of a committed range while keeping the reservation intact.
    /// </summary>
    /// <param name="address">The page-aligned start of the range.</param>
    /// <param name="size">The number of bytes to decommit (a multiple of the page size).</param>
    /// <returns><see langword="true"/> on success; otherwise, <see langword="false"/>.</returns>
    private static bool Decommit(void* address, nuint size)
    {
        if (isWindows)
        {
            return VirtualFree(address, size, Win32.MEM_DECOMMIT);
        }

        // mprotect(PROT_NONE) alone keeps the physical pages around, so let the OS reclaim them first.
        // MADV_DONTNEED on Linux and MADV_FREE on macOS both preserve the reservation and zero-fill on reuse.
        madvise(address, size, isMacOs ? Unix.MADV_FREE : Unix.MADV_DONTNEED);
        return mprotect(address, size, Unix.PROT_NONE) == 0;
    }

    /// <summary>
    /// Releases the whole reservation back to the operating system.
    /// </summary>
    /// <param name="address">The base address of the reservation.</param>
    /// <param name="size">The size that was passed to <see cref="Reserve"/>.</param>
    /// <returns><see langword="true"/> on success; otherwise, <see langword="false"/>.</returns>
    private static bool Release(void* address, nuint size)
    {
        if (isWindows)
        {
            // MEM_RELEASE requires the address to be the reservation base and the size to be zero.
            return VirtualFree(address, 0, Win32.MEM_RELEASE);
        }

        return munmap(address, size) == 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void* VirtualAlloc(void* lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFree(void* lpAddress, nuint dwSize, uint dwFreeType);

    // The last argument is `off_t`, which is not machine-sized on every Unix; a 64-bit value is safe even where
    // off_t is 32-bit because extra stack arguments are ignored by the cdecl calling convention.
    [DllImport("libc", SetLastError = true)]
    private static extern void* mmap(void* addr, nuint length, int prot, int flags, int fd, long offset);

    [DllImport("libc", SetLastError = true)]
    private static extern int mprotect(void* addr, nuint len, int prot);

    [DllImport("libc", SetLastError = true)]
    private static extern int madvise(void* addr, nuint length, int advice);

    [DllImport("libc", SetLastError = true)]
    private static extern int munmap(void* addr, nuint length);

    private static class Win32
    {
        internal const uint MEM_RESERVE = 0x00002000;
        internal const uint MEM_COMMIT = 0x00001000;
        internal const uint MEM_DECOMMIT = 0x00004000;
        internal const uint MEM_RELEASE = 0x00008000;
        internal const uint PAGE_NOACCESS = 0x01;
        internal const uint PAGE_READWRITE = 0x04;
    }

    private static class Unix
    {
        internal const int PROT_NONE = 0x0;
        internal const int PROT_READ = 0x1;
        internal const int PROT_WRITE = 0x2;
        internal const int MAP_PRIVATE = 0x02;
        internal const int MAP_ANONYMOUS_LINUX = 0x20;
        internal const int MAP_ANONYMOUS_MACOS = 0x1000;
        internal const int MADV_DONTNEED = 4;
        internal const int MADV_FREE = 5;
    }
}

/// <summary>
/// Represents an allocator that serves allocations from pinned managed byte arrays.
/// Memory is therefore managed by the garbage collector rather than the native heap, at the cost of
/// pinning the backing arrays for as long as their blocks are alive.
/// </summary>
public unsafe struct ManagedAllocator : IAllocatable
{
    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable methods = new(&Dispose, &Reset, &Alloc, &Resize, &Free, &Remap);

    /// <summary>
    /// The global instance of the <see cref="ManagedAllocator"/> that can be used to allocate memory from pinned managed byte arrays.
    /// </summary>
    /// <remarks>
    /// This is a static field rather than a native allocation so that its <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// field is initialized by the runtime and keeps a normal GC reference. It must be mutable (not <see langword="readonly"/>)
    /// so that <see cref="GetAllocator"/> can capture its address without the compiler making a defensive copy.
    /// </remarks>
    private static readonly ManagedAllocator global;

    /// <summary>
    /// The global <see cref="Allocator"/> instance that can be used to allocate memory from pinned managed byte arrays.
    /// </summary>
    public static readonly Allocator Global = global.GetAllocator();

    /// <summary>
    /// Maps the address of each live allocation to the <see cref="GCHandle"/> that keeps its backing array pinned,
    /// so that the array can be released later.
    /// </summary>
    /// <remarks>
    /// This is a static reference field (rather than an instance field) so the <see cref="ManagedAllocator"/> struct
    /// stays unmanaged: <see cref="Allocator.Context"/> points at the allocator instance and must not move when the
    /// GC relocates static storage containing references. All allocator instances share the same address map.
    /// </remarks>
    private static readonly ConcurrentDictionary<IntPtr, GCHandle> pinnedMaps = new();

    /// <inheritdoc/>
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), methods);
#else
        fixed (ManagedAllocator* pThis = &this)
        {
            return new Allocator(pThis, methods);
        }
#endif
    }

    /// <summary>
    /// Releases every pinned array and clears the address map, returning the allocator to its initial state.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    private static void Dispose(void* ctx) => Reset(ctx);

    /// <summary>
    /// Releases every pinned array and clears the address map, returning the allocator to its initial state.
    /// </summary>
    /// <param name="ctx">The context pointer to the ManagedAllocator instance.</param>
    private static void Reset(void* ctx)
    {
        foreach (var kv in pinnedMaps)
        {
            kv.Value.Free();
        }

        pinnedMaps.Clear();
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
        // A managed array length is a 32-bit value, so larger requests cannot be represented.
        if (len > int.MaxValue)
        {
            throw new OutOfMemoryException($"A managed array cannot hold {len} bytes.");
        }

        var handle = GCHandle.Alloc(ArrayPool<byte>.Shared.Rent((int)len), GCHandleType.Pinned);
        var ptr = handle.AddrOfPinnedObject();
        pinnedMaps[ptr] = handle;
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
        if (!pinnedMaps.TryRemove(new IntPtr(ptr), out var handle))
        {
            throw new InvalidOperationException("Not allocated address");
        }

        var array = handle.Target as byte[];

        // Remove the entry and unpin before handing the array back to the pool, so a new renter can never
        // observe an array that is still pinned by this allocator.
        handle.Free();

        if (array != null)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
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
        // Validate the new length before removing the old mapping so that a failed remap leaves the
        // allocation registered and reachable.
        if (newLen > int.MaxValue)
        {
            throw new OutOfMemoryException($"A managed array cannot hold {newLen} bytes.");
        }

        var safePtr = new IntPtr(ptr);
        if (!pinnedMaps.TryRemove(safePtr, out var handle))
        {
            throw new InvalidOperationException("Not allocated address");
        }

        var oldArray = (byte[])handle.Target!;

        // Unpin before touching the contents so the GC is free to move the old array.
        handle.Free();

        // Rent the replacement and copy before returning the old array to the pool; returning it first
        // would let another renter overwrite it while the copy is still reading from it.
        var newArray = ArrayPool<byte>.Shared.Rent((int)newLen);
        Array.Copy(oldArray, newArray, (int)Math.Min(oldLen, newLen));
        ArrayPool<byte>.Shared.Return(oldArray);

        handle = GCHandle.Alloc(newArray, GCHandleType.Pinned);
        safePtr = handle.AddrOfPinnedObject();
        pinnedMaps[safePtr] = handle;
        return safePtr.ToPointer();
    }
}

/// <summary>
/// Represents an allocator that serves allocations from the native heap that is not managed by the garbage collector.
/// </summary>
public unsafe struct UnmanagedAllocator : IAllocatable
{
    /// <summary>
    /// Allocator methods for vtable.
    /// </summary>
    private static readonly Allocator.VTable methods = new(&Dispose, &Reset, &Alloc, &Resize, &Free, &Remap);

    /// <summary>
    /// The global instance of the <see cref="UnmanagedAllocator"/> that can be used to allocate memory from the native heap.
    /// </summary>
    /// <remarks>
    /// The allocator has no mutable state, but the instance still has to exist so that <see cref="GetAllocator"/>
    /// can capture a stable context address. It is intentionally not <see langword="readonly"/> to avoid a defensive copy.
    /// </remarks>
    private static UnmanagedAllocator s_Global;

    /// <summary>
    /// The global <see cref="Allocator"/> instance that can be used to allocate memory from the native heap.
    /// </summary>
    public static readonly Allocator Global = s_Global.GetAllocator();

    /// <inheritdoc/>
    public Allocator GetAllocator()
    {
#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), methods);
#else
        fixed (UnmanagedAllocator* pThis = &this)
        {
            return new Allocator(pThis, methods);
        }
#endif
    }

    /// <summary>
    /// Does nothing: the allocator has no state to dispose, and each block is freed independently.
    /// </summary>
    /// <param name="ctx"></param>
    private static void Dispose(void* ctx) { }

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
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment) => false;

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
        return Marshal.ReAllocHGlobal((nint)ptr, (nint)newLen).ToPointer();
#endif
    }
}

/// <summary>
/// Provides extension members for <see cref="Allocator"/> and <see cref="IAllocatable"/> implementations that
/// allocate, initialize, free, resize and remap typed memory blocks.
/// </summary>
public static unsafe class AllocatorExtensions
{
    /// <param name="allocator">The <see cref="Allocator"/> instance to extend.</param>
    extension(ref Allocator allocator)
    {
        /// <summary>
        /// Allocates a block of memory for a specified number of elements of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="count">The number of elements of type <typeparamref name="T"/> to allocate.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of elements to allocate memory for.</typeparam>
        /// <returns> A <see cref="Span{T}"/> representing the allocated memory block.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> exceeds the maximum span length.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Alloc<T>(nuint count, byte alignment)
        {
            // Span lengths are 32-bit, so anything larger cannot be represented even if the allocator could serve it.
            if (count > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

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
                // A `ref T` for a reference type refers to a reference-sized slot, not to the object itself,
                // so reserve one extra slot in front of the object to hold the object reference.
                var slot = (nint*)allocator.Alloc((nuint)IntPtr.Size + typeInfo.FixedSize, alignment);
                var obj = (byte*)slot + IntPtr.Size;
                *(nint*)obj = 0; // sync block index
                *((nint*)obj + 1) = typeof(T).TypeHandle.Value; // method table pointer
                *slot = (nint)obj; // the managed reference
                return ref Native.Cast<T>(slot);
            }

            var ptr = allocator.Alloc(typeInfo.Size, alignment);
            return ref Native.Cast<T>(ptr);
        }

        /// <summary>
        /// Allocates a new instance of type <typeparamref name="T"/> with the default alignment for that type, and returns a reference to it.
        /// </summary>
        /// <typeparam name="T">The type of the object to allocate.</typeparam>
        /// <returns> A reference to the newly allocated instance of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T New<T>() => ref allocator.New<T>(Reflection.Type<T>.Info.Alignment);

        /// <summary>
        /// Reallocates the memory block at <paramref name="src"/> to a new size of <paramref name="len"/> bytes, preserving its contents.
        /// </summary>
        /// <param name="src">The pointer to the original memory block.</param>
        /// <param name="len">The desired new size of the memory block in bytes.</param>
        /// <param name="alignment"> The alignment requirement for the new memory block (in bytes).</param>
        /// <returns>A pointer to the reallocated memory block.</returns>
        public void* Realloc(void* src, nuint len, byte alignment)
        {
            if (allocator.Resize(src, len, len, alignment))
            {
                return src;
            }

            var newPtr = allocator.Remap(src, len, len, alignment);
            if (newPtr != null)
            {
                return newPtr;
            }

            newPtr = allocator.Alloc(len, alignment);
            Native.CopyMemory(newPtr, src, (uint)len);
            allocator.Free(src, len, alignment);
            return newPtr;
        }

        /// <summary>
        /// Reallocates the memory block at <paramref name="src"/> to a new size of <paramref name="len"/> bytes, preserving its contents, with the default alignment for that type.
        /// </summary>
        /// <param name="src">The pointer to the original memory block.</param>
        /// <param name="len">The desired new size of the memory block in bytes.</param>
        /// <param name="alignment"> The alignment requirement for the new memory block (in bytes).</param>
        /// <typeparam name="T">The type of the elements in the memory block.</typeparam>
        /// <returns>A pointer to the reallocated memory block.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Realloc<T>(ref T src, nuint len, byte alignment)
        {
            var ptr = Native.AsPointer(ref src);
            var newPtr = allocator.Realloc(ptr, len, alignment);
            return ref Native.Cast<T>(newPtr);
        }

        /// <summary>
        /// Reallocates the memory block at <paramref name="src"/> to a new size of <paramref name="len"/> bytes, preserving its contents, with the default alignment for that type.
        /// </summary>
        /// <param name="src">The pointer to the original memory block.</param>
        /// <param name="len">The desired new size of the memory block in bytes.</param>
        /// <typeparam name="T">The type of the elements in the memory block.</typeparam>
        /// <returns>A pointer to the reallocated memory block.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Realloc<T>(ref T src, nuint len)
        {
            var ptr = Native.AsPointer(ref src);
            var newPtr = allocator.Realloc(ptr, len, Reflection.Type<T>.Info.Alignment);
            return ref Native.Cast<T>(newPtr);
        }

        /// <summary>
        /// Reallocates the memory block represented by the specified <see cref="Span{T}"/> to a new size, preserving its contents.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> representing the original memory block.</param>
        /// <param name="alignment"> The alignment requirement for the new memory block (in bytes).</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/>.</typeparam>
        /// <returns>A new <see cref="Span{T}"/> representing the reallocated memory block.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Realloc<T>(Span<T> span, byte alignment)
        {
            var ptr = Native.AsPointer(span);
            var newPtr = allocator.Realloc(ptr, (nuint)span.Length * Reflection.Type<T>.Info.Size, alignment);
            return new Span<T>(newPtr, span.Length);
        }

        /// <summary>
        /// Reallocates the memory block represented by the specified <see cref="Span{T}"/> to a new size, preserving its contents.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> representing the original memory block.</param>
        /// <typeparam name="T">The type of the elements in the <see cref="Span{T}"/>.</typeparam>
        /// <returns>A new <see cref="Span{T}"/> representing the reallocated memory block.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Realloc<T>(Span<T> span) => allocator.Realloc(span, Reflection.Type<T>.Info.Alignment);

        /// <summary>
        /// Frees the memory block associated with the specified reference of type <typeparamref name="T"/> with the specified alignment.
        /// </summary>
        /// <param name="src">The reference returned by <see cref="New{T}(byte)"/> whose memory block is to be freed.</param>
        /// <param name="alignment"> The alignment requirement for the memory block (in bytes).</param>
        /// <typeparam name="T">The type of the object whose memory block is to be freed.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free<T>(ref T src, byte alignment)
        {
            var typeInfo = Reflection.Type<T>.Info;

            // Classes are stored behind a reference slot; see New<T>.
            var size = typeInfo.IsClass ? (nuint)IntPtr.Size + typeInfo.FixedSize : typeInfo.FixedSize;
            allocator.Free(Native.AsPointer(ref src), size, alignment);
        }

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
            allocator.Free(Native.AsPointer(span), (nuint)span.Length * typeInfo.Size, alignment);
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
            if (newSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newSize));
            }

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
            if (newCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(newCount));
            }

            var typeInfo = Reflection.Type<T>.Info;
            var oldSize = (nuint)span.Length * typeInfo.Size;
            var newSize = newCount * typeInfo.Size;
            var ptr = allocator.Remap(Native.AsPointer(span), oldSize, newSize, alignment);

            // The allocator returns null when the block cannot be remapped in place.
            return ptr == null ? Span<T>.Empty : new Span<T>(ptr, (int)newCount);
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