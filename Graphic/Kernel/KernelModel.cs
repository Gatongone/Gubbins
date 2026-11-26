namespace Gubbins.Graphic.Kernels;

/// <summary>
/// The kinds of number a kernel computes with, which are the ones every backend has.
/// </summary>
public enum KernelScalar
{
    /// <summary>
    /// A signed 32 bit integer.
    /// </summary>
    Int = 0,

    /// <summary>
    /// An unsigned 32 bit integer.
    /// </summary>
    UInt = 1,

    /// <summary>
    /// A single precision number.
    /// </summary>
    Float = 2,

    /// <summary>
    /// A value an <c>if</c> or a loop tests.
    /// </summary>
    Bool = 3
}

/// <summary>
/// A type of the device, as one translation of a kernel describes it.
/// </summary>
/// <remarks>
/// The model holds the types the shading languages agree on, which is the subset a kernel is written in: a scalar, a
/// vector of two to four of them, a structure of the kernel's own, an array and void. Comparing two types is done with
/// <see cref="Same"/>, because a structure carries the arguments it was instantiated with.
/// </remarks>
public abstract record KernelType
{
    /// <summary>
    /// Gets the name a source of the device declares the type with, which is also what a refusal says.
    /// </summary>
    public abstract string TypeName { get; }

    /// <summary>
    /// Reports whether two types are the same type.
    /// </summary>
    /// <param name="left">One type.</param>
    /// <param name="right">The other type.</param>
    /// <returns><see langword="true"/> when they describe the same value.</returns>
    public static bool Same(KernelType? left, KernelType? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return (left, right) switch
        {
            (ScalarType one, ScalarType other) => one.Scalar == other.Scalar,
            (VectorType one, VectorType other) => one.Scalar == other.Scalar && one.Components == other.Components,
            (StructType one, StructType other) => one.Name == other.Name && one.Arguments.Count == other.Arguments.Count &&
                                                  one.Arguments.Zip(other.Arguments, Same).All(same => same),
            (TypeParameterType one, TypeParameterType other) => one.Name == other.Name,
            (ArrayType one, ArrayType other) => one.Length == other.Length && Same(one.Element, other.Element),
            (VoidType, VoidType) => true,
            _ => false
        };
    }

    /// <summary>
    /// Reads the scalar a value is made of: the scalar itself, or the one every component of a vector has.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The scalar, or <see cref="KernelScalar.Bool"/> when the type is not a number.</returns>
    public static KernelScalar ScalarOf(KernelType type) => type switch
    {
        ScalarType one => one.Scalar,
        VectorType vector => vector.Scalar,
        _ => KernelScalar.Bool
    };

    /// <summary>
    /// Reads the number of components a value has: one for a scalar, and the count of a vector.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The number of components.</returns>
    public static int ComponentsOf(KernelType type) => type is VectorType vector ? vector.Components : 1;

    /// <summary>
    /// Reads the type of one component of a value.
    /// </summary>
    /// <param name="type">The value's type.</param>
    /// <returns>The scalar type of a component.</returns>
    public static KernelType ComponentOf(KernelType type) => type is VectorType vector ? new ScalarType(vector.Scalar) : type;
}

/// <summary>
/// A single value of the device.
/// </summary>
/// <param name="Scalar">Which number it is.</param>
public sealed record ScalarType(KernelScalar Scalar) : KernelType
{
    /// <summary>
    /// The signed integer.
    /// </summary>
    public static readonly ScalarType Int = new(KernelScalar.Int);

    /// <summary>
    /// The unsigned integer.
    /// </summary>
    public static readonly ScalarType UInt = new(KernelScalar.UInt);

    /// <summary>
    /// The single precision number.
    /// </summary>
    public static readonly ScalarType Float = new(KernelScalar.Float);

    /// <summary>
    /// The value a condition holds.
    /// </summary>
    public static readonly ScalarType Bool = new(KernelScalar.Bool);

    /// <inheritdoc/>
    public override string TypeName => Scalar switch
    {
        KernelScalar.Int => "int",
        KernelScalar.UInt => "uint",
        KernelScalar.Float => "float",
        _ => "bool"
    };
}

