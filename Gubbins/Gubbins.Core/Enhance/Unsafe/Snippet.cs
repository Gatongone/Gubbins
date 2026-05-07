using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gubbins.Enhance;

public readonly unsafe struct Snippet<T>
{
    private static readonly int s_ElementSize = (int) Native.GetStackSize<T>();

    private readonly GCHandle m_Handle;
    private readonly int      m_Start;
    private readonly int      m_Length;
    private readonly int      m_Offset;

    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (m_Length == 0) return new Span<T>();
            var ptr = (byte*) Native.GetAddress(m_Handle.Target!);
            return new Span<T>(ptr + m_Offset + m_Start, Length);
        }
    }

    public int Length => m_Length / s_ElementSize;

    public Snippet(T[] array, GCHandle handle) : this(array, new Range(0, array.Length), handle) { }

    public Snippet(T[] array, Range range, GCHandle handle)
    {
        m_Handle = handle;
        var size = (int) Native.GetStackSize(typeof(T));
        m_Start  = range.Start.Value * size;
        m_Length = (range.End.Value - range.Start.Value) * size;
        if (array.Length > 0)
        {
            m_Offset = (int) Native.GetFirstElementAddress(array) - (int) Native.GetAddress(array);
        }
    }

    public Snippet(T[] array, int start, int length, GCHandle handle)
    {
        m_Handle = handle;
        m_Start  = start;
        m_Length = length;
        if (array.Length > 0)
        {
            m_Offset = (int) Native.GetFirstElementAddress(array) - (int) Native.GetAddress(array);
        }
    }

    private Snippet(GCHandle handle, int start, int length, int offset = 16)
    {
        m_Handle = handle;
        m_Start  = start;
        m_Length = length;
        m_Offset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snippet<TTo> Cast<TTo>() => new(m_Handle, m_Start, m_Length, m_Offset);
}