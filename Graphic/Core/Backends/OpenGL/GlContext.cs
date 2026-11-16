
using System.Runtime.InteropServices;

namespace Gubbins.Graphic;

/// <summary>
/// A headless OpenGL 4.5 core context, created on a hidden window because Windows has no surfaceless context path that a
/// driver is required to expose.
/// </summary>
/// <remarks>
/// The context is what makes the OpenGL backend expressible at all: with <c>ARB_buffer_storage</c> a buffer is created with
/// a persistent, coherent mapping, so the address the CPU writes is the address the shader reads, which is the same
/// promise the other backends keep.
/// </remarks>
internal sealed unsafe class GlContext : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint CsOwndc = 0x20;
    private const uint PfdSupportOpenGl = 0x20;
    private const uint PfdDrawToWindow = 0x4;

    private readonly nint m_Window;
    private readonly nint m_DeviceContext;
    private readonly nint m_Context;
    private readonly nint m_Module;

    /// <summary>
    /// Gets the entry points of this context.
    /// </summary>
    internal GlEntryPoints EntryPoints { get; }

    private bool m_Disposed;

    /// <summary>
    /// The window procedure the class is registered with, kept in a field so the delegate outlives the window class.
    /// </summary>
    private static readonly WndProcDelegate s_WindowProc = (window, message, word, value) => DefWindowProcW(window, message, word, value);

    private GlContext(nint window, nint deviceContext, nint context, nint module)
    {
        m_Window = window;
        m_DeviceContext = deviceContext;
        m_Context = context;
        m_Module = module;
        EntryPoints = new GlEntryPoints(module);
    }

    /// <summary>
    /// Creates the context.
    /// </summary>
    /// <param name="created">When the method returns, the context, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if a 4.5 core context was created.</returns>
    internal static bool TryCreate(out GlContext? created)
    {
        created = null;

        var instance = GetModuleHandleW(null);
        var windowClass = new WndClass
        {
            Style = CsOwndc,
            WindowProc = Marshal.GetFunctionPointerForDelegate(s_WindowProc),
            Instance = instance,
            ClassName = "gubbins-opengl"
        };

        if (RegisterClassW(ref windowClass) == 0)
        {
            return false;
        }

        var window = CreateWindowExW(0, "gubbins-opengl", "gubbins", WsPopup, 0, 0, 1, 1, 0, 0, instance, 0);

        if (window == 0)
        {
            return false;
        }

        var deviceContext = GetDC(window);

        if (deviceContext == 0)
        {
            DestroyWindow(window);
            return false;
        }

        var format = new PixelFormatDescriptor
        {
            Size = (ushort)sizeof(PixelFormatDescriptor),
            Version = 1,
            Flags = PfdDrawToWindow | PfdSupportOpenGl,
            ColorBits = 32,
            AlphaBits = 8,
            DepthBits = 24
        };

        var chosen = ChoosePixelFormat(deviceContext, ref format);

        if (chosen == 0 || !SetPixelFormat(deviceContext, chosen, ref format))
        {
            DestroyWindow(window);
            return false;
        }

        // A core profile has to be asked for with an extension entry point, which only exists once a legacy context is
        // current, so the legacy context is created first and dropped afterwards.
        var legacy = wglCreateContext(deviceContext);

        if (legacy == 0 || !wglMakeCurrent(deviceContext, legacy))
        {
            DestroyWindow(window);
            return false;
        }

        var module = LoadLibraryW("opengl32.dll");
        var createContext = (delegate* unmanaged[Cdecl]<nint, nint, int*, nint>)wglGetProcAddress("wglCreateContextAttribsARB");

        if (createContext == null)
        {
            wglMakeCurrent(0, 0);
            wglDeleteContext(legacy);
            DestroyWindow(window);
            return false;
        }

        // The attributes ask for OpenGL 4.5 with the core profile, which is what ARB_buffer_storage and compute need.
        var attributes = stackalloc int[7];
        attributes[0] = 0x2091; // GL_CONTEXT_MAJOR_VERSION
        attributes[1] = 4;
        attributes[2] = 0x2092; // GL_CONTEXT_MINOR_VERSION
        attributes[3] = 5;
        attributes[4] = 0x9126; // GL_CONTEXT_PROFILE_MASK
        attributes[5] = 1; // GL_CONTEXT_CORE_PROFILE_BIT
        attributes[6] = 0;

        var context = createContext(deviceContext, 0, attributes);

        if (context == 0 || !wglMakeCurrent(deviceContext, context))
        {
            wglMakeCurrent(0, 0);
            wglDeleteContext(legacy);
            DestroyWindow(window);
            return false;
        }

        wglDeleteContext(legacy);
        created = new GlContext(window, deviceContext, context, module);
        return true;
    }

    /// <summary>
    /// Releases the context, the window and the renderer.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        wglMakeCurrent(0, 0);
        wglDeleteContext(m_Context);
        ReleaseDC(m_Window, m_DeviceContext);
        DestroyWindow(m_Window);
        _ = m_Module;
    }

    /// <summary>
    /// The window procedure the hidden window is registered with.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="message">The message.</param>
    /// <param name="word">The first message parameter.</param>
    /// <param name="value">The second message parameter.</param>
    /// <returns>The default handling.</returns>
    private delegate nint WndProcDelegate(nint window, uint message, nint word, nint value);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint Style;
        public nint WindowProc;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public string? MenuName;
        public string ClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PixelFormatDescriptor
    {
        public ushort Size;
        public ushort Version;
        public uint Flags;
        public byte PixelType;
        public byte ColorBits;
        public byte RedBits;
        public byte RedShift;
        public byte GreenBits;
        public byte GreenShift;
        public byte BlueBits;
        public byte BlueShift;
        public byte AlphaBits;
        public byte AlphaShift;
        public byte AccumBits;
        public byte AccumRedBits;
        public byte AccumGreenBits;
        public byte AccumBlueBits;
        public byte AccumAlphaBits;
        public byte DepthBits;
        public byte StencilBits;
        public byte AuxiliaryBuffers;
        public byte LayerType;
        public byte Reserved;
        public uint LayerMask;
        public uint VisibleMask;
        public uint DamageMask;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassW(ref WndClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowExW(uint extendedStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProcW(nint window, uint message, nint word, nint value);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint deviceContext);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint window);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? name);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadLibraryW(string name);

    [DllImport("gdi32.dll")]
    private static extern int ChoosePixelFormat(nint deviceContext, ref PixelFormatDescriptor format);

    [DllImport("gdi32.dll")]
    private static extern bool SetPixelFormat(nint deviceContext, int format, ref PixelFormatDescriptor description);

    [DllImport("opengl32.dll")]
    private static extern nint wglCreateContext(nint deviceContext);

    [DllImport("opengl32.dll")]
    private static extern nint wglGetProcAddress(string name);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern nint GetProcAddress(nint module, string name);

    /// <summary>
    /// Resolves one entry point from the current context, falling back to the library export for the functions that are
    /// not extensions.
    /// </summary>
    /// <param name="module">The OpenGL library.</param>
    /// <param name="name">The name of the entry point.</param>
    /// <returns>The address, or zero when the context does not have it.</returns>
    internal static nint Resolve(nint module, string name)
    {
        var address = wglGetProcAddress(name);
        return address != 0 ? address : GetProcAddress(module, name);
    }

    [DllImport("opengl32.dll")]
    private static extern bool wglMakeCurrent(nint deviceContext, nint context);

    [DllImport("opengl32.dll")]
    private static extern bool wglDeleteContext(nint context);
}

