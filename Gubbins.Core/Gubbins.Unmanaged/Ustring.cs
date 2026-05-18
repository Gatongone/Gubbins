using Gubbins.Unsafe;
using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Span;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Unmanaged;

/// <summary>
/// Unmanaged string.
/// </summary>
public struct Ustring : IDisposable, IEquatable<string>, IEquatable<Ustring>
{
    /// <summary>
    /// Represents an empty <see cref="Ustring"/> instance with no allocated memory and zero length.
    /// This static field provides a reusable empty instance to avoid unnecessary allocations.
    /// </summary>
    public static readonly Ustring Empty = new(IntPtr.Zero, 0);

    /// <summary>
    /// Unmanaged string address.
    /// </summary>
    internal readonly nint Address;

    /// <summary>
    /// Whether the current String is disposed.
    /// </summary>
    private bool m_IsDisposed;

    /// <summary>
    /// Gets the <see cref="char"/> object at a specified position in the current String.
    /// </summary>
    /// <param name="index"></param>
    public unsafe char this[int index]
    {
        get
        {
            var handle = GCHandle.FromIntPtr(Address);
            if (handle.Target is not char[] str) throw new ObjectDisposedException(nameof(Ustring));
            fixed (char* ptr = str)
            {
                return Native.AsRef<char>(ptr + index);
            }
        }
    }

    /// <summary>
    /// String length.
    /// </summary>
    public readonly int Length;

