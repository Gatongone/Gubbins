using System.Runtime.InteropServices;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Provides hardware device information and utilities for cross-platform system configuration queries.
/// This class contains functionality to determine CPU cache line sizes and other hardware characteristics
/// across different operating systems including Windows, Linux, and macOS.
/// </summary>
internal class Device
{
    /// <summary>
    /// System configuration parameter constant for retrieving Level 1 data cache line size on POSIX systems.
    /// Used with the sysconf system call to query cache line size information.
    /// </summary>
    private const int SC_LEVEL1_DCACHE_LINESIZE = 190;

    /// <summary>
    /// Windows API error code indicating that the provided buffer is too small to contain the requested data.
    /// Used when calling GetLogicalProcessorInformation to determine the required buffer size.
    /// </summary>
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    /// <summary>
    /// Gets the CPU cache line size in bytes for the current system.
    /// This value is determined at runtime based on the operating system platform and represents
    /// the optimal alignment boundary for memory operations to avoid cache line splits.
    /// </summary>
    public static readonly int CacheLineSize = GetCacheLineSize();

    /// <summary>
    /// Determines the CPU cache line size for the current operating system platform.
    /// </summary>
    /// <returns>
    /// The cache line size in bytes. Returns the platform-specific cache line size for Linux, macOS, or Windows.
    /// If the platform is not supported or cache line size cannot be determined, returns a default value of 64 bytes.
    /// </returns>
    private static int GetCacheLineSize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxSize();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetOsxSize();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && TryGetWindowsSize(out var size))
        {
            return size;
        }

        return 64;
    }

    /// <summary>
    /// Attempts to retrieve the CPU cache line size on Windows systems using the Windows API.
    /// </summary>
    /// <param name="size">When this method returns, contains the cache line size in bytes if successful; otherwise, -1.</param>
    /// <returns>
    /// <c>true</c> if the cache line size was successfully retrieved; otherwise, <c>false</c>.
    /// </returns>
    private static bool TryGetWindowsSize(out int size)
    {
        size = -1;
        var info = ManagedGetLogicalProcessorInformation();
        if (info == null) return false;
        size = info.First(x => x.Relationship == LogicalProcessorRelationship.RelationCache).ProcessorInformation.Cache.LineSize;
        return true;
    }

    /// <summary>
    /// Retrieves the CPU cache line size on Linux systems using the sysconf system call.
    /// </summary>
    /// <returns>The cache line size in bytes as reported by the Linux system configuration.</returns>
    private static int GetLinuxSize() => (int) sysconf(SC_LEVEL1_DCACHE_LINESIZE);

    /// <summary>
    /// Retrieves the CPU cache line size on macOS systems using the sysctlbyname system call.
    /// </summary>
    /// <returns>The cache line size in bytes as reported by the macOS hardware configuration.</returns>
    private static int GetOsxSize()
    {
        var sizeOfLineSize = (IntPtr) IntPtr.Size;
        sysctlbyname("hw.cachelinesize", out var lineSize, ref sizeOfLineSize, IntPtr.Zero, IntPtr.Zero);
        return lineSize.ToInt32();
    }

    /// <summary>
    /// Retrieves logical processor information from the Windows API and converts it to a managed array.
    /// </summary>
    /// <returns>
    /// An array of <see cref="SystemLogicalProcessorInformation"/> structures containing processor information,
    /// or <c>null</c> if the information could not be retrieved.
    /// </returns>
    private static SystemLogicalProcessorInformation[]? ManagedGetLogicalProcessorInformation()
    {
        uint returnLength = 0;
        GetLogicalProcessorInformation(IntPtr.Zero, ref returnLength);
        if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            return null;
        var ptr = Native.Alloc((int) returnLength);
        try
        {
            if (GetLogicalProcessorInformation(ptr, ref returnLength))
            {
                var size = Marshal.SizeOf<SystemLogicalProcessorInformation>();
                var len = (int) returnLength / size;
                var buffer = new SystemLogicalProcessorInformation[len];
                var item = ptr;
                for (var i = 0; i < len; i++)
                {
                    buffer[i] =  Marshal.PtrToStructure<SystemLogicalProcessorInformation>(item);
                    item      += size;
                }

                return buffer;
            }
        }
        finally
        {
            Native.Free(ptr);
        }

        return null;
    }

    /// <summary>
    /// Retrieves information about logical processors and their relationships on Windows systems.
    /// This function is a P/Invoke wrapper for the Windows API GetLogicalProcessorInformation function.
    /// </summary>
    /// <param name="buffer">A pointer to a buffer that receives an array of SYSTEM_LOGICAL_PROCESSOR_INFORMATION structures. If this parameter is NULL, the function returns the required buffer size.</param>
    /// <param name="returnLength">On input, specifies the length of the buffer pointed to by the buffer parameter, in bytes. On output, receives the number of bytes returned in the buffer.</param>
    /// <returns>
    /// <c>true</c> if the function succeeds and the buffer contains the requested information; otherwise, <c>false</c>.
    /// If the function fails, call GetLastError to get extended error information.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformation(IntPtr buffer, ref uint returnLength);

    /// <summary>
    /// Gets configuration information at runtime on POSIX-compliant systems.
    /// This function is a P/Invoke wrapper for the POSIX sysconf system call.
    /// </summary>
    /// <param name="name">The system configuration parameter to query, such as cache line size or page size constants.</param>
    /// <returns>
    /// The current value of the specified configuration parameter, or -1 if the parameter is not supported or an error occurs.
    /// </returns>
    [DllImport("libc")]
    private static extern long sysconf(int name);

    /// <summary>
    /// Retrieves system information by name on macOS and BSD systems.
    /// This function is a P/Invoke wrapper for the BSD sysctlbyname system call.
    /// </summary>
    /// <param name="name">The name of the system information to retrieve, specified as a string (e.g., "hw.cachelinesize").</param>
    /// <param name="oldp">A pointer that receives the current value of the specified system information.</param>
    /// <param name="oldlenp">On input, specifies the size of the buffer pointed to by oldp. On output, receives the actual size of the data returned.</param>
    /// <param name="newp">A pointer to a new value to set for the system information. Use IntPtr.Zero if not setting a new value.</param>
    /// <param name="newlen">The size of the new value pointed to by newp. Use IntPtr.Zero if not setting a new value.</param>
    /// <returns>
    /// 0 if the function succeeds; otherwise, -1 and errno is set to indicate the error.
    /// </returns>
    [DllImport("libc")]
    private static extern int sysctlbyname(string name, out IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, IntPtr newlen);

    /// <summary>
    /// Represents processor core information in the Windows logical processor information structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorCore
    {
        /// <summary>
        /// Flags that describe the processor core characteristics.
        /// </summary>
        public byte Flags;
    }

    /// <summary>
    /// Represents NUMA (Non-Uniform Memory Access) node information in the Windows logical processor information structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Numanode
    {
        /// <summary>
        /// The NUMA node number identifier.
        /// </summary>
        public uint NodeNumber;
    }

    /// <summary>
    /// Specifies the type of processor cache.
    /// </summary>
    private enum ProcessorCacheType
    {
        /// <summary>
        /// Unified cache that stores both instructions and data.
        /// </summary>
        CacheUnified,

        /// <summary>
        /// Instruction cache that stores only processor instructions.
        /// </summary>
        CacheInstruction,

        /// <summary>
        /// Data cache that stores only data.
        /// </summary>
        CacheData,

        /// <summary>
        /// Trace cache that stores decoded micro-operations.
        /// </summary>
        CacheTrace
    }

    /// <summary>
    /// Describes the cache characteristics of a logical processor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct CacheDescriptor
    {
        /// <summary>
        /// The cache level (1, 2, 3, etc.).
        /// </summary>
        public byte Level;

        /// <summary>
        /// The cache associativity. If this member is 0xFF, the cache is fully associative.
        /// </summary>
        public byte Associativity;

        /// <summary>
        /// The cache line size in bytes.
        /// </summary>
        public ushort LineSize;

        /// <summary>
        /// The cache size in bytes.
        /// </summary>
        public uint Size;

        /// <summary>
        /// The type of cache (unified, instruction, data, or trace).
        /// </summary>
        public ProcessorCacheType Type;
    }

    /// <summary>
    /// A union structure that contains different types of processor information based on the relationship type.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct SystemLogicalProcessorInformationUnion
    {
        /// <summary>
        /// Processor core information, valid when the relationship is RelationProcessorCore.
        /// </summary>
        [FieldOffset(0)] public ProcessorCore ProcessorCore;

        /// <summary>
        /// NUMA node information, valid when the relationship is RelationNumaNode.
        /// </summary>
        [FieldOffset(0)] public Numanode NumaNode;

        /// <summary>
        /// Cache descriptor information, valid when the relationship is RelationCache.
        /// </summary>
        [FieldOffset(0)] public CacheDescriptor Cache;

        /// <summary>
        /// Reserved field for future use.
        /// </summary>
        [FieldOffset(0)] private UInt64 Reserved1;

        /// <summary>
        /// Reserved field for future use.
        /// </summary>
        [FieldOffset(8)] private UInt64 Reserved2;
    }

    /// <summary>
    /// Specifies the relationship between logical processors.
    /// </summary>
    private enum LogicalProcessorRelationship
    {
        /// <summary>
        /// The specified logical processors share a single processor core.
        /// </summary>
        RelationProcessorCore,

        /// <summary>
        /// The specified logical processors are part of the same NUMA node.
        /// </summary>
        RelationNumaNode,

        /// <summary>
        /// The specified logical processors share a cache.
        /// </summary>
        RelationCache,

        /// <summary>
        /// The specified logical processors share a physical package.
        /// </summary>
        RelationProcessorPackage,

        /// <summary>
        /// The specified logical processors share a processor group.
        /// </summary>
        RelationGroup,

        /// <summary>
        /// Represents all possible relationships.
        /// </summary>
        RelationAll = 0xffff
    }

    /// <summary>
    /// MayContains information about the relationships of logical processors and related hardware.
    /// This structure is used by the GetLogicalProcessorInformation function.
    /// </summary>
    private struct SystemLogicalProcessorInformation
    {
#pragma warning disable 0649
        /// <summary>
        /// A bitmap that specifies the affinity for zero or more logical processors.
        /// </summary>
        public UIntPtr ProcessorMask;

        /// <summary>
        /// The relationship between the logical processors specified by the ProcessorMask member.
        /// </summary>
        public LogicalProcessorRelationship Relationship;

        /// <summary>
        /// A union that contains additional information about the logical processor relationship.
        /// The structure used depends on the value of the Relationship member.
        /// </summary>
        public SystemLogicalProcessorInformationUnion ProcessorInformation;
#pragma warning restore 0649
    }
}