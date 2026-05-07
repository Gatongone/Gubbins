using System.Runtime.CompilerServices;

namespace Gubbins.Enhance;

public class ArgumentNoneException(string message) : Exception(message);

public struct Option<T>(T? value) : IEquatable<T>, IEquatable<Option<T>>
{
    private T m_Value = value!;

    public delegate void RefAction(ref T value);

    public delegate TResult RefFunc<out TResult>(ref T value);

    public T? Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Value;
    }

    internal bool IsNone;

    public static readonly Option<T> None = new() {IsNone = true};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Some(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwarp(string message = "Value can't be none.") => !IsNone ? m_Value : throw new ArgumentNoneException(message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TResult> Map<TResult>(Func<T, TResult> func) => IsNone ? new Option<TResult> {IsNone = true} : new Option<TResult>(func(Value!));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> func) => IsNone ? new Option<TResult> {IsNone = true} : new Option<TResult>(func(Value!));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Iterate(Action<T> some)
    {
        if (IsNone) return;
        some(Value!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => IsNone ? none() : some(Value!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(out T? value)
    {
        value = m_Value;
        return IsNone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        m_Value = default!;
        IsNone  = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => IsNone ? 0 : m_Value!.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj switch
    {
        null               => IsNone,
        Option<T> nullable => Equals(nullable),
        T value            => Equals(value),
        _                  => false
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Option<T> other) => IsNone == other.IsNone && EqualityComparer<T?>.Default.Equals(m_Value, other.m_Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T? other) => EqualityComparer<T>.Default.Equals(other!, m_Value) || other == null && IsNone;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Option<T> option) => option.m_Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Option<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Option<T>(DBNull? _) => new() {m_Value = default!, IsNone = true};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Option<T> left, Option<T> right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Option<T> left, T right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Option<T> left, T right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Option<T> left, DBNull? right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Option<T> left, DBNull? right) => !(left == right);
}