using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Gubbins.Unsafe
{
    internal unsafe class UnityMemory : Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint GetFieldOffset(FieldInfo fieldInfo) => (uint) UnsafeUtility.GetFieldOffset(fieldInfo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint GetStackSize(Type type) => type.CheckType().IsValueType ? (uint) UnsafeUtility.SizeOf(type) : (uint) IntPtr.Size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CopyMemory(void* source, void* destination, uint offset) => UnsafeUtility.MemCpy(destination, source, offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetObjectAddress(object source, void* destination) => UnsafeUtility.CopyObjectAddressToPtr(source, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Free(IntPtr ptr) => UnsafeUtility.Free(ptr.ToPointer(), Allocator.Persistent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override IntPtr Allocate(int size, int align = 8) => (IntPtr) UnsafeUtility.Malloc(size, align, Allocator.Persistent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T ConvertToStructure<T>(void* ptr) => UnsafeUtility.ReadArrayElement<T>(ptr, 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ReplaceInstance() => Native.Operation = new UnityMemory();

        /// <remarks>
        /// Prevent preload phase call but <see cref="ReplaceInstance"/> not called.
        /// It will cause a null reference exception when preload phase call <see cref="Memory"/> for the first time.
        /// </remarks>
        static UnityMemory() => ReplaceInstance();
    }
}
