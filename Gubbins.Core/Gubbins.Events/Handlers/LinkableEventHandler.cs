using Gubbins.Enhance;
namespace Gubbins.Events;

/// <summary>
/// Event handler with <see cref="System.Func<TResult, TResult>"/>.
/// </summary>
public sealed class LinkableEventHandler<TResult> : ILinkableEventHandler<TResult>, IEquatable<Func<TResult, TResult>>, IEquatable<LinkableEventHandler<TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TResult, TResult> Invocation;

    /// <summary>
    /// Create an event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="ILinkableEventHandler{TNotification, TResult}.Handle"/>
    /// </summary>
    public TResult Handle(TResult previousResult) => Invocation.Invoke(previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<Unit, TResult>.Handle(Unit arg, TResult previousResult) => Handle(previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        LinkableEventHandler<TResult> handler => Equals(handler),
        Func<TResult, TResult> action         => Equals(action),
        _                                     => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg, TResult> : ILinkableEventHandler<TArg, TResult>, IEquatable<Func<TArg, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>   
    public LinkableEventHandler(Func<TArg, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="ILinkableEventHandler{TNotification, TResult}.Handle"/>
    /// </summary>
    public TResult Handle(TArg arg, TResult previousResult) => Invocation.Invoke(arg, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        LinkableEventHandler<TArg, TResult> handler => Equals(handler),
        Func<TArg, TResult, TResult> action                => Equals(action),
        _                                  => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TResult> : ILinkableEventHandler<(TArg1, TArg2), TResult>, IEquatable<Func<TArg1, TArg2, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TResult previousResult) => Invocation(arg1, arg2, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2), TResult>.Handle((TArg1, TArg2) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2> handler => Equals(handler),
        Func<TArg1, TArg2, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TResult previousResult) => Invocation(arg1, arg2, arg3, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3), TResult>.Handle((TArg1, TArg2, TArg3) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4), TResult>.Handle((TArg1, TArg2, TArg3, TArg4) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, arg.Item13, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Func{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult}"/>.
/// </summary>
public sealed class LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult> : ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14), TResult>, IEquatable<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult>>, IEquatable<LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult> Invocation;

    /// <summary>
    /// Create a event handler from a Func.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public LinkableEventHandler(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public TResult Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TResult previousResult) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, previousResult);

    /// <inheritdoc/>
    TResult ILinkableEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14), TResult>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14) arg, TResult previousResult) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, arg.Item13, arg.Item14, previousResult);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> handler => Equals(handler),
        Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult> action => Equals(action),
        _                                                 => false
    };
}
