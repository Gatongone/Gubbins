using System.Runtime.InteropServices;
using Gubbins.Unsafe;

#pragma warning disable CS8500

namespace Gubbins.Graphic;

/// <summary>
/// The two platform services the backends cannot take from the base class library: allocating the small structures they
/// keep outside the garbage collector, and loading a graphics library by name.
/// </summary>
/// <remarks>
/// <para>
/// Both are written against the allocator this project already uses and against the platform loaders, so the graphic
/// assembly needs nothing newer than <c>netstandard2.1</c> and every target framework compiles the same backends.
/// </para>
/// <para>
/// An allocation keeps its size in a header in front of the block, which is what lets a release take only the pointer, and
/// is why the size a caller asks for is rounded up to a sixteen byte boundary.
/// </para>
/// </remarks>
internal static unsafe class NativeInterop
{
    /// <summary>
    /// The alignment every block is allocated with: enough for any structure a backend keeps outside the heap.
    /// </summary>
    private const byte Alignment = 16;

    /// <summary>
    /// The number of bytes the size header takes, aligned to <see cref="Alignment"/>.
    /// </summary>
    private const nuint Header = 16;

    /// <summary>
    /// Searches for a library only next to the executable and in the system directory, never in the working directory.
    /// </summary>
    private const uint SearchSystem32 = 0x00000800;

    /// <summary>
    /// Searches the directories the application normally loads from.
    /// </summary>
    private const uint SearchDefaultDirectories = 0x00000100;

    /// <summary>
    /// The mode <c>dlopen</c> is called with: resolve symbols on use, and keep the library local to this process.
    /// </summary>
    private const int Lazy = 0x0001;

    /// <summary>
    /// Allocates a zeroed block.
    /// </summary>
    /// <param name="size">The size the caller asked for.</param>
    /// <returns>The block, or <see langword="null"/> when the arena is out of memory.</returns>
    internal static void* AllocZeroed(nuint size)
    {
        var total = Header + size;
        var block = (byte*)UnmanagedAllocator.Global.Alloc(total, Alignment);

        if (block == null)
        {
            return null;
        }

        *(nuint*)block = total;
        new Span<byte>(block, (int)total).Clear();
        return block + Header;
    }

    /// <summary>
    /// Allocates a block without clearing it.
    /// </summary>
    /// <param name="size">The size the caller asked for.</param>
    /// <returns>The block, or <see langword="null"/> when the arena is out of memory.</returns>
    internal static void* Alloc(nuint size)
    {
        var total = Header + size;
        var block = (byte*)UnmanagedAllocator.Global.Alloc(total, Alignment);

        if (block == null)
        {
            return null;
        }

        *(nuint*)block = total;
        return block + Header;
    }

    /// <summary>
    /// Releases a block.
    /// </summary>
    /// <param name="pointer">The pointer <see cref="Alloc"/> or <see cref="AllocZeroed"/> returned.</param>
    internal static void Free(void* pointer)
    {
        if (pointer == null)
        {
            return;
        }

        var block = (byte*)pointer - Header;
        UnmanagedAllocator.Global.Free(block, *(nuint*)block, Alignment);
    }

    /// <summary>
    /// Gets a value indicating whether this is a Windows process.
    /// </summary>
    internal static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets a value indicating whether this is a macOS process.
    /// </summary>
    internal static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Gets a value indicating whether this is a Linux process.
    /// </summary>
    internal static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Loads a library by name.
    /// </summary>
    /// <param name="name">The library file name.</param>
    /// <param name="handle">When the method returns, the library handle, or zero when it could not be loaded.</param>
    /// <returns><see langword="true"/> if the library was loaded.</returns>
    internal static bool TryLoad(string name, out nint handle)
    {
        handle = 0;

        if (IsWindows)
        {
            // The system directory is searched explicitly so a file next to the executable cannot answer for a
            // system library; a name that is not there falls back to the default search.
            handle = LoadLibraryExW(name, 0, SearchSystem32);

            if (handle == 0)
            {
                handle = LoadLibraryW(name);
            }
        }
        else
        {
            handle = DlOpen(name, Lazy);
        }

        return handle != 0;
    }

    /// <summary>
    /// Looks up one export of a library.
    /// </summary>
    /// <param name="library">The library handle.</param>
    /// <param name="name">The name of the export.</param>
    /// <param name="address">When the method returns, the address, or zero when the library has no such export.</param>
    /// <returns><see langword="true"/> if the export was found.</returns>
    internal static bool TryGetExport(nint library, string name, out nint address)
    {
        address = library == 0
            ? 0
            : NativeInterop.IsWindows ? GetProcAddress(library, name) : DlSym(library, name);

        return address != 0;
    }


    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW")]
    private static extern nint LoadLibraryW(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW")]
    private static extern nint LoadLibraryExW(string name, nint file, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, EntryPoint = "GetProcAddress")]
    private static extern nint GetProcAddress(nint library, string name);

    [DllImport("libdl", EntryPoint = "dlopen")]
    private static extern nint DlOpen(string name, int mode);

    [DllImport("libdl", EntryPoint = "dlsym")]
    private static extern nint DlSym(nint library, string name);
}
