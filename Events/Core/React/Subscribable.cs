using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Factory methods for the reactive sources.
/// </summary>
public static class Subscribable
{
    /// <summary>
    /// Creates a source that emits one value and completes.
    /// </summary>
    /// <param name="value">The value to emit.</param>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>A cold source that emits <paramref name="value"/> to every subscriber.</returns>
    public static IEventSubscribable<TNotification> Return<TNotification>(TNotification value) => new ReturnEvent<TNotification>(value);

    /// <summary>
    /// Creates a source that completes without a value.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>A cold source that completes immediately.</returns>
    public static IEventSubscribable<TNotification> Empty<TNotification>() => new EmptyEvent<TNotification>();

    /// <summary>
    /// Creates a source that never emits and never completes.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>A source that never terminates.</returns>
    public static IEventSubscribable<TNotification> Never<TNotification>() => new NeverEvent<TNotification>();

    /// <summary>
    /// Creates a source that completes with <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The terminal error.</param>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>A cold source that fails immediately.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static IEventSubscribable<TNotification> Throw<TNotification>(Exception error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));

        return new ThrowEvent<TNotification>(error);
    }

    /// <summary>
    /// Creates a source that emits an integer range and completes.
    /// </summary>
    /// <param name="start">The first value.</param>
    /// <param name="count">The number of values.</param>
    /// <returns>A cold source that emits the range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static IEventSubscribable<int> Range(int start, int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "The count must not be negative.");

        return new RangeEvent(start, count);
    }

    private sealed class ReturnEvent<TNotification>(TNotification value) : IEventSubscribable<TNotification>
    {
        public void Subscribe(IEventHandler<TNotification> handler)
        {
            handler.Handle(value);
            handler.OnComplete(default);
        }

        public bool Unsubscribe(IEventHandler<TNotification> handler) => false;

        public void Clear() { }
    }

    private sealed class EmptyEvent<TNotification> : IEventSubscribable<TNotification>
    {
        public void Subscribe(IEventHandler<TNotification> handler) => handler.OnComplete(default);

        public bool Unsubscribe(IEventHandler<TNotification> handler) => false;

        public void Clear() { }
    }

    private sealed class NeverEvent<TNotification> : IEventSubscribable<TNotification>
    {
        public void Subscribe(IEventHandler<TNotification> handler) { }

        public bool Unsubscribe(IEventHandler<TNotification> handler) => false;

        public void Clear() { }
    }

    private sealed class ThrowEvent<TNotification>(Exception error) : IEventSubscribable<TNotification>
    {
        public void Subscribe(IEventHandler<TNotification> handler) => handler.OnComplete(error);

        public bool Unsubscribe(IEventHandler<TNotification> handler) => false;

        public void Clear() { }
    }

    private sealed class RangeEvent(int start, int count) : IEventSubscribable<int>
    {
        public void Subscribe(IEventHandler<int> handler)
        {
            for (var index = 0; index < count; index++)
            {
                handler.Handle(start + index);
            }

            handler.OnComplete(default);
        }

        public bool Unsubscribe(IEventHandler<int> handler) => false;

        public void Clear() { }
    }
}
