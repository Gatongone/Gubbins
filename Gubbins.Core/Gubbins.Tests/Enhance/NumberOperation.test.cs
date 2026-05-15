using Gubbins.Span;

namespace Gubbins.Unsafe.Tests;

public abstract class NumberOperationTestsBase<T> where T : struct
{
    protected abstract ISpanRealOperations<T> CreateOperation();

    protected void TestFloor()
    {
        var op = CreateOperation();
        var (src, expected) = CreateFloorTestData();
        var result = new T[src.Length];

        op.Floor(src, result);

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    protected void TestCeiling()
    {
        var op = CreateOperation();
        var (src, expected) = CreateCeilingTestData();
        var result = new T[src.Length];

        op.Ceiling(src, result);

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    protected void TestSqrt()
    {
        var op = CreateOperation();
        var (src, expected) = CreateSqrtTestData();
        var result = new T[src.Length];

        op.Sqrt(src, result);

        AssertTolerance(expected, result, GetTolerance());
    }

    protected void TestRound()
    {
        var op = CreateOperation();
        var (src, expected) = CreateRoundTestData();
        var result = new T[src.Length];

        op.Round(src, result);

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    protected void TestExp()
    {
        var op = CreateOperation();
        var (src, expected) = CreateExpTestData();
        var result = new T[src.Length];

        op.Exp(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestLog()
    {
        var op = CreateOperation();
        var (src, expected) = CreateLogTestData();
        var result = new T[src.Length];

        op.Log(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestTruncate()
    {
        var op = CreateOperation();
        var (src, expected) = CreateTruncateTestData();
        var result = new T[src.Length];

        op.Truncate(src, result);

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    protected abstract (T[] src, T[] expected) CreateFloorTestData();
    protected abstract (T[] src, T[] expected) CreateCeilingTestData();
    protected abstract (T[] src, T[] expected) CreateSqrtTestData();
    protected abstract (T[] src, T[] expected) CreateRoundTestData();
    protected abstract (T[] src, T[] expected) CreateExpTestData();
    protected abstract (T[] src, T[] expected) CreateLogTestData();
    protected abstract (T[] src, T[] expected) CreateTruncateTestData();
    protected abstract double GetTolerance();

    protected void AssertTolerance(T[] expected, T[] actual, double tolerance)
    {
        Assert.Multiple(() =>
        {
            for (var i = 0; i < expected.Length; i++)
            {
                if (typeof(T) == typeof(float))
                {
                    var exp = (float)(object)expected[i];
                    var act = (float)(object)actual[i];
                    Assert.That(act, Is.EqualTo(exp).Within((float)tolerance));
                }
                else if (typeof(T) == typeof(double))
                {
                    var exp = (double)(object)expected[i];
                    var act = (double)(object)actual[i];
                    Assert.That(act, Is.EqualTo(exp).Within(tolerance));
                }
            }
        });
    }
}

public abstract class FloatNumberOperationTestsBase : NumberOperationTestsBase<float>
{
    [Test]
    public void Floor_ShouldMatchScalar() => TestFloor();

    [Test]
    public void Ceiling_ShouldMatchScalar() => TestCeiling();

    [Test]
    public void Sqrt_ShouldMatchScalar() => TestSqrt();

    [Test]
    public void Round_ShouldMatchScalar() => TestRound();

    [Test]
    public void Exp_ShouldMatchScalar() => TestExp();

    [Test]
    public void Log_ShouldMatchScalar() => TestLog();

    [Test]
    public void Truncate_ShouldMatchScalar() => TestTruncate();

    protected override (float[] src, float[] expected) CreateFloorTestData()
    {
        var src = new[] { 1.5f, 2.9f, -3.2f, 0f, -0.5f, 100.99f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Floor(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateCeilingTestData()
    {
        var src = new[] { 1.5f, 2.1f, -3.9f, 0f, -0.5f, 100.01f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Ceiling(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateSqrtTestData()
    {
        var src = new[] { 0f, 1f, 4f, 9f, 16f, 25f, 100f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Sqrt(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateRoundTestData()
    {
        var src = new[] { 1.4f, 1.5f, 2.5f, -1.5f, 0f, 100.49f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Round(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateExpTestData()
    {
        var src = new[] { 0f, 1f, -1f, 2f, -2f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Exp(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateLogTestData()
    {
        var src = new[] { 1f, 2f, 10f, 100f, 0.5f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Log(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateTruncateTestData()
    {
        var src = new[] { 1.9f, -1.9f, 0f, 100.5f, -0.1f };
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Truncate(src[i]);
        return (src, expected);
    }

    protected override double GetTolerance() => 1e-5;
}

public abstract class DoubleNumberOperationTestsBase : NumberOperationTestsBase<double>
{
    [Test]
    public void Floor_ShouldMatchScalar() => TestFloor();

    [Test]
    public void Ceiling_ShouldMatchScalar() => TestCeiling();

    [Test]
    public void Sqrt_ShouldMatchScalar() => TestSqrt();

    [Test]
    public void Round_ShouldMatchScalar() => TestRound();

    [Test]
    public void Exp_ShouldMatchScalar() => TestExp();

    [Test]
    public void Log_ShouldMatchScalar() => TestLog();

    [Test]
    public void Truncate_ShouldMatchScalar() => TestTruncate();

    protected override (double[] src, double[] expected) CreateFloorTestData()
    {
        var src = new[] { 1.5, 2.9, -3.2, 0.0, -0.5, 100.99 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Floor(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateCeilingTestData()
    {
        var src = new[] { 1.5, 2.1, -3.9, 0.0, -0.5, 100.01 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Ceiling(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateSqrtTestData()
    {
        var src = new[] { 0.0, 1.0, 4.0, 9.0, 16.0, 25.0, 100.0 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Sqrt(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateRoundTestData()
    {
        var src = new[] { 1.4, 1.5, 2.5, -1.5, 0.0, 100.49 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Round(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateExpTestData()
    {
        var src = new[] { 0.0, 1.0, -1.0, 2.0, -2.0 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Exp(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateLogTestData()
    {
        var src = new[] { 1.0, 2.0, 10.0, 100.0, 0.5 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Log(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateTruncateTestData()
    {
        var src = new[] { 1.9, -1.9, 0.0, 100.5, -0.1 };
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Truncate(src[i]);
        return (src, expected);
    }

    protected override double GetTolerance() => 1e-10;
}

[TestFixture]
public class ParallelFloatOperationTests : FloatNumberOperationTestsBase
{
    protected override ISpanRealOperations<float> CreateOperation()
    {
        var type = typeof(ISpanRealOperations<float>).Assembly.GetType("Gubbins.Enhance.ParallelFloatOperation", throwOnError: true)!;
        return (ISpanRealOperations<float>)Activator.CreateInstance(type, nonPublic: true)!;
    }
}

[TestFixture]
public class ParallelDoubleOperationTests : DoubleNumberOperationTestsBase
{
    protected override ISpanRealOperations<double> CreateOperation()
    {
        var type = typeof(ISpanRealOperations<double>).Assembly.GetType("Gubbins.Enhance.ParallelDoubleOperation", throwOnError: true)!;
        return (ISpanRealOperations<double>)Activator.CreateInstance(type, nonPublic: true)!;
    }
}

[TestFixture]
public class SerialFloatOperationTests : FloatNumberOperationTestsBase
{
    protected override ISpanRealOperations<float> CreateOperation()
    {
        var type = typeof(ISpanRealOperations<float>).Assembly.GetType("Gubbins.Enhance.SerialFloatOperations", throwOnError: true)!;
        return (ISpanRealOperations<float>)Activator.CreateInstance(type, nonPublic: true)!;
    }
}

[TestFixture]
public class SerialDoubleOperationTests : DoubleNumberOperationTestsBase
{
    protected override ISpanRealOperations<double> CreateOperation()
    {
        var type = typeof(ISpanRealOperations<double>).Assembly.GetType("Gubbins.Enhance.SerialDoubleOperations", throwOnError: true)!;
        return (ISpanRealOperations<double>)Activator.CreateInstance(type, nonPublic: true)!;
    }
}

