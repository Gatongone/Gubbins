using System.Runtime.CompilerServices;

namespace Gubbins.Enhance;

/// <summary>
/// Represents a mutable container for value types that provides change notifications and implicit conversions.
/// This class wraps a value type and allows for monitoring changes through events while maintaining 
/// compatibility with the wrapped type and Option&lt;T&gt; through implicit operators.
/// </summary>
/// <typeparam name="T">The value type to be contained within the box. Must be a struct.</typeparam>
/// <param name="value">The initial value to store in the box.</param>
public sealed class Box<T>(T value) : IEquatable<T>, IEquatable<Box<T>>, IEquatable<Option<T>> where T : struct
{
    private T m_Value = value;

    /// <summary>
    /// Gets or sets the value contained within the box.
    /// When set, triggers the ValueChangedListener event with the new value.
    /// </summary>
    /// <value>The current value stored in the box.</value>
    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            m_Value = value;
            ValueChangedListener?.Invoke(value);
        }
    }

    /// <summary>
    /// Event that is triggered when the Value property is changed.
    /// Subscribers will receive the new value as a parameter.
    /// </summary>
    public event Action<T>? ValueChangedListener;

    /// <summary>
    /// Implicitly converts a Box&lt;T&gt; to its contained value type T.
    /// </summary>
    /// <param name="box">The box to extract the value from.</param>
    /// <returns>The value contained within the box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Box<T> box) => box.m_Value;

    /// <summary>
    /// Implicitly converts a value of type T to a Box&lt;T&gt; containing that value.
    /// </summary>
    /// <param name="value">The value to wrap in a box.</param>
    /// <returns>A new Box&lt;T&gt; instance containing the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Box<T>(T value) => new(value);

    /// <summary>
    /// Implicitly converts a Box&lt;T&gt; to an Option&lt;T&gt; containing the box's value.
    /// </summary>
    /// <param name="box">The box to convert to an option.</param>
    /// <returns>An Option&lt;T&gt; containing the box's value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Option<T>(Box<T> box) => box.m_Value;

    /// <summary>
    /// Implicitly converts an Option&lt;T&gt; to a Box&lt;T&gt; containing the option's value.
    /// </summary>
    /// <param name="option">The option to convert to a box.</param>
    /// <returns>A new Box&lt;T&gt; instance containing the option's value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Box<T>(Option<T> option) => new(option);

    /// <summary>
    /// Determines whether the current box's value is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare with the current box's value.</param>
    /// <returns>true if the current box's value is equal to the other value; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T other) => m_Value.Equals(other);

    /// <summary>
    /// Determines whether the current box is equal to another box by comparing their contained values.
    /// </summary>
    /// <param name="other">The other box to compare with the current box.</param>
    /// <returns>true if both boxes are not null and contain equal values; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Box<T>? other) => other != null && m_Value.Equals(other.m_Value);

    /// <summary>
    /// Determines whether the current box's value is equal to the value contained in the specified option.
    /// </summary>
    /// <param name="other">The option to compare with the current box's value.</param>
    /// <returns>true if the option has a value and it equals the current box's value; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Option<T> other) => !other.IsNone && m_Value.Equals(other.Value);
}