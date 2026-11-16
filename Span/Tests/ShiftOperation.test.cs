namespace Gubbins.Span.Tests;

public abstract class ShiftOperationTestsBase<T> where T : unmanaged
{
    protected abstract ISpanShift<T> CreateOperation();
    protected abstract T[] GetSource();
    protected abstract int GetShiftCount();
    protected abstract T ShiftLeftScalar(T value, int count);
    protected abstract T ShiftRightScalar(T value, int count);

    [Test]
    public void ShiftLeft_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = GetSource();
        var count = GetShiftCount();
        var result = new T[src.Length];
        op.ShiftLeft(src, count, result);
        for (var i = 0; i < src.Length; i++)
            Assert.That(result[i], Is.EqualTo(ShiftLeftScalar(src[i], count)), $"ShiftLeft failed at {i}");
    }

    [Test]
    public void ShiftRight_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = GetSource();
        var count = GetShiftCount();
        var result = new T[src.Length];
        op.ShiftRight(src, count, result);
        for (var i = 0; i < src.Length; i++)
            Assert.That(result[i], Is.EqualTo(ShiftRightScalar(src[i], count)), $"ShiftRight failed at {i}");
    }
}

[TestFixture]
public class SerialIntShiftOperationTests : ShiftOperationTestsBase<int>
{
    protected override ISpanShift<int> CreateOperation() => new SerialIntOperation();
    protected override int[] GetSource() => [1, -2, 7, 15, 0, 123456, -9999];
    protected override int GetShiftCount() => 2;
    protected override int ShiftLeftScalar(int value, int count) => value << count;
    protected override int ShiftRightScalar(int value, int count) => value >> count;
}

[TestFixture]
public class SerialUintShiftOperationTests : ShiftOperationTestsBase<uint>
{
    protected override ISpanShift<uint> CreateOperation() => new SerialUintOperation();
    protected override uint[] GetSource() => [1, 2, 7, 15, 0, 123456, 9999];
    protected override int GetShiftCount() => 2;
    protected override uint ShiftLeftScalar(uint value, int count) => value << count;
    protected override uint ShiftRightScalar(uint value, int count) => value >> count;
}

[TestFixture]
public class SerialLongShiftOperationTests : ShiftOperationTestsBase<long>
{
    protected override ISpanShift<long> CreateOperation() => new SerialLongOperation();
    protected override long[] GetSource() => [1L, -2L, 7L, 15L, 0L, 1234567890123L, -999999999L];
    protected override int GetShiftCount() => 3;
    protected override long ShiftLeftScalar(long value, int count) => value << count;
    protected override long ShiftRightScalar(long value, int count) => value >> count;
}

[TestFixture]
public class SerialUlongShiftOperationTests : ShiftOperationTestsBase<ulong>
{
    protected override ISpanShift<ulong> CreateOperation() => new SerialUlongOperation();
    protected override ulong[] GetSource() => [1UL, 2UL, 7UL, 15UL, 0UL, 1234567890123UL, 999999999UL];
    protected override int GetShiftCount() => 3;
    protected override ulong ShiftLeftScalar(ulong value, int count) => value << count;
    protected override ulong ShiftRightScalar(ulong value, int count) => value >> count;
}

[TestFixture]
public class ParallelIntShiftOperationTests : ShiftOperationTestsBase<int>
{
    protected override ISpanShift<int> CreateOperation() => new ParallelIntOperation();
    protected override int[] GetSource() => [1, -2, 7, 15, 0, 123456, -9999];
    protected override int GetShiftCount() => 2;
    protected override int ShiftLeftScalar(int value, int count) => value << count;
    protected override int ShiftRightScalar(int value, int count) => value >> count;
}

[TestFixture]
public class ParallelUintShiftOperationTests : ShiftOperationTestsBase<uint>
{
    protected override ISpanShift<uint> CreateOperation() => new ParallelUintOperation();
    protected override uint[] GetSource() => [1, 2, 7, 15, 0, 123456, 9999];
    protected override int GetShiftCount() => 2;
    protected override uint ShiftLeftScalar(uint value, int count) => value << count;
    protected override uint ShiftRightScalar(uint value, int count) => value >> count;
}

[TestFixture]
public class ParallelLongShiftOperationTests : ShiftOperationTestsBase<long>
{
    protected override ISpanShift<long> CreateOperation() => new ParallelLongOperation();
    protected override long[] GetSource() => [1L, -2L, 7L, 15L, 0L, 1234567890123L, -999999999L];
    protected override int GetShiftCount() => 3;
    protected override long ShiftLeftScalar(long value, int count) => value << count;
    protected override long ShiftRightScalar(long value, int count) => value >> count;
}

