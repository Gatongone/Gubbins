namespace Gubbins.Enhance.Tests;

[Duck]
internal interface IAdder
{
    int Add(int value);
}

[Duck]
internal interface IGenericEcho
{
    T Echo<T>(T value);
}

[Duck]
public delegate int NumberMapper(int value);

internal sealed class AdderHost
{
    public int Add(int value) => value + 1;
}

internal sealed class IncompleteAdderHost
{
    public int Subtract(int value) => value - 1;
}

internal sealed class GenericEchoHost
{
    public T Echo<T>(T value) => value;
}

internal sealed class NumberMapperHost
{
    public int NumberMapper(int value) => value * 2;
}

internal sealed class MissingNumberMapperHost
{
    public int Other(int value) => value;
}

internal sealed class DirectAdder : IAdder
{
    public int Add(int value) => value + 10;
}

internal sealed class AdderHostTen
{
    public int Add(int value) => value + 10;
}

[TestFixture]
public class DuckTests
{
    [Test]
    public void Like_Interface_ShouldCreateProxyAndInvoke()
    {
        var ok = Duck.Like<IAdder>(new AdderHost(), out var result, out var handle);
        using var _ = handle;

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Add(2), Is.EqualTo(3));
        });
    }

    [Test]
    public void Like_DirectTypeHit_ShouldReturnSameInstance()
    {
        var direct = new DirectAdder();

        var ok = Duck.Like<IAdder>(direct, out var result, out var handle);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(result, Is.SameAs(direct));
            Assert.That(result!.Add(1), Is.EqualTo(11));
        });

        // Direct hit path should not allocate a pooled proxy handle.
        Assert.DoesNotThrow(() => handle.Dispose());
    }

    [Test]
    public void Like_NullInput_ShouldReturnFalse()
    {
        var ok = Duck.Like<IAdder>(null!, out var result, out var handle);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(result, Is.Null);
        });

        Assert.DoesNotThrow(() => handle.Dispose());
    }

    [Test]
    public void Like_InterfaceMissingSignature_ShouldReturnFalse()
    {
        var ok = Duck.Like<IAdder>(new IncompleteAdderHost(), out var result, out var handle);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(result, Is.Null);
        });

        Assert.DoesNotThrow(() => handle.Dispose());
    }

    [Test]
    public void Like_Delegate_ShouldBindAndInvoke()
    {
        var ok = Duck.Like<NumberMapper>(new NumberMapperHost(), out var result, out var handle);
        using var _ = handle;

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Invoke(3), Is.EqualTo(6));
        });
    }

    [Test]
    public void Like_DelegateMissingSignature_ShouldReturnFalse()
    {
        var ok = Duck.Like<NumberMapper>(new MissingNumberMapperHost(), out var result, out var handle);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(result, Is.Null);
        });

        Assert.DoesNotThrow(() => handle.Dispose());
    }

    [Test]
    public void Like_GenericMethodInterface_ShouldAdaptAndInvoke()
    {
        var ok = Duck.Like<IGenericEcho>(new GenericEchoHost(), out var result, out var handle);
        using var _ = handle;

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Echo(42), Is.EqualTo(42));
            Assert.That(result.Echo("duck"), Is.EqualTo("duck"));
        });
    }

    [Test]
    public void Like_AfterHandleDispose_PreviousProxyReferenceShouldNotBeReusedByCaller()
    {
        var firstHost = new AdderHost();
        var secondHost = new AdderHostTen();

        Assert.That(Duck.Like<IAdder>(firstHost, out var firstProxy, out var firstHandle), Is.True);
        Assert.That(firstProxy!.Add(1), Is.EqualTo(2));

        firstHandle.Dispose();

        Assert.That(Duck.Like<IAdder>(secondHost, out var secondProxy, out var secondHandle), Is.True);
        using var _ = secondHandle;

        Assert.Multiple(() =>
        {
            Assert.That(secondProxy, Is.Not.Null);
            Assert.That(secondProxy!.Add(1), Is.EqualTo(11));
            Assert.That(firstProxy.Add(1), Is.EqualTo(11));
        });
    }
}

