using Gubbins.Context;
using Gubbins.Spawner;

namespace Gubbins.Context.Tests;

[TestFixture]
public sealed class InjectAttributeTests
{
    private interface IDep;

    private sealed class Dep : IDep;

    private sealed class CtorTarget(IDep dep)
    {
        public readonly IDep Dep = dep;
    }

    private sealed class MarkedCtorTarget
    {
        public readonly bool  UsedInject;
        public readonly IDep? Dep;

        public MarkedCtorTarget() => UsedInject = false;

        [Inject]
        public MarkedCtorTarget(IDep dep)
        {
            Dep        = dep;
            UsedInject = true;
        }
    }

    private sealed class GreedyCtorTarget
    {
        public readonly int   Count;
        public readonly IDep? A;
        public readonly IDep? B;

        public GreedyCtorTarget() => Count = 0;

        public GreedyCtorTarget(IDep a)
        {
            A     = a;
            Count = 1;
        }

        public GreedyCtorTarget(IDep a, IDep b)
        {
            A     = a;
            B     = b;
            Count = 2;
        }
    }

    private sealed class KeyedCtorTarget([Inject("special")] IDep dep)
    {
        public readonly IDep Dep = dep;
    }

    private sealed class MethodTarget
    {
        public IDep? Dep;

        [Inject]
        public void Setup(IDep dep) => Dep = dep;
    }

    private sealed class KeyedMethodTarget
    {
        public IDep? Dep;

        [Inject]
        public void Setup([Inject("special")] IDep dep) => Dep = dep;
    }

    private sealed class CombinedTarget(IDep ctorDep)
    {
        public readonly IDep CtorDep = ctorDep;

        [Inject] public IDep FieldDep = null!;

        [Inject] public IDep PropDep { get; set; } = null!;

        public IDep? MethodDep;

        [Inject]
        public void Init(IDep dep) => MethodDep = dep;
    }

    private interface IMissing;

    private sealed class Plain;

    private sealed class MultiInjectCtor
    {
        [Inject] public MultiInjectCtor(IDep a) { }

        [Inject] public MultiInjectCtor(IDep a, IDep b) { }
    }

    private sealed class NeedsMissingCtor(IMissing missing)
    {
        public readonly IMissing Missing = missing;
    }

    private sealed class Leaf;

    private sealed class Root(Leaf leaf)
    {
        public readonly Leaf Leaf = leaf;
    }

    private sealed class PingTarget
    {
        public bool Pinged;

        [Inject] public void Ping() => Pinged = true;
    }

    private sealed class MultiMethodTarget
    {
        public IDep? First;
        public IDep? Second;

        [Inject] public void A(IDep dep) => First = dep;

        [Inject] public void B(IDep dep) => Second = dep;
    }

    private class BaseWithInject
    {
        public int Calls;

        [Inject] public void OnInject(IDep dep) => Calls++;
    }

    private sealed class DerivedTarget : BaseWithInject;

    private sealed class MethodNeedsMissing
    {
        [Inject] public void Setup(IMissing missing) { }
    }

    private sealed class CycleA(CycleB b)
    {
        public readonly CycleB B = b;
    }

    private sealed class CycleB(CycleA a)
    {
        public readonly CycleA A = a;
    }

    private sealed class SelfCycle(SelfCycle self)
    {
        public readonly SelfCycle Self = self;
    }

    private sealed class TestSpawner(Func<object> factory) : ISpawner
    {
        public object? Spawn() => factory();
    }

    private sealed class DelegateInstaller(Action<IDependenciesRegistry> install) : IDependenciesInstaller
    {
        public void Install(IDependenciesRegistry registry) => install(registry);
    }

    private static ApplicationContext Build(Action<IDependenciesRegistry> install)
        => new([new DelegateInstaller(install)]);

    // ---- Constructor injection ----

    [Test]
    public void Resolve_SpawnerlessTypeWithCtorDependency_ShouldInjectConstructor()
    {
        var dep = new Dep();
        var context = Build(r =>
        {
            r.Register(dep).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(CtorTarget)).AsSingleton();
        });

