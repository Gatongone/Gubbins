using Gubbins.Enhance;

namespace Gubbins.Events.Tests;

[TestFixture]
public sealed class EventTests
{
    [Test]
    public void Broadcast_WithSubscriber_ShouldDeliverNotification()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);

        bus.Broadcast(42);

        Assert.That(handler.Values, Is.EqualTo(new[] { 42 }));
        Assert.That(handler.Results, Is.Empty);
    }

    [Test]
    public void Broadcast_WithoutSubscribers_ShouldNotThrow()
    {
        var bus = new Event<int>();

        Assert.DoesNotThrow(() => bus.Broadcast(1));
    }

    [Test]
    public void Broadcast_WithMultipleSubscribers_ShouldInvokeThemInSubscriptionOrder()
    {
        var bus = new Event<int>();
        var order = new List<string>();
        bus.Subscribe(new ActionEventHandler<int>(_ => order.Add("first")));
        bus.Subscribe(new ActionEventHandler<int>(_ => order.Add("second")));
        bus.Subscribe(new ActionEventHandler<int>(_ => order.Add("third")));

        bus.Broadcast(0);

        Assert.That(order, Is.EqualTo(new[] { "first", "second", "third" }));
    }

    [Test]
    public void Broadcast_WhenHandlerThrows_ShouldPropagateTheSameExceptionAndKeepTheBusUsable()
    {
        var bus = new Event<int>();
        var error = new InvalidOperationException("boom");
        var throwing = new ThrowingHandler<int>(error);
        var later = new RecordingHandler<int>();
        bus.Subscribe(throwing);
        bus.Subscribe(later);

        var thrown = Assert.Throws<InvalidOperationException>(() => bus.Broadcast(1));

        Assert.That(thrown, Is.SameAs(error));
        Assert.That(later.Values, Is.Empty, "A throwing handler aborts the current broadcast.");
        Assert.That(bus.Unsubscribe(throwing), Is.True, "The dispatch depth should be restored by the broadcast.");

        bus.Broadcast(2);

        Assert.That(later.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Broadcast_WhenHandlerSubscribesDuringDispatch_ShouldNotDeliverCurrentNotificationToTheNewHandler()
    {
        var bus = new Event<int>();
        var late = new RecordingHandler<int>();
        var early = new RecordingHandler<int>();
        var subscribed = false;
        bus.Subscribe(new ActionEventHandler<int>(_ =>
        {
            if (subscribed) return;
            subscribed = true;
            bus.Subscribe(late);
        }));
        bus.Subscribe(early);

        bus.Broadcast(1);

        Assert.That(late.Values, Is.Empty);

        bus.Broadcast(2);

        Assert.That(late.Values, Is.EqualTo(new[] { 2 }));
        Assert.That(early.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Broadcast_WhenHandlerClearsTheBus_ShouldStopRemainingHandlers()
    {
        var bus = new Event<int>();
        var cleared = 0;
        var later = new RecordingHandler<int>();
        bus.Subscribe(new ActionEventHandler<int>(_ =>
        {
            cleared++;
            bus.Clear();
        }));
        bus.Subscribe(later);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(cleared, Is.EqualTo(1));
        Assert.That(later.Values, Is.Empty);
    }

    [Test]
    public void Broadcast_AfterComplete_ShouldBeIgnored()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);
        bus.OnComplete(Result<int, Exception>.Success(0));

        bus.Broadcast(1);

        Assert.That(handler.Values, Is.Empty);
    }

    [Test]
    public void Subscribe_NullHandler_ShouldThrowArgumentNullException()
    {
        var bus = new Event<int>();

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe(null!));
    }

    [Test]
    public void Subscribe_AfterComplete_ShouldCompleteTheNewHandlerImmediately()
    {
        var bus = new Event<int>();
        bus.OnComplete(Result<int, Exception>.Success(7));
        var handler = new RecordingHandler<int>();

        bus.Subscribe(handler);

        Assert.That(handler.Results, Has.Count.EqualTo(1));
        Assert.That(handler.Results[0].TryGetValue(out var value, out _), Is.True);
        Assert.That(value, Is.EqualTo(7));
        Assert.That(handler.Values, Is.Empty);

        bus.Broadcast(1);

        Assert.That(handler.Values, Is.Empty);
    }

    [Test]
    public void Subscribe_AfterClear_ShouldDeliverNotificationsAgain()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);
        bus.Clear();

        bus.Subscribe(handler);
        bus.Broadcast(1);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Unsubscribe_SubscribedHandler_ShouldRemoveItAndReturnTrue()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);

        var removed = bus.Unsubscribe(handler);
        bus.Broadcast(1);

        Assert.That(removed, Is.True);
        Assert.That(handler.Values, Is.Empty);
        Assert.That(bus.Unsubscribe(handler), Is.False, "The handler is no longer subscribed.");
    }

    [Test]
    public void Unsubscribe_UnsubscribedHandler_ShouldReturnFalse()
    {
        var bus = new Event<int>();
        var subscribed = new RecordingHandler<int>();
        var other = new RecordingHandler<int>();
        bus.Subscribe(subscribed);

        var removed = bus.Unsubscribe(other);
        bus.Broadcast(1);

        Assert.That(removed, Is.False);
        Assert.That(subscribed.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Unsubscribe_NullHandler_ShouldReturnFalse()
    {
        var bus = new Event<int>();

        Assert.That(bus.Unsubscribe(null!), Is.False);
    }

    [Test]
    public void Unsubscribe_HandlerSubscribedTwice_ShouldRemoveOnlyOneSubscription()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);
        bus.Subscribe(handler);

        bus.Broadcast(1);
        var removed = bus.Unsubscribe(handler);
        bus.Broadcast(2);

        Assert.That(removed, Is.True);
        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 1, 2 }));
    }

    [Test]
    public void Unsubscribe_DuringBroadcast_ShouldSkipTheHandlerForTheCurrentAndLaterBroadcasts()
    {
        var bus = new Event<int>();
        var first = new RecordingHandler<int>();
        var second = new RecordingHandler<int>();
        bus.Subscribe(new ActionEventHandler<int>(value =>
        {
            first.Handle(value);
            bus.Unsubscribe(second);
        }));
        bus.Subscribe(second);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(first.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(second.Values, Is.Empty);
    }

    [Test]
    public void Unsubscribe_SelfDuringBroadcast_ShouldNotAffectHandlersRegisteredAfterIt()
    {
        var bus = new Event<int>();
        var self = new List<int>();
        var later = new RecordingHandler<int>();
        ActionEventHandler<int>? selfHandler = null;
        selfHandler = new ActionEventHandler<int>(value =>
        {
            self.Add(value);
            bus.Unsubscribe(selfHandler!);
        });
        bus.Subscribe(selfHandler);
        bus.Subscribe(later);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(self, Is.EqualTo(new[] { 1 }));
        Assert.That(later.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Clear_ShouldRemoveEveryHandler()
    {
        var bus = new Event<int>();
        var first = new RecordingHandler<int>();
        var second = new RecordingHandler<int>();
        bus.Subscribe(first);
        bus.Subscribe(second);

        bus.Clear();
        bus.Broadcast(1);
        bus.OnComplete(Result<int, Exception>.Success(0));

        Assert.That(first.Values, Is.Empty);
        Assert.That(second.Values, Is.Empty);
        Assert.That(first.Results, Is.Empty);
        Assert.That(second.Results, Is.Empty);
    }

    [Test]
    public void Clear_OnEmptyBus_ShouldNotThrow()
    {
        var bus = new Event<int>();

        Assert.DoesNotThrow(bus.Clear);
    }

    [Test]
    public void OnComplete_WithHandlers_ShouldNotifyEveryHandlerWithTheResult()
    {
        var bus = new Event<int>();
        var first = new RecordingHandler<int>();
        var second = new RecordingHandler<int>();
        bus.Subscribe(first);
        bus.Subscribe(second);

        bus.OnComplete(Result<int, Exception>.Success(5));

        Assert.That(first.Results, Has.Count.EqualTo(1));
        Assert.That(second.Results, Has.Count.EqualTo(1));
        Assert.That(first.Results[0].TryGetValue(out var firstValue, out _), Is.True);
        Assert.That(second.Results[0].TryGetValue(out var secondValue, out _), Is.True);
        Assert.That(firstValue, Is.EqualTo(5));
        Assert.That(secondValue, Is.EqualTo(5));
    }

    [Test]
    public void OnComplete_WithError_ShouldDeliverTheSameError()
    {
        var bus = new Event<int>();
        var error = new InvalidOperationException("boom");
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);

        bus.OnComplete(Result<int, Exception>.Fail(error));

        Assert.That(handler.Results, Has.Count.EqualTo(1));
        Assert.That(GetError(handler.Results[0]), Is.SameAs(error));
    }

    [Test]
    public void OnComplete_WhenCalledAgain_ShouldKeepTheFirstResultAndNotifyOnlyOnce()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);

        bus.OnComplete(Result<int, Exception>.Success(1));
        bus.OnComplete(Result<int, Exception>.Fail(new InvalidOperationException("later")));

        Assert.That(handler.Results, Has.Count.EqualTo(1));
        Assert.That(handler.Results[0].TryGetValue(out var value, out _), Is.True);
        Assert.That(value, Is.EqualTo(1));
    }

    [Test]
    public void OnComplete_DuringBroadcast_ShouldCompleteHandlersWithoutDeliveringTheNotificationToRemainingOnes()
    {
        var bus = new Event<int>();
        var later = new RecordingHandler<int>();
        bus.Subscribe(new ActionEventHandler<int>(_ => bus.OnComplete(Result<int, Exception>.Success(1))));
        bus.Subscribe(later);

        bus.Broadcast(0);

        Assert.That(later.Values, Is.Empty);
        Assert.That(later.Results, Has.Count.EqualTo(1));

        bus.Broadcast(2);

        Assert.That(later.Values, Is.Empty);
    }

    [Test]
    public void OnComplete_AfterClear_ShouldNotNotifyClearedHandlers()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Subscribe(handler);
        bus.Clear();

        bus.OnComplete(Result<int, Exception>.Success(0));

        Assert.That(handler.Results, Is.Empty);
    }

    // ---- Non-generic bus ----

    [Test]
    public void Event_ShouldExposeTheUnitBasedInterfaces()
    {
        var bus = new Event();

        Assert.That(bus, Is.InstanceOf<Event<Unit>>());
        Assert.That(bus, Is.InstanceOf<IEvent>());
        Assert.That(bus, Is.InstanceOf<IEventSubscribable<Unit>>());
        Assert.That(bus, Is.InstanceOf<IEventBroadcastable<Unit>>());
    }

    [Test]
    public void Event_BroadcastWithoutArguments_ShouldInvokeParameterlessSubscriber()
    {
        IEvent bus = new Event();
        var calls = 0;
        bus.Subscribe(() => calls++);

        bus.Broadcast();

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Event_OnComplete_ShouldNotifyUnitHandler()
    {
        var bus = new Event();
        var handler = new RecordingHandler<Unit>();
        bus.Subscribe(handler);

        bus.OnComplete(Result<Unit, Exception>.Success(Unit.Instance));

        Assert.That(handler.Results, Has.Count.EqualTo(1));
        Assert.That(handler.Results[0].Valid, Is.True);
    }

    // ---- Subscribe extensions ----

    [Test]
    public void Subscribe_WithAction_ShouldForwardNotificationsAndAllowUnsubscribeByTheSameAction()
    {
        var bus = new Event<int>();
        var values = new List<int>();
        Action<int> action = values.Add;
        bus.Subscribe(action);

        bus.Broadcast(1);
        bus.Unsubscribe(action);
        bus.Broadcast(2);

        Assert.That(values, Is.EqualTo(new[] { 1 }));
    }

    private static Exception? GetError<TNotification>(Result<TNotification, Exception> result)
    {
        result.TryGetValue(out _, out var error);
        return error;
    }

    private sealed class RecordingHandler<TNotification> : IEventHandler<TNotification>
    {
        public readonly List<TNotification> Values = [];
        public readonly List<Result<TNotification, Exception>> Results = [];

        public void Handle(TNotification notification) => Values.Add(notification);

        public void OnComplete(Result<TNotification, Exception> result) => Results.Add(result);
    }

    private sealed class ThrowingHandler<TNotification>(Exception error) : IEventHandler<TNotification>
    {
        public void Handle(TNotification notification) => throw error;

        public void OnComplete(Result<TNotification, Exception> result) { }
    }
}