/// <summary>
/// Two to four values of the same kind, which is what a buffer of a kernel can hold.
/// </summary>
/// <param name="Scalar">Which number each component is.</param>
/// <param name="Components">How many components there are.</param>
public sealed record VectorType(KernelScalar Scalar, int Components) : KernelType
{
    /// <inheritdoc/>
    public override string TypeName => (Scalar switch
    {
        KernelScalar.Int => "int",
        KernelScalar.UInt => "uint",
        _ => "float"
    }) + Components;
}

/// <summary>
/// A structure the kernel declares, instantiated with the arguments it holds.
/// </summary>
/// <param name="Name">The name the structure is declared under for the device, which carries its arguments.</param>
/// <param name="Arguments">The type arguments it was instantiated with, in the order it declares them.</param>
public sealed record StructType(string Name, IReadOnlyList<KernelType> Arguments) : KernelType
{
    /// <inheritdoc/>
    public override string TypeName => Name;
}

/// <summary>
/// The place a generic structure's field or method takes, before the structure is instantiated.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
public sealed record TypeParameterType(string Name) : KernelType
{
    /// <inheritdoc/>
    public override string TypeName => Name;
}

/// <summary>
/// A fixed number of values of the same type, which is a local array.
/// </summary>
/// <param name="Element">The type of one element.</param>
/// <param name="Length">How many elements there are.</param>
public sealed record ArrayType(KernelType Element, int Length) : KernelType
{
    /// <inheritdoc/>
    public override string TypeName => Element.TypeName + "[" + Length + "]";
}

/// <summary>
/// Nothing, which is what a kernel and the helpers that write their result through a parameter return.
/// </summary>
public sealed record VoidType : KernelType
{
    /// <summary>
    /// The only value of the type.
    /// </summary>
    public static readonly VoidType Void = new();

    /// <inheritdoc/>
    public override string TypeName => "void";
}

/// <summary>
/// The operations a kernel body computes with.
/// </summary>
public enum KernelBinaryOperator
{
    /// <summary>
    /// <c>left + right</c>.
    /// </summary>
    Add = 0,

    /// <summary>
    /// <c>left - right</c>.
    /// </summary>
    Subtract = 1,

    /// <summary>
    /// <c>left * right</c>.
    /// </summary>
    Multiply = 2,

    /// <summary>
    /// <c>left / right</c>.
    /// </summary>
    Divide = 3,

    /// <summary>
    /// <c>left % right</c>.
    /// </summary>
    Remainder = 4,

    /// <summary>
    /// <c>left &amp; right</c>.
    /// </summary>
    And = 5,

    /// <summary>
    /// <c>left | right</c>.
    /// </summary>
    Or = 6,

    /// <summary>
    /// <c>left ^ right</c>.
    /// </summary>
    Xor = 7,

    /// <summary>
    /// <c>left &lt;&lt; right</c>.
    /// </summary>
    ShiftLeft = 8,

    /// <summary>
    /// <c>left &gt;&gt; right</c>.
    /// </summary>
    ShiftRight = 9,

    /// <summary>
    /// <c>left == right</c>.
    /// </summary>
    Equal = 10,

    /// <summary>
    /// <c>left != right</c>.
    /// </summary>
    NotEqual = 11,

    /// <summary>
    /// <c>left &lt; right</c>.
    /// </summary>
    Less = 12,

    /// <summary>
    /// <c>left &lt;= right</c>.
    /// </summary>
    LessOrEqual = 13,

    /// <summary>
    /// <c>left &gt; right</c>.
    /// </summary>
    Greater = 14,

    /// <summary>
    /// <c>left &gt;= right</c>.
    /// </summary>
    GreaterOrEqual = 15,

    /// <summary>
    /// <c>left &amp;&amp; right</c>.
    /// </summary>
    LogicalAnd = 16,

    /// <summary>
    /// <c>left || right</c>.
    /// </summary>
    LogicalOr = 17
}

/// <summary>
/// The operations that take one value.
/// </summary>
public enum KernelUnaryOperator
{
    /// <summary>
    /// <c>-value</c>.
    /// </summary>
    Negate = 0,

