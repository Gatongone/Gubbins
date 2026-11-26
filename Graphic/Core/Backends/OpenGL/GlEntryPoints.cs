
namespace Gubbins.Graphic;

/// <summary>
/// The OpenGL entry points the bridge calls, resolved once for the context they belong to.
/// </summary>
internal sealed unsafe class GlEntryPoints
{
    /// <param name="module">The OpenGL library the exports are looked up in.</param>
    internal GlEntryPoints(nint module)
    {
        GenBuffers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glGenBuffers");
        DeleteBuffers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glDeleteBuffers");
        BindBuffer = (delegate* unmanaged[Cdecl]<uint, uint, void>)GlContext.Resolve(module, "glBindBuffer");
        BufferStorage = (delegate* unmanaged[Cdecl]<uint, ulong, void*, uint, void>)GlContext.Resolve(module, "glBufferStorage");
        MapBufferRange = (delegate* unmanaged[Cdecl]<uint, ulong, ulong, uint, void*>)GlContext.Resolve(module, "glMapNamedBufferRange");
        UnmapBuffer = (delegate* unmanaged[Cdecl]<uint, byte>)GlContext.Resolve(module, "glUnmapNamedBuffer");
        BufferSubData = (delegate* unmanaged[Cdecl]<uint, ulong, nint, void*, void>)GlContext.Resolve(module, "glNamedBufferSubData");
        GetBufferSubData = (delegate* unmanaged[Cdecl]<uint, ulong, nint, void*, void>)GlContext.Resolve(module, "glGetNamedBufferSubData");
        CopyBufferSubData = (delegate* unmanaged[Cdecl]<uint, uint, ulong, ulong, nint, void>)GlContext.Resolve(module, "glCopyNamedBufferSubData");
        GenVertexArrays = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glGenVertexArrays");
        BindVertexArray = (delegate* unmanaged[Cdecl]<uint, void>)GlContext.Resolve(module, "glBindVertexArray");
        CreateShader = (delegate* unmanaged[Cdecl]<uint, uint>)GlContext.Resolve(module, "glCreateShader");
        ShaderSource = (delegate* unmanaged[Cdecl]<uint, int, byte**, int*, void>)GlContext.Resolve(module, "glShaderSource");
        CompileShader = (delegate* unmanaged[Cdecl]<uint, void>)GlContext.Resolve(module, "glCompileShader");
        GetShaderiv = (delegate* unmanaged[Cdecl]<uint, uint, int*, void>)GlContext.Resolve(module, "glGetShaderiv");
        GetShaderInfoLog = (delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)GlContext.Resolve(module, "glGetShaderInfoLog");
        CreateProgram = (delegate* unmanaged[Cdecl]<uint>)GlContext.Resolve(module, "glCreateProgram");
        AttachShader = (delegate* unmanaged[Cdecl]<uint, uint, void>)GlContext.Resolve(module, "glAttachShader");
        LinkProgram = (delegate* unmanaged[Cdecl]<uint, void>)GlContext.Resolve(module, "glLinkProgram");
        GetProgramiv = (delegate* unmanaged[Cdecl]<uint, uint, int*, void>)GlContext.Resolve(module, "glGetProgramiv");
        GetProgramInfoLog = (delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)GlContext.Resolve(module, "glGetProgramInfoLog");
        UseProgram = (delegate* unmanaged[Cdecl]<uint, void>)GlContext.Resolve(module, "glUseProgram");
        BindBufferRange = (delegate* unmanaged[Cdecl]<uint, uint, uint, nint, nint, void>)GlContext.Resolve(module, "glBindBufferRange");
        DispatchCompute = (delegate* unmanaged[Cdecl]<uint, uint, uint, void>)GlContext.Resolve(module, "glDispatchCompute");
        MemoryBarrier = (delegate* unmanaged[Cdecl]<uint, void>)GlContext.Resolve(module, "glMemoryBarrier");
        Finish = (delegate* unmanaged[Cdecl]<void>)GlContext.Resolve(module, "glFinish");
        GetError = (delegate* unmanaged[Cdecl]<uint>)GlContext.Resolve(module, "glGetError");
        GenTextures = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glGenTextures");
        DeleteTextures = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glDeleteTextures");
        BindTexture = (delegate* unmanaged[Cdecl]<uint, uint, void>)GlContext.Resolve(module, "glBindTexture");
        TexImage2D = (delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void>)GlContext.Resolve(module, "glTexImage2D");
        TexSubImage2D = (delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void>)GlContext.Resolve(module, "glTexSubImage2D");
        GetTexImage = (delegate* unmanaged[Cdecl]<uint, int, uint, uint, void*, void>)GlContext.Resolve(module, "glGetTexImage");
        PixelStore = (delegate* unmanaged[Cdecl]<uint, int, void>)GlContext.Resolve(module, "glPixelStorei");
        ActiveTexture = (delegate* unmanaged[Cdecl]<uint, void>)GlContext.Resolve(module, "glActiveTexture");
        GenSamplers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glGenSamplers");
        DeleteSamplers = (delegate* unmanaged[Cdecl]<int, uint*, void>)GlContext.Resolve(module, "glDeleteSamplers");
        BindSampler = (delegate* unmanaged[Cdecl]<uint, uint, void>)GlContext.Resolve(module, "glBindSampler");
        SamplerParameter = (delegate* unmanaged[Cdecl]<uint, uint, int, void>)GlContext.Resolve(module, "glSamplerParameteri");
    }

