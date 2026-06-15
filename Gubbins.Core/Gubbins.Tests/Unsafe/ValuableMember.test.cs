using System.Runtime.InteropServices;

namespace Gubbins.Unsafe.Tests;

[TestFixture]
public sealed class ValuableMemberTests
{
    private enum DayKind
    {
        None,
        Work,
        Rest
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Pair
    {
        public int Left;
        public int Right;
    }

    private struct StructTarget
    {
        public int     FieldValue;
        public Pair    FieldPair;
        public int     PropertyValue { get; set; }
        public DayKind Kind          { get; set; }
    }

    private sealed class ClassTarget
    {
        public int     FieldValue;
        public int     PropertyValue { get; set; }
        public string  Name          { get; set; } = "init";
        public DayKind Kind          { get; set; }
    }

    private static ValuableMember Member(Type type, string name) => Native.GetValuableMember(type, name)!;

    [Test]
    public void MemberType_FieldProvided_ShouldExposeFieldType()
    {
        Assert.That(Member(typeof(ClassTarget), nameof(ClassTarget.FieldValue)).MemberType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void MemberType_PropertyProvided_ShouldExposePropertyType()
    {
        Assert.That(Member(typeof(ClassTarget), nameof(ClassTarget.Name)).MemberType, Is.EqualTo(typeof(string)));
    }

    // ---- Field, struct source ----

    [Test]
    public void GetValue_FieldStructSourceTyped_ShouldReturnFieldValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.FieldValue));
        var source = new StructTarget {FieldValue = 42};

        member.GetValue<StructTarget, int>(ref source, out var result);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetValue_FieldStructSourceObject_ShouldReturnBoxedFieldValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.FieldValue));
        var source = new StructTarget {FieldValue = 7};

        member.GetValue(ref source, out object result);

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void GetValue_FieldStructSourceStructResult_ShouldReturnStructValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.FieldPair));
        var source = new StructTarget {FieldPair = new Pair {Left = 3, Right = 6}};

        member.GetValue<StructTarget, Pair>(ref source, out var result);

        Assert.That(result.Left, Is.EqualTo(3));
        Assert.That(result.Right, Is.EqualTo(6));
    }

    [Test]
    public void SetValue_FieldStructSourceTyped_ShouldWriteFieldValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.FieldValue));
        var source = new StructTarget();
        var value = 17;

        member.SetValue(ref source, ref value);

        Assert.That(source.FieldValue, Is.EqualTo(17));
    }

    [Test]
    public void SetValue_FieldStructSourceObject_ShouldWriteFieldValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.FieldValue));
        var source = new StructTarget();

        member.SetValue(ref source, (object) 23);

        Assert.That(source.FieldValue, Is.EqualTo(23));
    }

    // ---- Field, class source ----

    [Test]
    public void GetValue_FieldClassSourceTyped_ShouldReturnFieldValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.FieldValue));
        var source = new ClassTarget {FieldValue = 11};

        member.GetValue<ClassTarget, int>(source, out var result);

        Assert.That(result, Is.EqualTo(11));
    }

    [Test]
    public void GetValue_FieldClassSourceObject_ShouldReturnBoxedFieldValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.FieldValue));
        var source = new ClassTarget {FieldValue = 19};

        member.GetValue(source, out object result);

        Assert.That(result, Is.EqualTo(19));
    }

    [Test]
    public void SetValue_FieldClassSourceTyped_ShouldWriteFieldValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.FieldValue));
        var source = new ClassTarget();
        var value = 13;

        member.SetValue<ClassTarget, int>(source, ref value);

        Assert.That(source.FieldValue, Is.EqualTo(13));
    }

    [Test]
    public void SetValue_FieldClassSourceObject_ShouldWriteFieldValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.FieldValue));
        var source = new ClassTarget();

        member.SetValue(source, (object) 29);

        Assert.That(source.FieldValue, Is.EqualTo(29));
    }

    // ---- Property, struct source ----

    [Test]
    public void GetValue_PropertyStructSourceTyped_ShouldReturnPropertyValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.PropertyValue));
        var source = new StructTarget {PropertyValue = 8};

        member.GetValue<StructTarget, int>(ref source, out var result);

        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void GetValue_PropertyStructSourceObject_ShouldReturnBoxedPropertyValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.PropertyValue));
        var source = new StructTarget {PropertyValue = 5};

        member.GetValue(ref source, out object result);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SetValue_PropertyStructSourceTyped_ShouldWritePropertyValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.PropertyValue));
        var source = new StructTarget();
        var value = 20;

        member.SetValue<StructTarget, int>(ref source, ref value);

        Assert.That(source.PropertyValue, Is.EqualTo(20));
    }

    [Test]
    public void SetValue_PropertyStructSourceObject_ShouldWritePropertyValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.PropertyValue));
        var source = new StructTarget();

        member.SetValue(ref source, (object) 15);

        Assert.That(source.PropertyValue, Is.EqualTo(15));
    }

    // ---- Property, class source ----

    [Test]
    public void GetValue_PropertyClassSourceTyped_ShouldReturnPropertyValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.PropertyValue));
        var source = new ClassTarget {PropertyValue = 6};

        member.GetValue<ClassTarget, int>(source, out var result);

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void GetValue_PropertyClassSourceObject_ShouldReturnReferenceProperty()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.Name));
        var source = new ClassTarget {Name = "hello"};

        member.GetValue(source, out object result);

        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void SetValue_PropertyClassSourceTyped_ShouldWritePropertyValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.PropertyValue));
        var source = new ClassTarget();
        var value = 9;

        member.SetValue<ClassTarget, int>(source, ref value);

        Assert.That(source.PropertyValue, Is.EqualTo(9));
    }

    [Test]
    public void SetValue_PropertyClassSourceObject_ShouldWriteReferenceProperty()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.Name));
        var source = new ClassTarget();

        member.SetValue(source, (object) "world");

        Assert.That(source.Name, Is.EqualTo("world"));
    }

    // ---- Enum property (boxing wrapper path) ----

    [Test]
    public void GetValue_EnumPropertyClassSourceObject_ShouldReturnEnumValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.Kind));
        var source = new ClassTarget {Kind = DayKind.Work};

        member.GetValue(source, out object result);

        Assert.That(result, Is.EqualTo(DayKind.Work));
    }

    [Test]
    public void SetValue_EnumPropertyClassSourceObject_ShouldWriteEnumValue()
    {
        var member = Member(typeof(ClassTarget), nameof(ClassTarget.Kind));
        var source = new ClassTarget();

        member.SetValue(source, (object) DayKind.Rest);

        Assert.That(source.Kind, Is.EqualTo(DayKind.Rest));
    }

    [Test]
    public void GetValue_EnumPropertyStructSourceObject_ShouldReturnEnumValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.Kind));
        var source = new StructTarget {Kind = DayKind.Work};

        member.GetValue(ref source, out object result);

        Assert.That(result, Is.EqualTo(DayKind.Work));
    }

    [Test]
    public void SetValue_EnumPropertyStructSourceObject_ShouldWriteEnumValue()
    {
        var member = Member(typeof(StructTarget), nameof(StructTarget.Kind));
        var source = new StructTarget();

        member.SetValue(ref source, (object) DayKind.Rest);

        Assert.That(source.Kind, Is.EqualTo(DayKind.Rest));
    }
}