    /// <summary>
    /// Whether the current String is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Length == 0;
    }

    /// <summary>
    /// Checks if the current String is equal to another String.
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !m_IsDisposed && GCHandle.FromIntPtr(Address) is {IsAllocated: true, Target: not null};
    }

    /// <summary>
    /// Whether the current String is invalid or empty.
    /// </summary>
    public bool IsInvalidOrEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsEmpty || !IsValid;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ustring"/> struct with the specified address and length.
    /// </summary>
    /// <param name="address">The unmanaged memory address pointing to the character data.</param>
    /// <param name="length">The number of characters in the string.</param>
    public Ustring(nint address, int length)
    {
        Address = address;
        Length  = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ustring"/> struct from a managed string.
    /// Creates an unmanaged copy of the string data using the shared string pool.
    /// </summary>
    /// <param name="value">The managed string to copy into unmanaged memory.</param>
    public Ustring(string value)
    {
        if (value == null!)
        {
            value = "";
        }

        var str = UstringPool.Shared.Rent(value.Length, out var handle).AsSpan();
        value.AsSpan().CopyTo(str);
        Address = GCHandle.ToIntPtr(handle);
        Length  = value.Length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ustring"/> struct from a span of characters.
    /// Creates an unmanaged copy of the character data using the shared string pool.
    /// </summary>
    /// <param name="value">The span of characters to copy into unmanaged memory.</param>
    public Ustring(Span<char> value)
    {
        var str = UstringPool.Shared.Rent(value.Length, out var handle).AsSpan();
        value.CopyTo(str);
        Address = GCHandle.ToIntPtr(handle);
        Length  = value.Length;
    }

    /// <summary>
    /// Casts a managed string to an unmanaged string.
    /// </summary>
    /// <param name="str">Managed string.</param>
    /// <returns>Unmanaged string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Ustring(string str) => new(str);

    /// <summary>
    /// Casts an unmanaged string to a managed string.
    /// </summary>
    /// <param name="str">Unmanaged string.</param>
    /// <returns>Managed string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Ustring str) => str.ToString();

    /// <summary>
    /// Casts an unmanaged string to a readonly span.
    /// </summary>
    /// <param name="str">Unmanaged string.</param>
    /// <returns>Readonly span from current <see cref="Ustring"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<char>(Ustring str) => str.AsSpan();

    /// <summary>
    /// Casts an unmanaged string to a span.
    /// </summary>
    /// <param name="str">Unmanaged string.</param>
    /// <returns>Span from current <see cref="Ustring"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<char>(Ustring str) => str.AsSpan();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (m_IsDisposed || GCHandle.FromIntPtr(Address).Target is not char[] str)
        {
            return;
        }

        m_IsDisposed = true;
        UstringPool.Shared.Return(str);
    }

    /// <summary>
    /// Create a <see cref="Ustring"/> from current instance with a new unmanaged handle.
    /// </summary>
    /// <returns>New <see cref="Ustring"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ustring Clone()
    {
        var str = UstringPool.Shared.Rent(Length, out var handle).AsSpan();
        AsSpan().CopyTo(str);
        return new Ustring(GCHandle.ToIntPtr(handle), Length);
    }

    /// <summary>
    /// Creates a new span over a portion of the target string from a specified position for a specified number of characters.
    /// </summary>
    /// <returns>The span representation of the string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AsSpan() => GCHandle.FromIntPtr(Address).Target is not char[] str ? Span<char>.Empty : str.AsSpan(0, Length);

    /// <summary>
    /// Creates a new span over a portion of the target string from a specified position to the end of the string.
    /// </summary>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="start"/> is less than 0 or greater than <see cref="Length"/>.
    /// </exception>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <returns>The span representation of the string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AsSpan(int start)
    {
        if (start < 0 || start >= Length) throw new ArgumentOutOfRangeException(nameof(start));
        return GCHandle.FromIntPtr(Address).Target is not char[] str ? Span<char>.Empty : str.AsSpan(start, Length - start);
    }

    /// <summary>
    /// Creates a new span over a string.
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <param name="length">The desired length for the slice.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="start"/>, <paramref name="length"/>, or <paramref name="start"/> + <paramref name="length"/> is not in the range of this string.
    /// </exception>
    /// <returns>The span representation of the string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AsSpan(int start, int length)
    {
        if (start < 0 || start >= Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || length > Length) throw new ArgumentOutOfRangeException(nameof(length));
        return GCHandle.FromIntPtr(Address).Target is not char[] str ? Span<char>.Empty : str.AsSpan(start, length);
    }

    /// <summary>
    /// Creates a new span over a string.
    /// </summary>
    /// <param name="range">The range of characters to include in the span.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="range.Start"/>, <paramref name="range.End - range.Start"/>, or <paramref name="range.Start"/> + <paramref name="range.End - range.Start"/> is not in the range of this string.
    /// </exception>
    /// <returns>The span representation of the string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AsSpan(Range range) => AsSpan(range.Start.Value, range.End.Value - range.Start.Value);

    /// <inheritdoc cref="AsSpan()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsReadOnlySpan() => AsSpan();

    /// <inheritdoc cref="AsSpan(int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsReadOnlySpan(int start) => AsSpan(start);

    /// <inheritdoc cref="AsSpan(int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsReadOnlySpan(int start, int length) => AsSpan(start, length);

    /// <inheritdoc cref="AsSpan(Range)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsReadOnlySpan(Range range) => AsSpan(range.Start.Value, range.End.Value - range.Start.Value);

    /// <summary>
    /// Returns a reference to the first character in the unmanaged string.
    /// </summary>
    /// <returns>First character reference.</returns>
    public unsafe ref char GetPinnableReference()
    {
        var handle = GCHandle.FromIntPtr(Address);
        if (handle.Target is not char[] str) throw new ObjectDisposedException(nameof(Ustring));
        fixed (char* ptr = str)
        {
            return ref Native.AsRef<char>(ptr);
        }
    }

    /// <summary>
    /// Determines whether the current <see cref="Ustring"/> instance is equal to the specified <see cref="ReadOnlySpan{T}"/> of characters.
    /// Uses SIMD-optimized comparison when available, falling back to sequential comparison if not.
    /// </summary>
    /// <param name="other">The <see cref="ReadOnlySpan{T}"/> of characters to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the current instance and <paramref name="other"/> have the same length and contain identical character sequences; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ReadOnlySpan<char> other)
    {
        if (other.Length != Length) return false;
        var left = AsSpan();
        var right = other;
        var operation = SpanOperations.GetOperation<char, ISpanEquals<char>>(SpanOperationMask.Simd);
        return operation?.EqualsAll(left, right) ?? left.SequenceEqual(right);
    }

    /// <summary>
    /// Determines whether the current <see cref="Ustring"/> instance is equal to the specified string.
    /// Uses SIMD-optimized comparison when available, falling back to sequential comparison if not.
    /// </summary>
    /// <param name="other">The string to compare with the current instance. Can be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current instance and <paramref name="other"/> have the same length and contain identical character sequences; otherwise, <see langword="false"/>. Returns <see langword="false"/> if <paramref name="other"/> is <see langword="null"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other)
    {
        if (other?.Length != Length) return false;
        var left = AsSpan();
        var right = other.AsSpan();

        var operation = SpanOperations.GetOperation<char, ISpanEquals<char>>(SpanOperationMask.Simd);
        return operation?.EqualsAll(left, right) ?? left.SequenceEqual(right);
    }

    /// <summary>
    /// Determines whether the current <see cref="Ustring"/> instance is equal to another <see cref="Ustring"/> instance.
    /// Uses SIMD-optimized comparison when available, falling back to sequential comparison if not.
    /// </summary>
    /// <param name="other">The <see cref="Ustring"/> instance to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the current instance and <paramref name="other"/> have the same length and contain identical character sequences; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Ustring other)
    {
        if (other.Length != Length) return false;
        var left = AsSpan();
        var right = other.AsSpan();

        var operation = SpanOperations.GetOperation<char, ISpanEquals<char>>(SpanOperationMask.Simd);
        return operation?.EqualsAll(left, right) ?? left.SequenceEqual(right);
    }

    /// <summary>
    /// Determines whether two instances are equal by comparing a <see cref="Ustring"/> instance with a string.
    /// </summary>
    /// <param name="left">The <see cref="Ustring"/> instance to compare.</param>
    /// <param name="right">The string to compare with the <see cref="Ustring"/> instance.</param>
    /// <returns><see langword="true"/> if the <see cref="Ustring"/> instance and string are equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Ustring left, string right) => left.Equals(right);

    /// <summary>
    /// Determines whether two instances are not equal by comparing a <see cref="Ustring"/> instance with a string.
    /// </summary>
    /// <param name="left">The <see cref="Ustring"/> instance to compare.</param>
    /// <param name="right">The string to compare with the <see cref="Ustring"/> instance.</param>
    /// <returns><see langword="true"/> if the <see cref="Ustring"/> instance and string are not equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Ustring left, string right) => !(left == right);

    /// <summary>
    /// Determines whether two <see cref="Ustring"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="Ustring"/> instance to compare.</param>
    /// <param name="right">The second <see cref="Ustring"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the two <see cref="Ustring"/> instances are equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Ustring left, Ustring right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="Ustring"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="Ustring"/> instance to compare.</param>
    /// <param name="right">The second <see cref="Ustring"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the two <see cref="Ustring"/> instances are not equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Ustring left, Ustring right) => !(left == right);

    /// <summary>
    /// Determines whether two instances are equal by comparing a <see cref="Ustring"/> instance with a <see cref="ReadOnlySpan{T}"/> of characters.
    /// </summary>
    /// <param name="left">The <see cref="Ustring"/> instance to compare.</param>
    /// <param name="right">The <see cref="ReadOnlySpan{T}"/> of characters to compare with the <see cref="Ustring"/> instance.</param>
    /// <returns><see langword="true"/> if the <see cref="Ustring"/> instance and <see cref="ReadOnlySpan{T}"/> are equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Ustring left, ReadOnlySpan<char> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two instances are not equal by comparing a <see cref="Ustring"/> instance with a <see cref="ReadOnlySpan{T}"/> of characters.
    /// </summary>
    /// <param name="left">The <see cref="Ustring"/> instance to compare.</param>
    /// <param name="right">The <see cref="ReadOnlySpan{T}"/> of characters to compare with the <see cref="Ustring"/> instance.</param>
    /// <returns><see langword="true"/> if the <see cref="Ustring"/> instance and <see cref="ReadOnlySpan{T}"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Ustring left, ReadOnlySpan<char> right) => !(left == right);

    /// <summary>
    /// Determines whether two instances are not equal by comparing a <see cref="ReadOnlySpan{T}"/> of characters with a <see cref="Ustring"/> instance.
    /// </summary>
    /// <param name="left">The <see cref="ReadOnlySpan{T}"/> of characters to compare.</param>
    /// <param name="right">The <see cref="Ustring"/> instance to compare with the <see cref="ReadOnlySpan{T}"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="ReadOnlySpan{T}"/> and <see cref="Ustring"/> instance are not equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ReadOnlySpan<char> left, Ustring right) => !(left == right);

    /// <summary>
    /// Determines whether two instances are equal by comparing a <see cref="ReadOnlySpan{T}"/> of characters with a <see cref="Ustring"/> instance.
    /// </summary>
    /// <param name="left">The <see cref="ReadOnlySpan{T}"/> of characters to compare.</param>
    /// <param name="right">The <see cref="Ustring"/> instance to compare with the <see cref="ReadOnlySpan{T}"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="ReadOnlySpan{T}"/> and <see cref="Ustring"/> instance are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ReadOnlySpan<char> left, Ustring right) => right.Equals(left);

    /// <summary>
    /// Concatenates two <see cref="Ustring"/> instances into a new <see cref="Ustring"/> instance.
    /// </summary>
    /// <param name="left">The first <see cref="Ustring"/> instance to concatenate.</param>
    /// <param name="right">The second <see cref="Ustring"/> instance to concatenate.</param>
    /// <returns>A new <see cref="Ustring"/> instance that contains the concatenated character sequences from both operands.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring operator +(Ustring left, Ustring right) => left.Concat(right.AsSpan());

    /// <summary>
    /// Concatenates a <see cref="Ustring"/> instance with a <see cref="ReadOnlySpan{T}"/> of characters into a new <see cref="Ustring"/> instance.
    /// </summary>
    /// <param name="left">The <see cref="Ustring"/> instance to concatenate.</param>
    /// <param name="right">The <see cref="ReadOnlySpan{T}"/> of characters to concatenate.</param>
    /// <returns>A new <see cref="Ustring"/> instance that contains the concatenated character sequences from both operands.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring operator +(Ustring left, ReadOnlySpan<char> right) => left.Concat(right);

    /// <summary>
    /// Concatenates a string with a <see cref="Ustring"/> instance into a new string.
    /// </summary>
    /// <param name="left">The string to concatenate.</param>
    /// <param name="right">The <see cref="Ustring"/> instance to concatenate.</param>
    /// <returns>A new string that contains the concatenated character sequences from both operands.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string operator +(string left, Ustring right) => string.Concat(left, right.ToString());

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => GCHandle.FromIntPtr(Address).Target is not char[] str ? string.Empty : new string(str, 0, Length);

    /// <summary>Concatenates an array of strings, using the specified separator between each member.</summary>
    /// <param name="separator">The character to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="value" /> has more than one element.</param>
    /// <param name="values">An array of strings to concatenate.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.OutOfMemoryException">The length of the resulting string overflows the maximum allowed length (<see cref="F:System.Int32.MaxValue" />).</exception>
    /// <returns
    /// >A string that consists of the elements of <paramref name="values" /> delimited by the <paramref name="separator" /> character.
    /// -or-
    /// <see cref="F:System.String.Empty" /> if <paramref name="values" /> has zero elements.
    /// </returns>
    public static Ustring Join(ReadOnlySpan<char> separator, params object[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(values[i].ToString());
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, object[])" />
    /// <remarks>Dispose all  after joining.</remarks>
    public static Ustring Join(ReadOnlySpan<char> separator, params Ustring[] values) => Join(separator, true, values);

    /// <summary>Concatenates an array of strings, using the specified separator between each member.</summary>
    /// <param name="separator">The character to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="value" /> has more than one element.</param>
    /// <param name="disposeAll">Dispose all elements in <paramref name="values"/> after Joining.</param>
    /// <param name="values">An array of strings to concatenate.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.OutOfMemoryException">The length of the resulting string overflows the maximum allowed length (<see cref="F:System.Int32.MaxValue" />).</exception>
    /// <returns
    /// >A string that consists of the elements of <paramref name="values" /> delimited by the <paramref name="separator" /> character.
    /// -or-
    /// <see cref="F:System.String.Empty" /> if <paramref name="values" /> has zero elements.
    /// </returns>
    public static Ustring Join(ReadOnlySpan<char> separator, bool disposeAll, params Ustring[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(value);
            if (disposeAll)
            {
                value.Dispose();
            }
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, object[])" />
    public static Ustring Join<T>(ReadOnlySpan<char> separator, params T[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(values[i]);
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, object[])" />
    public static Ustring Join(ReadOnlySpan<char> separator, params string[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(values[i]);
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, object[])" />
    public static Ustring Join(char separator, params object[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(values[i].ToString());
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, object[])" />
    public static Ustring Join(char separator, params string[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(values[i]);
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, ReadOnlySpan{Ustring})" />
    public static Ustring Join(char separator, params Ustring[] values) => Join(separator, true, values);

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, bool, Ustring[])" />
    public static Ustring Join(char separator, bool disposeAll, params Ustring[] values)
    {
        var builder = new UstringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (i > 0)
            {
                builder.Append(separator);
            }

            builder.Append(value);
            if (disposeAll)
            {
                value.Dispose();
            }
        }

        return builder.ToChars();
    }

    /// <summary>
    /// Concat strings.
    /// </summary>
    /// <param name="strings">Concat targets.</param>
    /// <returns>Conceited string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Concat(params ReadOnlySpan<Ustring> strings) => Concat(true, strings);

    /// <summary>
    /// Concat strings.
    /// </summary>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <param name="strings">Concat targets.</param>
    /// <returns>Conceited string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Concat(bool disposeOrigin, params ReadOnlySpan<Ustring> strings)
    {
        using var builder = new UstringBuilder();
        if (!disposeOrigin)
        {
            for (var index = 0; index < strings.Length; index++)
            {
                builder.Append(strings[index].AsSpan());
            }
        }
        else
        {
            for (var index = 0; index < strings.Length; index++)
            {
                var str = strings[index];
                builder.Append(str.AsSpan());
                str.Dispose();
            }
        }

        return builder.ToChars();
    }

    /// <summary>
    /// Concat strings.
    /// </summary>
    /// <param name="strings">Concat targets.</param>
    /// <returns>Conceited string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Concat(params ReadOnlySpan<char> strings)
    {
        using var builder = new UstringBuilder();

        for (var index = 0; index < strings.Length; index++)
        {
            var str = strings[index];
            builder.Append(str);
        }

        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T>(string format, T arg)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2>(string format, T1 arg1, T2 arg2)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        return builder.ToChars();
    }

    /// <inheritdoc cref="string.Format(string, object[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
    {
        var builder = new UstringBuilder();
        builder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        return builder.ToChars();
    }
}

/// <inheritdoc cref="System.Text.StringBuilder"/>
public ref struct UstringBuilder() : IDisposable
{
    private UstringWriter m_Writer = new();

    /// <summary>
    /// Converts the value of this instance to a <see cref="Ustring"/>.
    /// </summary>
    /// <returns>A string whose value is the same as this instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ustring ToChars(bool dispose = true) => m_Writer.Flush(dispose);

    /// <inheritdoc cref="System.Text.StringBuilder.ToString()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(bool dispose) => ToChars(dispose);

    /// <inheritdoc cref="System.Text.StringBuilder.ToString()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => ToChars();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => m_Writer.Dispose();

    /// <inheritdoc cref="System.Text.StringBuilder.Append(string)"/>
    public void Append<T>(T value) => Parse(value);

    /// <inheritdoc cref="System.Text.StringBuilder.Append(string)"/>
    public void Append(char value) => m_Writer.Write(value);

    /// <inheritdoc cref="System.Text.StringBuilder.Append(string)"/>
    public void Append(ReadOnlySpan<char> value) => m_Writer.Write(value);

    /// <inheritdoc cref="System.Text.StringBuilder.Append(string)"/>
    public void AppendLine(ReadOnlySpan<char> value)
    {
        m_Writer.Write(value);
        m_Writer.Write('\n');
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T>(ReadOnlySpan<char> format, T arg)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '0' && i + 2 < format.Length && format[i + 2] == '}')
                {
                    Parse(arg);
                    i += 2;
                }
                else if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2>(ReadOnlySpan<char> format, T1 arg1, T2 arg2)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else if (i + 3 < format.Length && format[i + 3] == '}')
                {
                    if (next == '1' && format[i + 2] == '0')
                    {
                        Parse(arg11);
                        i += 3;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else if (i + 3 < format.Length && format[i + 3] == '}')
                {
                    if (next == '1' && format[i + 2] == '0')
                    {
                        Parse(arg11);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '1')
                    {
                        Parse(arg12);
                        i += 3;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else if (i + 3 < format.Length && format[i + 3] == '}')
                {
                    if (next == '1' && format[i + 2] == '0')
                    {
                        Parse(arg11);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '1')
                    {
                        Parse(arg12);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '2')
                    {
                        Parse(arg13);
                        i += 3;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else if (i + 3 < format.Length && format[i + 3] == '}')
                {
                    if (next == '1' && format[i + 2] == '0')
                    {
                        Parse(arg11);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '1')
                    {
                        Parse(arg12);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '2')
                    {
                        Parse(arg13);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '3')
                    {
                        Parse(arg14);
                        i += 3;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else if (i + 3 < format.Length && format[i + 3] == '}')
                {
                    if (next == '1' && format[i + 2] == '0')
                    {
                        Parse(arg11);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '1')
                    {
                        Parse(arg12);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '2')
                    {
                        Parse(arg13);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '3')
                    {
                        Parse(arg14);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '4')
                    {
                        Parse(arg15);
                        i += 3;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <inheritdoc cref="System.Text.StringBuilder.AppendFormat(string, object[])"/>
    public void AppendFormat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ReadOnlySpan<char> format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{' && i + 1 < format.Length)
            {
                var next = format[i + 1];
                if (next == '{')
                {
                    m_Writer.Write('{');
                    i++;
                }
                else if (i + 2 < format.Length && format[i + 2] == '}')
                {
                    if (next == '0')
                    {
                        Parse(arg1);
                        i += 2;
                    }
                    else if (next == '1')
                    {
                        Parse(arg2);
                        i += 2;
                    }
                    else if (next == '2')
                    {
                        Parse(arg3);
                        i += 2;
                    }
                    else if (next == '3')
                    {
                        Parse(arg4);
                        i += 2;
                    }
                    else if (next == '4')
                    {
                        Parse(arg5);
                        i += 2;
                    }
                    else if (next == '5')
                    {
                        Parse(arg6);
                        i += 2;
                    }
                    else if (next == '6')
                    {
                        Parse(arg7);
                        i += 2;
                    }
                    else if (next == '7')
                    {
                        Parse(arg8);
                        i += 2;
                    }
                    else if (next == '8')
                    {
                        Parse(arg9);
                        i += 2;
                    }
                    else if (next == '9')
                    {
                        Parse(arg10);
                        i += 2;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else if (i + 3 < format.Length && format[i + 3] == '}')
                {
                    if (next == '1' && format[i + 2] == '0')
                    {
                        Parse(arg11);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '1')
                    {
                        Parse(arg12);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '2')
                    {
                        Parse(arg13);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '3')
                    {
                        Parse(arg14);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '4')
                    {
                        Parse(arg15);
                        i += 3;
                    }
                    else if (next == '1' && format[i + 2] == '5')
                    {
                        Parse(arg16);
                        i += 3;
                    }
                    else
                    {
                        m_Writer.Write(c);
                    }
                }
                else
                {
                    m_Writer.Write(c);
                }
            }
            else if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
            {
                m_Writer.Write('}');
                i++;
            }
            else
            {
                m_Writer.Write(c);
            }
        }
    }

    /// <summary>
    /// Parse <paramref name="arg"/> as string and write result to <see cref="m_Writer"/>.
    /// </summary>
    /// <param name="arg">The object to parse.</param>
    /// <typeparam name="T">Type of <paramref name="arg"/>.</typeparam>
    private unsafe void Parse<T>(T arg)
    {
        if (arg == null)
        {
            m_Writer.Write("null");
        }
        else if (arg is string str)
        {
            m_Writer.Write(str);
        }
        else if (arg is Ustring chars)
        {
            m_Writer.Write(chars);
        }
        else if (arg is char character)
        {
            m_Writer.Write(character);
        }
        else if (arg is bool boolean)
        {
            m_Writer.Write(boolean ? "true" : "false");
        }
#pragma warning disable CS9080 // Use of variable in this context may expose referenced variables outside of their declaration scope
        else if (arg is DateTime dateTime)
        {
            // ISO 8601 format: "yyyy-MM-ddTHH:mm:ss.fffffffZ" = 28 chars + buffer
            Span<char> result = stackalloc char[33];
            if (dateTime.TryFormat(result, out var charsWritten, "O")) // "O" is the round-trip format
            {
                m_Writer.Write(result[..charsWritten]);
            }
            else
            {
                // Fallback to ToString if TryFormat fails
                m_Writer.Write(dateTime.ToString("O"));
            }
        }
        else if (arg is byte byteValue)
        {
            Span<char> result = stackalloc char[Number.MaxInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(byteValue, dest)]);
            }
        }
        else if (arg is sbyte sbyteValue)
        {
            Span<char> result = stackalloc char[Number.MaxUInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(sbyteValue, dest)]);
            }
        }
        else if (arg is short shortValue)
        {
            Span<char> result = stackalloc char[Number.MaxInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(shortValue, dest)]);
            }
        }
        else if (arg is ushort ushortValue)
        {
            Span<char> result = stackalloc char[Number.MaxUInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(ushortValue, dest)]);
            }
        }
        else if (arg is int intValue)
        {
            Span<char> result = stackalloc char[Number.MaxInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(intValue, dest)]);
            }
        }
        else if (arg is uint uintValue)
        {
            Span<char> result = stackalloc char[Number.MaxUInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(uintValue, dest)]);
            }
        }
        else if (arg is long longValue)
        {
            Span<char> result = stackalloc char[Number.MaxInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(longValue, dest)]);
            }
        }
        else if (arg is uint ulongValue)
        {
            Span<char> result = stackalloc char[Number.MaxUInt64StringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(ulongValue, dest)]);
            }
        }
        else if (arg is float floatValue)
        {
            Span<char> result = stackalloc char[Number.MaxSingleStringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(floatValue, dest)]);
            }
        }
        else if (arg is double doubleValue)
        {
            Span<char> result = stackalloc char[Number.MaxDoubleStringLength];
            fixed (char* dest = result)
            {
                m_Writer.Write(result[..Number.WriteString(doubleValue, dest)]);
            }
        }
        else if (arg is decimal decimalValue)
        {
            Span<char> result = stackalloc char[Number.MaxDecimalStringLength];
            if (decimalValue.TryFormat(result, out var digits))
            {
                m_Writer.Write(result[..digits]);
            }
            else
            {
                m_Writer.Write(decimalValue.ToString(CultureInfo.InvariantCulture));
            }
        }
