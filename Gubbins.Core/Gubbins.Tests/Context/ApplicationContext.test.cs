using Gubbins.Spawner;

namespace Gubbins.Context.Tests;

[TestFixture]
public sealed class ApplicationContextTests
{
    private interface IService;

    private sealed class ServiceA : IService
    {
        public int Value;
    }

    private sealed class ServiceB : IService;

    private sealed class Consumer
    {
        [Inject] public IService Service = null!;
    }

    private sealed class LifecycleConsumer : IResolvePreprocessing, IResolvePostprocessing
    {
        public bool Before;
        public bool After;

        public void BeforeResolve() => Before = true;
        public void AfterResolve()  => After = true;
    }

    private sealed class TestSpawner(Func<object> factory) : ISpawner
    {
        public object? Spawn() => factory();
    }

    private sealed class DelegateInstaller(Action<IDependenciesRegistry> install) : IDependenciesInstaller
    {
        public void Install(IDependenciesRegistry registry) => install(registry);
    }

    private static ApplicationContext Build(Action<IDependenciesRegistry> install, IContext? parent = null)
        => new([new DelegateInstaller(install)], parent);

    // ---- Construction ----

    [Test]
    public void Global_Always_ShouldExposeSharedContext()
    {
        Assert.That(ApplicationContext.Global, Is.Not.Null);
    }

    [Test]
    public void Parent_ConstructedWithParent_ShouldExposeParent()
    {
        var parent = Build(_ => { });
        var child = Build(_ => { }, parent);

        Assert.That(child.Parent, Is.SameAs(parent));
    }

    // ---- Resolution by type / key / binding ----

    [Test]
    public void Resolve_RegisteredSingletonInstance_ShouldReturnSameInstance()
    {
        var instance = new ServiceA {Value = 7};
        var context = Build(r => r.Register(instance).AsSingleton());

        Assert.That(context.Resolve<ServiceA>(), Is.SameAs(instance));
    }

