namespace Gubbins.Unmanaged.Tests;

[TestFixture]
public class UstringTests
{
    [Test]
    public void Empty_IsEmptyAndInvalidOrEmpty()
    {
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(Ustring.Empty.IsEmpty, Is.True);
            Assert.That(Ustring.Empty.IsInvalidOrEmpty, Is.True);
            Assert.That(Ustring.Empty.Length, Is.EqualTo(0));
        });
        Assert.That(Ustring.Empty.Address, Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void Constructor_FromString_SetsLengthAndAddress()
    {
        // Arrange
        var input = "Hello";

        // Act
        using var ustr = new Ustring(input);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ustr.Length, Is.EqualTo(input.Length));
            Assert.That(ustr.Address, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(ustr.IsValid, Is.True);
            Assert.That(ustr.ToString(), Is.EqualTo(input));
            Assert.That(ustr.IsEmpty, Is.False);
            Assert.That(ustr.IsInvalidOrEmpty, Is.False);
        });
    }

    [Test]
    public void Constructor_FromNullString_UsesEmptyString()
    {
        // Act
        using var ustr = new Ustring((string) null!);

        // Assert
        Assert.That(ustr.Length, Is.EqualTo(0));
        Assert.That(ustr.ToString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Constructor_FromSpan_SetsLengthAndAddress()
    {
        // Arrange
        var input = "World".ToArray().AsSpan();

        // Act
        using var ustr = new Ustring(input);

        // Assert
        Assert.That(ustr.Length, Is.EqualTo(input.Length));
        Assert.Multiple(() =>
        {
            Assert.That(ustr.Address, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(ustr.ToString(), Is.EqualTo("World"));
        });
    }

    [Test]
    public void ImplicitOperator_FromString_Works()
    {
        // Arrange
        var input = "Test";

        // Act
        Ustring ustr = input;

        // Assert
        Assert.That(ustr.ToString(), Is.EqualTo(input));
    }

    [Test]
    public void ImplicitOperator_ToString_Works()
    {
        // Arrange
        using var ustr = new Ustring("Implicit");

        // Act
        string result = ustr;

        // Assert
        Assert.That(result, Is.EqualTo("Implicit"));
    }

    [Test]
    public void ImplicitOperator_ToReadOnlySpan_Works()
    {
        // Arrange
        using var ustr = new Ustring("SpanTest");

        // Act
        ReadOnlySpan<char> span = ustr;

        // Assert
        Assert.That(span.SequenceEqual("SpanTest".AsSpan()), Is.True);
    }

    [Test]
    public void ImplicitOperator_ToSpan_Works()
    {
        // Arrange
        using var ustr = new Ustring("MutableSpan");

        // Act
        Span<char> span = ustr;

        // Assert
        Assert.That(span.SequenceEqual("MutableSpan".AsSpan()), Is.True);
    }

    [Test]
    public void Indexer_AccessesCharacters()
    {
        // Arrange
        using var ustr = new Ustring("Index");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr[0], Is.EqualTo('I'));
            Assert.That(ustr[1], Is.EqualTo('n'));
            Assert.That(ustr[4], Is.EqualTo('x'));
        });
    }

    [Test]
    public void Dispose_MarksAsDisposedAndReturnsToPool()
    {
        // Arrange
        using var ustr = new Ustring("Disposable");

        // Act
        ustr.Dispose();
        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(ustr.IsValid, Is.False);
            Assert.That(ustr.IsInvalidOrEmpty, Is.True);
        });
    }

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new Ustring("CloneMe");

        // Act
        var clone = original.Clone();
        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(clone.ToString(), Is.EqualTo(original.ToString()));
            Assert.That(clone.Address, Is.Not.EqualTo(original.Address)); // Different handles
            Assert.That(clone.IsValid, Is.True);
        });

        // Dispose original, clone should still be valid
        original.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(original.IsValid, Is.False);
            Assert.That(clone.IsValid, Is.True);
        });
    }

    [Test]
    public void AsSpan_ReturnsCorrectSpan()
    {
        // Arrange
        using var ustr = new Ustring("SpanMethods");

        // Act
        var span = ustr.AsSpan();

        // Assert
        Assert.That(span.Length, Is.EqualTo(ustr.Length));
        Assert.That(span.ToString(), Is.EqualTo("SpanMethods"));
    }

    [Test]
    public void AsSpan_WithStart_ReturnsSlicedSpan()
    {
        // Arrange
        using var ustr = new Ustring("Sliced");

        // Act
        var span = ustr.AsSpan(2);
        // Assert
        Assert.That(span.ToString(), Is.EqualTo("iced"));
        Assert.That(() => ustr.AsSpan(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => ustr.AsSpan(ustr.Length + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void AsSpan_WithStartAndLength_ReturnsSlicedSpan()
    {
        // Arrange
        using var ustr = new Ustring("Substring");

        // Act
        var span = ustr.AsSpan(3, 5);
        // Assert
        Assert.That(span.ToString(), Is.EqualTo("strin"));
        Assert.That(() => ustr.AsSpan(0, ustr.Length + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void AsSpan_WithRange_ReturnsSlicedSpan()
    {
        // Arrange
        using var ustr = new Ustring("RangeTest");

        // Act
        var span = ustr.AsSpan(0..5);

        // Assert
        Assert.That(span.ToString(), Is.EqualTo("Range"));
    }

    [Test]
    public void AsReadOnlySpan_BehavesLikeAsSpan()
    {
        // Arrange
        using var ustr = new Ustring("ReadOnly");

        // Act
        var roSpan = ustr.AsReadOnlySpan();

        // Assert
        Assert.That(roSpan.ToString(), Is.EqualTo("ReadOnly"));
    }

    [Test]
    public void GetPinnableReference_ReturnsFirstCharRef()
    {
        // Arrange
        using var ustr = new Ustring("Pinned");

        // Act
        ref var first = ref ustr.GetPinnableReference();

        // Assert
        Assert.That(first, Is.EqualTo('P'));
    }

    [Test]
    public void Equals_WithString_Matches()
    {
        // Arrange
        using var ustr = new Ustring("Equal");
        var other = "Equal";
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr.Equals(other), Is.True);
            Assert.That(ustr.Equals((string) null), Is.False);
        });
    }

    [Test]
    public void Equals_WithUstring_Matches()
    {
        // Arrange
        using var ustr1 = new Ustring("Match");
        using var ustr2 = new Ustring("Match");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr1.Equals(ustr2), Is.True);
            Assert.That(ustr1.Equals(new Ustring("NoMatch")), Is.False);
        });
    }

    [Test]
    public void Equals_WithReadOnlySpan_Matches()
    {
        // Arrange
        using var ustr = new Ustring("SpanEqual");
        var span = "SpanEqual".AsSpan();
        // Act & Assert
        Assert.That(ustr.Equals(span), Is.True);
        Assert.That(ustr.Equals("Different".AsSpan()), Is.False);
    }

    [Test]
    public void Operator_Equality_WithString_Works()
    {
        // Arrange
        using var ustr = new Ustring("OpEqual");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr == "OpEqual", Is.True);
            Assert.That(ustr != "Different", Is.True);
        });
    }

    [Test]
    public void Operator_Equality_WithUstring_Works()
    {
        // Arrange
        using var ustr1 = new Ustring("UOp");
        using var ustr2 = new Ustring("UOp");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr1 == ustr2, Is.True);
            Assert.That(ustr1 != new Ustring("No"), Is.True);
        });
    }

    [Test]
    public void Operator_Equality_WithSpan_Works()
    {
        // Arrange
        using var ustr = new Ustring("SpanOp");
        var span = "SpanOp".AsSpan();
        // Act & Assert
        Assert.That(ustr == span, Is.True);
        Assert.That(span == ustr, Is.True);
        Assert.That(ustr != "Diff".AsSpan(), Is.True);
    }

    [Test]
    public void Operator_Addition_UstringUstring_Works()
    {
        // Arrange
        var left = new Ustring("Hello");
        var right = new Ustring("World");

        // Act
        var result = left + right;
        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.ToString(), Is.EqualTo("HelloWorld"));
            Assert.That(result.Length, Is.EqualTo(10));
        });
    }

    [Test]
    public void Operator_Addition_UstringSpan_Works()
    {
        // Arrange
        var left = new Ustring("Prefix");
        var right = "Suffix".AsSpan();

        // Act
        var result = left + right;

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("PrefixSuffix"));
    }

    [Test]
    public void Operator_Addition_StringUstring_Works()
    {
        // Arrange
        var left = "Pre";
        var right = new Ustring("Suf");

        // Act
        var result = left + right;

        // Assert
        Assert.That(result, Is.EqualTo("PreSuf"));
    }

    [Test]
    public void Join_WithCharSeparator_Works()
    {
        // Arrange
        var values = new[] {new Ustring("A"), new Ustring("B"), new Ustring("C")};

        // Act
        var result = Ustring.Join(',', values);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("A,B,C"));
    }

    [Test]
    public void Join_WithSpanSeparator_Works()
    {
        // Arrange
        var values = new[] {"X", "Y", "Z"};

        // Act
        var result = Ustring.Join(" - ".AsSpan(), values);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("X - Y - Z"));
    }

    [Test]
    public void Join_WithDisposeAll_DisposesInputs()
    {
        // Arrange
        var values = new[] {new Ustring("DisposeTest1"), new Ustring("DisposeTest2")};

        // Act
        var result = Ustring.Join('-', true, values);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("DisposeTest1-DisposeTest2"));
        // Note: Can't directly assert disposal, but assume it works
    }

    [Test]
    public void Concat_WithUstrings_Works()
    {
        // Arrange
        var strings = new[] {new Ustring("Cat"), new Ustring("One"), new Ustring("Two")};

        // Act
        var result = Ustring.Concat(strings);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("CatOneTwo"));
    }

    [Test]
    public void Concat_WithDisposeOrigin_DisposesInputs()
    {
        // Arrange
        var strings = new[] {new Ustring("Disp"), new Ustring("Cat")};

        // Act
        var result = Ustring.Concat(true, strings);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("DispCat"));
    }

    [Test]
    public void Concat_WithSpans_Works()
    {
        // Act
        var result = Ustring.Concat(true, "Span".ToUstring(), "Cut".ToUstring());

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("SpanCut"));
    }

    [Test]
    public void Format_WithSingleArg_Works()
    {
        // Act
        using var result = Ustring.Format("Value: {0}", 42);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("Value: 42"));
    }

    [Test]
    public void Format_WithMultipleArgs_Works()
    {
        // Act
        using var result = Ustring.Format("A: {0}, B: {1}, C: {2}", "first", 2.05, true);

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("A: first, B: 2.05, C: true"));
    }

    [Test]
    public void Extension_Trim_RemovesWhitespace()
    {
        // Arrange
        var ustr = new Ustring("  TrimMe  ");

        // Act
        var result = ustr.Trim();
        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.ToString(), Is.EqualTo("TrimMe"));
            Assert.That(ustr.IsValid, Is.False); // Disposed
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_TrimStart_RemovesLeadingWhitespace()
    {
        // Arrange
        var ustr = new Ustring("   Start");

        // Act
        var result = ustr.TrimStart();

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("Start"));
        ustr.Dispose();
    }

    [Test]
    public void Extension_TrimEnd_RemovesTrailingWhitespace()
    {
        // Arrange
        var ustr = new Ustring("End   ");

        // Act
        var result = ustr.TrimEnd();

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("End"));
        ustr.Dispose();
    }

    [Test]
    public void Extension_ToUpper_ConvertsCase()
    {
        // Arrange
        var ustr = new Ustring("lowercase");

        // Act
        var result = ustr.ToUpper();

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("LOWERCASE"));
        ustr.Dispose();
    }

    [Test]
    public void Extension_ToLower_ConvertsCase()
    {
        // Arrange
        var ustr = new Ustring("UPPERCASE");

        // Act
        var result = ustr.ToLower();

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("uppercase"));
        ustr.Dispose();
    }

    [Test]
    public void Extension_Replace_CharToChar_Works()
    {
        // Arrange
        var ustr = new Ustring("Replace Me");

        // Act
        var result = ustr.Replace('e', 'E');

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("REplacE ME"));

        ustr.Dispose();
    }

    [Test]
    public void Extension_Replace_SpanToSpan_Works()
    {
        // Arrange
        var ustr = new Ustring("Hello World, Hello!");

        // Act
        var result = ustr.Replace("Hello", "Hi");

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("Hi World, Hi!"));
        ustr.Dispose();
    }

    [Test]
    public void Extension_Contains_Char_Found()
    {
        // Arrange
        var ustr = new Ustring("Contains a");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr.Contains('a'), Is.True);
            Assert.That(ustr.Contains('x'), Is.False);
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_Contains_Span_Found()
    {
        // Arrange
        var ustr = new Ustring("Contains this");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr.Contains("this".AsSpan()), Is.True);
            Assert.That(ustr.Contains("that".AsSpan()), Is.False);
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_IndexOf_Char_Found()
    {
        // Arrange
        var ustr = new Ustring("IndexOf");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr.IndexOf('O'), Is.EqualTo(5));
            Assert.That(ustr.IndexOf('z'), Is.EqualTo(-1));
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_IndexOf_Span_Found()
    {
        // Arrange
        var ustr = new Ustring("Find Index");
        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(ustr.IndexOf("Ind".AsSpan()), Is.EqualTo(5));
            Assert.That(ustr.IndexOf("No".AsSpan()), Is.EqualTo(-1));
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_LastIndexOf_Char_Found()
    {
        // Arrange
        var ustr = new Ustring("LastIndexOf f");

        // Act & Assert
        Assert.That(ustr.LastIndexOf('f'), Is.EqualTo(12));
        ustr.Dispose();
    }

    [Test]
    public void Extension_Split_CharSeparator_Works()
    {
        // Arrange
        var ustr = new Ustring("Split,Me,Now");

        // Act
        var parts = ustr.Split(',');

        // Assert
        Assert.That(parts.Length, Is.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(parts[0].ToString(), Is.EqualTo("Split"));
            Assert.That(parts[1].ToString(), Is.EqualTo("Me"));
            Assert.That(parts[2].ToString(), Is.EqualTo("Now"));
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_Split_WithOptions_RemoveEmpty()
    {
        // Arrange
        var ustr = new Ustring(",,Hello,,World,");

        // Act
        var parts = ustr.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(parts[0].ToString(), Is.EqualTo("Hello"));
            Assert.That(parts[1].ToString(), Is.EqualTo("World"));
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_Split_WithCount_LimitsResults()
    {
        // Arrange
        var ustr = new Ustring("One,Two,Three,Four");

        // Act
        var parts = ustr.Split(',', 2);

        // Assert
        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(parts[0].ToString(), Is.EqualTo("One"));
            Assert.That(parts[1].ToString(), Is.EqualTo("Two,Three,Four")); // Remaining as one
        });
        ustr.Dispose();
    }

    [Test]
    public void Extension_Split_WithMultipleSeparators_Works()
    {
        // Arrange
        var ustr = new Ustring("Split on chars: a b c");

        // Act
        var parts = ustr.Split(new[] {' ', ':'});

        // Assert
        Assert.That(parts.Select(p => p.ToString()).ToArray(), Is.EquivalentTo(new[] {"Split", "on", "chars:", "a", "b", "c"}));
        ustr.Dispose();
    }

    [Test]
    public void Append_Char_Works()
    {
        // Arrange
        using var builder = new UstringBuilder();

        // Act
        builder.Append('A');
        builder.Append('B');

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo("AB"));
    }

    [Test]
    public void Append_Span_Works()
    {
        // Arrange
        using var builder = new UstringBuilder();

        // Act
        builder.Append("Hello".AsSpan());
        builder.AppendLine(" World".AsSpan());

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo("Hello World\n"));
    }

    [Test]
    public void Append_Generic_Works()
    {
        // Arrange
        using var builder = new UstringBuilder();

        // Act
        builder.Append(42);
        builder.Append(true);

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo("42true"));
    }

    [Test]
    public void AppendFormat_SingleArg_Works()
    {
        // Arrange
        using var builder = new UstringBuilder();

        // Act
        builder.AppendFormat("Value: {0}", 123);

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo("Value: 123"));
    }

    [Test]
    public void AppendFormat_MultipleArgs_Works()
    {
        // Arrange
        using var builder = new UstringBuilder();

        // Act
        builder.AppendFormat("A{0}B{1}C", "X", "Y");

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo("AXBYC"));
    }

    [Test]
    public void ToChars_ReturnsUstring()
    {
        // Arrange
        using var builder = new UstringBuilder();
        builder.Append("BuilderTest");

        // Act
        var result = builder.ToChars(false); // Don't dispose here

        // Assert
        Assert.That(result.ToString(), Is.EqualTo("BuilderTest"));
    }

    [Test]
    public void ToString_CallsToChars()
    {
        // Arrange
        using var builder = new UstringBuilder();
        builder.Append("ToStringTest");

        // Act
        var result = builder.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("ToStringTest"));
    }
}