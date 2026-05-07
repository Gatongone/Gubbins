namespace Gubbins.Context.Tests;

[TestFixture]
public class ApplicationContextTests
{
    [Test]
    public void InjectTest()
    {
        var a = new A();
        var b = new B {value = 5};
        var c = new C {value = 6};
        var bInstaller = new InstallInfo
        {
            Instance = b,
            Scope = Scope.Singleton,
        };
        var cInstaller = new InstallInfo
        {
            Instance = c,
            Scope = Scope.Singleton,
        };
        var context = new ApplicationContext(new[] {bInstaller, cInstaller});
        context.Inject(a);
        Assert.That(a.B, Is.Not.Null);
        Assert.That(a.B.value, Is.EqualTo(5));
        Assert.That(a.C, Is.Not.Null);
        Assert.That(a.C.value, Is.EqualTo(6));
    }

    private class A
    {
        [Inject] public B B;
        [Inject] public C C;
    }

    private class B
    {
        public int value;
    }

    private class C
    {
        public int value;
    }
}