using Gubbins.Enhance;

namespace Gubbins.Unsafe.Tests;

[TestFixture]
public class NewableTests
{
    [Test]
    public void PublicEmptyParamsCtorTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(PublicCtor).IsNewable(out var newer), Is.True);
            Assert.That(newer, Is.Not.Null);
        });
    }

    [Test]
    public void PrivateEmptyParamsCtorTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(PrivateCtor).IsNewable(out var newer), Is.False);
            Assert.That(newer, Is.Null);
        });
    }

    [Test]
    public void NonEmptyParamsCtorTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(NonEmptyParamsCtor).IsNewable(out var newer), Is.False);
            Assert.That(newer, Is.Null);
        });
    }

    [Test]
    public void StructCtorTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(Struct).IsNewable(out var newer), Is.True);
            Assert.That(newer, Is.Not.Null);
        });
    }

    private struct Struct
    {
        public Struct(int _) { }
    }

    private class PublicCtor
    {
        public PublicCtor() { }
    }

    private class PrivateCtor
    {
        private PrivateCtor() { }
    }

    private class NonEmptyParamsCtor
    {
        public NonEmptyParamsCtor(int _) { }
    }
}