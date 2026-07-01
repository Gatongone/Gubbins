namespace Gubbins.Span.Tests;

public abstract class NumberOperationTestsBase<T> where T : unmanaged
{
    protected abstract ISpanNumberOperation<T> CreateOperation();
    protected abstract T[] CreateReduceSource();
    protected abstract (T[] left, T[] right) CreatePairSources();
    protected abstract (T[] src, T operand) CreateOperandSource();

    [Test]
    public void GetMax_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateReduceSource();
        var expected = GetMaxScalar(src);

        var actual = op.GetMax(src);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetMin_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateReduceSource();
        var expected = GetMinScalar(src);

        var actual = op.GetMin(src);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Max_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (left, right) = CreatePairSources();
        var result = new T[left.Length];

        op.Max(left, right, result);

        var expected = new T[left.Length];
        for (var i = 0; i < left.Length; i++)
        {
            expected[i] = GreaterThan(left[i], right[i]) ? left[i] : right[i];
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Min_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (left, right) = CreatePairSources();
        var result = new T[left.Length];

        op.Min(left, right, result);

        var expected = new T[left.Length];
        for (var i = 0; i < left.Length; i++)
        {
            expected[i] = LessThan(left[i], right[i]) ? left[i] : right[i];
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Add_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (src, operand) = CreateOperandSource();
        var result = new T[src.Length];

        op.Add(src, operand, result);

        var expected = new T[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            expected[i] = AddScalar(src[i], operand);
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Subtract_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (src, operand) = CreateOperandSource();
        var result = new T[src.Length];

        op.Subtract(src, operand, result);

        var expected = new T[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            expected[i] = SubtractScalar(src[i], operand);
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Multiply_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (src, operand) = CreateOperandSource();
        var result = new T[src.Length];

        op.Multiply(src, operand, result);

        var expected = new T[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            expected[i] = MultiplyScalar(src[i], operand);
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Divide_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (src, operand) = CreateOperandSource();
        var result = new T[src.Length];

        op.Divide(src, operand, result);

        var expected = new T[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            expected[i] = DivideScalar(src[i], operand);
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Modulo_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var (src, operand) = CreateOperandSource();
        var result = new T[src.Length];

        op.Modulo(src, operand, result);

        var expected = new T[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            expected[i] = ModuloScalar(src[i], operand);
        }

        Assert.That(result, Is.EqualTo(expected).AsCollection);
    }

    private static T GetMaxScalar(T[] src)
    {
        var max = src[0];
        for (var i = 1; i < src.Length; i++)
        {
            if (GreaterThan(src[i], max))
            {
                max = src[i];
            }
        }

        return max;
    }

    private static T GetMinScalar(T[] src)
    {
        var min = src[0];
        for (var i = 1; i < src.Length; i++)
        {
            if (LessThan(src[i], min))
            {
                min = src[i];
            }
        }

        return min;
    }

    private static bool GreaterThan(T left, T right) => Comparer<T>.Default.Compare(left, right) > 0;
    private static bool LessThan(T left, T right) => Comparer<T>.Default.Compare(left, right) < 0;

    protected abstract T AddScalar(T left, T right);
    protected abstract T SubtractScalar(T left, T right);
    protected abstract T MultiplyScalar(T left, T right);
    protected abstract T DivideScalar(T left, T right);
    protected abstract T ModuloScalar(T left, T right);
}

public abstract class IntNumberOperationTestsBase : NumberOperationTestsBase<int>
{
    protected override int[] CreateReduceSource() => [3, -2, 7, 7, 0, -9, 5];

    protected override (int[] left, int[] right) CreatePairSources() =>
        ([1, -5, 9, 4, 0], [2, -5, 3, 10, 0]);

    protected override (int[] src, int operand) CreateOperandSource() =>
        ([3, -2, 7, 0, -9, 5], 2);

    protected override int AddScalar(int left, int right) => left + right;
    protected override int SubtractScalar(int left, int right) => left - right;
    protected override int MultiplyScalar(int left, int right) => left * right;
    protected override int DivideScalar(int left, int right) => left / right;
    protected override int ModuloScalar(int left, int right) => left % right;
}

public abstract class LongNumberOperationTestsBase : NumberOperationTestsBase<long>
{
    protected override long[] CreateReduceSource() => [3L, -2L, 7L, 7L, 0L, -9L, 5L];

    protected override (long[] left, long[] right) CreatePairSources() =>
        ([1L, -5L, 9L, 4L, 0L], [2L, -5L, 3L, 10L, 0L]);

    protected override (long[] src, long operand) CreateOperandSource() =>
        ([3L, -2L, 7L, 0L, -9L, 5L], 2L);

    protected override long AddScalar(long left, long right) => left + right;
    protected override long SubtractScalar(long left, long right) => left - right;
    protected override long MultiplyScalar(long left, long right) => left * right;
    protected override long DivideScalar(long left, long right) => left / right;
    protected override long ModuloScalar(long left, long right) => left % right;
}

public abstract class UintNumberOperationTestsBase : NumberOperationTestsBase<uint>
{
    protected override uint[] CreateReduceSource() => [3u, 2u, 7u, 7u, 0u, 9u, 5u];

    protected override (uint[] left, uint[] right) CreatePairSources() =>
        ([1u, 5u, 9u, 4u, 0u], [2u, 5u, 3u, 10u, 0u]);

    protected override (uint[] src, uint operand) CreateOperandSource() =>
        ([3u, 2u, 7u, 0u, 9u, 5u], 2u);

    protected override uint AddScalar(uint left, uint right) => left + right;
    protected override uint SubtractScalar(uint left, uint right) => left - right;
    protected override uint MultiplyScalar(uint left, uint right) => left * right;
    protected override uint DivideScalar(uint left, uint right) => left / right;
    protected override uint ModuloScalar(uint left, uint right) => left % right;
}

public abstract class UlongNumberOperationTestsBase : NumberOperationTestsBase<ulong>
{
    protected override ulong[] CreateReduceSource() => [3ul, 2ul, 7ul, 7ul, 0ul, 9ul, 5ul];

    protected override (ulong[] left, ulong[] right) CreatePairSources() =>
        ([1ul, 5ul, 9ul, 4ul, 0ul], [2ul, 5ul, 3ul, 10ul, 0ul]);

    protected override (ulong[] src, ulong operand) CreateOperandSource() =>
        ([3ul, 2ul, 7ul, 0ul, 9ul, 5ul], 2ul);

    protected override ulong AddScalar(ulong left, ulong right) => left + right;
    protected override ulong SubtractScalar(ulong left, ulong right) => left - right;
    protected override ulong MultiplyScalar(ulong left, ulong right) => left * right;
    protected override ulong DivideScalar(ulong left, ulong right) => left / right;
    protected override ulong ModuloScalar(ulong left, ulong right) => left % right;
}

public abstract class FloatNumberOperationTestsBase : NumberOperationTestsBase<float>
{
    protected override float[] CreateReduceSource() => [3.5f, -2f, 7f, 7.25f, 0f, -9f, 5.1f];

    protected override (float[] left, float[] right) CreatePairSources() =>
        ([1.5f, -5.1f, 9f, 4.2f, 0f], [2.5f, -5.1f, 3f, 10.1f, 0f]);

    protected override (float[] src, float operand) CreateOperandSource() =>
        ([3.5f, -2f, 7f, 0f, -9f, 5.1f], 2f);

    protected override float AddScalar(float left, float right) => left + right;
    protected override float SubtractScalar(float left, float right) => left - right;
    protected override float MultiplyScalar(float left, float right) => left * right;
    protected override float DivideScalar(float left, float right) => left / right;
    protected override float ModuloScalar(float left, float right) => left % right;
}

public abstract class DoubleNumberOperationTestsBase : NumberOperationTestsBase<double>
{
    protected override double[] CreateReduceSource() => [3.5, -2.0, 7.0, 7.25, 0.0, -9.0, 5.1];

    protected override (double[] left, double[] right) CreatePairSources() =>
        ([1.5, -5.1, 9.0, 4.2, 0.0], [2.5, -5.1, 3.0, 10.1, 0.0]);

    protected override (double[] src, double operand) CreateOperandSource() =>
        ([3.5, -2.0, 7.0, 0.0, -9.0, 5.1], 2.0);

    protected override double AddScalar(double left, double right) => left + right;
    protected override double SubtractScalar(double left, double right) => left - right;
    protected override double MultiplyScalar(double left, double right) => left * right;
    protected override double DivideScalar(double left, double right) => left / right;
    protected override double ModuloScalar(double left, double right) => left % right;
}

[TestFixture]
public class SerialIntNumberOperationTests : IntNumberOperationTestsBase
{
    protected override ISpanNumberOperation<int> CreateOperation() => new SerialNumberOperation<int>();
}

[TestFixture]
public class SerialLongNumberOperationTests : LongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<long> CreateOperation() => new SerialNumberOperation<long>();
}

[TestFixture]
public class SerialUintNumberOperationTests : UintNumberOperationTestsBase
{
    protected override ISpanNumberOperation<uint> CreateOperation() => new SerialNumberOperation<uint>();
}

[TestFixture]
public class SerialUlongNumberOperationTests : UlongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<ulong> CreateOperation() => new SerialNumberOperation<ulong>();
}

[TestFixture]
public class SerialFloatNumberOperationTests : FloatNumberOperationTestsBase
{
    protected override ISpanNumberOperation<float> CreateOperation() => new SerialNumberOperation<float>();
}

[TestFixture]
public class SerialDoubleNumberOperationTests : DoubleNumberOperationTestsBase
{
    protected override ISpanNumberOperation<double> CreateOperation() => new SerialNumberOperation<double>();
}

[TestFixture]
public class ParallelIntNumberOperationTests : IntNumberOperationTestsBase
{
    protected override ISpanNumberOperation<int> CreateOperation() => new ParallelNumberOperation<int>();
}

[TestFixture]
public class ParallelLongNumberOperationTests : LongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<long> CreateOperation() => new ParallelNumberOperation<long>();
}

[TestFixture]
public class ParallelUintNumberOperationTests : UintNumberOperationTestsBase
{
    protected override ISpanNumberOperation<uint> CreateOperation() => new ParallelNumberOperation<uint>();
}

[TestFixture]
public class ParallelUlongNumberOperationTests : UlongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<ulong> CreateOperation() => new ParallelNumberOperation<ulong>();
}

[TestFixture]
public class ParallelFloatNumberOperationTests : FloatNumberOperationTestsBase
{
    protected override ISpanNumberOperation<float> CreateOperation() => new ParallelNumberOperation<float>();
}

[TestFixture]
public class ParallelDoubleNumberOperationTests : DoubleNumberOperationTestsBase
{
    protected override ISpanNumberOperation<double> CreateOperation() => new ParallelNumberOperation<double>();
}

[TestFixture]
public class SimdIntNumberOperationTests : IntNumberOperationTestsBase
{
    protected override ISpanNumberOperation<int> CreateOperation() => new SimdNumberOperation<int>();
}

[TestFixture]
public class SimdLongNumberOperationTests : LongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<long> CreateOperation() => new SimdNumberOperation<long>();
}

