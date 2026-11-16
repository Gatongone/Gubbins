using System.Reflection;
using Gubbins.Enhance;
using Type = System.Type;

namespace Gubbins.Events.Tests;

[TestFixture]
public sealed class EventAttributeTests
{
    [Test]
    public void EventAttribute_ShouldOnlyBeAppliedToMethods()
    {
        var usage = typeof(EventAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        Assert.That(usage, Is.Not.Null);
        Assert.That(usage!.ValidOn, Is.EqualTo(AttributeTargets.Method));
        Assert.That(usage.AllowMultiple, Is.False);
    }

    [Test]
    public void GenericEventAttribute_ShouldOnlyBeAppliedToMethods()
    {
        var usage = typeof(EventAttribute<>).GetCustomAttribute<AttributeUsageAttribute>();

        Assert.That(usage, Is.Not.Null);
        Assert.That(usage!.ValidOn, Is.EqualTo(AttributeTargets.Method));
        Assert.That(usage.AllowMultiple, Is.False);
    }

    [Test]
    public void EventAttribute_ShouldExposeBusTypeConstructor()
    {
        Assert.That(typeof(Attribute).IsAssignableFrom(typeof(EventAttribute)), Is.True);

        var constructor = typeof(EventAttribute).GetConstructor([typeof(Type)]);

        Assert.That(constructor, Is.Not.Null);
        Assert.That(constructor!.GetParameters(), Has.Length.EqualTo(1));
        Assert.That(constructor.GetParameters()[0].Name, Is.EqualTo("busType"));
        Assert.That(constructor.GetParameters()[0].ParameterType, Is.EqualTo(typeof(Type)));
    }

    [Test]
    public void GenericEventAttribute_ShouldBeGenericOverTheEventType()
    {
        Assert.That(typeof(Attribute).IsAssignableFrom(typeof(EventAttribute<MessageEvent>)), Is.True);
        Assert.That(typeof(EventAttribute<MessageEvent>).IsGenericType, Is.True);
        Assert.That(
            typeof(EventAttribute<MessageEvent>).GetGenericTypeDefinition(),
            Is.EqualTo(typeof(EventAttribute<>)));
        Assert.That(
            typeof(EventAttribute<MessageEvent>).GetGenericArguments(),
            Is.EqualTo(new[] { typeof(MessageEvent) }));
    }

    // ---- Reflection over decorated methods ----

    [Test]
    public void EventAttribute_OnMethod_ShouldCarryTheBusType()
    {
        var method = GetMethod("OnCount");
        var attribute = method.GetCustomAttribute<EventAttribute>();
        var data = method.GetCustomAttributesData()
            .Single(static data => data.AttributeType == typeof(EventAttribute));

        Assert.That(attribute, Is.Not.Null);
        Assert.That(data.AttributeType, Is.EqualTo(typeof(EventAttribute)));
        Assert.That(data.ConstructorArguments, Has.Count.EqualTo(1));
        Assert.That(data.ConstructorArguments[0].ArgumentType, Is.EqualTo(typeof(Type)));
        Assert.That(data.ConstructorArguments[0].Value, Is.EqualTo(typeof(CountEvent)));
    }

    [Test]
    public void GenericEventAttribute_OnMethod_ShouldCarryTheEventType()
    {
        var method = GetMethod("OnMessage");
        var attribute = method.GetCustomAttribute<EventAttribute<MessageEvent>>();
        var data = method.GetCustomAttributesData()
            .Single(static data => data.AttributeType == typeof(EventAttribute<MessageEvent>));

        Assert.That(attribute, Is.Not.Null);
        Assert.That(data.AttributeType, Is.EqualTo(typeof(EventAttribute<MessageEvent>)));
        Assert.That(data.ConstructorArguments, Is.Empty);
        Assert.That(
            data.AttributeType.GetGenericArguments(),
            Is.EqualTo(new[] { typeof(MessageEvent) }));
    }

    // ---- Source generator integration ----

    [Test]
    public void Listen_ShouldSubscribeEveryDecoratedMethodToItsBus()
    {
        var listener = new AttributeListener();
        var factory = new TestEventFactory();

        ((IEventListener)listener).Listen(factory);

        factory.RequireEvent<CountEvent>().Broadcast(1);
        factory.RequireEvent<MessageEvent>().Broadcast("message");
        factory.RequireEvent<PairEvent>().Broadcast((2, "pair"));
        factory.RequireEvent<TickEvent>().Fire();

        Assert.That(listener.Counts, Is.EqualTo(new[] { 1 }));
        Assert.That(listener.Messages, Is.EqualTo(new[] { "message" }));
        Assert.That(listener.Pairs, Is.EqualTo(new[] { (2, "pair") }));
        Assert.That(listener.Ticks, Is.EqualTo(1));
    }

    [Test]
    public void Clear_ShouldUnsubscribeEveryDecoratedMethodFromItsBus()
    {
        var listener = new AttributeListener();
        var factory = new TestEventFactory();
        var events = (IEventListener)listener;
        events.Listen(factory);
        factory.RequireEvent<CountEvent>().Broadcast(1);

        events.Clear(factory);

        factory.RequireEvent<CountEvent>().Broadcast(2);
        factory.RequireEvent<MessageEvent>().Broadcast("ignored");
        factory.RequireEvent<PairEvent>().Broadcast((3, "ignored"));
        factory.RequireEvent<TickEvent>().Fire();

        Assert.That(listener.Counts, Is.EqualTo(new[] { 1 }));
        Assert.That(listener.Messages, Is.Empty);
        Assert.That(listener.Pairs, Is.Empty);
        Assert.That(listener.Ticks, Is.Zero);
    }

    [Test]
    public void Clear_WhenCalledAgain_ShouldNotThrow()
    {
        var listener = new AttributeListener();
        var factory = new TestEventFactory();
        var events = (IEventListener)listener;
        events.Listen(factory);
        events.Clear(factory);

        Assert.DoesNotThrow(() => events.Clear(factory));
    }

    private static MethodInfo GetMethod(string name)
        => typeof(AttributeListener).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"Method '{name}' was not found on {nameof(AttributeListener)}.");

