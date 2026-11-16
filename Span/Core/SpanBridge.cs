using System;
using System.Runtime.InteropServices;

namespace Gubbins.Span;

/// <summary>
/// Zero-copy bridges between a component type and the value type its layout is equivalent to.
/// </summary>
/// <remarks>
/// <para>
/// These exist for the code generated from <see cref="SpanToAttribute"/>: the component has no member of the value
/// type, yet one component occupies the same bytes as one value, so a component can be viewed as a value (and back)
/// without copying.
/// </para>
/// <para>
/// Both helpers reinterpret memory; the caller is responsible for the layout being equivalent, which the generator
/// verifies at compile time (<c>SpanTo</c> requires a padding-free sequential layout of the same size as the value).
/// </para>
/// </remarks>
public static class SpanBridge
{
    /// <summary>
    /// Views a single component <b>by reference</b> as the equivalent value.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TValue">The layout-equivalent value type.</typeparam>
    /// <param name="component">The component to view.</param>
    /// <returns>A reference to the value sharing the component's storage.</returns>
    public static ref TValue AsValue<TComponent, TValue>(ref TComponent component)
        where TComponent : struct
        where TValue : struct
        => ref MemoryMarshal.Cast<TComponent, TValue>(MemoryMarshal.CreateSpan(ref component, 1))[0];

    /// <summary>
    /// Builds a component value from the layout-equivalent value, without allocating.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TValue">The layout-equivalent value type.</typeparam>
    /// <param name="value">The value to reinterpret.</param>
    /// <returns>A component whose storage holds <paramref name="value"/>.</returns>
    public static TComponent FromValue<TComponent, TValue>(TValue value)
        where TComponent : struct
        where TValue : struct
        => MemoryMarshal.Cast<TValue, TComponent>(MemoryMarshal.CreateReadOnlySpan(ref value, 1))[0];
}