[TestFixture]
public class SimdUintNumberOperationTests : UintNumberOperationTestsBase
{
    protected override ISpanNumberOperation<uint> CreateOperation() => new SimdNumberOperation<uint>();
}

[TestFixture]
public class SimdUlongNumberOperationTests : UlongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<ulong> CreateOperation() => new SimdNumberOperation<ulong>();
}

[TestFixture]
public class SimdFloatNumberOperationTests : FloatNumberOperationTestsBase
{
    protected override ISpanNumberOperation<float> CreateOperation() => new SimdNumberOperation<float>();
}

[TestFixture]
public class SimdDoubleNumberOperationTests : DoubleNumberOperationTestsBase
{
    protected override ISpanNumberOperation<double> CreateOperation() => new SimdNumberOperation<double>();
}

[TestFixture]
public class ParallelSimdIntNumberOperationTests : IntNumberOperationTestsBase
{
    protected override ISpanNumberOperation<int> CreateOperation() => new ParallelSimdNumberOperation<int>();
}

[TestFixture]
public class ParallelSimdLongNumberOperationTests : LongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<long> CreateOperation() => new ParallelSimdNumberOperation<long>();
}

[TestFixture]
public class ParallelSimdUintNumberOperationTests : UintNumberOperationTestsBase
{
    protected override ISpanNumberOperation<uint> CreateOperation() => new ParallelSimdNumberOperation<uint>();
}

[TestFixture]
public class ParallelSimdUlongNumberOperationTests : UlongNumberOperationTestsBase
{
    protected override ISpanNumberOperation<ulong> CreateOperation() => new ParallelSimdNumberOperation<ulong>();
}

[TestFixture]
public class ParallelSimdFloatNumberOperationTests : FloatNumberOperationTestsBase
{
    protected override ISpanNumberOperation<float> CreateOperation() => new ParallelSimdNumberOperation<float>();
}

[TestFixture]
public class ParallelSimdDoubleNumberOperationTests : DoubleNumberOperationTestsBase
{
    protected override ISpanNumberOperation<double> CreateOperation() => new ParallelSimdNumberOperation<double>();
}