    private sealed class TestEventFactory : IEventFactory
    {
        private readonly Dictionary<Type, object> m_Events = [];

        public TEvent RequireEvent<TEvent>() where TEvent : class, new()
        {
            if (m_Events.TryGetValue(typeof(TEvent), out var existing)) return (TEvent)existing;

            var created = new TEvent();
            m_Events.Add(typeof(TEvent), created);
            return created;
        }
    }
}

internal sealed class CountEvent : Event<int>;

internal sealed class MessageEvent : Event<string>;

internal sealed class PairEvent : Event<(int, string)>;

/// <summary>
/// A subscribe-only bus, used to cover the generated <c>Unit</c> (parameterless) path.
/// </summary>
internal sealed class TickEvent : IEventSubscribable<Unit>
{
    private readonly List<IEventHandler<Unit>> m_Handlers = [];

    public void Subscribe(IEventHandler<Unit> handler) => m_Handlers.Add(handler);

    public bool Unsubscribe(IEventHandler<Unit> handler) => m_Handlers.Remove(handler);

    public void Clear() => m_Handlers.Clear();

    public void Fire()
    {
        foreach (var handler in m_Handlers.ToArray()) handler.Handle(Unit.Instance);
    }
}

/// <summary>
/// A listener whose <see cref="EventAttribute"/> methods receive their generated
/// <see cref="IEventListener"/> implementation from the Events source generator.
/// </summary>
internal sealed partial class AttributeListener : IEventListener
{
    public readonly List<int> Counts = [];
    public readonly List<string> Messages = [];
    public readonly List<(int, string)> Pairs = [];
    public int Ticks;

    [Event(typeof(CountEvent))]
    private void OnCount(int value) => Counts.Add(value);

    [Event<MessageEvent>]
    private void OnMessage(string message) => Messages.Add(message);

    [Event<PairEvent>]
    private void OnPair(int number, string text) => Pairs.Add((number, text));

    [Event(typeof(TickEvent))]
    private void OnTick() => Ticks++;
}