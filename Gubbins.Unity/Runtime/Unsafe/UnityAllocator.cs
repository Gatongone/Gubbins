using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace Gubbins.Unsafe
{
    /// <summary>
    /// An allocator that uses Unity's memory allocation system for unmanaged memory management.
    /// </summary>
    public unsafe struct UnityAllocator : IAllocatable
    {
        /// <summary>
        /// Allocator methods for vtable.
        /// </summary>
        [AutoStaticsCleanup]
        private static readonly Allocator.VTable s_Methods = new(&Reset, &Alloc, &Resize, &Free, &Remap);

        /// <summary>
        /// The Unity allocator to use for memory allocation and deallocation.
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Unity.Collections.Allocator m_Allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityAllocator"/> struct with the specified Unity allocator.
        /// </summary>
        /// <param name="allocator"></param>
        public UnityAllocator(Unity.Collections.Allocator allocator) => m_Allocator = allocator;

        /// <inheritdoc/>
        public Allocator GetAllocator() => new(System.Runtime.CompilerServices.Unsafe.AsPointer(ref this), s_Methods);

        /// <summary>
        /// Does nothing; the Unity allocator does not require any special reset logic.
        /// </summary>
        /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
        private static void Reset(void* ctx) { }

        /// <summary>
        /// Allocates a block of memory of the specified length and alignment.
        /// </summary>
        /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
        /// <param name="len">The length of the memory block to allocate in bytes.</param>
        /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
        /// <returns>A pointer to the allocated memory block.</returns>
        private static void* Alloc(void* ctx, nuint len, byte alignment)
        {
            var self = (UnityAllocator*)ctx;
            return UnsafeUtility.Malloc((long)len, alignment, self->m_Allocator);
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
        /// Frees a block of memory previously allocated with <see cref="Alloc"/>.
        /// </summary>
        /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
        /// <param name="ptr">The pointer to the memory block to be freed.</param>
        /// <param name="len">The size of the memory block to be freed in bytes.</param>
        /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
        private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
        {
            var self = (UnityAllocator*)ctx;
            UnsafeUtility.Free(ptr, self->m_Allocator);
        }

        /// <summary>
        /// Resizing blocks in place is not supported. This method always returns <see langword="null"/> to indicate that the memory block cannot be resized in place.
        /// </summary>
        /// <param name="ctx">The context pointer to the UnmanagedAllocator instance.</param>
        /// <param name="ptr"> The pointer to the memory block to be resized.</param>
        /// <param name="oldLen">The current size of the memory block in bytes.</param>
        /// <param name="newLen">The desired new size of the memory block in bytes.</param>
        /// <param name="alignment">The alignment requirement for the memory block (not used in this implementation, but required for the signature).</param>
        /// <returns>Always <see langword="null"/>; use <see cref="Resize"/> instead.</returns>
        private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment) => null;
    }
}