#pragma warning restore CS9080 // Use of variable in this context may expose referenced variables outside of their declaration scope
        else
        {
            m_Writer.Write(arg.ToString());
        }
    }
}

/// <summary>
/// Extension methods for Chars.
/// </summary>
public static unsafe class UstringExtensions
{
    /// <summary>
    /// Converts a managed string to an unmanaged string.
    /// </summary>
    /// <param name="str">Managed string.</param>
    /// <returns>Unmanaged string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring ToUstring(this string str) => new(str);

    /// <summary>
    /// Converts a <see cref="Ustring"/> to a <see cref="Uarray{T}"/> of characters.
    /// </summary>
    /// <param name="str">The <see cref="Ustring"/> to convert.</param>
    /// <param name="disposeOrigin">
    /// <see langword="true"/> to dispose the original <see cref="Ustring"/> after conversion; 
    /// otherwise, <see langword="false"/>. Default is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="Uarray{T}"/> of characters containing the same character data as the input <see cref="Ustring"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Uarray<char> ToUarray(this ref Ustring str, bool disposeOrigin = true)
    {
        var result = new Uarray<char>(str);
        if (disposeOrigin)
        {
            str.Dispose();
        }

        return result;
    }

    /// <summary>
    /// Converts a <see cref="Uarray{T}"/> of characters to a <see cref="Ustring"/>.
    /// </summary>
    /// <param name="array">The <see cref="Uarray{T}"/> of characters to convert.</param>
    /// <param name="disposeOrigin">
    /// <see langword="true"/> to dispose the original <see cref="Uarray{T}"/> after conversion; 
    /// otherwise, <see langword="false"/>. Default is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="Ustring"/> containing the same character data as the input <see cref="Uarray{T}"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring ToUstring(this Uarray<char> array, bool disposeOrigin = true)
    {
        var result = new Ustring(array);
        if (disposeOrigin)
        {
            array.Dispose();
        }

        return result;
    }