        var target = context.Resolve<CtorTarget>();

        Assert.That(target, Is.Not.Null);
        Assert.That(target!.Dep, Is.SameAs(dep));
    }

    [Test]
    public void Resolve_MultipleConstructors_ShouldPreferInjectMarkedConstructor()
    {
        var context = Build(r =>
        {
            r.Register(new Dep()).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(MarkedCtorTarget)).AsSingleton();
        });

        var target = context.Resolve<MarkedCtorTarget>();

        Assert.That(target!.UsedInject, Is.True);
        Assert.That(target.Dep, Is.Not.Null);
    }

    [Test]
    public void Resolve_UnmarkedConstructors_ShouldPickGreediestPublicConstructor()
    {
        var context = Build(r =>
        {
            r.Register(new Dep()).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(GreedyCtorTarget)).AsSingleton();
        });

        var target = context.Resolve<GreedyCtorTarget>();

        Assert.That(target!.Count, Is.EqualTo(2));
        Assert.That(target.A, Is.Not.Null);
        Assert.That(target.B, Is.Not.Null);
    }

    [Test]
    public void Resolve_ConstructorParameterWithKey_ShouldResolveByKey()
    {
        var special = new Dep();
        var context = Build(r =>
        {
            r.Register(special).WithKey("special").AsSingleton();
            r.Register(typeof(KeyedCtorTarget)).AsSingleton();
        });

        var target = context.Resolve<KeyedCtorTarget>();

        Assert.That(target!.Dep, Is.SameAs(special));
    }

    [Test]
    public void Resolve_SpawnerRegistered_ShouldNotUseConstructorInjection()
    {
        var fromSpawner = new CtorTarget(new Dep());
        var context = Build(r =>
        {
            r.Register(new Dep()).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(CtorTarget)).AsSingleton().WithSpawner(new TestSpawner(() => fromSpawner));
        });

        Assert.That(context.Resolve<CtorTarget>(), Is.SameAs(fromSpawner));
    }

    // ---- Method injection ----

    [Test]
    public void Inject_MethodWithDependency_ShouldInvokeWithResolvedArgument()
    {
        var dep = new Dep();
        var target = new MethodTarget();
        Build(r =>
        {
            r.Register(dep).BindTo(typeof(IDep)).AsSingleton();
            r.Register(target).AsSingleton();
        });

        Assert.That(target.Dep, Is.SameAs(dep));
    }

    [Test]
    public void Inject_MethodParameterWithKey_ShouldResolveByKey()
    {
        var special = new Dep();
        var target = new KeyedMethodTarget();
        Build(r =>
        {
            r.Register(special).WithKey("special").AsSingleton();
            r.Register(target).AsSingleton();
        });

        Assert.That(target.Dep, Is.SameAs(special));
    }

    // ---- Combined ----

    [Test]
    public void Resolve_TargetWithCtorFieldPropertyAndMethod_ShouldInjectAll()
    {
        var dep = new Dep();
        var context = Build(r =>
        {
            r.Register(dep).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(CombinedTarget)).AsSingleton();
        });

        var target = context.Resolve<CombinedTarget>();

        Assert.That(target, Is.Not.Null);
        Assert.That(target!.CtorDep, Is.SameAs(dep));
        Assert.That(target.FieldDep, Is.SameAs(dep));
        Assert.That(target.PropDep, Is.SameAs(dep));
        Assert.That(target.MethodDep, Is.SameAs(dep));
    }

    // ---- Boundary cases ----

    [Test]
    public void Resolve_SpawnerlessParameterlessType_ShouldConstructViaDefaultConstructor()
    {
        var context = Build(r => r.Register(typeof(Plain)).AsSingleton());

        Assert.That(context.Resolve<Plain>(), Is.Not.Null);
    }

    [Test]
    public void Resolve_MultipleInjectConstructors_ShouldThrowArgumentException()
    {
        var context = Build(r =>
        {
            r.Register(new Dep()).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(MultiInjectCtor)).AsSingleton();
        });

        Assert.Throws<ArgumentException>(() => context.Resolve<MultiInjectCtor>());
    }

    [Test]
    public void Resolve_UnresolvableConstructorParameter_ShouldThrowArgumentException()
    {
        var context = Build(r => r.Register(typeof(NeedsMissingCtor)).AsSingleton());

        Assert.Throws<ArgumentException>(() => context.Resolve<NeedsMissingCtor>());
    }

    [Test]
    public void Resolve_TransitiveConstructorInjection_ShouldConstructDependencyChain()
    {
        var context = Build(r =>
        {
            r.Register(typeof(Leaf)).AsSingleton();
            r.Register(typeof(Root)).AsSingleton();
        });

        var root = context.Resolve<Root>();

        Assert.That(root, Is.Not.Null);
        Assert.That(root!.Leaf, Is.Not.Null);
    }

    [Test]
    public void Resolve_MultitonConstructorInjection_ShouldReturnDistinctInstancesSharingSingletonDependency()
    {
        var dep = new Dep();
        var context = Build(r =>
        {
            r.Register(dep).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(CtorTarget)).AsMultiton();
        });

        var first = context.Resolve<CtorTarget>();
        var second = context.Resolve<CtorTarget>();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first!.Dep, Is.SameAs(dep));
        Assert.That(second!.Dep, Is.SameAs(dep));
    }

    [Test]
    public void Inject_ParameterlessMethod_ShouldStillInvoke()
    {
        var target = new PingTarget();
        Build(r => r.Register(target).AsSingleton());

        Assert.That(target.Pinged, Is.True);
    }

    [Test]
    public void Inject_MultipleMethods_ShouldInvokeAll()
    {
        var dep = new Dep();
        var target = new MultiMethodTarget();
        Build(r =>
        {
            r.Register(dep).BindTo(typeof(IDep)).AsSingleton();
            r.Register(target).AsSingleton();
        });

        Assert.That(target.First, Is.SameAs(dep));
        Assert.That(target.Second, Is.SameAs(dep));
    }

    [Test]
    public void Inject_InheritedMethod_ShouldInvokeExactlyOnce()
    {
        var target = new DerivedTarget();
        Build(r =>
        {
            r.Register(new Dep()).BindTo(typeof(IDep)).AsSingleton();
            r.Register(target).AsSingleton();
        });

        Assert.That(target.Calls, Is.EqualTo(1));
    }

    [Test]
    public void Inject_UnresolvableMethodParameter_ShouldThrowArgumentException()
    {
        var context = Build(r => r.Register(typeof(MethodNeedsMissing)).AsSingleton());

        Assert.Throws<ArgumentException>(() => context.Resolve<MethodNeedsMissing>());
    }

    [Test]
    public void Resolve_CircularConstructorDependency_ShouldThrowInvalidOperationException()
    {
        var context = Build(r =>
        {
            r.Register(typeof(CycleA)).AsSingleton();
            r.Register(typeof(CycleB)).AsSingleton();
        });

        Assert.Throws<InvalidOperationException>(() => context.Resolve<CycleA>());
    }

    [Test]
    public void Resolve_SelfConstructorDependency_ShouldThrowInvalidOperationException()
    {
        var context = Build(r => r.Register(typeof(SelfCycle)).AsSingleton());

        Assert.Throws<InvalidOperationException>(() => context.Resolve<SelfCycle>());
    }

    [Test]
    public void Resolve_AfterCircularDependencyFailure_ShouldStillResolveOtherTypes()
    {
        var dep = new Dep();
        var context = Build(r =>
        {
            r.Register(dep).BindTo(typeof(IDep)).AsSingleton();
            r.Register(typeof(CycleA)).AsSingleton();
            r.Register(typeof(CycleB)).AsSingleton();
            r.Register(typeof(CtorTarget)).AsSingleton();
        });

        Assert.Throws<InvalidOperationException>(() => context.Resolve<CycleA>());
        Assert.That(context.Resolve<CtorTarget>()!.Dep, Is.SameAs(dep));
    }
}