[TestFixture]
public class ParallelUlongShiftOperationTests : ShiftOperationTestsBase<ulong>
{
    protected override ISpanShift<ulong> CreateOperation() => new ParallelUlongOperation();
    protected override ulong[] GetSource() => [1UL, 2UL, 7UL, 15UL, 0UL, 1234567890123UL, 999999999UL];
    protected override int GetShiftCount() => 3;
    protected override ulong ShiftLeftScalar(ulong value, int count) => value << count;
    protected override ulong ShiftRightScalar(ulong value, int count) => value >> count;
}
#if NET7_0_OR_GREATER
[TestFixture]
public class SimdIntShiftOperationTests : ShiftOperationTestsBase<int>
{
    protected override ISpanShift<int> CreateOperation() => new SimdIntOperation();
    protected override int[] GetSource() => [1, -2, 7, 15, 0, 123456, -9999];
    protected override int GetShiftCount() => 2;
    protected override int ShiftLeftScalar(int value, int count) => value << count;
    protected override int ShiftRightScalar(int value, int count) => value >> count;
}

[TestFixture]
public class SimdUintShiftOperationTests : ShiftOperationTestsBase<uint>
{
    protected override ISpanShift<uint> CreateOperation() => new SimdUintOperation();
    protected override uint[] GetSource() => [1, 2, 7, 15, 0, 123456, 9999];
    protected override int GetShiftCount() => 2;
    protected override uint ShiftLeftScalar(uint value, int count) => value << count;
    protected override uint ShiftRightScalar(uint value, int count) => value >> count;
}

[TestFixture]
public class SimdLongShiftOperationTests : ShiftOperationTestsBase<long>
{
    protected override ISpanShift<long> CreateOperation() => new SimdLongOperation();
    protected override long[] GetSource() => [1L, -2L, 7L, 15L, 0L, 1234567890123L, -999999999L];
    protected override int GetShiftCount() => 3;
    protected override long ShiftLeftScalar(long value, int count) => value << count;
    protected override long ShiftRightScalar(long value, int count) => value >> count;
}

[TestFixture]
public class SimdUlongShiftOperationTests : ShiftOperationTestsBase<ulong>
{
    protected override ISpanShift<ulong> CreateOperation() => new SimdUlongOperation();
    protected override ulong[] GetSource() => [1UL, 2UL, 7UL, 15UL, 0UL, 1234567890123UL, 999999999UL];
    protected override int GetShiftCount() => 3;
    protected override ulong ShiftLeftScalar(ulong value, int count) => value << count;
    protected override ulong ShiftRightScalar(ulong value, int count) => value >> count;
}

[TestFixture]
public class ParallelSimdIntShiftOperationTests : ShiftOperationTestsBase<int>
{
    protected override ISpanShift<int> CreateOperation() => new ParallelSimdIntOperation();
    protected override int[] GetSource() => [1, -2, 7, 15, 0, 123456, -9999];
    protected override int GetShiftCount() => 2;
    protected override int ShiftLeftScalar(int value, int count) => value << count;
    protected override int ShiftRightScalar(int value, int count) => value >> count;
}

[TestFixture]
public class ParallelSimdUintShiftOperationTests : ShiftOperationTestsBase<uint>
{
    protected override ISpanShift<uint> CreateOperation() => new ParallelSimdUintOperation();
    protected override uint[] GetSource() => [1, 2, 7, 15, 0, 123456, 9999];
    protected override int GetShiftCount() => 2;
    protected override uint ShiftLeftScalar(uint value, int count) => value << count;
    protected override uint ShiftRightScalar(uint value, int count) => value >> count;
}

[TestFixture]
public class ParallelSimdLongShiftOperationTests : ShiftOperationTestsBase<long>
{
    protected override ISpanShift<long> CreateOperation() => new ParallelSimdLongOperation();
    protected override long[] GetSource() => [1L, -2L, 7L, 15L, 0L, 1234567890123L, -999999999L];
    protected override int GetShiftCount() => 3;
    protected override long ShiftLeftScalar(long value, int count) => value << count;
    protected override long ShiftRightScalar(long value, int count) => value >> count;
}

[TestFixture]
public class ParallelSimdUlongShiftOperationTests : ShiftOperationTestsBase<ulong>
{
    protected override ISpanShift<ulong> CreateOperation() => new ParallelSimdUlongOperation();
    protected override ulong[] GetSource() => [1UL, 2UL, 7UL, 15UL, 0UL, 1234567890123UL, 999999999UL];
    protected override int GetShiftCount() => 3;
    protected override ulong ShiftLeftScalar(ulong value, int count) => value << count;
    protected override ulong ShiftRightScalar(ulong value, int count) => value >> count;
}
#endif