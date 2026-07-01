using System.Runtime.InteropServices;

namespace Gubbins.Unsafe.Tests;

[TestFixture]
public sealed unsafe class NativeTests
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Pair
    {
        public int Left;
        public int Right;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct OffsetStruct
    {
        public byte Head;
        public int  Tail;
    }

    private sealed class ValuableTarget
    {
        public int Number = 9;
        public string Name { get; set; } = "n";
    }

    private interface IMarker;

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 1)]
    [TestCase(4, 4)]
    [TestCase(8, 8)]
    [TestCase(12, 4)]
    [TestCase(16, 8)]
    public void GetAlignment_SizeProvided_ShouldReturnExpectedAlignment(int size, int expected)
    {
        Assert.That(Native.GetAlignment(size), Is.EqualTo(expected));
    }

    [Test]
    public void GetAlignment_GenericTypeProvided_ShouldReturnTypeAlignment()
    {
        Assert.That(Native.GetAlignment<int>(), Is.EqualTo(4));
    }

    [Test]
    public void Allocate_ValidSizeProvided_ShouldReturnWritablePointer()
    {
        var ptr = Native.Allocate(sizeof(int), 16);
        try
        {
            *(int*) ptr = 321;
            Assert.That(*(int*) ptr, Is.EqualTo(321));
        }
        finally
        {
            Native.Free(ptr);
        }
    }

    [Test]
    public void Cast_ByRefStructProvided_ShouldReinterpretMemory()
    {
        var bits = BitConverter.SingleToInt32Bits(1.5f);
        ref var value = ref Native.Cast<int, float>(ref bits);
        Assert.That(value, Is.EqualTo(1.5f));
    }

    [Test]
    public void Cast_ObjectProvided_ShouldExposeObjectReferenceBits()
    {
        object source = new();
        ref var referenceBits = ref Native.Cast<nint>(source);
        Assert.That(referenceBits, Is.Not.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void BitCast_FloatAndIntRoundTrip_ShouldPreserveBits()
    {
        const float value = 1.5f;
        var bits = Native.BitCast<float, int>(value);
        var roundTrip = Native.BitCast<int, float>(bits);

        Assert.That(bits, Is.EqualTo(BitConverter.SingleToInt32Bits(value)));
        Assert.That(roundTrip, Is.EqualTo(value));
    }

    [Test]
    public void GetFieldOffset_FieldProvided_ShouldReturnExpectedOffset()
    {
        var field = typeof(OffsetStruct).GetField(nameof(OffsetStruct.Tail));
        var offset = Native.GetFieldOffset(field!);
        Assert.That(offset, Is.EqualTo(1));
    }

    [Test]
    public void GetFirstElementAddress_ArrayProvided_ShouldPointToFirstElement()
    {
        var values = new[] {10, 20, 30};
        var ptr = (int*) Native.GetFirstElementAddress(values);

        ptr[0] = 99;
        Assert.That(values[0], Is.EqualTo(99));
    }

    [Test]
    public void GetFirstElementAddress_SpanProvided_ShouldPointToFirstElement()
    {
        var values = new[] {7, 8, 9};
        var ptr = (int*) Native.GetFirstElementAddress(values.AsSpan());

        ptr[0] = 77;
        Assert.That(values[0], Is.EqualTo(77));
    }

    [Test]
    public void GetFirstElementAddress_ReadOnlySpanProvided_ShouldPointToFirstElement()
    {
        var values = new[] {4, 5, 6};
        ReadOnlySpan<int> readOnly = values;
        var ptr = (int*) Native.GetFirstElementAddress(readOnly);
        Assert.That(ptr[0], Is.EqualTo(4));
    }

    [Test]
    public void GetAddress_StructReferenceProvided_ShouldReturnWritablePointer()
    {
        var pair = new Pair {Left = 1, Right = 2};
        var ptr = (Pair*) Native.GetAddress(ref pair);

        ptr->Right = 5;
        Assert.That(pair.Right, Is.EqualTo(5));
    }

    [Test]
    public void GetAddress_ObjectProvided_ShouldReturnNonZeroPointer()
    {
        var pointer = (nint) Native.GetAddress(new object());
        Assert.That(pointer, Is.Not.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void AddByteOffset_OffsetProvided_ShouldMoveReferenceToSiblingElement()
    {
        Span<int> values = [3, 6, 9];
        ref var second = ref Native.AddByteOffset(ref values[0], sizeof(int));

        second = 42;
        ref var first = ref Native.AddByteOffset(ref second, -sizeof(int));

        Assert.That(values[1], Is.EqualTo(42));
        Assert.That(first, Is.EqualTo(3));
    }

    [Test]
    public void GetValuableMember_FieldProvided_ShouldReturnFieldMember()
    {
        var member = Native.GetValuableMember(typeof(ValuableTarget), nameof(ValuableTarget.Number));
        Assert.That(member, Is.Not.Null);
        Assert.That(member!.MemberType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GetValuableMember_PropertyProvided_ShouldReturnPropertyMember()
    {
        var member = Native.GetValuableMember(typeof(ValuableTarget), nameof(ValuableTarget.Name));
        Assert.That(member, Is.Not.Null);
        Assert.That(member!.MemberType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GetValuableMember_MissingMemberProvided_ShouldReturnNull()
    {
        var member = Native.GetValuableMember(typeof(ValuableTarget), "MissingMember");
        Assert.That(member, Is.Null);
    }

    [Test]
    public void GetValuableMember_InterfaceTypeProvided_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Native.GetValuableMember(typeof(IMarker), "Any"));
    }

    [TestCase(typeof(int), 4u)]
    [TestCase(typeof(long), 8u)]
    [TestCase(typeof(double), 8u)]
    public void GetStackSize_TypeProvided_ShouldReturnExpectedSize(Type type, uint expected)
    {
        Assert.That(Native.GetStackSize(type), Is.EqualTo(expected));
    }

    [Test]
    public void GetStackSize_GenericTypeProvided_ShouldMatchTypeOverload()
    {
        Assert.That(Native.GetStackSize<int>(), Is.EqualTo(Native.GetStackSize(typeof(int))));
    }

    [Test]
    public void GetStackSize_ReferenceTypeProvided_ShouldReturnPointerSize()
    {
        Assert.That(Native.GetStackSize(typeof(ValuableTarget)), Is.EqualTo((uint) IntPtr.Size));
    }

    [Test]
    public void GetStackSize_ValueTypeProvided_ShouldReturnStructSize()
    {
        Assert.That(Native.GetStackSize(typeof(Pair)), Is.EqualTo((uint) sizeof(Pair)));
    }

    [Test]
    public void CopyMemory_SourceAndDestinationProvided_ShouldCopyBytes()
    {
        Span<byte> source = [1, 2, 3, 4];
        Span<byte> destination = stackalloc byte[4];

        fixed (byte* sourcePtr = source)
        fixed (byte* destinationPtr = destination)
        {
            Native.CopyMemory(sourcePtr, destinationPtr, 4);
        }

        Assert.That(destination.ToArray(), Is.EqualTo(source.ToArray()));
    }

    [Test]
    public void Box_ValueTypePointerProvided_ShouldReturnBoxedInstance()
    {
        var value = stackalloc int[1];
        value[0] = 456;

        var boxed = Native.Box(value, typeof(int), sizeof(int));
        Assert.That(boxed, Is.TypeOf<int>());
    }

    [Test]
    public void Unbox_BoxedValueProvided_ShouldPointToUnderlyingData()
    {
        object boxed = 789;
        var ptr = Native.Unbox(boxed);
        Assert.That(Native.GetValue<int>(ptr), Is.EqualTo(789));
    }

    [Test]
    public void GetValue_GenericPointerProvided_ShouldReturnTypedValue()
    {
        long value = 42;
        Assert.That(Native.GetValue<long>(&value), Is.EqualTo(42));
    }

    [Test]
    public void AsRef_PointerProvided_ShouldExposeWritableReference()
    {
        var value = stackalloc int[1];
        value[0] = 9;
        ref var reference = ref Native.AsRef<int>(value);

        reference = 19;
        Assert.That(value[0], Is.EqualTo(19));
    }

    [Test]
    public void AsRef_SpanProvided_ShouldReinterpretSpanHead()
    {
        Span<int> values = [1234];
        var bytes = MemoryMarshal.AsBytes(values);
        ref var value = ref Native.AsRef<byte, int>(bytes);

        value = 4321;
        Assert.That(values[0], Is.EqualTo(4321));
    }

    [Test]
    public void GetValue_TypeAndPointerProvided_ShouldReturnBoxedValue()
    {
        double value = 3.5d;
        var result = Native.GetValue(&value, typeof(double));
        Assert.That(result, Is.EqualTo(3.5d));
    }

    [Test]
    public void GetValue_TypeAndStructPointerProvided_ShouldReturnBoxedStruct()
    {
        var value = new Pair {Left = 11, Right = 22};

        var result = Native.GetValue(&value, typeof(Pair));

        Assert.That(result, Is.TypeOf<Pair>());
        var pair = (Pair) result;
        Assert.That(pair.Left, Is.EqualTo(11));
        Assert.That(pair.Right, Is.EqualTo(22));
    }

    [Test]
    public void GetValue_TypeAndReferenceSlotProvided_ShouldReturnSameClassInstance()
    {
        var instance = new ValuableTarget {Number = 27, Name = "target"};
        object reference = instance;

        var result = Native.GetValue(&reference, typeof(ValuableTarget));

        Assert.That(result, Is.SameAs(instance));
        Assert.That(((ValuableTarget) result).Number, Is.EqualTo(27));
        Assert.That(((ValuableTarget) result).Name, Is.EqualTo("target"));
    }

    [Test]
    public void SetValue_TypeAndObjectProvided_ShouldWriteBooleanValue()
    {
        bool value = false;

        Native.SetValue(&value, true);

        Assert.That(value, Is.True);
    }

    [Test]
    public void SetValue_TypeAndObjectProvided_ShouldWriteDateTimeValue()
    {
        var expected = new DateTime(2026, 5, 28, 12, 30, 0, DateTimeKind.Utc);
        var value = default(DateTime);

        Native.SetValue(&value, expected);

        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public void SetValue_TypeAndObjectProvided_ShouldWriteStructValue()
    {
        var expected = new Pair {Left = 8, Right = 16};
        var value = default(Pair);

        Native.SetValue(&value, expected);

        Assert.That(value.Left, Is.EqualTo(8));
        Assert.That(value.Right, Is.EqualTo(16));
    }

    [Test]
    public void SetValue_TypeAndObjectProvided_ShouldWriteClassReference()
    {
        ValuableTarget value = null!;
        var expected = new ValuableTarget {Number = 31, Name = "set"};

        Native.SetValue(&value, expected);

        Assert.That(value, Is.SameAs(expected));
    }

    [Test]
    public void SetValue_GenericPointerProvided_ShouldWriteStructValue()
    {
        Pair pair = default;
        var value = new Pair {Left = 8, Right = 16};

        Native.SetValue(&pair, ref value);
        Assert.That(pair.Left, Is.EqualTo(8));
        Assert.That(pair.Right, Is.EqualTo(16));
    }

    [Test]
    public void SetValue_TypeAndObjectProvided_ShouldWritePrimitiveValue()
    {
        int value = 0;
        Native.SetValue(&value, 123);
        Assert.That(value, Is.EqualTo(123));
    }
}