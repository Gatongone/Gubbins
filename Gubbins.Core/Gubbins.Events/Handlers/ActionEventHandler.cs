using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Event handler with <see cref="System.Action"/>.
/// </summary>
public sealed class ActionEventHandler : IEventHandler, IEquatable<Action>, IEquatable<ActionEventHandler>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action Invocation;

    /// <summary>
    /// Create an event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>   
    public ActionEventHandler(Action delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle() => Invocation.Invoke();

    /// <inheritdoc/>
    void IEventHandler<Unit>.Handle(Unit arg) => Handle();

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler handler => Equals(handler),
        Action action              => Equals(action),
        _                          => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg> : IEventHandler<TArg>, IEquatable<Action<TArg>>, IEquatable<ActionEventHandler<TArg>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>   
    public ActionEventHandler(Action<TArg> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg arg) => Invocation.Invoke(arg);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg> handler => Equals(handler),
        Action<TArg> action              => Equals(action),
        _                                => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2> : IEventHandler<(TArg1, TArg2)>, IEquatable<Action<TArg1, TArg2>>, IEquatable<ActionEventHandler<TArg1, TArg2>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2) => Invocation(arg1, arg2);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2)>.Handle((TArg1, TArg2) arg) => Handle(arg.Item1, arg.Item2);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2> handler => Equals(handler),
        Action<TArg1, TArg2> action              => Equals(action),
        _                                        => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3> : IEventHandler<(TArg1, TArg2, TArg3)>, IEquatable<Action<TArg1, TArg2, TArg3>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3) => Invocation(arg1, arg2, arg3);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3)>.Handle((TArg1, TArg2, TArg3) arg) => Handle(arg.Item1, arg.Item2, arg.Item3);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3> action              => Equals(action),
        _                                               => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4> : IEventHandler<(TArg1, TArg2, TArg3, TArg4)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) => Invocation(arg1, arg2, arg3, arg4);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4)>.Handle((TArg1, TArg2, TArg3, TArg4) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4> action              => Equals(action),
        _                                                      => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) => Invocation(arg1, arg2, arg3, arg4, arg5);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5> action              => Equals(action),
        _                                                             => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action              => Equals(action),
        _                                                                    => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action              => Equals(action),
        _                                                                           => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> action              => Equals(action),
        _                                                                                  => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> action              => Equals(action),
        _                                                                                         => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> action              => Equals(action),
        _                                                                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> action              => Equals(action),
        _                                                                                                         => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> action              => Equals(action),
        _                                                                                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, arg.Item13);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> action              => Equals(action),
        _                                                                                                                         => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, arg.Item13, arg.Item14);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> action              => Equals(action),
        _                                                                                                                                 => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, arg.Item13, arg.Item14, arg.Item15);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> action              => Equals(action),
        _                                                                                                                                         => false
    };
}

/// <summary>
/// Event handler with <see cref="System.Action{TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16}"/>.
/// </summary>
public sealed class ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> : IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16)>, IEquatable<Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>>, IEquatable<ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>>
{
    /// <summary>
    /// Action with method provider.
    /// </summary>
    public readonly Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> Invocation;

    /// <summary>
    /// Create a event handler from a Action.
    /// </summary>
    /// <param name="delegation">Delegation for creating EventHandler.</param>
    public ActionEventHandler(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> delegation) => Invocation = delegation;

    /// <summary>
    /// <inheritdoc cref="IEventHandler{TNotification}.Handle"/>
    /// </summary>
    public void Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16) => Invocation(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);

    /// <inheritdoc/>
    void IEventHandler<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16)>.Handle((TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16) arg) => Handle(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, arg.Item9, arg.Item10, arg.Item11, arg.Item12, arg.Item13, arg.Item14, arg.Item15, arg.Item16);

    /// <inheritdoc/>
    public override int GetHashCode() => Invocation.GetHashCode();

    /// <summary>
    /// Verify if the target equals to event handler.
    /// </summary>
    public static bool operator ==(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> left, object right) => !Equals(left, null) && left.Equals(right);

    /// <summary>
    /// Verify if the target not equals to event handler.
    /// </summary>
    public static bool operator !=(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> left, object right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> other) => Invocation.Equals(other.Invocation);

    /// <inheritdoc/>
    public bool Equals(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> other) => Invocation.Equals(other);

    /// <inheritdoc/>
    public override bool Equals(object? other) => other switch
    {
        ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> handler => Equals(handler),
        Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> action              => Equals(action),
        _                                                                                                                                                 => false
    };
}