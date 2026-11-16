using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Context.Tests;

[TestFixture]
public sealed class DependencyExtensionsTests
{
    private interface IService;

    private interface IOther;

    private sealed class ServiceA : IService, IOther;

    private sealed class ServiceB : IService;

    private sealed class Consumer
    {
        [Inject] public IService Service = null!;
    }

    private sealed class TestSpawner(Func<object> factory) : ISpawner
    {
        public object? Spawn() => factory();
    }

    private sealed class FakeNewable(object instance) : INewable
    {
        public object New() => instance;
    }

    private sealed class DelegateInstaller(Action<IDependenciesRegistry> install) : IDependenciesInstaller
    {
        public void Install(IDependenciesRegistry registry) => install(registry);
    }

    private static ApplicationContext Build(Action<IDependenciesRegistry> install)
        => new([new DelegateInstaller(install)]);

    // ---- DependencyResolverExtensions ----

    [Test]
    public void ResolveGeneric_RegisteredType_ShouldReturnTypedInstance()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).AsSingleton());

        Assert.That(context.Resolve<ServiceA>(), Is.SameAs(instance));
    }

    [Test]
    public void ResolveGenericWithKey_RegisteredWithKey_ShouldReturnTypedInstance()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).WithKey("k").AsSingleton());

        Assert.That(context.Resolve<ServiceA>("k"), Is.SameAs(instance));
    }

    [Test]
    public void ResolveByKey_RegisteredWithKey_ShouldReturnInstance()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).WithKey("k").AsSingleton());

        Assert.That(context.Resolve("k"), Is.SameAs(instance));
    }

    [Test]
    public void ResolveByType_BoundToInterface_ShouldReturnInstance()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).BindTo(typeof(IService)).AsSingleton());

        Assert.That(context.Resolve(typeof(IService)), Is.SameAs(instance));
    }

    [Test]
    public void ResolveByType_ValueType_ShouldThrowInvalidOperationException()
    {
        var context = Build(_ => { });

        Assert.Throws<InvalidOperationException>(() => context.Resolve(typeof(int)));
    }

    [Test]
    public void ResolveAllGeneric_MultipleBindings_ShouldReturnTypedArray()
    {
        var a = new ServiceA();
        var b = new ServiceB();
        var context = Build(r =>
        {
            r.Register(a).BindTo(typeof(IService)).AsSingleton();
            r.Register(b).BindTo(typeof(IService)).AsSingleton();
        });

        IService[] all = context.ResolveAll<IService>();

        Assert.That(all, Has.Length.EqualTo(2));
        Assert.That(all, Does.Contain(a));
        Assert.That(all, Does.Contain(b));
    }

    [Test]
    public void Inject_TargetWithInjectMember_ShouldPopulateDependency()
    {
        var service = new ServiceA();
        var context = Build(r => r.Register(service).BindTo(typeof(IService)).AsSingleton());
        var consumer = new Consumer();

        context.Inject(consumer);

        Assert.That(consumer.Service, Is.SameAs(service));
    }

    // ---- BindingDecoratorExtensions (generic BindTo) ----

    [Test]
    public void BindToGeneric_SingleType_ShouldResolveByBindingInterface()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).BindTo<IService>().AsSingleton());

        Assert.That(context.Resolve(typeof(IService)), Is.SameAs(instance));
    }

    [Test]
    public void BindToGeneric_TwoTypes_ShouldResolveByBothInterfaces()
    {
        var instance = new ServiceA();
        var context = Build(r => r.Register(instance).BindTo<IService, IOther>().AsSingleton());

        Assert.That(context.Resolve(typeof(IService)), Is.SameAs(instance));
        Assert.That(context.Resolve(typeof(IOther)), Is.SameAs(instance));
    }

    [Test]
    public void BindToGeneric_OnTypeDecorator_ShouldResolveByBindingInterface()
    {
        var context = Build(r => r
            .Register(typeof(ServiceA))
            .BindTo<IOther>()
            .AsSingleton()
            .WithSpawner(new TestSpawner(() => new ServiceA())));

        Assert.That(context.Resolve(typeof(IOther)), Is.InstanceOf<ServiceA>());
    }

    // ---- DependencyRegistryExtensions (generic Register) ----

    [Test]
    public void RegisterGeneric_TypeOnly_ShouldResolveSpawnedInstance()
    {
        var context = Build(r => r
            .Register<ServiceA>()
            .AsSingleton()
            .WithSpawner(new TestSpawner(() => new ServiceA())));

        Assert.That(context.Resolve<ServiceA>(), Is.InstanceOf<ServiceA>());
    }

    [Test]
    public void RegisterGeneric_WithBinding_ShouldResolveByBindingInterface()
    {
        var context = Build(r => r
            .Register<ServiceA, IService>()
            .AsSingleton()
            .WithSpawner(new TestSpawner(() => new ServiceA())));

        Assert.That(context.Resolve(typeof(IService)), Is.InstanceOf<ServiceA>());
    }

    // ---- NewableExtensions ----

    [Test]
    public void ToSpawner_NewableProvided_ShouldSpawnFromNewable()
    {
        var instance = new ServiceA();
        var spawner = new FakeNewable(instance).ToSpawner();

        Assert.That(spawner.Spawn() as ServiceA, Is.SameAs(instance));
    }
}
