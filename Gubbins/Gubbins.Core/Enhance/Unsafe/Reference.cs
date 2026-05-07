// using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;
//
// namespace Gubbins.Enhance;
//
// /// <summary>
// /// Object reference.
// /// </summary>
// /// <remarks>
// /// As we all know, CLR-GC may change object pointer address so that we could lose the actual object address without pinning.
// /// So we need to get the pointer reference to make sure we could get the correct pointer.
// /// </remarks>
// public unsafe struct Reference
// {
//     public void** Handle;
//     public readonly void* Address
//     {
//         get
//         {
//             Console.WriteLine($"Handle: {(int)Handle}");
//             Console.WriteLine($"HandleValue: {(int) *(void**)Handle}");
//             return *(void**) Handle;
//         }
//     }
//
//     public object Value
//     {
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         get => *(object*) Handle;
//     }
//
//     /// <param name="obj">The handle provider.</param>
//     public Reference(object obj)
//     {
//         void**
//     }
// }