    /// <summary>
    /// Returns a new string that right-aligns the characters in this instance
    /// by padding them with spaces on the left, for a specified total length.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="totalWidth">
    /// The number of characters in the resulting string, equal to the
    /// number of original characters plus any additional padding characters.
    /// </param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// A new string that is equivalent to this instance, but right-aligned and padded on the left with
    /// as many <paramref name="paddingChar" /> characters as needed to create a length of <paramref name="totalWidth" />.
    /// However, if <paramref name="totalWidth" /> is less than the length of this instance, the method returns a reference
    /// to the existing instance. If <paramref name="totalWidth" /> is equal to the length of this instance,
    /// the method returns a new string that is identical to this instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring PadLeft(this ref Ustring str, int totalWidth, bool disposeOrigin = true) => str.PadLeft(totalWidth, ' ', disposeOrigin);

    /// <summary>
    /// Returns a new string that right-aligns the characters in this instance by padding them on the left
    /// with a specified Unicode character, for a specified total length.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="totalWidth">
    /// The number of characters in the resulting string, equal to the
    /// number of original characters plus any additional padding characters.
    /// </param>
    /// <param name="paddingChar">A Unicode padding character.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// A new string that is equivalent to this instance, but right-aligned and padded on the left with
    /// as many <paramref name="paddingChar" /> characters as needed to create a length of <paramref name="totalWidth" />.
    /// However, if <paramref name="totalWidth" /> is less than the length of this instance, the method returns a reference
    /// to the existing instance. If <paramref name="totalWidth" /> is equal to the length of this instance,
    /// the method returns a new string that is identical to this instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring PadLeft(this ref Ustring str, int totalWidth, char paddingChar, bool disposeOrigin = true)
    {
        var length = str.Length + totalWidth;
        var result = UstringPool.Shared.Rent(length, out var handle).AsSpan();
        Native.CopyMemory((void*) str.Address, (char*) Native.GetFirstElementAddress(result) + totalWidth, (uint) str.Length * sizeof(char));
        fixed (char* ptr = str)
        {
            SetChars(ptr, totalWidth, paddingChar);
        }

        if (disposeOrigin) str.Dispose();
        return new Ustring(GCHandle.ToIntPtr(handle), length);
    }

    /// <summary>
    /// Returns a new string that left-aligns the characters in this string by padding them with spaces on the right, for a specified total length.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="totalWidth">
    /// The number of characters in the resulting string, equal to the
    /// number of original characters plus any additional padding characters.
    /// </param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// A new string that is equivalent to this instance, but left-aligned and padded on the right with
    /// as many spaces as needed to create a length of <paramref name="totalWidth" />.
    /// However, if <paramref name="totalWidth" /> is less than the length of this instance, the method returns a reference
    /// to the existing instance. If <paramref name="totalWidth" /> is equal to the length of this instance,
    /// the method returns a new string that is identical to this instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring PadRight(this ref Ustring str, int totalWidth, bool disposeOrigin = true) => str.PadRight(totalWidth, ' ', disposeOrigin);

    /// <summary>
    /// Returns a new string that left-aligns the characters in this string by padding them on the right
    /// with a specified Unicode character, for a specified total length.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="totalWidth">
    /// The number of characters in the resulting string, equal to the number of
    /// original characters plus any additional padding characters.
    /// </param>
    /// <param name="paddingChar">A Unicode padding character.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// A new string that is equivalent to this instance, but left-aligned and padded on the right with
    /// as many <paramref name="paddingChar" /> characters as needed to create a length of <paramref name="totalWidth" />.
    /// However, if <paramref name="totalWidth" /> is less than the length of this instance, the method returns a reference
    /// to the existing instance. If <paramref name="totalWidth" /> is equal to the length of this instance,
    /// the method returns a new string that is identical to this instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring PadRight(this ref Ustring str, int totalWidth, char paddingChar, bool disposeOrigin = true)
    {
        var length = str.Length + totalWidth;
        var result = UstringPool.Shared.Rent(length, out var handle).AsSpan();
        Native.CopyMemory((void*) str.Address, (char*) Native.GetFirstElementAddress(result), (uint) str.Length * sizeof(char));
        fixed (char* ptr = str)
        {
            SetChars(ptr + str.Length, totalWidth, paddingChar);
        }

        if (disposeOrigin) str.Dispose();
        return new Ustring(GCHandle.ToIntPtr(handle), length);
    }

    /// <summary>
    /// Retrieves a substring from this instance. The substring starts at a specified character position and continues to the end of the string.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> is less than zero or greater than the length of this instance.</exception>
    /// <returns>A string that is equivalent to the substring that begins at <paramref name="startIndex" /> in this instance, or <see cref="F:System.String.Empty" /> if <paramref name="startIndex" /> is equal to the length of this instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Substring(this ref Ustring str, int startIndex, bool disposeOrigin = true) => str.Substring(startIndex, str.Length - startIndex, disposeOrigin);

    /// <summary>
    /// Retrieves a substring from this instance. The substring starts at a specified character position and has a specified length.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
    /// <param name="length">The number of characters in the substring.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> plus <paramref name="length" /> indicates a position not within this instance.
    /// -or-
    /// <paramref name="startIndex" /> or <paramref name="length" /> is less than zero.</exception>
    /// <returns>A string that is equivalent to the substring of length <paramref name="length" /> that begins at <paramref name="startIndex" /> in this instance, or <see cref="F:System.String.Empty" /> if <paramref name="startIndex" /> is equal to the length of this instance and <paramref name="length" /> is zero.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Substring(this ref Ustring str, int startIndex, int length, bool disposeOrigin = true)
    {
        if (startIndex < 0 || startIndex > str.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index is out of range.");
        }

        if (startIndex + length > str.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length is out of range.");
        }

        var result = UstringPool.Shared.Rent(length, out var handle).AsSpan();
        str.AsSpan(startIndex, length).CopyTo(result);
        if (disposeOrigin) str.Dispose();
        return new Ustring(GCHandle.ToIntPtr(handle), length);
    }

    /// <summary>Removes all leading and trailing white-space characters from the current string.</summary>
    /// <param name="str">Origin string.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// The string that remains after all white-space characters are removed from the start and end of the current string.
    /// If no characters can be trimmed from the current instance, the method returns the current instance unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Trim(this ref Ustring str, bool disposeOrigin = true)
    {
        var length = str.Length;
        fixed (char* chars = str)
        {
            var ptr = chars;
            while (length > 0 &&
                IsWhiteSpace(*ptr))
            {
                ++ptr;
                --length;
            }

            while (length > 0 &&
                IsWhiteSpace(ptr[length - 1]))
            {
                --length;
            }

            var result = new Ustring(new Span<char>(ptr, length));
            if (disposeOrigin) str.Dispose();
            return result;
        }
    }

    /// <summary>Removes all leading and trailing instances of a character from the current string.</summary>
    /// <param name="str">Origin string.</param>
    /// <param name="trimChar">A Unicode character to remove.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// The string that remains after all instances of the <paramref name="trimChar" /> character are removed from the start and end of the current string.
    /// If no characters can be trimmed from the current instance, the method returns the current instance unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Trim(this ref Ustring str, char trimChar, bool disposeOrigin = true)
    {
        var length = str.Length;
        fixed (char* chars = str)
        {
            var ptr = chars;
            while (length > 0 &&
                *ptr == trimChar)
            {
                ++ptr;
                --length;
            }

            while (length > 0 &&
                ptr[length - 1] == trimChar)
            {
                --length;
            }

            var result = new Ustring(new Span<char>(ptr, length));
            if (disposeOrigin) str.Dispose();
            return result;
        }
    }

    /// <summary>Removes all the leading white-space characters from the current string.</summary>
    /// <param name="str">Origin string.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// The string that remains after all white-space characters are removed from the start of the current string.
    /// If no characters can be trimmed from the current instance, the method returns the current instance unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring TrimStart(this ref Ustring str, bool disposeOrigin = true)
    {
        var length = str.Length;
        fixed (char* chars = str)
        {
            var ptr = chars;
            while (length > 0 &&
                IsWhiteSpace(*ptr))
            {
                ++ptr;
                --length;
            }

            var result = new Ustring(new Span<char>(ptr, length));
            if (disposeOrigin) str.Dispose();
            return result;
        }
    }

