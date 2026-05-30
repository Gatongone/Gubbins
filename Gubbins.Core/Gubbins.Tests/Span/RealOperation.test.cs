namespace Gubbins.Span.Tests;

public abstract class RealOperationTestsBase<T> where T : struct
{
    protected abstract ISpanRealOperation<T> CreateOperation();

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

    protected void TestClampSpan()
    {
        var op = CreateOperation();
        var (src, min, max, expected) = CreateClampSpanTestData();
        var result = new T[src.Length];

        op.Clamp(src, min, max, result);

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    protected void TestClampScalar()
    {
        var op = CreateOperation();
        var (src, min, max, expected) = CreateClampScalarTestData();
        var result = new T[src.Length];

        op.Clamp(src, min, max, result);

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    protected void TestLerpSpan()
    {
        var op = CreateOperation();
        var (x, y, amount, expected) = CreateLerpSpanTestData();
        var result = new T[x.Length];

        op.Lerp(x, y, amount, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestLerpScalar()
    {
        var op = CreateOperation();
        var (x, y, amount, expected) = CreateLerpScalarTestData();
        var result = new T[x.Length];

        op.Lerp(x, y, amount, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestHypotSpan()
    {
        var op = CreateOperation();
        var (x, y, expected) = CreateHypotSpanTestData();
        var result = new T[x.Length];

        op.Hypot(x, y, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestHypotScalar()
    {
        var op = CreateOperation();
        var (x, y, expected) = CreateHypotScalarTestData();
        var result = new T[x.Length];

        op.Hypot(x, y, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestPowScalar()
    {
        var op = CreateOperation();
        var (src, exponent, expected) = CreatePowScalarTestData();
        var result = new T[src.Length];

        op.Pow(src, exponent, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestPowSpan()
    {
        var op = CreateOperation();
        var (src, exponent, expected) = CreatePowSpanTestData();
        var result = new T[src.Length];

        op.Pow(src, exponent, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestSin()
    {
        var op = CreateOperation();
        var (src, expected) = CreateSinTestData();
        var result = new T[src.Length];

        op.Sin(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestCos()
    {
        var op = CreateOperation();
        var (src, expected) = CreateCosTestData();
        var result = new T[src.Length];

        op.Cos(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestTan()
    {
        var op = CreateOperation();
        var (src, expected) = CreateTanTestData();
        var result = new T[src.Length];

        op.Tan(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestSinh()
    {
        var op = CreateOperation();
        var (src, expected) = CreateSinhTestData();
        var result = new T[src.Length];

        op.Sinh(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestCosh()
    {
        var op = CreateOperation();
        var (src, expected) = CreateCoshTestData();
        var result = new T[src.Length];

        op.Cosh(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestTanh()
    {
        var op = CreateOperation();
        var (src, expected) = CreateTanhTestData();
        var result = new T[src.Length];

        op.Tanh(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestAsin()
    {
        var op = CreateOperation();
        var (src, expected) = CreateAsinTestData();
        var result = new T[src.Length];

        op.Asin(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestAcos()
    {
        var op = CreateOperation();
        var (src, expected) = CreateAcosTestData();
        var result = new T[src.Length];

        op.Acos(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestAtan()
    {
        var op = CreateOperation();
        var (src, expected) = CreateAtanTestData();
        var result = new T[src.Length];

        op.Atan(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestAsinh()
    {
        var op = CreateOperation();
        var (src, expected) = CreateAsinhTestData();
        var result = new T[src.Length];

        op.Asinh(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestAcosh()
    {
        var op = CreateOperation();
        var (src, expected) = CreateAcoshTestData();
        var result = new T[src.Length];

        op.Acosh(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected void TestAtanh()
    {
        var op = CreateOperation();
        var (src, expected) = CreateAtanhTestData();
        var result = new T[src.Length];

        op.Atanh(src, result);

        AssertTolerance(expected, result, GetTolerance() * 10);
    }

    protected abstract (T[] src, T[] expected) CreateFloorTestData();
    protected abstract (T[] src, T[] expected) CreateCeilingTestData();
    protected abstract (T[] src, T[] expected) CreateSqrtTestData();
    protected abstract (T[] src, T[] expected) CreateRoundTestData();
    protected abstract (T[] src, T[] expected) CreateExpTestData();
    protected abstract (T[] src, T[] expected) CreateLogTestData();
    protected abstract (T[] src, T[] expected) CreateTruncateTestData();
    protected abstract (T[] src, T[] min, T[] max, T[] expected) CreateClampSpanTestData();
    protected abstract (T[] src, T min, T max, T[] expected) CreateClampScalarTestData();
    protected abstract (T[] x, T[] y, T[] amount, T[] expected) CreateLerpSpanTestData();
    protected abstract (T[] x, T y, T[] amount, T[] expected) CreateLerpScalarTestData();
    protected abstract (T[] x, T[] y, T[] expected) CreateHypotSpanTestData();
    protected abstract (T[] x, T y, T[] expected) CreateHypotScalarTestData();
    protected abstract (T[] src, T exponent, T[] expected) CreatePowScalarTestData();
    protected abstract (T[] src, T[] exponent, T[] expected) CreatePowSpanTestData();
    protected abstract (T[] src, T[] expected) CreateSinTestData();
    protected abstract (T[] src, T[] expected) CreateCosTestData();
    protected abstract (T[] src, T[] expected) CreateTanTestData();
    protected abstract (T[] src, T[] expected) CreateSinhTestData();
    protected abstract (T[] src, T[] expected) CreateCoshTestData();
    protected abstract (T[] src, T[] expected) CreateTanhTestData();
    protected abstract (T[] src, T[] expected) CreateAsinTestData();
    protected abstract (T[] src, T[] expected) CreateAcosTestData();
    protected abstract (T[] src, T[] expected) CreateAtanTestData();
    protected abstract (T[] src, T[] expected) CreateAsinhTestData();
    protected abstract (T[] src, T[] expected) CreateAcoshTestData();
    protected abstract (T[] src, T[] expected) CreateAtanhTestData();
    protected abstract double GetTolerance();

    protected void AssertTolerance(T[] expected, T[] actual, double tolerance)
    {
        Assert.Multiple(() =>
        {
            for (var i = 0; i < expected.Length; i++)
            {
                if (typeof(T) == typeof(float))
                {
                    var exp = (float) (object) expected[i];
                    var act = (float) (object) actual[i];
                    Assert.That(act, Is.EqualTo(exp).Within((float) tolerance));
                }
                else if (typeof(T) == typeof(double))
                {
                    var exp = (double) (object) expected[i];
                    var act = (double) (object) actual[i];
                    Assert.That(act, Is.EqualTo(exp).Within(tolerance));
                }
            }
        });
    }
}

public abstract class FloatRealOperationTestsBase : RealOperationTestsBase<float>
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

    [Test]
    public void Clamp_WithSpan_ShouldMatchScalar() => TestClampSpan();

    [Test]
    public void Clamp_WithScalar_ShouldMatchScalar() => TestClampScalar();

    [Test]
    public void Lerp_WithSpan_ShouldMatchScalar() => TestLerpSpan();

    [Test]
    public void Lerp_WithScalar_ShouldMatchScalar() => TestLerpScalar();

    [Test]
    public void Hypot_WithSpan_ShouldMatchScalar() => TestHypotSpan();

    [Test]
    public void Hypot_WithScalar_ShouldMatchScalar() => TestHypotScalar();

    [Test]
    public void Pow_WithScalar_ShouldMatchScalar() => TestPowScalar();

    [Test]
    public void Pow_WithSpan_ShouldMatchScalar() => TestPowSpan();

    [Test]
    public void Sin_ShouldMatchScalar() => TestSin();

    [Test]
    public void Cos_ShouldMatchScalar() => TestCos();

    [Test]
    public void Tan_ShouldMatchScalar() => TestTan();

    [Test]
    public void Sinh_ShouldMatchScalar() => TestSinh();

    [Test]
    public void Cosh_ShouldMatchScalar() => TestCosh();

    [Test]
    public void Tanh_ShouldMatchScalar() => TestTanh();

    [Test]
    public void Asin_ShouldMatchScalar() => TestAsin();

    [Test]
    public void Acos_ShouldMatchScalar() => TestAcos();

    [Test]
    public void Atan_ShouldMatchScalar() => TestAtan();

    [Test]
    public void Asinh_ShouldMatchScalar() => TestAsinh();

    [Test]
    public void Acosh_ShouldMatchScalar() => TestAcosh();

    [Test]
    public void Atanh_ShouldMatchScalar() => TestAtanh();

    protected override (float[] src, float[] expected) CreateFloorTestData()
    {
        var src = new[] {1.5f, 2.9f, -3.2f, 0f, -0.5f, 100.99f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Floor(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateCeilingTestData()
    {
        var src = new[] {1.5f, 2.1f, -3.9f, 0f, -0.5f, 100.01f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Ceiling(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateSqrtTestData()
    {
        var src = new[] {0f, 1f, 4f, 9f, 16f, 25f, 100f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Sqrt(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateRoundTestData()
    {
        var src = new[] {1.4f, 1.5f, 2.5f, -1.5f, 0f, 100.49f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Round(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateExpTestData()
    {
        var src = new[] {0f, 1f, -1f, 2f, -2f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Exp(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateLogTestData()
    {
        var src = new[] {1f, 2f, 10f, 100f, 0.5f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Log(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateTruncateTestData()
    {
        var src = new[] {1.9f, -1.9f, 0f, 100.5f, -0.1f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Truncate(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] min, float[] max, float[] expected) CreateClampSpanTestData()
    {
        var src = new[] {1.5f, -2.5f, 5f, 0f, -1f};
        var min = new[] {1f, -3f, 2f, -1f, -2f};
        var max = new[] {2f, -1f, 10f, 1f, -1f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Clamp(src[i], min[i], max[i]);
        return (src, min, max, expected);
    }

    protected override (float[] src, float min, float max, float[] expected) CreateClampScalarTestData()
    {
        var src = new[] {1.5f, -2.5f, 5f, 0f, -1f};
        var expected = new float[src.Length];
        const float min = -1f;
        const float max = 2f;
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Clamp(src[i], min, max);
        return (src, min, max, expected);
    }

    protected override (float[] x, float[] y, float[] amount, float[] expected) CreateLerpSpanTestData()
    {
        var x = new[] {0f, 1f, 2f};
        var y = new[] {10f, -1f, 2f};
        var amount = new[] {0f, 0.5f, 1f};
        var expected = new float[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = x[i] + (y[i] - x[i]) * amount[i];
        return (x, y, amount, expected);
    }

    protected override (float[] x, float y, float[] amount, float[] expected) CreateLerpScalarTestData()
    {
        var x = new[] {1f, 2f, 3f, 4f};
        const float y = 2.5f;
        var amount = new[] {0f, 0.25f, 0.5f, 1f};
        var expected = new float[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = x[i] + (y - x[i]) * amount[i];
        return (x, y, amount, expected);
    }

    protected override (float[] x, float[] y, float[] expected) CreateHypotSpanTestData()
    {
        var x = new[] {3f, 0f, -3f};
        var y = new[] {4f, 5f, 0f};
        var expected = new float[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = MathF.Sqrt(x[i] * x[i] + y[i] * y[i]);
        return (x, y, expected);
    }

    protected override (float[] x, float y, float[] expected) CreateHypotScalarTestData()
    {
        var x = new[] {3f, 0f, -3f};
        const float y = 4.2f;
        var expected = new float[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = MathF.Sqrt(x[i] * x[i] + y * y);
        return (x, y, expected);
    }

    protected override (float[] src, float exponent, float[] expected) CreatePowScalarTestData()
    {
        var src = new[] {1f, 2f, 4f, 9f};
        const float exponent = 2f;
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Pow(src[i], exponent);
        return (src, exponent, expected);
    }

    protected override (float[] src, float[] exponent, float[] expected) CreatePowSpanTestData()
    {
        var src = new[] {1f, 2f, 4f, 9f};
        var exponent = new[] {1f, 2f, 0.5f, 3f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Pow(src[i], exponent[i]);
        return (src, exponent, expected);
    }

    protected override (float[] src, float[] expected) CreateSinTestData()
    {
        var src = new[] {0f, MathF.PI / 6f, -MathF.PI / 4f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Sin(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateCosTestData()
    {
        var src = new[] {0f, MathF.PI / 6f, -MathF.PI / 4f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Cos(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateTanTestData()
    {
        var src = new[] {0f, MathF.PI / 6f, -MathF.PI / 4f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Tan(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateSinhTestData()
    {
        var src = new[] {-1f, 0f, 1f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Sinh(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateCoshTestData()
    {
        var src = new[] {-1f, 0f, 1f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Cosh(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateTanhTestData()
    {
        var src = new[] {-1f, 0f, 1f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Tanh(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateAsinTestData()
    {
        var src = new[] {0f, 0.5f, -0.5f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Asin(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateAcosTestData()
    {
        var src = new[] {0f, 0.5f, -0.5f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Acos(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateAtanTestData()
    {
        var src = new[] {-1f, 0f, 1f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Atan(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateAsinhTestData()
    {
        var src = new[] {-2f, 0f, 2f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Asinh(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateAcoshTestData()
    {
        var src = new[] {1f, 2f, 10f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Acosh(src[i]);
        return (src, expected);
    }

    protected override (float[] src, float[] expected) CreateAtanhTestData()
    {
        var src = new[] {-0.5f, 0f, 0.5f};
        var expected = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = MathF.Atanh(src[i]);
        return (src, expected);
    }

    protected override double GetTolerance() => 1e-5;
}

public abstract class DoubleRealOperationTestsBase : RealOperationTestsBase<double>
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

    [Test]
    public void Clamp_WithSpan_ShouldMatchScalar() => TestClampSpan();

    [Test]
    public void Clamp_WithScalar_ShouldMatchScalar() => TestClampScalar();

    [Test]
    public void Lerp_WithSpan_ShouldMatchScalar() => TestLerpSpan();

    [Test]
    public void Lerp_WithScalar_ShouldMatchScalar() => TestLerpScalar();

    [Test]
    public void Hypot_WithSpan_ShouldMatchScalar() => TestHypotSpan();

    [Test]
    public void Hypot_WithScalar_ShouldMatchScalar() => TestHypotScalar();

    [Test]
    public void Pow_WithScalar_ShouldMatchScalar() => TestPowScalar();

    [Test]
    public void Pow_WithSpan_ShouldMatchScalar() => TestPowSpan();

    [Test]
    public void Sin_ShouldMatchScalar() => TestSin();

    [Test]
    public void Cos_ShouldMatchScalar() => TestCos();

    [Test]
    public void Tan_ShouldMatchScalar() => TestTan();

    [Test]
    public void Sinh_ShouldMatchScalar() => TestSinh();

    [Test]
    public void Cosh_ShouldMatchScalar() => TestCosh();

    [Test]
    public void Tanh_ShouldMatchScalar() => TestTanh();

    [Test]
    public void Asin_ShouldMatchScalar() => TestAsin();

    [Test]
    public void Acos_ShouldMatchScalar() => TestAcos();

    [Test]
    public void Atan_ShouldMatchScalar() => TestAtan();

    [Test]
    public void Asinh_ShouldMatchScalar() => TestAsinh();

    [Test]
    public void Acosh_ShouldMatchScalar() => TestAcosh();

    [Test]
    public void Atanh_ShouldMatchScalar() => TestAtanh();

    protected override (double[] src, double[] expected) CreateFloorTestData()
    {
        var src = new[] {1.5, 2.9, -3.2, 0.0, -0.5, 100.99};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Floor(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateCeilingTestData()
    {
        var src = new[] {1.5, 2.1, -3.9, 0.0, -0.5, 100.01};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Ceiling(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateSqrtTestData()
    {
        var src = new[] {0.0, 1.0, 4.0, 9.0, 16.0, 25.0, 100.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Sqrt(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateRoundTestData()
    {
        var src = new[] {1.4, 1.5, 2.5, -1.5, 0.0, 100.49};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Round(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateExpTestData()
    {
        var src = new[] {0.0, 1.0, -1.0, 2.0, -2.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Exp(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateLogTestData()
    {
        var src = new[] {1.0, 2.0, 10.0, 100.0, 0.5};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Log(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateTruncateTestData()
    {
        var src = new[] {1.9, -1.9, 0.0, 100.5, -0.1};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Truncate(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] min, double[] max, double[] expected) CreateClampSpanTestData()
    {
        var src = new[] {1.5, -2.5, 5.0, 0.0, -1.0};
        var min = new[] {1.0, -3.0, 2.0, -1.0, -2.0};
        var max = new[] {2.0, -1.0, 10.0, 1.0, -1.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Clamp(src[i], min[i], max[i]);
        return (src, min, max, expected);
    }

    protected override (double[] src, double min, double max, double[] expected) CreateClampScalarTestData()
    {
        var src = new[] {1.5, -2.5, 5.0, 0.0, -1.0};
        var expected = new double[src.Length];
        const double min = -1.0;
        const double max = 2.0;
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Clamp(src[i], min, max);
        return (src, min, max, expected);
    }

    protected override (double[] x, double[] y, double[] amount, double[] expected) CreateLerpSpanTestData()
    {
        var x = new[] {0.0, 1.0, 2.0};
        var y = new[] {10.0, -1.0, 2.0};
        var amount = new[] {0.0, 0.5, 1.0};
        var expected = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = x[i] + (y[i] - x[i]) * amount[i];
        return (x, y, amount, expected);
    }

    protected override (double[] x, double y, double[] amount, double[] expected) CreateLerpScalarTestData()
    {
        var x = new[] {1.0, 2.0, 3.0, 4.0};
        const double y = 2.5;
        var amount = new[] {0.0, 0.25, 0.5, 1.0};
        var expected = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = x[i] + (y - x[i]) * amount[i];
        return (x, y, amount, expected);
    }

    protected override (double[] x, double[] y, double[] expected) CreateHypotSpanTestData()
    {
        var x = new[] {3.0, 0.0, -3.0};
        var y = new[] {4.0, 5.0, 0.0};
        var expected = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = Math.Sqrt(x[i] * x[i] + y[i] * y[i]);
        return (x, y, expected);
    }

    protected override (double[] x, double y, double[] expected) CreateHypotScalarTestData()
    {
        var x = new[] {3.0, 0.0, -3.0};
        const double y = 4.2;
        var expected = new double[x.Length];
        for (var i = 0; i < x.Length; i++)
            expected[i] = Math.Sqrt(x[i] * x[i] + y * y);
        return (x, y, expected);
    }

    protected override (double[] src, double exponent, double[] expected) CreatePowScalarTestData()
    {
        var src = new[] {1.0, 2.0, 4.0, 9.0};
        const double exponent = 2.0;
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Pow(src[i], exponent);
        return (src, exponent, expected);
    }

    protected override (double[] src, double[] exponent, double[] expected) CreatePowSpanTestData()
    {
        var src = new[] {1.0, 2.0, 4.0, 9.0};
        var exponent = new[] {1.0, 2.0, 0.5, 3.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Pow(src[i], exponent[i]);
        return (src, exponent, expected);
    }

    protected override (double[] src, double[] expected) CreateSinTestData()
    {
        var src = new[] {0.0, Math.PI / 6.0, -Math.PI / 4.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Sin(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateCosTestData()
    {
        var src = new[] {0.0, Math.PI / 6.0, -Math.PI / 4.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Cos(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateTanTestData()
    {
        var src = new[] {0.0, Math.PI / 6.0, -Math.PI / 4.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Tan(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateSinhTestData()
    {
        var src = new[] {-1.0, 0.0, 1.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Sinh(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateCoshTestData()
    {
        var src = new[] {-1.0, 0.0, 1.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Cosh(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateTanhTestData()
    {
        var src = new[] {-1.0, 0.0, 1.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Tanh(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateAsinTestData()
    {
        var src = new[] {0.0, 0.5, -0.5};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Asin(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateAcosTestData()
    {
        var src = new[] {0.0, 0.5, -0.5};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Acos(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateAtanTestData()
    {
        var src = new[] {-1.0, 0.0, 1.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Atan(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateAsinhTestData()
    {
        var src = new[] {-2.0, 0.0, 2.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Asinh(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateAcoshTestData()
    {
        var src = new[] {1.0, 2.0, 10.0};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Acosh(src[i]);
        return (src, expected);
    }

    protected override (double[] src, double[] expected) CreateAtanhTestData()
    {
        var src = new[] {-0.5, 0.0, 0.5};
        var expected = new double[src.Length];
        for (var i = 0; i < src.Length; i++)
            expected[i] = Math.Atanh(src[i]);
        return (src, expected);
    }

    protected override double GetTolerance() => 1e-10;
}

[TestFixture]
public class ParallelFloatOperationTests : FloatRealOperationTestsBase
{
        protected override ISpanRealOperation<float> CreateOperation() => new ParallelFloatOperation();
    }

    [TestFixture]
    public class ParallelDoubleOperationTests : DoubleRealOperationTestsBase
    {
        protected override ISpanRealOperation<double> CreateOperation() => new ParallelDoubleOperation();
    }

    [TestFixture]
    public class SerialFloatOperationTests : FloatRealOperationTestsBase
    {
        protected override ISpanRealOperation<float> CreateOperation() => new SerialFloatOperation();
    }

    [TestFixture]
    public class SerialDoubleOperationTests : DoubleRealOperationTestsBase
    {
        protected override ISpanRealOperation<double> CreateOperation() => new SerialDoubleOperation();
    }

    [TestFixture]
    public class ParallelSimdFloatOperationTests : FloatRealOperationTestsBase
    {
        protected override ISpanRealOperation<float> CreateOperation() => new ParallelSimdFloatOperation();
    }

    [TestFixture]
    public class ParallelSimdDoubleOperationTests : DoubleRealOperationTestsBase
    {
        protected override ISpanRealOperation<double> CreateOperation() => new ParallelSimdDoubleOperation();
    }

    [TestFixture]
    public class SimdFloatOperationTests : FloatRealOperationTestsBase
    {
        protected override ISpanRealOperation<float> CreateOperation() => new SimdFloatOperation();
    }

    [TestFixture]
    public class SimdDoubleOperationTests : DoubleRealOperationTestsBase
    {
        protected override ISpanRealOperation<double> CreateOperation() => new SimdDoubleOperation();
    }

