using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// The component bytes and their types that describe one new entity, built for
/// <see cref="IEntityCommand.Insert"/> and <see cref="IEntityCommand.InsertAll"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the hand-written half of <c>Components</c>; <c>Components.tt</c> generates the typed <c>Create</c>
/// factories and the interned type lists on top of the buffer and helpers declared here.
/// </para>
/// <para>
/// The payload is opaque on purpose: there is no public constructor and no raw byte entry point, so an insert can
/// only be driven by real component types. Use the <c>Create</c> factories (or the typed <c>Insert</c> extensions,
/// which build the payload for you).
/// </para>
/// <para>
/// It is a <see langword="ref struct"/> holding an inline buffer, so building one allocates nothing and cannot
/// outlive the call. Types are interned per closed generic, so the type list is not reallocated per insert.
/// </para>
/// <para>
/// A payload wider than <see cref="InlineBuffer.Capacity"/> bytes still works: it falls back to an array, so a
/// sixteen-component insert of large components costs one allocation instead of throwing.
/// </para>
/// </remarks>
public unsafe ref partial struct Components
{
    /// <summary>The inline buffer backing every payload.</summary>
    internal unsafe struct InlineBuffer
    {
        /// <summary>Maximum payload size in bytes.</summary>
        public const int Capacity = 512;

        /// <summary>The raw bytes.</summary>
        public fixed byte Data[Capacity];

        /// <summary>Views the buffer as a span without copying it.</summary>
        /// <param name="buffer">The buffer to view.</param>
        /// <param name="length">The number of valid bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> GetSpan(ref InlineBuffer buffer, int length)
            => MemoryMarshal.CreateSpan(ref buffer.Data[0], length);
    }

    private InlineBuffer m_Buffer;
    private byte[]? m_Fallback;
    private int m_Length;
    private Type[]? m_Types;

    /// <summary>Gets the number of valid bytes in the payload.</summary>
    internal readonly int Length => m_Length;

    /// <summary>Gets the component bytes.</summary>
    internal ReadOnlySpan<byte> Bytes
        => m_Fallback is null ? InlineBuffer.GetSpan(ref m_Buffer, m_Length) : m_Fallback.AsSpan(0, m_Length);

    /// <summary>Gets the component types, in payload order.</summary>
    internal readonly ReadOnlySpan<Type> Types => m_Types;

    /// <summary>Gets the interned type array behind <see cref="Types"/>.</summary>
    /// <remarks>
    /// The arrays are interned per closed generic in <see cref="PayloadTypes{T1}"/>, so this reference is a stable
    /// cache key: two payloads with the same component types share it, which lets the repository resolve the
    /// archetype with an identity lookup instead of a scan.
    /// </remarks>
    internal readonly Type[]? TypeArray => m_Types;

    /// <summary>Gets the size of a component type in bytes.</summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The size in bytes.</returns>
    /// <remarks>
    /// <c>Native.GetStackSize&lt;T&gt;()</c> folds to a <c>sizeof</c> constant at the call site, so the generated
    /// <c>Create</c> overloads pay nothing for asking twice per component.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int SizeOf<T>() => (int)Native.GetStackSize<T>();

    /// <summary>Claims the writable bytes of a payload.</summary>
    /// <param name="payload">The payload being built.</param>
    /// <param name="length">The number of bytes the payload needs.</param>
    /// <returns>A span of exactly <paramref name="length"/> bytes.</returns>
    /// <remarks>
    /// Payloads that fit in <see cref="InlineBuffer"/> never allocate. Larger payloads fall back to an array, so a
    /// wide <c>Insert</c> degrades to a single allocation instead of failing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> Reserve(ref Components payload, int length)
    {
        if (length <= InlineBuffer.Capacity)
        {
            return InlineBuffer.GetSpan(ref payload.m_Buffer, length);
        }

        var buffer = new byte[length];
        payload.m_Fallback = buffer;
        return buffer;
    }

    /// <summary>Writes one component value into a payload buffer.</summary>
    /// <param name="buffer">The payload bytes.</param>
    /// <param name="offset">The byte offset to write at.</param>
    /// <param name="value">The component value to write.</param>
    /// <remarks>
    /// Keeps the <c>ref</c> write in exactly one place. <c>MemoryMarshal.Write</c> takes <c>ref T</c> on
    /// netstandard2.1 but <c>in T</c> on current targets, so every generated call site would otherwise raise
    /// CS9191 (hundreds of warnings).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write<T>(Span<byte> buffer, int offset, in T value) where T : unmanaged
    {
        var copy = value;
#pragma warning disable CS9191 // ref vs in: the netstandard2.1 overload has no by-readonly-reference form.
        MemoryMarshal.Write(buffer.Slice(offset), ref copy);
#pragma warning restore CS9191
    }
}
