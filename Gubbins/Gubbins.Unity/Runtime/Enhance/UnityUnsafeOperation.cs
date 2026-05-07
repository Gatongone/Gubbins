// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/23-20:29:33
// Github: https://github.com/Gatongone

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Gubbins.Unity
{
    internal unsafe class UnityUnsafeOperation : UnsafeOperation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint GetFieldOffset(FieldInfo fieldInfo) => (uint) UnsafeUtility.GetFieldOffset(fieldInfo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint GetStackSize(Type type) => type.CheckType().IsValueType ? (uint) UnsafeUtility.SizeOf(type) : (uint) IntPtr.Size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CopyMemory(void* source, void* destination, uint offset) => UnsafeUtility.MemCpy(destination, source, offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetObjectAddress(object source, void* destination) => UnsafeUtility.CopyObjectAddressToPtr(source, destination);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReplaceInstance() => Native.Operation = new UnityUnsafeOperation();
    }
}