    /// <summary>Removes all the leading occurrences of a specified character from the current string.</summary>
    /// <param name="str">Origin string.</param>
    /// <param name="trimChar">A Unicode character to remove.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// The string that remains after all occurrences of the <paramref name="trimChar" /> character are removed from the start of
    /// the current string. If no characters can be trimmed from the current instance, the method returns the current instance unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring TrimStart(this ref Ustring str, char trimChar, bool disposeOrigin = true)
    {
        var length = str.Length;
        fixed (char* chars = str)
        {
            var ptr = chars;
            while (length > 0 &&
                *ptr == trimChar)
            {
                ++ptr;
                --length;
            }

            var result = new Ustring(new Span<char>(ptr, length));
            if (disposeOrigin) str.Dispose();
            return result;
        }
    }

    /// <summary>Removes all the trailing white-space characters from the current string.</summary>
    /// <param name="str">Origin string.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// The string that remains after all white-space characters are removed from the end of
    /// the current string. If no characters can be trimmed from the current instance, the method returns the current instance unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring TrimEnd(this ref Ustring str, bool disposeOrigin = true)
    {
        var length = str.Length;
        fixed (char* chars = str)
        {
            var ptr = chars;

            while (length > 0 &&
                IsWhiteSpace(ptr[length - 1]))
            {
                --length;
            }

            var result = new Ustring(new Span<char>(ptr, length));
            if (disposeOrigin) str.Dispose();
            return result;
        }
    }

    /// <summary>Removes all the trailing occurrences of a character from the current string.</summary>
    /// <param name="str">Origin string.</param>
    /// <param name="trimChar">A Unicode character to remove.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>
    /// The string that remains after all occurrences of the <paramref name="trimChar" /> character are removed from the end of
    /// the current string. If no characters can be trimmed from the current instance, the method returns the current instance unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring TirmEnd(this ref Ustring str, char trimChar, bool disposeOrigin = true)
    {
        var length = str.Length;
        fixed (char* chars = str)
        {
            var ptr = chars;

            while (length > 0 &&
                ptr[length - 1] == trimChar)
            {
                --length;
            }

            var result = new Ustring(new Span<char>(ptr, length));
            if (disposeOrigin) str.Dispose();
            return result;
        }
    }

    /// <summary>
    /// Concat strings.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="others">Concat target.</param>
    /// <returns>Conceited string.</returns>
    public static Ustring Concat(this ref Ustring str, params ReadOnlySpan<string> others) => str.Concat(true, others);

    /// <summary>
    /// Concat strings.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <param name="others">Concat target.</param>
    /// <returns>Conceited string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Concat(this ref Ustring str, bool disposeOrigin, params ReadOnlySpan<string> others)
    {
        using var builder = new UstringBuilder();
        builder.Append(str);
        for (var index = 0; index < others.Length; index++)
        {
            builder.Append(others[index]);
        }

        if (disposeOrigin) str.Dispose();
        return builder.ToChars();
    }

    /// <inheritdoc cref="Concat(Ustring, string, bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Concat(this ref Ustring str, string other, bool disposeOrigin = true) => str.Concat(other.AsSpan(), disposeOrigin);

    /// <summary>
    /// Concat strings.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="other">Concat target.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>Conceited string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring Concat(this ref Ustring str, ReadOnlySpan<char> other, bool disposeOrigin = true)
    {
        using var builder = new UstringBuilder();
        builder.Append(str);
        builder.Append(other);
        if (disposeOrigin) str.Dispose();
        return builder.ToChars();
    }

    /// <summary>
    /// Returns a copy of this string converted to uppercase.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>The uppercase equivalent of the current string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring ToUpper(this ref Ustring str, bool disposeOrigin = true)
    {
        var result = UstringPool.Shared.Rent(str.Length, out var handle).AsSpan();
        fixed (char* ptr = str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                result[i] = ToUpper(ptr[i]);
            }
        }

