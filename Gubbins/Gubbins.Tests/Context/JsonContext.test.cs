
namespace Gubbins.Context.Tests;

[TestFixture]
public class JsonContextTests
{
    [Test]
    public void InjectTest()
    {
        Assert.IsNotNull(Type.GetType("Gubbins.Tests.ApplicationContext.JsonContextTests+A"));
        var json = File.ReadAllText("Assets/context.json");
        var context = new JsonContext(json);
    }

    public class A
    {
        [Inject] public B B;
        [Inject] public C C;
    }

    public class B
    {
        public int value;
    }

    public class C
    {
        public int value;
    }
}