    /// <summary>
    /// <c>~value</c>.
    /// </summary>
    Complement = 1,

    /// <summary>
    /// <c>!value</c>.
    /// </summary>
    Not = 2
}

/// <summary>
/// The calls a kernel makes that the device has as an operation of its own.
/// </summary>
public enum KernelIntrinsic
{
    /// <summary>
    /// The magnitude of a value.
    /// </summary>
    Abs = 0,

    /// <summary>
    /// The smaller of two values.
    /// </summary>
    Min = 1,

    /// <summary>
    /// The larger of two values.
    /// </summary>
    Max = 2,

    /// <summary>
    /// The square root of a value.
    /// </summary>
    Sqrt = 3,

    /// <summary>
    /// The largest whole number below a value.
    /// </summary>
    Floor = 4,

    /// <summary>
    /// The smallest whole number above a value.
    /// </summary>
    Ceiling = 5,

    /// <summary>
    /// A value raised to a power.
    /// </summary>
    Pow = 6,

    /// <summary>
    /// A value held between two others.
    /// </summary>
    Clamp = 7,

    /// <summary>
    /// The sine of a value.
    /// </summary>
    Sin = 8,

    /// <summary>
    /// The cosine of a value.
    /// </summary>
    Cos = 9,

    /// <summary>
    /// The sum of a value over the lanes of a wave.
    /// </summary>
    WaveSum = 10,

    /// <summary>
    /// The smallest value over the lanes of a wave.
    /// </summary>
    WaveMin = 11,

    /// <summary>
    /// The largest value over the lanes of a wave.
    /// </summary>
    WaveMax = 12,

    /// <summary>
    /// The product of a value over the lanes of a wave.
    /// </summary>
    WaveProduct = 13,

    /// <summary>
    /// The sum of the values of the lanes below the calling one.
    /// </summary>
    WavePrefixSum = 14,

    /// <summary>
    /// Whether any lane of a wave holds a true value.
    /// </summary>
    WaveAny = 15,

    /// <summary>
    /// Whether every lane of a wave holds a true value.
    /// </summary>
    WaveAll = 16
}

/// <summary>
/// A value a kernel body computes.
/// </summary>
public abstract record KernelExpression
{
    /// <summary>
    /// Gets the type of the value.
    /// </summary>
    public abstract KernelType ResultType { get; }
}