    [Test]
    public void Resolve_BoundToInterface_ShouldResolveByBindingType()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).BindTo(typeof(IService)).AsSingleton());

        Assert.That(context.Resolve(typeof(IService)), Is.SameAs(instance));
    }

    [Test]
    public void Resolve_RegisteredWithKey_ShouldResolveByKey()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).WithKey("my-key").AsSingleton());

        Assert.That(context.Resolve<ServiceA>("my-key"), Is.SameAs(instance));
    }

    [Test]
    public void Resolve_UnknownType_ShouldReturnNull()
    {
        var context = Build(_ => { });

        Assert.That(context.Resolve<ServiceA>(), Is.Null);
    }

    // ---- Scope semantics ----

    [Test]
    public void Resolve_SingletonViaSpawner_ShouldCacheAndSpawnOnce()
    {
        var spawnCount = 0;
        var context = Build(r => r
            .Register(typeof(ServiceA))
            .AsSingleton()
            .WithSpawner(new TestSpawner(() =>
            {
                spawnCount++;
                return new ServiceA();
            })));

        var first = context.Resolve<ServiceA>();
        var second = context.Resolve<ServiceA>();

        Assert.That(first, Is.SameAs(second));
        Assert.That(spawnCount, Is.EqualTo(1));
    }

    [Test]
    public void Resolve_MultitonViaSpawner_ShouldReturnDistinctInstances()
    {
        var context = Build(r => r
            .Register(typeof(ServiceA))
            .AsMultiton()
            .WithSpawner(new TestSpawner(() => new ServiceA())));

        var first = context.Resolve<ServiceA>();
        var second = context.Resolve<ServiceA>();

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(first, Is.Not.SameAs(second));
    }

    [Test]
    public void Resolve_MultitonRegisteredWithInstances_ShouldReturnRegisteredInstances()
    {
        var first = new ServiceA();
        var second = new ServiceA();
        var context = Build(r => r.Register(new object[] {first, second}).WithKey("multi"));

        var resolvedA = context.Resolve("multi");
        var resolvedB = context.Resolve("multi");

        Assert.That(new[] {resolvedA, resolvedB}, Is.EquivalentTo(new object[] {first, second}));
    }

    [Test]
    public void Resolve_MultitonBaked_ShouldReturnDistinctBakedInstances()
    {
        var context = Build(r => r
            .Register(typeof(ServiceA))
            .AsMultiton()
            .WithSpawner(new TestSpawner(() => new ServiceA()))
            .Bake(2));

        var first = context.Resolve<ServiceA>();
        var second = context.Resolve<ServiceA>();

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(first, Is.Not.SameAs(second));
    }

    // ---- ResolveAll ----

    [Test]
    public void ResolveAll_MultipleBindings_ShouldReturnAllImplementations()
    {
        var a = new ServiceA();
        var b = new ServiceB();
        var context = Build(r =>
        {
            r.Register(a).BindTo(typeof(IService)).AsSingleton();
            r.Register(b).BindTo(typeof(IService)).AsSingleton();
        });

        var all = context.ResolveAll<IService>();

        Assert.That(all, Has.Length.EqualTo(2));
        Assert.That(all, Does.Contain(a));
        Assert.That(all, Does.Contain(b));
    }

    [Test]
    public void ResolveAll_NoRegistration_ShouldReturnEmpty()
    {
        var context = Build(_ => { });

        Assert.That(context.ResolveAll<IService>(), Is.Empty);
    }

    // ---- Parent fallback ----

    [Test]
    public void Resolve_MissingInChild_ShouldFallBackToParent()
    {
        var parentInstance = new ServiceA();
        var parent = Build(r => r.Register(parentInstance).AsSingleton());
        var child = Build(_ => { }, parent);

        Assert.That(child.Resolve<ServiceA>(), Is.SameAs(parentInstance));
    }

    [Test]
    public void Resolve_BoundInterfaceMissingInChild_ShouldFallBackToParent()
    {
        var parentInstance = new ServiceA();
        var parent = Build(r => r.Register(parentInstance).BindTo(typeof(IService)).AsSingleton());
        var child = Build(_ => { }, parent);

        Assert.That(child.Resolve(typeof(IService)), Is.SameAs(parentInstance));
    }

    // ---- Injection ----

    [Test]
    public void Inject_SingletonWithInjectMember_ShouldPopulateDependency()
    {
        var service = new ServiceA();
        var consumer = new Consumer();
        Build(r =>
        {
            r.Register(service).BindTo(typeof(IService)).AsSingleton();
            r.Register(consumer).AsSingleton();
        });

        Assert.That(consumer.Service, Is.SameAs(service));
    }

    [Test]
    public void Inject_LifecycleConsumer_ShouldInvokePreAndPostProcessing()
    {
        var consumer = new LifecycleConsumer();
        Build(r => r.Register(consumer).AsSingleton());

        Assert.That(consumer.Before, Is.True);
        Assert.That(consumer.After, Is.True);
    }

    // ---- Validation ----

    [Test]
    public void Register_InterfaceType_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Build(r => r.Register(typeof(IService))));
    }

    [Test]
    public void Register_NullInstance_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Build(r => r.Register((object) null!)));
    }

    [Test]
    public void Resolve_NullTypeAndNullKey_ShouldThrowArgumentException()
    {
        var context = Build(_ => { });

        Assert.Throws<ArgumentException>(() => context.Resolve(null, null));
    }

    [Test]
    public void Resolve_ValueType_ShouldThrowInvalidOperationException()
    {
        var context = Build(_ => { });

        Assert.Throws<InvalidOperationException>(() => context.Resolve(typeof(int)));
    }

    // ---- Disposal ----

    [Test]
    public void Dispose_WithSubscriber_ShouldInvokeOnScopeFinish()
    {
        var context = Build(_ => { });
        var fired = false;
        context.OnScopeFinish += () => fired = true;

        context.Dispose();

        Assert.That(fired, Is.True);
    }

    [Test]
    public void Dispose_AfterRegistration_ShouldClearResolutions()
    {
        var context = Build(r => r.Register(new ServiceA()).AsSingleton());

        context.Dispose();

        Assert.That(context.Resolve<ServiceA>(), Is.Null);
    }
}
