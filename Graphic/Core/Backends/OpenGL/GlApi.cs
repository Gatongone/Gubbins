
using System.Runtime.InteropServices;

#pragma warning disable CS8500

namespace Gubbins.Graphic;

/// <summary>
/// The OpenGL entry points this bridge uses, loaded from a headless EGL context.
/// </summary>
/// <remarks>
/// <para>
/// OpenGL is the only candidate backend whose context the library has to create itself: a window would tie the device to a
/// message loop, so the context is EGL surfaceless, which the driver exposes without a display.
/// </para>
/// <para>
/// The memory model maps onto <c>ARB_buffer_storage</c>: a buffer is created with a persistent, coherent mapping, so the
/// address the CPU writes is the address the shader reads. That is the same promise the other backends keep, which is why
/// this backend exists at all.
/// </para>
/// </remarks>
internal static unsafe class GlApi
{
    private const string Egl = "libEGL.dll";
    private const string Gl = "opengl32.dll";

    /// <summary>
    /// The bit that asks for a persistent mapping.
    /// </summary>
    internal const uint MapPersistentBit = 0x0040;

    /// <summary>
    /// The bit that asks for a coherent mapping, so no explicit flush is needed.
    /// </summary>
    internal const uint MapCoherentBit = 0x0080;

    /// <summary>
    /// The bit that asks for a mapping the CPU may write.
    /// </summary>
    internal const uint MapWriteBit = 0x0002;

    /// <summary>
    /// The bit that asks for a mapping the CPU may read.
    /// </summary>
    internal const uint MapReadBit = 0x0001;

    /// <summary>
    /// The target a shader storage buffer is bound to.
    /// </summary>
    internal const int ShaderStorageBuffer = 0x90D2;

    /// <summary>
    /// The compute stage bit used by the barrier call.
    /// </summary>
    internal const uint ShaderImageAccessBarrierBit = 0x0020;

    /// <summary>
    /// The usage flag that makes a buffer's contents the caller's business.
    /// </summary>
    internal const uint DynamicStorageBit = 0x0100;

    /// <summary>
    /// The usage flag that keeps a buffer's contents across frames.
    /// </summary>
    internal const uint MapReadBitStorage = 0x0001;

    [DllImport(Egl)] private static extern nint eglGetPlatformDisplayEXT(uint platform, void* nativeDisplay, int* attributes);

    [DllImport(Egl)] private static extern nint eglGetDisplay(nint nativeDisplay);

    [DllImport(Egl)] private static extern int eglInitialize(nint display, int* major, int* minor);

    [DllImport(Egl)] private static extern int eglBindAPI(uint api);

    [DllImport(Egl)] private static extern nint eglCreateContext(nint display, nint config, nint share, int* attributes);

    [DllImport(Egl)] private static extern int eglMakeCurrent(nint display, nint draw, nint read, nint context);

    [DllImport(Egl)] private static extern nint eglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(Egl)] private static extern int eglChooseConfig(nint display, int* attributes, nint* configs, int count, int* chosen);

    [DllImport(Egl)] private static extern int eglTerminate(nint display);

    [DllImport(Egl)] private static extern int eglReleaseThread();

    [DllImport(Gl)] private static extern void glGetIntegerv(uint name, int* value);

    [DllImport(Gl)] private static extern int wglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string name);

    internal static delegate* unmanaged[Cdecl]<uint, int> CreateBuffer;
    internal static delegate* unmanaged[Cdecl]<int, uint*, ulong*, uint*, void> NamedBufferStorageEXT;
    internal static delegate* unmanaged[Cdecl]<int, uint, void*> MapNamedBufferEXT;
    internal static delegate* unmanaged[Cdecl]<uint, uint, ulong, ulong, void> BindBufferRange;
    internal static delegate* unmanaged[Cdecl]<uint, uint, int> CreateShader;
    internal static delegate* unmanaged[Cdecl]<uint, int, byte**, int, void> ShaderSource;
    internal static delegate* unmanaged[Cdecl]<uint, void> CompileShader;
    internal static delegate* unmanaged[Cdecl]<uint, int*, void> GetShaderiv;
    internal static delegate* unmanaged[Cdecl]<uint, uint, int> CreateProgram;
    internal static delegate* unmanaged[Cdecl]<uint, void> AttachShader;
    internal static delegate* unmanaged[Cdecl]<uint, void> LinkProgram;
    internal static delegate* unmanaged[Cdecl]<uint, int*, void> GetProgramiv;
    internal static delegate* unmanaged[Cdecl]<uint, int, uint*, void> GetProgramResourceIndex;
    internal static delegate* unmanaged[Cdecl]<uint, uint, void> UseProgram;
    internal static delegate* unmanaged[Cdecl]<uint, uint, uint, void> DispatchCompute;
    internal static delegate* unmanaged[Cdecl]<uint, void> MemoryBarrier;
    internal static delegate* unmanaged[Cdecl]<nint> FenceSync;
    internal static delegate* unmanaged[Cdecl]<nint, ulong, uint> ClientWaitSync;
    internal static delegate* unmanaged[Cdecl]<nint, void> DeleteSync;
    internal static delegate* unmanaged[Cdecl]<int, uint, void> DeleteBuffers;
    internal static delegate* unmanaged[Cdecl]<uint, void> DeleteShader;
    internal static delegate* unmanaged[Cdecl]<uint, void> DeleteProgram;
    internal static delegate* unmanaged[Cdecl]<int*, void> GetIntegerv;
}