        if (disposeOrigin) str.Dispose();
        return new Ustring(GCHandle.ToIntPtr(handle), str.Length);
    }

    /// <summary>
    /// Returns a copy of this string converted to lowercase.
    /// </summary>
    /// <param name="str">Origin string.</param>
    /// <param name="disposeOrigin">True then dispose origin <see cref="Ustring"/>.</param>
    /// <returns>A string in lowercase.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring ToLower(this ref Ustring str, bool disposeOrigin = true)
    {
        var result = UstringPool.Shared.Rent(str.Length, out var handle).AsSpan();
        fixed (char* ptr = str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                result[i] = ToLower(ptr[i]);
            }
        }

        if (disposeOrigin) str.Dispose();
        return new Ustring(GCHandle.ToIntPtr(handle), str.Length);
    }

    /// <summary>
    /// Returns a new string in which all occurrences of a specified character in the current instance are replaced with another specified character.
    /// </summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="oldChar">The character to be replaced.</param>
    /// <param name="newChar">The character to replace all occurrences of <paramref name="oldChar"/>.</param>
    /// <param name="disposeOrigin">True to dispose the original <see cref="Ustring"/> instance after replacement; otherwise, false.</param>
    /// <returns>
    /// A new <see cref="Ustring"/> instance that is equivalent to the current string except that all instances of
    /// <paramref name="oldChar"/> are replaced with <paramref name="newChar"/>.
    /// </returns>
    public static Ustring Replace(this ref Ustring str, char oldChar, char newChar, bool disposeOrigin = true)
    {
        var result = UstringPool.Shared.Rent(str.Length, out var handle).AsSpan();
        fixed (char* ptr = str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                var cur = ptr[i];
                result[i] = cur == oldChar ? newChar : cur;
            }
        }

        if (disposeOrigin) str.Dispose();
        return new Ustring(GCHandle.ToIntPtr(handle), str.Length);
    }

    /// <summary>
    /// Returns a new string in which all occurrences of a specified character sequence in the current instance are replaced with another specified character sequence.
    /// </summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="oldChar">The character sequence to be replaced.</param>
    /// <param name="newChar">The character sequence to replace all occurrences of <paramref name="oldChar"/>.</param>
    /// <param name="disposeOrigin">True to dispose the original <see cref="Ustring"/> instance after replacement; otherwise, false.</param>
    /// <returns>
    /// A new <see cref="Ustring"/> instance that is equivalent to the current string except that all instances of
    /// <paramref name="oldChar"/> are replaced with <paramref name="newChar"/>.
    /// </returns>
    public static Ustring Replace(this ref Ustring str, ReadOnlySpan<char> oldChar, ReadOnlySpan<char> newChar, bool disposeOrigin = true)
    {
        var operation = SpanOperations.GetOperation<char, ISpanEquals<char>>(SpanOperationMask.Simd);
        using var builder = new UstringBuilder();
        var text = str.AsSpan();
        var start = oldChar[0];

        for (var i = 0; i < str.Length; i++)
        {
            var cur = text[i];
            if (cur != start)
            {
                builder.Append(cur);
                continue;
            }

            var isMatch = true;
            if (i + oldChar.Length <= text.Length)
            {
                if (operation != null)
                {
                    isMatch = operation.EqualsAll(str.AsSpan().Slice(i, oldChar.Length), oldChar);
                }
                else
                {
                    for (var j = 1; j < oldChar.Length; j++)
                    {
                        if (text[i + j] != oldChar[j])
                        {
                            isMatch = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                isMatch = false;
            }

            if (isMatch)
            {
                builder.Append(newChar);
                i += oldChar.Length - 1;
            }
            else
            {
                builder.Append(cur);
            }
        }

        if (disposeOrigin) str.Dispose();
        return builder.ToChars();
    }

    /// <summary>Returns a value indicating whether a specified character occurs within this string.</summary>
    /// <param name="str">The original string.</param>
    /// <param name="pattern">The character to seek.</param>
    /// <returns>
    /// <see langword="true" /> if the <paramref name="pattern" /> parameter occurs within this string; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this ref Ustring str, char pattern) => str.IndexOf(pattern) >= 0;

    /// <summary>Returns a value indicating whether a specified string occurs within this string, using the specified comparison rules.</summary>
    /// <param name="str">The original string.</param>
    /// <param name="pattern">The string to seek.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns>
    /// <see langword="true" /> if the <paramref name="pattern" /> parameter occurs within this string, or if <paramref name="pattern" /> is the empty string (""); otherwise, <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this ref Ustring str, ReadOnlySpan<char> pattern, StringComparison comparisonType = StringComparison.Ordinal) => str.IndexOf(pattern, comparisonType) >= 0;

    /// <summary>Reports the zero-based index of the first occurrence of the specified Unicode character in this string.</summary>
    /// <param name="str">The original string.</param>
    /// <param name="pattern">A Unicode character to seek.</param>
    /// <returns>The zero-based index position of <paramref name="value" /> if that character is found, or -1 if it is not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf(this ref Ustring str, char pattern) => str.AsReadOnlySpan().IndexOf(pattern);

    /// <summary>
    /// Reports the zero-based index of the first occurrence in this instance of any character in a specified array of Unicode characters.
    /// Searches for the first index of any of the specified values similar to calling IndexOf several times with the logical OR operator.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="anyOf">A Unicode character array containing one or more characters to seek.</param>
    /// <returns>The zero-based index position of the first occurrence in this instance where any character in <paramref name="anyOf" /> was found; -1 if no character in <paramref name="anyOf" /> was found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfAny(this ref Ustring str, params ReadOnlySpan<char> anyOf) => str.AsReadOnlySpan().IndexOfAny(anyOf);

    /// <summary>Reports the zero-based index of the first occurrence of the specified string in the current <see cref="T:System.String" /> object. A parameter specifies the type of search to use for the specified string.</summary>
    /// <param name="str">The original string.</param>
    /// <param name="pattern">The string to seek.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="pattern" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="comparisonType" /> is not a valid <see cref="T:System.StringComparison" /> value.</exception>
    /// <returns>The index position of the <paramref name="pattern" /> parameter if that string is found, or -1 if it is not. If <paramref name="pattern" /> is <see cref="F:System.String.Empty" />, the return value is 0.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf(this ref Ustring str, ReadOnlySpan<char> pattern, StringComparison comparisonType = StringComparison.Ordinal) => str.AsReadOnlySpan().IndexOf(pattern, comparisonType);

    /// <summary>Reports the zero-based index position of the last occurrence of a specified Unicode character within this instance.</summary>
    /// <param name="str">The original string.</param>
    /// <param name="pattern">The Unicode character to seek.</param>
    /// <returns>The zero-based index position of <paramref name="pattern" /> if that character is found, or -1 if it is not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOf(this ref Ustring str, char pattern) => str.AsReadOnlySpan().LastIndexOf(pattern);

#if NETCOREAPP3_0_OR_GREATER
    /// <summary>Reports the zero-based index of the last occurrence of a specified string within the current <see cref="T:System.String" /> object. A parameter specifies the type of search to use for the specified string.</summary>
    /// <param name="str"></param>
    /// <param name="pattern">The string to seek.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="pattern" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="comparisonType" /> is not a valid <see cref="T:System.StringComparison" /> value.</exception>
    /// <returns>The zero-based starting index position of the <paramref name="pattern" /> parameter if that string is found, or -1 if it is not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOf(this ref Ustring str, ReadOnlySpan<char> pattern, StringComparison comparisonType) => str.AsReadOnlySpan().LastIndexOf(pattern, comparisonType);
#endif

    /// <summary>Reports the zero-based index of the last occurrence of a specified string within the current <see cref="T:System.String" /> object. A parameter specifies the type of search to use for the specified string.</summary>
    /// <param name="str"></param>
    /// <param name="pattern">The string to seek.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="pattern" /> is <see langword="null" />.</exception>
    /// <returns>The zero-based starting index position of the <paramref name="pattern" /> parameter if that string is found, or -1 if it is not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOf(this ref Ustring str, ReadOnlySpan<char> pattern) => str.AsReadOnlySpan().LastIndexOf(pattern);

    /// <summary>Reports the zero-based index position of the last occurrence in this instance of one or more characters specified in a Unicode array.</summary>
    /// <param name="str"></param>
    /// <param name="anyOf">A Unicode character array containing one or more characters to seek.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="anyOf" /> is <see langword="null" />.</exception>
    /// <returns>The index position of the last occurrence in this instance where any character in <paramref name="anyOf" /> was found; -1 if no character in <paramref name="anyOf" /> was found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfAny(this ref Ustring str, params ReadOnlySpan<char> anyOf) => str.AsReadOnlySpan().LastIndexOfAny(anyOf);

    /// <summary>
    /// Splits a string into a maximum number of substrings based on a specified delimiting character and, optionally, options.
    /// Splits a string into a maximum number of substrings based on the provided character separator, optionally omitting empty substrings from the result.
    /// </summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">A character that delimits the substrings in this instance.</param>
    /// <param name="count">The maximum number of elements expected in the array.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns>An array that contains at most <paramref name="count" /> substrings from this instance that are delimited by <paramref name="separator" />.</returns>
    public static Ustring[] Split(this ref Ustring str, char separator, int count, StringSplitOptions options = StringSplitOptions.None)
    {
        switch (count)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            case 0:
                return [];
            case 1:
                return [new Ustring(str.ToString())];
        }

        var results = Pool<List<Ustring>>.Default.Spawn();
        var start = 0;
        var removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;

        for (var i = 0; i < str.Length && results.Count < count - 1; i++)
        {
            if (str[i] == separator)
            {
                var substring = str.Substring(start, i - start, false);

                if (!removeEmpty || substring.Length > 0)
                    results.Add(substring);

                start = i + 1;
            }
        }

        // Add the remaining part
        if (start < str.Length)
        {
            var remaining = str.Substring(start, false);

            if (!removeEmpty || remaining.Length > 0)
                results.Add(remaining);
        }
        else if (!removeEmpty && start == str.Length)
        {
            results.Add(new Ustring(""));
        }

        var result = results.ToArray();
        Pool<List<Ustring>>.Default.Recycle(results);
        return result;
    }

    /// <summary>Splits a string into substrings based on a specified delimiting character and, optionally, options. </summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">A character that delimits the substrings in this string.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns>An array whose elements contain the substrings from this instance that are delimited by <paramref name="separator" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring[] Split(this ref Ustring str, char separator, StringSplitOptions options = StringSplitOptions.None) => str.Split(separator, int.MaxValue, options);

    /// <summary>Splits a string into substrings based on specified delimiting characters.</summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">An array of delimiting characters, an empty array that contains no delimiters, or <see langword="null" />.</param>
    /// <returns>An array whose elements contain the substrings from this instance that are delimited by one or more characters in <paramref name="separator" />. For more information, see the Remarks section.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring[] Split(this ref Ustring str, params char[]? separator) => str.Split(separator == null ? separator.AsSpan() : ReadOnlySpan<char>.Empty, StringSplitOptions.None);

    /// <summary>Splits a string into a maximum number of substrings based on specified delimiting characters.</summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">An array of characters that delimit the substrings in this string, an empty array that contains no delimiters, or <see langword="null" />.</param>
    /// <param name="count">The maximum number of substrings to return.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="count" /> is negative.</exception>
    /// <returns>An array whose elements contain the substrings in this instance that are delimited by one or more characters in <paramref name="separator" />. For more information, see the Remarks section.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring[] Split(this ref Ustring str, char[]? separator, int count) => str.Split(separator == null ? separator.AsSpan() : ReadOnlySpan<char>.Empty, count, StringSplitOptions.None);

    /// <summary>Splits a string into a maximum number of substrings based on specified delimiting characters and, optionally, options.</summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">An array of characters that delimit the substrings in this string, an empty array that contains no delimiters, or <see langword="null" />.</param>
    /// <param name="count">The maximum number of substrings to return.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="options" /> is not one of the <see cref="T:System.StringSplitOptions" /> values.</exception>
    /// <returns>An array that contains the substrings in this string that are delimited by one or more characters in <paramref name="separator" />. For more information, see the Remarks section.</returns>
    public static Ustring[] Split(this ref Ustring str, ReadOnlySpan<char> separator, int count, StringSplitOptions options)
    {
        switch (count)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            case 0:
                return [];
            case 1:
                return [new Ustring(str.ToString())];
        }

        var results = Pool<List<Ustring>>.Default.Spawn();
        var start = 0;
        var removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;

        if (separator.IsEmpty)
        {
            // Split on whitespace
            for (var i = 0; i < str.Length && results.Count < count - 1; i++)
            {
                if (!char.IsWhiteSpace(str[i])) continue;
                if (i > start)
                {
                    var substring = str.Substring(start, i - start, false);

                    if (!removeEmpty || substring.Length > 0)
                        results.Add(substring);
                }

                // Skip consecutive whitespace
                while (i + 1 < str.Length &&
                    char.IsWhiteSpace(str[i + 1]))
                    i++;

                start = i + 1;
            }
        }
        else
        {
            for (var i = 0; i < str.Length && results.Count < count - 1; i++)
            {
                var found = false;
                for (var j = 0; j < separator.Length; j++)
                {
                    if (str[i] == separator[j])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) continue;
                var substring = str.Substring(start, i - start, false);


                if (!removeEmpty || substring.Length > 0)
                    results.Add(substring);

                start = i + 1;
            }
        }

        // Add the remaining part
        if (start < str.Length)
        {
            var remaining = str.Substring(start, false);

            if (!removeEmpty || remaining.Length > 0)
                results.Add(remaining);
        }
        else if (!removeEmpty && start == str.Length)
        {
            results.Add(new Ustring(""));
        }

        var result = results.ToArray();
        Pool<List<Ustring>>.Default.Recycle(results);
        return result;
    }

    /// <summary>Splits a string into substrings based on specified delimiting characters and options.</summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">An array of characters that delimit the substrings in this string, an empty array that contains no delimiters, or <see langword="null" />.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="options" /> is not one of the <see cref="T:System.StringSplitOptions" /> values.</exception>
    /// <returns>An array whose elements contain the substrings in this string that are delimited by one or more characters in <paramref name="separator" />. For more information, see the Remarks section.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ustring[] Split(this ref Ustring str, ReadOnlySpan<char> separator, StringSplitOptions options) => str.Split(separator, int.MaxValue, options);

    /// <summary>Splits a string into a maximum number of substrings based on specified delimiting strings and, optionally, options.</summary>
    /// <param name="str">The original string to perform the replacement on.</param>
    /// <param name="separator">The strings that delimit the substrings in this string, an empty array that contains no delimiters, or <see langword="null" />.</param>
    /// <param name="count">The maximum number of substrings to return.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="options" /> is not one of the <see cref="T:System.StringSplitOptions" /> values.</exception>
    /// <returns>An array whose elements contain the substrings in this string that are delimited by one or more strings in <paramref name="separator" />. For more information, see the Remarks section.</returns>
    public static Ustring[] Split(this ref Ustring str, string[]? separator, int count, StringSplitOptions options = StringSplitOptions.None)
    {
        switch (count)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            case 0:
                return [];
            case 1:
                return [new Ustring(str.ToString())];
        }

        var results = Pool<List<Ustring>>.Default.Spawn();
        var start = 0;
        var removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;

        if (separator == null || separator.Length == 0)
        {
            // Split on whitespace
            for (var i = 0; i < str.Length && results.Count < count - 1; i++)
            {
                if (!char.IsWhiteSpace(str[i])) continue;
                if (i > start)
                {
                    var substring = str.Substring(start, i - start, false);

                    if (!removeEmpty || substring.Length > 0)
                        results.Add(substring);
                }

                // Skip consecutive whitespace
                while (i + 1 < str.Length &&
                    char.IsWhiteSpace(str[i + 1]))
                    i++;

                start = i + 1;
            }
        }
        else
        {
            for (var i = 0; i < str.Length && results.Count < count - 1; i++)
            {
                var foundSeparator = false;
                var separatorLength = 0;

                // Check each separator string
                foreach (var sep in separator)
                {
                    if (string.IsNullOrEmpty(sep)) continue;

                    if (i + sep.Length > str.Length) continue;
                    var isMatch = true;
                    for (var j = 0; j < sep.Length; j++)
                    {
                        if (str[i + j] != sep[j])
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        foundSeparator  = true;
                        separatorLength = sep.Length;
                        break;
                    }
                }

                if (foundSeparator)
                {
                    var substring = str.Substring(start, i - start, false);

                    if (!removeEmpty || substring.Length > 0)
                        results.Add(substring);

                    i     += separatorLength - 1; // -1 because the loop will increment i
                    start =  i + 1;
                }
            }
        }

        // Add the remaining part
        if (start < str.Length)
        {
            var remaining = str.Substring(start, false);

            if (!removeEmpty || remaining.Length > 0)
                results.Add(remaining);
        }
        else if (!removeEmpty && start == str.Length)
        {
            results.Add(new Ustring(""));
        }

        var result = results.ToArray();
        Pool<List<Ustring>>.Default.Recycle(results);
        return result;
    }

    /// <summary>
    /// Sets a specified number of characters in the current String with a specified character.
    /// </summary>
    /// <param name="ptr">Characters array beginning pointer.</param>
    /// <param name="totalWidth">Total width of the string.</param>
    /// <param name="character">Character to set.</param>
    private static void SetChars(char* ptr, int totalWidth, char character)
    {
        var length = totalWidth * sizeof(char);
        var current = (long*) ptr;
        var chars = GetLongChar(character);

        // Set 8 bytes at a time.
        while (length >= sizeof(long))
        {
            length   -= sizeof(long);
            *current =  chars;
            current  += sizeof(long);
        }

        // Set spare bytes.
        while (length >= sizeof(char))
        {
            length   -= sizeof(char);
            *current =  chars;
            current  += sizeof(char);
        }
    }

    /// <summary>
    /// Fills character to 64 bits with the same character.
    /// </summary>
    /// <param name="character">The character to fill.</param>
    /// <returns>Filled character to 64 bits.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetLongChar(char character)
    {
        const int byteBitSize = 8;
        var result = (long) character;
        result <<= byteBitSize;
        result +=  character;
        result <<= byteBitSize;
        result +=  character;
        result <<= byteBitSize;
        result +=  character;
        result <<= byteBitSize;
        result +=  character;
        result <<= byteBitSize;
        result +=  character;
        result <<= byteBitSize;
        result +=  character;
        result <<= byteBitSize;
        result +=  character;
        return result;
    }

    /// <summary>
    /// Converts a lowercase ASCII character to its uppercase equivalent using bitwise operations for optimal performance.
    /// </summary>
    /// <param name="c">The character to convert to uppercase.</param>
    /// <returns>
    /// The uppercase equivalent of the character if it is a lowercase ASCII letter (a-z); 
    /// otherwise, returns the original character unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToUpper(char c) => c is >= 'a' and <= 'z' ? (char) (c & ~0x20) : c;

    /// <summary>
    /// Converts an uppercase ASCII character to its lowercase equivalent using bitwise operations for optimal performance.
    /// </summary>
    /// <param name="c">The character to convert to lowercase.</param>
    /// <returns>
    /// The lowercase equivalent of the character if it is an uppercase ASCII letter (A-Z); 
    /// otherwise, returns the original character unchanged.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToLower(char c) => c is >= 'A' and <= 'Z' ? (char) (c | 0x20) : c;

    /// <summary>
    /// Determines whether the specified character is a whitespace character according to a subset of Unicode whitespace definitions.
    /// This method checks for common whitespace characters including space, tab, line feed, carriage return, and other control characters.
    /// </summary>
    /// <param name="c">The character to test for whitespace.</param>
    /// <returns>
    /// <c>true</c> if the character is a whitespace character (space, tab, line feed, vertical tab, form feed, carriage return, 
    /// next line, or non-breaking space); otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhiteSpace(char c) => c == 0x20 || (c >= 0x9 && c <= 0xd) || c == 0x85 || c == 0xa0;
}

