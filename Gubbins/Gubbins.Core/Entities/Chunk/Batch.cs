using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Entities;

public readonly struct Batch<T>(List<Snippet<T>> data) : IEnumerable<T>
{
    public Span<T> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => data[index].Span;
    }

    public readonly int ElementCount = data.Sum(static x => x.Length);
    public readonly int SegmentCount = data.Count;

    public ref T GetElement(int elementIndex)
    {
        for (var i = 0; i < data.Count; i++)
        {
            if (data[i].Length > elementIndex)
            {
                return ref data[i].Span[elementIndex];
            }

            elementIndex -= data[i].Length;
        }

        throw new ArgumentOutOfRangeException(nameof(elementIndex));
    }

    public Enumerator GetEnumerator() => new(data);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(List<Snippet<T>> data) : IEnumerator<T>
    {
        private readonly List<Snippet<T>> m_Data = data;

        private int m_SnippetIndex = 0;
        private int m_ElementIndex = 0;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (m_Data[m_SnippetIndex].Length > m_ElementIndex++)
            {
                return true;
            }

            m_ElementIndex = 0;
            return ++m_SnippetIndex < m_Data.Count;
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_SnippetIndex = 0;
            m_ElementIndex = 0;
        }

        public ref T Current => ref m_Data[m_SnippetIndex].Span[m_ElementIndex];

        /// <inheritdoc />
        T IEnumerator<T>.Current => Current;

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() { }
    }
}