    internal delegate* unmanaged[Cdecl]<int, uint*, void> GenBuffers;
    internal delegate* unmanaged[Cdecl]<int, uint*, void> DeleteBuffers;
    internal delegate* unmanaged[Cdecl]<uint, uint, void> BindBuffer;
    internal delegate* unmanaged[Cdecl]<uint, ulong, void*, uint, void> BufferStorage;
    internal delegate* unmanaged[Cdecl]<uint, ulong, ulong, uint, void*> MapBufferRange;
    internal delegate* unmanaged[Cdecl]<uint, byte> UnmapBuffer;
    internal delegate* unmanaged[Cdecl]<uint, ulong, nint, void*, void> BufferSubData;
    internal delegate* unmanaged[Cdecl]<uint, ulong, nint, void*, void> GetBufferSubData;
    internal delegate* unmanaged[Cdecl]<uint, uint, ulong, ulong, nint, void> CopyBufferSubData;
    internal delegate* unmanaged[Cdecl]<int, uint*, void> GenVertexArrays;
    internal delegate* unmanaged[Cdecl]<uint, void> BindVertexArray;
    internal delegate* unmanaged[Cdecl]<uint, uint> CreateShader;
    internal delegate* unmanaged[Cdecl]<uint, int, byte**, int*, void> ShaderSource;
    internal delegate* unmanaged[Cdecl]<uint, void> CompileShader;
    internal delegate* unmanaged[Cdecl]<uint, uint, int*, void> GetShaderiv;
    internal delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void> GetShaderInfoLog;
    internal delegate* unmanaged[Cdecl]<uint> CreateProgram;
    internal delegate* unmanaged[Cdecl]<uint, uint, void> AttachShader;
    internal delegate* unmanaged[Cdecl]<uint, void> LinkProgram;
    internal delegate* unmanaged[Cdecl]<uint, uint, int*, void> GetProgramiv;
    internal delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void> GetProgramInfoLog;
    internal delegate* unmanaged[Cdecl]<uint, void> UseProgram;
    internal delegate* unmanaged[Cdecl]<uint, uint, uint, nint, nint, void> BindBufferRange;
    internal delegate* unmanaged[Cdecl]<uint, uint, uint, void> DispatchCompute;
    internal delegate* unmanaged[Cdecl]<uint, void> MemoryBarrier;
    internal delegate* unmanaged[Cdecl]<void> Finish;
    internal delegate* unmanaged[Cdecl]<uint> GetError;
    internal delegate* unmanaged[Cdecl]<int, uint*, void> GenTextures;
    internal delegate* unmanaged[Cdecl]<int, uint*, void> DeleteTextures;
    internal delegate* unmanaged[Cdecl]<uint, uint, void> BindTexture;
    internal delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void> TexImage2D;
    internal delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void> TexSubImage2D;
    internal delegate* unmanaged[Cdecl]<uint, int, uint, uint, void*, void> GetTexImage;
    internal delegate* unmanaged[Cdecl]<uint, int, void> PixelStore;
    internal delegate* unmanaged[Cdecl]<uint, void> ActiveTexture;
    internal delegate* unmanaged[Cdecl]<int, uint*, void> GenSamplers;
    internal delegate* unmanaged[Cdecl]<int, uint*, void> DeleteSamplers;
    internal delegate* unmanaged[Cdecl]<uint, uint, void> BindSampler;
    internal delegate* unmanaged[Cdecl]<uint, uint, int, void> SamplerParameter;
}