/// <summary>
/// Provides a specialized array pool for character arrays that maintains pinned GC handles for unmanaged string operations.
/// This pool extends the standard ArrayPool functionality by automatically creating and caching GC handles for rented arrays,
/// enabling safe interop with unmanaged code that requires pinned memory addresses.
/// </summary>
internal class UstringPool : ArrayPool<char>
{
    /// <summary>
    /// Gets a shared instance of the UnmanageStringPool for application-wide use.
    /// </summary>
    /// <value>A singleton instance of UnmanageStringPool.</value>
    public new static UstringPool Shared => new UstringPool();

    /// <summary>
    /// The underlying character array pool used for renting and returning character arrays.
    /// This pool is created as a new instance of the same type as the shared ArrayPool to ensure
    /// proper isolation while maintaining the same pooling behavior and performance characteristics.
    /// </summary>
    private readonly ArrayPool<char> m_Pool = (ArrayPool<char>) Activator.CreateInstance(ArrayPool<char>.Shared.GetType());

    /// <summary>
    /// A thread-safe cache that maintains the association between rented character arrays and their corresponding
    /// pinned GC handles. This cache ensures that each array has a persistent pinned handle for unmanaged interop
    /// operations, preventing the garbage collector from moving the array in memory while it's being used by
    /// unmanaged code. The cache persists handles across multiple rent/return cycles to avoid the overhead
    /// of repeatedly creating and destroying GC handles for the same arrays.
    /// </summary>
    private readonly ConcurrentDictionary<char[], GCHandle> m_HandleCache = new();

    /// <inheritdoc />
    public override char[] Rent(int minimumLength) => Rent(minimumLength, out _);

    /// <summary>
    /// Rents a character array from the pool with at least the specified minimum length and provides a pinned GC handle for the array.
    /// If the array is not already cached, a new pinned GC handle is created and stored for future use.
    /// </summary>
    /// <param name="minimumLength">The minimum length of the character array to rent.</param>
    /// <param name="handle">When this method returns, contains the pinned GC handle for the rented array, allowing safe access to its memory address from unmanaged code.</param>
    /// <returns>A character array with at least the specified minimum length, backed by a pinned GC handle.</returns>
    public char[] Rent(int minimumLength, out GCHandle handle)
    {
        var result = m_Pool.Rent(minimumLength);
        if (!m_HandleCache.TryGetValue(result, out handle))
        {
            handle                = GCHandle.Alloc(result, GCHandleType.Pinned);
            m_HandleCache[result] = handle;
        }

        return result;
    }

    /// <summary>
    /// Returns a character array to the pool and ensures it has a cached pinned GC handle for future use.
    /// If the array doesn't have a cached handle, a new pinned GC handle is created and stored.
    /// </summary>
    /// <param name="array">The character array to return to the pool.</param>
    /// <param name="clearArray">True to clear the contents of the array before returning it to the pool; otherwise, false.</param>
    public override void Return(char[] array, bool clearArray = false)
    {
        if (!m_HandleCache.TryGetValue(array, out var handle))
        {
            handle               = GCHandle.Alloc(array, GCHandleType.Pinned);
            m_HandleCache[array] = handle;
        }

        m_Pool.Return(array, clearArray);
    }
}

/// <summary>
/// Represents a buffer segment for character data with tracking of written content and automatic memory management.
/// </summary>
/// <param name="count">The initial capacity of the buffer segment in characters.</param>
internal struct UstringBufferSegment(int count)
{
    /// <summary>
    /// Gets or sets the character array buffer used to store character data.
    /// This buffer is rented from the UnmanageStringPool and should be returned when no longer needed.
    /// </summary>
    /// <value>
    /// A character array that serves as the underlying storage for the buffer segment.
    /// </value>
    internal char[] Buffer = UstringPool.Shared.Rent(count);

    /// <summary>
    /// Tracks the number of characters that have been written to the buffer.
    /// This field is used internally to maintain the current write position within the buffer.
    /// </summary>
    private int m_Written;

