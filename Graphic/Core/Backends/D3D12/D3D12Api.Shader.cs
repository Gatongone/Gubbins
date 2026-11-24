using System.Runtime.InteropServices;
using System.Text;

namespace Gubbins.Graphic;

/// <summary>
/// The compiler Windows ships, reached through its exported function so that a machine without it reports the absence
/// rather than failing to load the backend.
/// </summary>
/// <remarks>
/// <para>
/// A kernel is translated to HLSL and compiled here when a D3D12 device creates its pipeline, which is what lets the
/// artifact of a D3D12 kernel be the source the kernel was lowered to. The compiler is part of the operating system, so
/// a program that ships the artifacts ships nothing else.
/// </para>
/// <para>
/// The profile is <c>cs_5_1</c>, the newest the system compiler has. The calls over the lanes of a wave need a profile a
/// D3D12 device only takes as DXIL, so a kernel that makes one is refused here and loads on the backends that have the
/// operation in the language.
/// </para>
/// </remarks>
internal static unsafe partial class D3D12Api
{
    /// <summary>
    /// The library the compiler is exported from, which every Windows since 10 has.
    /// </summary>
    private const string COMPILER = "d3dcompiler_47.dll";

    /// <summary>
    /// The profile a kernel is compiled with.
    /// </summary>
    private const string PROFILE = "cs_5_1";

    /// <summary>
    /// <c>D3DCOMPILE_ENABLE_STRICTNESS</c>.
    /// </summary>
    private const uint STRICTNESS = 0x00000800;

    /// <summary>
    /// <c>D3DCOMPILE_OPTIMIZATION_LEVEL3</c>.
    /// </summary>
    private const uint OPTIMIZE = 0x00008000;

    private static readonly nint compiler = LoadCompiler();

    private static readonly delegate* unmanaged[Cdecl]<byte*, nuint, byte*, void*, void*, byte*, byte*, uint, uint, nint*, nint*, int> compile =
        (delegate* unmanaged[Cdecl]<byte*, nuint, byte*, void*, void*, byte*, byte*, uint, uint, nint*, nint*, int>)CompilerExport("D3DCompile");

    /// <summary>
    /// Gets a value indicating whether the system has the compiler that turns a kernel into bytecode.
    /// </summary>
    internal static bool HasShaderCompiler => compiler != 0 && compile != null;

    /// <summary>
    /// Compiles the HLSL of one kernel into the bytecode a pipeline state is created from.
    /// </summary>
    /// <param name="source">The HLSL.</param>
    /// <param name="entryPoint">The function to compile.</param>
    /// <returns>The bytecode.</returns>
    /// <exception cref="D3D12Exception">Thrown when the compiler is missing or refuses the program.</exception>
    internal static byte[] CompileShader(string source, string entryPoint)
    {
        if (!HasShaderCompiler)
        {
            throw new D3D12Exception("d3dcompiler_47.dll is not on this machine, so a kernel cannot be compiled for D3D12.");
        }

        var text = Encoding.UTF8.GetBytes(source);
        var entry = Encoding.UTF8.GetBytes(entryPoint + "\0");
        var profile = Encoding.UTF8.GetBytes(PROFILE + "\0");
        nint code;
        nint error;

        fixed (byte* program = text)
        fixed (byte* name = entry)
        fixed (byte* target = profile)
        {
            var hr = compile(program, (nuint)text.Length, null, null, null, name, target, STRICTNESS | OPTIMIZE, 0, &code, &error);
        }

        var message = error != 0 ? Read(error) : string.Empty;

        if (error != 0)
        {
            Release(error);
        }

        if (code == 0)
        {
            throw new D3D12Exception($"The D3D12 compiler refused the kernel:{(message.Length > 0 ? Environment.NewLine + message : string.Empty)}");
        }

        var bytecode = new byte[(int)BlobSize(code)];
        Marshal.Copy((nint)BlobData(code), bytecode, 0, bytecode.Length);
        Release(code);

        return bytecode;
    }

    /// <summary>
    /// Reads a blob of text out of an <c>ID3DBlob</c>.
    /// </summary>
    /// <param name="blob">The blob.</param>
    /// <returns>The text it holds.</returns>
    private static string Read(nint blob)
    {
        var size = (int)BlobSize(blob);
        var text = new byte[size];
        Marshal.Copy((nint)BlobData(blob), text, 0, size);

        return Encoding.UTF8.GetString(text).TrimEnd('\0');
    }

    /// <summary>
    /// Loads the compiler.
    /// </summary>
    /// <returns>The library handle, or <see langword="0"/> when the system does not have it.</returns>
    private static nint LoadCompiler()
    {
        if (!NativeInterop.IsWindows)
        {
            return 0;
        }

        return NativeInterop.TryLoad(COMPILER, out var handle) ? handle : 0;
    }

    /// <summary>
    /// Resolves one export of the compiler.
    /// </summary>
    /// <param name="name">The export name.</param>
    /// <returns>The address, or <see langword="0"/> when the compiler is not there.</returns>
    private static nint CompilerExport(string name)
        => NativeInterop.TryGetExport(compiler, name, out var address) ? address : 0;
}