/// <summary>
/// A number the kernel wrote.
/// </summary>
/// <param name="Value">The value, boxed as the number type the kernel used.</param>
/// <param name="Type">Its type.</param>
public sealed record KernelLiteral(object Value, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// A local of the kernel, named the way the body reads it.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Type">Its type.</param>
public sealed record KernelLocal(string Name, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// One element of a buffer the caller passed.
/// </summary>
/// <param name="Buffer">The name the kernel declares the buffer with.</param>
/// <param name="Index">The element index, which the backend adds to the address the buffer is bound at.</param>
/// <param name="Type">The type of the element.</param>
public sealed record KernelBufferElement(string Buffer, KernelExpression Index, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// One element of a local array.
/// </summary>
/// <param name="Array">The name of the array.</param>
/// <param name="Index">The element index.</param>
/// <param name="Type">The type of the element.</param>
public sealed record KernelArrayElement(string Array, KernelExpression Index, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// One element of the memory a group shares.
/// </summary>
/// <param name="Array">The name the kernel declares the array with.</param>
/// <param name="Index">The element index.</param>
/// <param name="Type">The type of the element.</param>
public sealed record KernelSharedElement(string Array, KernelExpression Index, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// One field of a structure.
/// </summary>
/// <param name="Target">The value that holds it.</param>
/// <param name="Field">The name of the field.</param>
/// <param name="Type">Its type.</param>
public sealed record KernelFieldAccess(KernelExpression Target, string Field, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// Two values combined into one.
/// </summary>
/// <param name="Operator">The operation.</param>
/// <param name="Left">The value on the left.</param>
/// <param name="Right">The value on the right.</param>
/// <param name="Type">The type of the result.</param>
public sealed record KernelBinary(KernelBinaryOperator Operator, KernelExpression Left, KernelExpression Right, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// One value operated on.
/// </summary>
/// <param name="Operator">The operation.</param>
/// <param name="Operand">The value.</param>
/// <param name="Type">The type of the result.</param>
public sealed record KernelUnary(KernelUnaryOperator Operator, KernelExpression Operand, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// A value read as another type of the same kind, or as another kind.
/// </summary>
/// <param name="Target">The type it is read as.</param>
/// <param name="Operand">The value.</param>
public sealed record KernelConvert(KernelType Target, KernelExpression Operand) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Target;
}

/// <summary>
/// One of two values, chosen by a condition.
/// </summary>
/// <param name="Condition">What is tested.</param>
/// <param name="WhenTrue">The value when it holds.</param>
/// <param name="WhenFalse">The value when it does not.</param>
/// <param name="Type">The type of both values.</param>
public sealed record KernelConditional(KernelExpression Condition, KernelExpression WhenTrue, KernelExpression WhenFalse, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// A call of a function of the program: a helper of the kernel, or a method of one of its structures.
/// </summary>
/// <param name="Function">The function the call reaches.</param>
/// <param name="Arguments">The values passed, in the order the function declares them.</param>
public sealed record KernelCall(KernelFunction Function, IReadOnlyList<KernelExpression> Arguments) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Function.ReturnType;
}

/// <summary>
/// A call the device has as an operation of its own.
/// </summary>
/// <param name="Intrinsic">The operation.</param>
/// <param name="Arguments">The values passed to it.</param>
/// <param name="Type">The type of the result.</param>
public sealed record KernelIntrinsicCall(KernelIntrinsic Intrinsic, IReadOnlyList<KernelExpression> Arguments, KernelType Type) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Type;
}

/// <summary>
/// The position of the thread in the dispatch, which is the first parameter of a kernel.
/// </summary>
public sealed record KernelThreadIndex : KernelExpression
{
    /// <summary>
    /// The only value of the type, which reads the x axis of the dispatch.
    /// </summary>
    public static readonly KernelThreadIndex Value = new();

    /// <inheritdoc/>
    public override KernelType ResultType => ScalarType.Int;
}

/// <summary>
/// One element of the block that carries the element counts of the buffers.
/// </summary>
/// <param name="Index">The buffer the count belongs to, in the order the kernel declares its buffers.</param>
public sealed record KernelLengthsElement(int Index) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => ScalarType.UInt;
}

/// <summary>
/// One statement of a kernel body.
/// </summary>
public abstract record KernelStatement;

/// <summary>
/// Statements run in order.
/// </summary>
/// <param name="Statements">The statements.</param>
public sealed record KernelBlock(IReadOnlyList<KernelStatement> Statements) : KernelStatement
{
    /// <summary>
    /// The block that runs nothing.
    /// </summary>
    public static readonly KernelBlock Empty = new(Array.Empty<KernelStatement>());
}

/// <summary>
/// A local the body declares.
/// </summary>
/// <param name="Name">The name it is read by.</param>
/// <param name="Type">Its type.</param>
/// <param name="Initializer">The value it starts with, or <see langword="null"/> when it starts undefined.</param>
public sealed record KernelDeclaration(string Name, KernelType Type, KernelExpression? Initializer) : KernelStatement;

/// <summary>
/// A value written where it lives.
/// </summary>
/// <param name="Target">What is written: a local, an element of a buffer, a field, or one of those behind another.</param>
/// <param name="Value">The value written.</param>
public sealed record KernelAssignment(KernelExpression Target, KernelExpression Value) : KernelStatement;

/// <summary>
/// An expression evaluated for what it does rather than for its value.
/// </summary>
/// <param name="Expression">The expression.</param>
public sealed record KernelExpressionStatement(KernelExpression Expression) : KernelStatement;

/// <summary>
/// One of two blocks run by a condition.
/// </summary>
/// <param name="Condition">What is tested.</param>
/// <param name="Then">The block the condition runs.</param>
/// <param name="Else">The block it does not run, or <see langword="null"/> when there is none.</param>
public sealed record KernelIf(KernelExpression Condition, KernelBlock Then, KernelBlock? Else) : KernelStatement;

/// <summary>
/// A block run while a condition holds.
/// </summary>
/// <param name="Condition">What is tested before each pass.</param>
/// <param name="Body">What a pass runs.</param>
public sealed record KernelWhile(KernelExpression Condition, KernelBlock Body) : KernelStatement;

/// <summary>
/// A block run once and then while a condition holds.
/// </summary>
/// <param name="Body">What a pass runs.</param>
/// <param name="Condition">What is tested after each pass.</param>
public sealed record KernelDoWhile(KernelBlock Body, KernelExpression Condition) : KernelStatement;

/// <summary>
/// A block run for a number of passes, which is how a loop the kernel wrote is emitted.
/// </summary>
/// <param name="Run">What runs before the first test, or <see langword="null"/> when nothing does.</param>
/// <param name="Condition">What is tested before each pass.</param>
/// <param name="Next">What runs after each pass, or <see langword="null"/> when nothing does.</param>
/// <param name="Body">What a pass runs.</param>
public sealed record KernelFor(KernelStatement? Run, KernelExpression? Condition, KernelStatement? Next, KernelBlock Body) : KernelStatement;

/// <summary>
/// Leaves the loop the block is in.
/// </summary>
public sealed record KernelBreak : KernelStatement
{
    /// <summary>
    /// The only value of the type.
    /// </summary>
    public static readonly KernelBreak Value = new();
}

/// <summary>
/// Starts the next pass of the loop the block is in.
/// </summary>
public sealed record KernelContinue : KernelStatement
{
    /// <summary>
    /// The only value of the type.
    /// </summary>
    public static readonly KernelContinue Value = new();
}

/// <summary>
/// Ends a function, with a value when it has one.
/// </summary>
/// <param name="Value">The value it returns, or <see langword="null"/> when it returns none.</param>
public sealed record KernelReturn(KernelExpression? Value) : KernelStatement;

/// <summary>
/// Makes the threads of a group wait for each other.
/// </summary>
public sealed record KernelBarrier : KernelStatement
{
    /// <summary>
    /// The only value of the type.
    /// </summary>
    public static readonly KernelBarrier Value = new();
}

/// <summary>
/// Adds a value to an element several threads write at once, as one operation.
/// </summary>
/// <param name="Target">The element the value is added to.</param>
/// <param name="Value">The value added.</param>
public sealed record KernelAtomicAdd(KernelExpression Target, KernelExpression Value) : KernelStatement;

/// <summary>
/// One parameter of a function of the program.
/// </summary>
/// <param name="Name">The name the body reads it by.</param>
/// <param name="Type">Its type.</param>
/// <param name="Mode">How the caller passes it.</param>
public sealed record KernelFunctionParameter(string Name, KernelType Type, KernelParameterMode Mode);

/// <summary>
/// How a parameter is passed.
/// </summary>
public enum KernelParameterMode
{
    /// <summary>
    /// The value is read, and a write to it does not reach the caller.
    /// </summary>
    In = 0,

    /// <summary>
    /// The value is read and written back.
    /// </summary>
    InOut = 1,

    /// <summary>
    /// The value is only written, and starts undefined.
    /// </summary>
    Out = 2
}

/// <summary>
/// An array every thread of a group addresses, which the kernel declares with <c>Shared&lt;T&gt;(count)</c>.
/// </summary>
/// <param name="Name">The name the body reads it by.</param>
/// <param name="Element">The type of one element.</param>
/// <param name="Length">How many elements the group shares.</param>
public sealed record KernelSharedArray(string Name, KernelType Element, int Length);

/// <summary>
/// One function of the program: a helper of the kernel, a method of one of its structures, or the kernel itself.
/// </summary>
/// <param name="Name">The name the program calls it by, which is unique.</param>
/// <param name="ReturnType">What it returns.</param>
/// <param name="Parameters">What it takes, in the order it declares them.</param>
/// <param name="Body">What it runs.</param>
/// <param name="SharedArrays">The arrays its group shares.</param>
/// <param name="IsEntryPoint">Whether it is the kernel the dispatch runs.</param>
public sealed record KernelFunction(
    string Name,
    KernelType ReturnType,
    IReadOnlyList<KernelFunctionParameter> Parameters,
    KernelBlock Body,
    IReadOnlyList<KernelSharedArray> SharedArrays,
    bool IsEntryPoint);

/// <summary>
/// A structure the program declares, instantiated with the arguments the kernel used it with.
/// </summary>
/// <param name="Name">The name it is declared under, which carries its arguments.</param>
/// <param name="Fields">Its fields, in the order it declares them.</param>
public sealed record KernelStruct(string Name, IReadOnlyList<KernelField> Fields);

/// <summary>
/// One field of a structure.
/// </summary>
/// <param name="Name">The name it is read by.</param>
/// <param name="Type">Its type.</param>
public sealed record KernelField(string Name, KernelType Type);

/// <summary>
/// A buffer a kernel takes, as one backend binds it.
/// </summary>
/// <param name="Name">The name the kernel declares it with.</param>
/// <param name="ElementType">The type of one element.</param>
/// <param name="Binding">The index every backend binds it at.</param>
/// <param name="ReadOnly">Whether the kernel declared it read only.</param>
public sealed record KernelBuffer(string Name, KernelType ElementType, int Binding, bool ReadOnly);

/// <summary>
/// One kernel, lowered to the values and statements every backend can express.
/// </summary>
/// <remarks>
/// <para>
/// This is what the translator produces and what each backend reads: the C# of the kernel is already resolved into
/// numbers, buffers, structures and statements here, so an emitter only has to write the language it targets. The shape
/// the backends bind is part of the model: the buffers in the order they are declared, and the block that carries their
/// element counts at the binding after the last of them.
/// </para>
/// <para>
/// A span, a range and a <c>foreach</c> have no representation here: the translator resolved each of them into the buffer
/// it reads and the index it reads it at, which is what the device can do. The structure a kernel takes as one value is a
/// block of one element, which the entry point copies into a local before the body runs.
/// </para>
/// </remarks>
/// <param name="Name">The name the kernel is built under.</param>
/// <param name="ThreadsPerGroup">The number of threads in a group of it.</param>
/// <param name="Buffers">The buffers it takes, in the order it declares them.</param>
/// <param name="Structs">The structures it works with, in an order where one comes before the one that holds it.</param>
/// <param name="Functions">The functions of the program, with the kernel itself last.</param>
/// <param name="EntryPoint">The function a dispatch runs.</param>
/// <param name="Parameters">
/// The block that carries the structure a kernel takes as one value, or <see langword="null"/> when it takes none. It is
/// bound after the buffers, which is why a program that has one binds the block of element counts after it.
/// </param>
public sealed record KernelProgram(
    string Name,
    int ThreadsPerGroup,
    IReadOnlyList<KernelBuffer> Buffers,
    IReadOnlyList<KernelStruct> Structs,
    IReadOnlyList<KernelFunction> Functions,
    KernelFunction EntryPoint,
    KernelBuffer? Parameters = null,
    IReadOnlyList<KernelTexture>? Textures = null,
    IReadOnlyList<KernelSampler>? Samplers = null);

/// <summary>
/// A texture a kernel samples.
/// </summary>
/// <param name="Name">The name the kernel declares it with.</param>
/// <param name="ElementType">The type one sample returns.</param>
/// <param name="Binding">The index every backend binds it at.</param>
public sealed record KernelTexture(string Name, KernelType ElementType, int Binding);

/// <summary>
/// A sampler state a kernel samples a texture through.
/// </summary>
/// <param name="Name">The name the kernel declares it with.</param>
/// <param name="Binding">The index every backend binds it at.</param>
public sealed record KernelSampler(string Name, int Binding);

/// <summary>
/// A sampled read of a texture at one coordinate.
/// </summary>
/// <param name="Texture">The texture.</param>
/// <param name="Sampler">The sampler state.</param>
/// <param name="Coordinates">The coordinate, in normalized texture space.</param>
public sealed record KernelTextureSample(KernelTexture Texture, KernelSampler Sampler, KernelExpression Coordinates) : KernelExpression
{
    /// <inheritdoc/>
    public override KernelType ResultType => Texture.ElementType;
}