    /// <summary>
    /// Gets a value indicating whether the buffer is null or has been freed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the buffer is null; otherwise, <c>false</c>.
    /// </value>
    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer == null;
    }

    /// <summary>
    /// Gets the number of characters that have been written to the buffer.
    /// </summary>
    /// <value>
    /// The count of characters written to the buffer.
    /// </value>
    public int WrittenCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Written;
    }

    /// <summary>
    /// Gets a span representing the portion of the buffer that contains written data.
    /// </summary>
    /// <value>
    /// A <see cref="Span{T}"/> of characters containing the written data.
    /// </value>
    public Span<char> WrittenBuffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer.AsSpan(0, m_Written);
    }

    /// <summary>
    /// Gets a memory region representing the portion of the buffer that contains written data.
    /// </summary>
    /// <value>
    /// A <see cref="Memory{T}"/> of characters containing the written data.
    /// </value>
    public Memory<char> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer.AsMemory(0, m_Written);
    }

    /// <summary>
    /// Gets a span representing the free (unwritten) portion of the buffer.
    /// </summary>
    /// <value>
    /// A <see cref="Span{T}"/> of characters representing the available space for writing.
    /// </value>
    public Span<char> FreeBuffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer.AsSpan(m_Written);
    }

    /// <summary>
    /// Advances the written position by the specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to advance the written position by.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => m_Written += count;

    /// <summary>
    /// Frees the buffer by returning it to the pool and resetting the segment state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free()
    {
        UstringPool.Shared.Return(Buffer);
        Buffer    = null!;
        m_Written = 0;
    }
}

/// <summary>
/// Exception thrown when an advance operation fails due to insufficient buffer space.
/// </summary>
/// <param name="count">The number of characters requested to advance.</param>
/// <param name="bufferLength">The number of characters remaining in the buffer.</param>
public sealed class AdvanceException(int count, int bufferLength) : Exception($"Advance operation failed. Requested to advance {count} characters, but only {bufferLength} characters remaining.");

/// <summary>
/// A high-performance character writer that provides efficient buffered writing operations for character data.
/// </summary>
internal ref struct UstringWriter() : IDisposable
{
    private readonly ReusableBufferWriter m_BufferWriter = Pool<ReusableBufferWriter>.Default.Spawn();

    private Span<char> m_BufferRef     = default;
    private int        m_AdvancedCount = 0;

    /// <summary>
    /// Gets a span of characters with at least the specified size hint for writing operations.
    /// </summary>
    /// <param name="sizeHint">The minimum number of characters required in the returned span.</param>
    /// <returns>A span of characters that can be written to, with at least <paramref name="sizeHint"/> capacity.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<char> GetSpan(int sizeHint)
    {
        if (m_BufferRef.Length < sizeHint)
        {
            RequestNewBuffer(sizeHint);
        }

        return m_BufferRef;
    }

    /// <summary>
    /// Requests a new buffer from the underlying buffer writer with the specified size hint.
    /// Commits any pending advances before requesting the new buffer.
    /// </summary>
    /// <param name="sizeHint">The minimum size required for the new buffer.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestNewBuffer(int sizeHint)
    {
        if (m_AdvancedCount != 0)
        {
            m_BufferWriter.Advance(m_AdvancedCount);
            m_AdvancedCount = 0;
        }

        m_BufferRef = m_BufferWriter.GetSpan(sizeHint);
    }

    /// <summary>
    /// Advances the writer position by the specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to advance the position by.</param>
    /// <exception cref="AdvanceException">Thrown when the advance count exceeds the available buffer space.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance(int count)
    {
        if (count == 0) return;
        if (m_BufferRef.Length - count < 0)
        {
            throw new AdvanceException(count, m_BufferRef.Length);
        }

        m_BufferRef     =  m_BufferRef.Slice(count);
        m_AdvancedCount += count;
    }

    /// <summary>
    /// Writes a span of characters to the buffer.
    /// </summary>
    /// <param name="value">The characters to write to the buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(ReadOnlySpan<char> value)
    {
        var requiredSize = value.Length;
        fixed (char* startPtr = GetSpan(requiredSize))
        {
            var destPtr = startPtr;
            value.CopyTo(new Span<char>(destPtr, requiredSize));
            destPtr += value.Length;
            Advance((int) (destPtr - startPtr));
        }
    }

    /// <summary>
    /// Writes a single character to the buffer.
    /// </summary>
    /// <param name="value">The character to write to the buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(char value)
    {
        fixed (char* startPtr = GetSpan(1))
        {
            *startPtr = value;
            Advance(1);
        }
    }

    /// <summary>
    /// Flushes all written data and returns it as a Chars instance.
    /// </summary>
    /// <param name="dispose">True to dispose the writer after flushing; otherwise, false.</param>
    /// <returns>A Chars instance containing all the written character data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ustring Flush(bool dispose)
    {
        if (m_AdvancedCount != 0)
        {
            m_BufferWriter.Advance(m_AdvancedCount);
            m_AdvancedCount = 0;
        }

        m_BufferRef = default;
        var result = new Ustring(new string(m_BufferWriter.GetWrittenBuffer()));
        if (dispose)
        {
            Dispose();
        }

        return result;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Pool<ReusableBufferWriter>.Default.Recycle(m_BufferWriter);
}

/// <summary>
/// Resizable  Cache Writer.
/// </summary>
internal sealed class ReusableBufferWriter : IResetable, IBufferWriter<char>
{
    /// <summary>
    /// The maximum length of an int[] array is just a tad smaller than int.MaxValue.
    /// </summary>
    private const int ARRAY_MAX_LENGTH = 0x7FFFFFC7;

    /// <summary>
    /// The default size of the buffer. Each subsequent increase in buffer size doubles the Size value.
    /// </summary>
    private const int INITIAL_BUFFER_SIZE = 262144; // 256KB

    /// <summary>
    /// The currently available buffer is created during the first GetSpan operation and is reallocated after expansion.
    /// </summary>
    private UstringBufferSegment m_CurrentBuffer;

    /// <summary>
    /// Default size for the next CurrentBuffer expansion.
    /// </summary>
    private int? m_NextBufferSize;

    /// <summary>
    /// Total length written
    /// </summary>
    public int TotalWritten
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    /// <summary>
    /// Obtain available free memory.
    /// </summary>
    /// <param name="sizeHint">Memory Length.</param>
    public Span<char> GetSpan(int sizeHint = 0)
    {
        // If CurrentBuffer still has free space, return CurrentBuffer.
        if (!m_CurrentBuffer.IsNull)
        {
            var buffer = m_CurrentBuffer.FreeBuffer;
            if (buffer.Length > sizeHint)
            {
                return buffer;
            }
        }

        // CurrentBuffer is full, then request a new buffer.
        UstringBufferSegment next;
        m_NextBufferSize ??= INITIAL_BUFFER_SIZE;
        if (sizeHint <= m_NextBufferSize)
        {
            next = new UstringBufferSegment(m_NextBufferSize.Value);

            m_NextBufferSize = GetNewArrayCapacity(m_NextBufferSize.Value);
        }
        else
        {
            next = new UstringBufferSegment(sizeHint);
        }

        // Replace CurrentBuffer with NextBuffer.
        // The old CurrentBuffer, if it already contains written data, must be copied to the new buffer.
        if (m_CurrentBuffer.WrittenCount != 0)
        {
            m_CurrentBuffer.WrittenBuffer.CopyTo(next.FreeBuffer);
            next.Advance(m_CurrentBuffer.WrittenCount);
        }

        if (!m_CurrentBuffer.IsNull)
        {
            m_CurrentBuffer.Free();
        }

        m_CurrentBuffer = next;
        return next.FreeBuffer;
    }

    /// <summary>
    /// Increase the buffer size if it's not enough.
    /// </summary>
    /// <param name="oldSize">Old array size.</param>
    /// <returns>New array size.</returns>
    private static int GetNewArrayCapacity(int oldSize)
    {
        var newSize = unchecked(oldSize * 2);
        if ((uint) newSize > ARRAY_MAX_LENGTH)
        {
            newSize = ARRAY_MAX_LENGTH;
        }

        return newSize;
    }

    /// <inheritdoc/>
    Memory<char> IBufferWriter<char>.GetMemory(int sizeHint) => throw new NotSupportedException(); // 不使用GetMemory

    /// <summary>
    /// Writer advance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        m_CurrentBuffer.Advance(count);
        TotalWritten += count;
    }

    /// <summary>
    /// Retrieve the buffer that has been written.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetWrittenBuffer() => m_CurrentBuffer.IsNull ? ReadOnlySpan<char>.Empty : m_CurrentBuffer.WrittenBuffer;

    /// <summary>
    ///Convert the written portion of the buffer into an array.
    /// </summary>
    public char[] CopyToArray()
    {
        if (TotalWritten == 0) return [];

        var result = new char[TotalWritten];
        if (!m_CurrentBuffer.IsNull)
        {
            m_CurrentBuffer.WrittenBuffer.CopyTo(result.AsSpan());
        }

        return result;
    }

    /// <summary>
    /// Clear the cache and release the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        if (TotalWritten == 0) return;
        m_CurrentBuffer.Free();
        m_CurrentBuffer  = default;
        TotalWritten     = 0;
        m_NextBufferSize = INITIAL_BUFFER_SIZE;
    }
}