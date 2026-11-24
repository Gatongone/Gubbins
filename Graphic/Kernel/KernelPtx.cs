using System.Globalization;
using System.Text;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Writes one program as the PTX text CUDA compiles for the card.
/// </summary>
/// <remarks>
/// <para>
/// The text is written here, in C#, with no compiler in between: the kernel is one function over the buffers the runtime
/// passes it, and the operations are the ones the kernel lowered to. The driver compiles the text for the card when the
/// module is loaded.
/// </para>
/// <para>
/// A program with helpers is written as the single function a dispatch runs: every call is written where it is made,
/// because a function of the model takes and returns values the device has no register for. The values the kernel holds
/// live in the local memory of the thread, which is what a value of a structure is; the numbers live in registers.
/// </para>
/// <para>
/// The text is written for the subset a card can run without the calls over the lanes of a wave, which CUDA has as a
/// reduction no instruction of the text performs, and for a kernel that takes exactly its two buffers: a program that makes
/// a wave call or takes a structure as one value is not written, and the artifact a CUDA device would load is not produced.
/// </para>
/// </remarks>
public sealed class KernelPtx
{
    /// <summary>
    /// Writes one program as PTX.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The text, or an empty string when the program is not one this backend can write.</returns>
    public string Emit(KernelProgram program)
    {
        if (program is null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        if (program.Buffers.Count != 2 || program.Parameters is not null || Wave(program))
        {
            return string.Empty;
        }

        return new PtxWriter(program).Write();
    }

    /// <summary>
    /// Reports whether a program makes a call over the lanes of a wave.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool Wave(KernelProgram program) => program.Functions.Any(function => Statements(function.Body));

    /// <summary>
    /// Reports whether a body makes a call over the lanes of a wave.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool Statements(KernelStatement statement) => statement switch
    {
        KernelBlock block => block.Statements.Any(Statements),
        KernelDeclaration declaration => declaration.Initializer is not null && Values(declaration.Initializer),
        KernelAssignment assignment => Values(assignment.Target) || Values(assignment.Value),
        KernelExpressionStatement expression => Values(expression.Expression),
        KernelIf branch => Values(branch.Condition) || Statements(branch.Then) || (branch.Else is not null && Statements(branch.Else)),
        KernelWhile loop => Values(loop.Condition) || Statements(loop.Body),
        KernelDoWhile loop => Values(loop.Condition) || Statements(loop.Body),
        KernelFor loop => Statements(loop.Body) || (loop.Run is not null && Statements(loop.Run)) || (loop.Next is not null && Statements(loop.Next)) || (loop.Condition is not null && Values(loop.Condition)),
        KernelReturn returned => returned.Value is not null && Values(returned.Value),
        KernelAtomicAdd atomic => Values(atomic.Target) || Values(atomic.Value),
        _ => false
    };

    /// <summary>
    /// Reports whether a value makes a call over the lanes of a wave.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool Values(KernelExpression expression) => expression switch
    {
        KernelIntrinsicCall call => call.Intrinsic >= KernelIntrinsic.WaveSum || call.Arguments.Any(Values),
        KernelBinary binary => Values(binary.Left) || Values(binary.Right),
        KernelUnary unary => Values(unary.Operand),
        KernelConvert convert => Values(convert.Operand),
        KernelConditional conditional => Values(conditional.Condition) || Values(conditional.WhenTrue) || Values(conditional.WhenFalse),
        KernelCall call => call.Arguments.Any(Values),
        KernelBufferElement element => Values(element.Index),
        KernelArrayElement element => Values(element.Index),
        KernelSharedElement element => Values(element.Index),
        KernelFieldAccess field => Values(field.Target),
        _ => false
    };
}

/// <summary>
/// Writes the text of one kernel: the entry point over the buffers the runtime passes it, with every call written where
/// it is made.
/// </summary>
internal sealed class PtxWriter
{
    private const string SOURCE = "__gubbinsSource";
    private const string DESTINATION = "__gubbinsDestination";
    private const string LENGTHS = "__gubbinsLengths";

    private readonly KernelProgram m_Program;
    private readonly StringBuilder m_Body = new();
    private readonly StringBuilder m_Locals = new();
    private readonly List<Dictionary<string, PtxSlot>> m_Scopes = new();
    private readonly Dictionary<string, string> m_Globals = new(StringComparer.Ordinal);
    private readonly Stack<(string Break, string Continue)> m_Loops = new();
    private int m_Registers;
    private int m_Floats;
    private int m_Pointers;
    private int m_Predicates;
    private int m_Locals_Count;
    private int m_Labels;
    private int m_Shared;

    internal PtxWriter(KernelProgram program) => m_Program = program;

    /// <summary>
    /// Writes the module.
    /// </summary>
    /// <returns>The text.</returns>
    internal string Write()
    {
        m_Scopes.Add(new Dictionary<string, PtxSlot>(StringComparer.Ordinal));

        // The runtime passes the two buffers and the block their counts live in, in that order.
        m_Globals[SOURCE] = Pointer("ld.param.u64 %rd, [__gubbinsSource];");
        m_Globals[DESTINATION] = Pointer("ld.param.u64 %rd, [__gubbinsDestination];");
        m_Globals[LENGTHS] = Pointer("ld.param.u64 %rd, [__gubbinsLengths];");

        foreach (var shared in m_Program.EntryPoint.SharedArrays)
        {
            var name = "__shared" + m_Shared++;
            m_Locals.AppendLine($"    .shared .align 4 .b8 {name}[{Stride(shared.Element) * shared.Length}];");
            m_Globals[shared.Name] = Pointer($"mov.u64 %rd, {name};");
        }

        foreach (var statement in m_Program.EntryPoint.Body.Statements)
        {
            Statement(statement);
        }

        Instruction("ret;");

        var text = new StringBuilder();
        text.AppendLine(".version 7.0");
        text.AppendLine(".target sm_70");
        text.AppendLine(".address_size 64");
        text.AppendLine();
        text.AppendLine(".visible .entry " + KernelTranspiler.ENTRY_POINT + "(");
        text.AppendLine($"    .param .u64 {SOURCE},");
        text.AppendLine($"    .param .u64 {DESTINATION},");
        text.AppendLine($"    .param .u64 {LENGTHS}");
        text.AppendLine(")");
        text.AppendLine("{");
        text.AppendLine($"    .reg .b32 %r<{Math.Max(m_Registers, 1)}>;");
        text.AppendLine($"    .reg .f32 %f<{Math.Max(m_Floats, 1)}>;");
        text.AppendLine($"    .reg .b64 %rd<{Math.Max(m_Pointers, 1)}>;");
        text.AppendLine($"    .reg .pred %p<{Math.Max(m_Predicates, 1)}>;");
        text.Append(m_Locals);
        text.Append(m_Body);
        text.AppendLine("}");

        return text.ToString();
    }

    /// <summary>
    /// Reads the address of the buffer a name is bound to.
    /// </summary>
    /// <param name="name">The name the kernel declares the buffer with.</param>
    /// <returns>The address.</returns>
    private string Global(string name)
    {
        var index = m_Program.Buffers.Select((candidate, position) => (candidate, position)).First(pair => pair.candidate.Name == name).position;

        return index == 0 ? m_Globals[SOURCE] : m_Globals[DESTINATION];
    }

    /// <summary>
    /// Writes one statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    private void Statement(KernelStatement statement)
    {
        switch (statement)
        {
            case KernelBlock block:
                foreach (var inner in block.Statements)
                {
                    Statement(inner);
                }

                return;

            case KernelDeclaration declaration:
            {
                var slot = Declare(declaration.Name, declaration.Type);

                if (declaration.Initializer is not null)
                {
                    Store(slot.Address, declaration.Type, Value(declaration.Initializer));
                }

                return;
            }

            case KernelAssignment assignment:
                Store(Address(assignment.Target), assignment.Target.ResultType, Value(assignment.Value));
                return;

            case KernelExpressionStatement expression:
                Value(expression.Expression);
                return;

            case KernelIf branch:
                If(branch);
                return;

            case KernelWhile loop:
                While(loop);
                return;

            case KernelDoWhile loop:
                DoWhile(loop);
                return;

            case KernelFor loop:
                For(loop);
                return;

            case KernelBreak:
                Instruction($"bra {m_Loops.Peek().Break};");
                return;

            case KernelContinue:
                Instruction($"bra {m_Loops.Peek().Continue};");
                return;

            case KernelReturn returned:
                if (returned.Value is not null)
                {
                    m_Return = Value(returned.Value);
                }

                Instruction($"bra {m_ReturnLabel};");
                return;

            case KernelBarrier:
                Instruction("bar.sync 0;");
                return;

            case KernelAtomicAdd atomic:
            {
                var address = Address(atomic.Target);
                var value = Value(atomic.Value);
                var result = Register();

                Instruction($"atom.add.u32 {result}, [{address}], {value.Registers[0]};");
                return;
            }

            default:
                throw new InvalidOperationException($"'{statement.GetType().Name}' is not a statement a kernel lowers to.");
        }
    }

    private PtxValue? m_Return;
    private string? m_ReturnLabel;

    /// <summary>
    /// Writes an <c>if</c>.
    /// </summary>
    /// <param name="branch">The statement.</param>
    private void If(KernelIf branch)
    {
        var then = Label();
        var otherwise = Label();
        var merge = Label();
        var condition = Predicate(branch.Condition);

        Instruction($"@%{condition} bra {then};");

        if (branch.Else is not null)
        {
            Instruction($"bra {otherwise};");
        }
        else
        {
            Instruction($"bra {merge};");
        }

        Instruction(then + ":");
        Statement(branch.Then);
        Instruction($"bra {merge};");

        if (branch.Else is not null)
        {
            Instruction(otherwise + ":");
            Statement(branch.Else);
            Instruction($"bra {merge};");
        }

        Instruction(merge + ":");
    }

    /// <summary>
    /// Writes a loop that tests its condition before each pass.
    /// </summary>
    /// <param name="loop">The statement.</param>
    private void While(KernelWhile loop)
    {
        var header = Label();
        var body = Label();
        var next = Label();
        var merge = Label();

        Instruction($"bra {header};");
        Instruction(header + ":");
        Instruction($"@%{Predicate(loop.Condition)} bra {body};");
        Instruction($"bra {merge};");
        Instruction(body + ":");
        m_Loops.Push((merge, next));
        Statement(loop.Body);
        m_Loops.Pop();
        Instruction($"bra {next};");
        Instruction(next + ":");
        Instruction($"bra {header};");
        Instruction(merge + ":");
    }

    /// <summary>
    /// Writes a loop that tests its condition after each pass.
    /// </summary>
    /// <param name="loop">The statement.</param>
    private void DoWhile(KernelDoWhile loop)
    {
        var body = Label();
        var next = Label();
        var merge = Label();

        Instruction($"bra {body};");
        Instruction(body + ":");
        m_Loops.Push((merge, next));
        Statement(loop.Body);
        m_Loops.Pop();
        Instruction($"bra {next};");
        Instruction(next + ":");
        Instruction($"@%{Predicate(loop.Condition)} bra {body};");
        Instruction($"bra {merge};");
        Instruction(merge + ":");
    }

    /// <summary>
    /// Writes a loop the kernel wrote as a <c>for</c>.
    /// </summary>
    /// <param name="loop">The statement.</param>
    private void For(KernelFor loop)
    {
        var header = Label();
        var body = Label();
        var next = Label();
        var merge = Label();

        if (loop.Run is not null)
        {
            Statement(loop.Run);
        }

        Instruction($"bra {header};");
        Instruction(header + ":");

        if (loop.Condition is not null)
        {
            Instruction($"@%{Predicate(loop.Condition)} bra {body};");
            Instruction($"bra {merge};");
        }
        else
        {
            Instruction($"bra {body};");
        }

        Instruction(body + ":");
        m_Loops.Push((merge, next));
        Statement(loop.Body);
        m_Loops.Pop();
        Instruction($"bra {next};");
        Instruction(next + ":");

        if (loop.Next is not null)
        {
            Statement(loop.Next);
        }

        Instruction($"bra {header};");
        Instruction(merge + ":");
    }

    /// <summary>
    /// Writes one value.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>Its registers, or the address of a value that lives in memory.</returns>
    private PtxValue Value(KernelExpression expression)
    {
        switch (expression)
        {
            case KernelLiteral literal:
                return Literal(literal);

            case KernelLocal local:
                return Load(Slot(local.Name).Address, local.ResultType);

            case KernelThreadIndex:
                return ThreadIndex();

            case KernelLengthsElement lengths:
            {
                var address = AddressOf(m_Globals[LENGTHS], lengths.Index * 4);
                var register = Register();
                Instruction($"ld.b32 {register}, [{address}];");

                return PtxValue.Word(register);
            }

            case KernelFieldAccess field:
                return Load(Address(field), field.ResultType);

            case KernelBufferElement element:
                return Load(Address(element), element.ResultType);

            case KernelArrayElement array:
                return Load(Address(array), array.ResultType);

            case KernelSharedElement shared:
                return Load(Address(shared), shared.ResultType);

            case KernelCall call:
                return Inline(call);

            case KernelIntrinsicCall intrinsic:
                return Intrinsic(intrinsic);

            case KernelBinary binary:
                return Binary(binary);

            case KernelUnary unary:
                return Unary(unary);

            case KernelConvert convert:
                return Convert(convert);

            case KernelConditional conditional:
                return Conditional(conditional);

            default:
                throw new InvalidOperationException($"'{expression.GetType().Name}' is not a value a kernel lowers to.");
        }
    }

    /// <summary>
    /// Writes the address of a value that lives somewhere.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>The register that holds its address.</returns>
    private string Address(KernelExpression expression)
    {
        switch (expression)
        {
            case KernelLocal local:
                return Slot(local.Name).Address;

            case KernelBufferElement element:
            {
                var baseAddress = Global(element.Buffer);

                return AddressOf(baseAddress, element.Index, Stride(m_Program.Buffers.First(buffer => buffer.Name == element.Buffer).ElementType));
            }

            case KernelArrayElement array:
                return AddressOf(Slot(array.Array).Address, array.Index, Stride(array.ResultType));

            case KernelSharedElement shared:
            {
                var sharedAddress = m_Globals[shared.Array];
                var offset = ElementOffset(BufferOf(shared.Array).Element, shared.Index);
                var result = "%rd" + m_Pointers++;

                Instruction($"add.u64 {result}, {sharedAddress}, {offset.Registers[0]};");

                return result;
            }

            case KernelFieldAccess field:
            {
                var offset = MemberOffset(field.Target.ResultType, field.Field);

                return Add(Address(field.Target), offset);
            }

            default:
                throw new KernelTranslationException($"'{expression}' is not a value the device addresses.");
        }
    }

    /// <summary>
    /// Reads the shared array a name declares.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The array.</returns>
    private KernelSharedArray BufferOf(string name) => m_Program.EntryPoint.SharedArrays.First(array => array.Name == name);

    /// <summary>
    /// Loads a value of a type from an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="type">The type.</param>
    /// <returns>The value.</returns>
    private PtxValue Load(string address, KernelType type)
    {
        switch (type)
        {
            case ScalarType { Scalar: KernelScalar.Float }:
            {
                var register = Float();
                Instruction($"ld.f32 {register}, [{address}];");

                return PtxValue.Single(register, type);
            }

            case ScalarType:
            {
                var register = Register();
                Instruction($"ld.b32 {register}, [{address}];");

                return PtxValue.Single(register, type);
            }

            case VectorType vector:
            {
                var registers = new List<string>();
                var element = new ScalarType(vector.Scalar);

                for (var index = 0; index < vector.Components; index++)
                {
                    registers.Add(Load(Offset(address, index * 4), element).Registers[0]);
                }

                return PtxValue.Many(registers, type);
            }

            default:
                return PtxValue.At(address, type);
        }
    }

    /// <summary>
    /// Stores a value of a type at an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="type">The type.</param>
    /// <param name="value">The value.</param>
    private void Store(string address, KernelType type, PtxValue value)
    {
        switch (type)
        {
            case ScalarType { Scalar: KernelScalar.Float }:
                Instruction($"st.f32 [{address}], {value.Registers[0]};");
                return;

            case ScalarType:
                Instruction($"st.b32 [{address}], {value.Registers[0]};");
                return;

            case VectorType vector:
            {
                for (var index = 0; index < vector.Components; index++)
                {
                    Store(Offset(address, index * 4), new ScalarType(vector.Scalar), PtxValue.Single(value.Registers[index], new ScalarType(vector.Scalar)));
                }

                return;
            }

            default:
            {
                if (value.Address is null)
                {
                    throw new KernelTranslationException($"'{type.TypeName}' is a value the device copies through the memory it lives in.");
                }

                Copy(address, value.Address, type);
                return;
            }
        }
    }

    /// <summary>
    /// Copies a value of a type from one address to another.
    /// </summary>
    /// <param name="target">The address written.</param>
    /// <param name="source">The address read.</param>
    /// <param name="type">The type.</param>
    private void Copy(string target, string source, KernelType type)
    {
        switch (type)
        {
            case StructType structure:
            {
                var declaration = m_Program.Structs.First(candidate => candidate.Name == structure.Name);

                foreach (var field in declaration.Fields)
                {
                    Copy(
                        Offset(target, MemberOffset(structure, field.Name)),
                        Offset(source, MemberOffset(structure, field.Name)),
                        field.Type);
                }

                return;
            }

            case ArrayType array:
            {
                for (var index = 0; index < array.Length; index++)
                {
                    Copy(Offset(target, index * Stride(array.Element)), Offset(source, index * Stride(array.Element)), array.Element);
                }

                return;
            }

            default:
            {
                var value = Load(source, type);
                Store(target, type, value);
                return;
            }
        }
    }

    /// <summary>
    /// Writes a call where it is made, which is what the device can run.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The value the call computes.</returns>
    private PtxValue Inline(KernelCall call)
    {
        var scope = new Dictionary<string, PtxSlot>(StringComparer.Ordinal);
        var result = call.Function.ReturnType is VoidType ? null : Declare("__result" + m_Locals_Count++, call.Function.ReturnType);
        var previousReturn = m_Return;
        var previousLabel = m_ReturnLabel;
        var end = Label();

        for (var index = 0; index < call.Function.Parameters.Count; index++)
        {
            var parameter = call.Function.Parameters[index];

            scope[parameter.Name] = parameter.Mode == KernelParameterMode.In
                ? new PtxSlot(Store(call.Arguments[index], parameter.Type), parameter.Type)
                : new PtxSlot(Address(call.Arguments[index]), parameter.Type);
        }

        m_Scopes.Add(scope);
        m_Return = null;
        m_ReturnLabel = end;

        foreach (var statement in call.Function.Body.Statements)
        {
            if (statement is KernelReturn returned)
            {
                m_Return = returned.Value is null ? null : Value(returned.Value);
                Instruction($"bra {end};");
                continue;
            }

            Statement(statement);
        }

        m_Scopes.RemoveAt(m_Scopes.Count - 1);
        Instruction(end + ":");

        var value = m_Return;
        m_Return = previousReturn;
        m_ReturnLabel = previousLabel;

        if (result is null)
        {
            return PtxValue.At("0", VoidType.Void);
        }

        if (value is not null)
        {
            Store(result.Address, call.Function.ReturnType, value);
        }

        return Load(result.Address, call.Function.ReturnType);
    }

    /// <summary>
    /// Stores a value into a local of the function being written and answers the address it was stored at.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <param name="type">The type.</param>
    /// <returns>The address.</returns>
    private string Store(KernelExpression expression, KernelType type)
    {
        var slot = Declare("__argument" + m_Locals_Count++, type);

        Store(slot.Address, type, Value(expression));

        return slot.Address;
    }

    /// <summary>
    /// Writes one of the calls the device has as an operation.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue Intrinsic(KernelIntrinsicCall call)
    {
        var type = call.ResultType;
        var floating = KernelType.ScalarOf(type) == KernelScalar.Float;
        var arguments = call.Arguments.Select(Value).ToList();

        switch (call.Intrinsic)
        {
            case KernelIntrinsic.Abs:
                return UnaryOperation(floating ? "abs.f32" : "abs.s32", type, arguments[0]);

            case KernelIntrinsic.Min:
                return BinaryOperation(floating ? "min.f32" : KernelType.ScalarOf(type) == KernelScalar.UInt ? "min.u32" : "min.s32", type, arguments[0], arguments[1]);

            case KernelIntrinsic.Max:
                return BinaryOperation(floating ? "max.f32" : KernelType.ScalarOf(type) == KernelScalar.UInt ? "max.u32" : "max.s32", type, arguments[0], arguments[1]);

            case KernelIntrinsic.Clamp:
                return BinaryOperation(floating ? "max.f32" : "max.u32", type,
                    BinaryOperation(floating ? "min.f32" : "min.u32", type, arguments[0], arguments[2]),
                    arguments[1]);

            case KernelIntrinsic.Sqrt:
                return UnaryOperation("sqrt.rn.f32", type, arguments[0]);

            case KernelIntrinsic.Floor:
                return UnaryOperation("cvt.rmi.f32.f32", type, arguments[0]);

            case KernelIntrinsic.Ceiling:
                return UnaryOperation("cvt.rpi.f32.f32", type, arguments[0]);

            case KernelIntrinsic.Sin:
                return UnaryOperation("sin.approx.f32", type, arguments[0]);

            case KernelIntrinsic.Cos:
                return UnaryOperation("cos.approx.f32", type, arguments[0]);

            default:
            {
                // The power is the exponential of the logarithm the card has, which is the pair of instructions it
                // computes one with.
                var logarithm = UnaryOperation("lg2.approx.f32", type, arguments[0]);
                var product = BinaryOperation("mul.f32", type, logarithm, arguments[1]);

                return UnaryOperation("ex2.approx.f32", type, product);
            }
        }
    }

    /// <summary>
    /// Writes an instruction that takes one value.
    /// </summary>
    /// <param name="instruction">The instruction.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue UnaryOperation(string instruction, KernelType type, PtxValue value)
    {
        var registers = Registers(type);

        for (var index = 0; index < registers.Count; index++)
        {
            Instruction($"{instruction} {registers[index]}, {value.Registers[index]};");
        }

        return PtxValue.Many(registers, type);
    }

    /// <summary>
    /// Writes an instruction that takes two values.
    /// </summary>
    /// <param name="instruction">The instruction.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="left">The value on the left.</param>
    /// <param name="right">The value on the right.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue BinaryOperation(string instruction, KernelType type, PtxValue left, PtxValue right)
    {
        var registers = Registers(type);

        for (var index = 0; index < registers.Count; index++)
        {
            Instruction($"{instruction} {registers[index]}, {left.Registers[index]}, {right.Registers[index]};");
        }

        return PtxValue.Many(registers, type);
    }

    /// <summary>
    /// Writes a binary operation.
    /// </summary>
    /// <param name="binary">The operation.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue Binary(KernelBinary binary)
    {
        var floating = KernelType.ScalarOf(binary.Left.ResultType) == KernelScalar.Float;
        var unsigned = KernelType.ScalarOf(binary.Left.ResultType) == KernelScalar.UInt;
        var left = Value(binary.Left);
        var right = Value(binary.Right);

        if (binary.Operator is KernelBinaryOperator.Equal or KernelBinaryOperator.NotEqual or KernelBinaryOperator.Less
            or KernelBinaryOperator.LessOrEqual or KernelBinaryOperator.Greater or KernelBinaryOperator.GreaterOrEqual)
        {
            var predicate = Compare(binary.Operator, binary.Left.ResultType, left, right);
            var result = "%r" + m_Registers++;
            Instruction($"selp.u32 {result}, 1, 0, %{predicate};");

            return PtxValue.Single(result, ScalarType.Bool);
        }

        var instruction = binary.Operator switch
        {
            KernelBinaryOperator.Add => floating ? "add.f32" : "add.u32",
            KernelBinaryOperator.Subtract => floating ? "sub.f32" : "sub.u32",
            KernelBinaryOperator.Multiply => floating ? "mul.f32" : "mul.lo.u32",
            KernelBinaryOperator.Divide => floating ? "div.f32" : unsigned ? "div.u32" : "div.s32",
            KernelBinaryOperator.Remainder => unsigned ? "rem.u32" : "rem.s32",
            KernelBinaryOperator.And => "and.b32",
            KernelBinaryOperator.Or => "or.b32",
            KernelBinaryOperator.Xor => "xor.b32",
            KernelBinaryOperator.ShiftLeft => "shl.b32",
            KernelBinaryOperator.ShiftRight => unsigned ? "shr.u32" : "shr.s32",
            KernelBinaryOperator.LogicalAnd => "and.b32",
            _ => "or.b32"
        };

        return BinaryOperation(instruction, binary.ResultType, left, right);
    }

    /// <summary>
    /// Writes the test a comparison is.
    /// </summary>
    /// <param name="operation">The comparison.</param>
    /// <param name="type">The type of the values.</param>
    /// <param name="left">The value on the left.</param>
    /// <param name="right">The value on the right.</param>
    /// <returns>The predicate.</returns>
    private string Compare(KernelBinaryOperator operation, KernelType type, PtxValue left, PtxValue right)
    {
        var floating = KernelType.ScalarOf(type) == KernelScalar.Float;
        var unsigned = KernelType.ScalarOf(type) == KernelScalar.UInt;

        var comparison = operation switch
        {
            KernelBinaryOperator.Equal => "eq",
            KernelBinaryOperator.NotEqual => "ne",
            KernelBinaryOperator.Less => floating ? "lt" : unsigned ? "lt.u32" : "lt.s32",
            KernelBinaryOperator.LessOrEqual => floating ? "le" : unsigned ? "le.u32" : "le.s32",
            KernelBinaryOperator.Greater => floating ? "gt" : unsigned ? "gt.u32" : "gt.s32",
            _ => floating ? "ge" : unsigned ? "ge.u32" : "ge.s32"
        };

        var suffix = comparison.Contains('.') ? string.Empty : floating ? ".f32" : ".b32";
        var predicate = "%p" + m_Predicates++;
        Instruction($"setp.{comparison}{suffix} {predicate}, {left.Registers[0]}, {right.Registers[0]};");

        return predicate.TrimStart('%');
    }

    /// <summary>
    /// Writes a unary operation.
    /// </summary>
    /// <param name="unary">The operation.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue Unary(KernelUnary unary)
    {
        var floating = KernelType.ScalarOf(unary.ResultType) == KernelScalar.Float;
        var value = Value(unary.Operand);

        if (unary.Operator == KernelUnaryOperator.Not)
        {
            var result = "%r" + m_Registers++;
            Instruction($"xor.b32 {result}, {value.Registers[0]}, 1;");

            return PtxValue.Single(result, ScalarType.Bool);
        }

        var instruction = unary.Operator switch
        {
            KernelUnaryOperator.Negate => floating ? "neg.f32" : "neg.s32",
            _ => "not.b32"
        };

        return UnaryOperation(instruction, unary.ResultType, value);
    }

    /// <summary>
    /// Writes a value read as another type.
    /// </summary>
    /// <param name="convert">The value.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue Convert(KernelConvert convert)
    {
        var source = KernelType.ScalarOf(convert.Operand.ResultType);
        var target = KernelType.ScalarOf(convert.Target);
        var value = Value(convert.Operand);

        if (source == target || (source != KernelScalar.Float && target != KernelScalar.Float))
        {
            return PtxValue.Many(value.Registers, convert.Target);
        }

        var instruction = (source, target) switch
        {
            (KernelScalar.Float, KernelScalar.Int) => "cvt.rzi.s32.f32",
            (KernelScalar.Float, KernelScalar.UInt) => "cvt.rzi.u32.f32",
            (KernelScalar.Int, KernelScalar.Float) => "cvt.rn.f32.s32",
            (KernelScalar.UInt, KernelScalar.Float) => "cvt.rn.f32.u32",
            _ => "mov.b32"
        };

        return UnaryOperation(instruction, convert.Target, value);
    }

    /// <summary>
    /// Writes one of the two values a condition chooses.
    /// </summary>
    /// <param name="conditional">The expression.</param>
    /// <returns>The value it computes.</returns>
    private PtxValue Conditional(KernelConditional conditional)
    {
        if (conditional.ResultType is StructType or ArrayType)
        {
            throw new KernelTranslationException($"'{conditional}' chooses between values the device selects only one of at a time.");
        }

        var predicate = Predicate(conditional.Condition);
        var whenTrue = Value(conditional.WhenTrue);
        var whenFalse = Value(conditional.WhenFalse);
        var registers = Registers(conditional.ResultType);
        var floating = KernelType.ScalarOf(conditional.ResultType) == KernelScalar.Float;

        for (var index = 0; index < registers.Count; index++)
        {
            Instruction($"selp.{(floating ? "f32" : "b32")} {registers[index]}, {whenTrue.Registers[index]}, {whenFalse.Registers[index]}, %{predicate};");
        }

        return PtxValue.Many(registers, conditional.ResultType);
    }

    /// <summary>
    /// Writes the value the entry point reads the thread position from.
    /// </summary>
    /// <returns>The value.</returns>
    private PtxValue ThreadIndex()
    {
        var block = "%r" + m_Registers++;
        var threads = "%r" + m_Registers++;
        var thread = "%r" + m_Registers++;
        var result = "%r" + m_Registers++;

        Instruction($"mov.u32 {block}, %ctaid.x;");
        Instruction($"mov.u32 {threads}, %ntid.x;");
        Instruction($"mov.u32 {thread}, %tid.x;");
        Instruction($"mad.lo.u32 {result}, {block}, {threads}, {thread};");

        return PtxValue.Single(result, ScalarType.Int);
    }

    /// <summary>
    /// Reads the predicate a condition is.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <returns>The predicate.</returns>
    private string Predicate(KernelExpression condition)
    {
        if (condition is KernelBinary comparison && comparison.Operator is KernelBinaryOperator.Equal or KernelBinaryOperator.NotEqual
            or KernelBinaryOperator.Less or KernelBinaryOperator.LessOrEqual or KernelBinaryOperator.Greater or KernelBinaryOperator.GreaterOrEqual)
        {
            return Compare(comparison.Operator, comparison.Left.ResultType, Value(comparison.Left), Value(comparison.Right));
        }

        if (condition is KernelUnary { Operator: KernelUnaryOperator.Not } negation)
        {
            return Predicate(negation.Operand);
        }

        var value = Value(condition);
        var predicate = "%p" + m_Predicates++;
        Instruction($"setp.ne.b32 {predicate}, {value.Registers[0]}, 0;");

        return predicate.TrimStart('%');
    }

    /// <summary>
    /// Reads a literal.
    /// </summary>
    /// <param name="literal">The literal.</param>
    /// <returns>The value.</returns>
    private PtxValue Literal(KernelLiteral literal)
    {
        switch (literal.Value)
        {
            case float number:
            {
                var register = "%f" + m_Floats++;
                Instruction($"mov.f32 {register}, 0f{BitConverter.ToUInt32(BitConverter.GetBytes(number), 0):X8};");

                return PtxValue.Single(register, ScalarType.Float);
            }

            case bool flag:
            {
                var register = "%r" + m_Registers++;
                Instruction($"mov.u32 {register}, {(flag ? 1 : 0)};");

                return PtxValue.Single(register, ScalarType.Bool);
            }

            default:
            {
                var register = "%r" + m_Registers++;
                var value = literal.Value switch { int number => (uint)number, uint number => number, _ => 0u };
                Instruction($"mov.u32 {register}, {value};");

                return PtxValue.Single(register, literal.Type);
            }
        }
    }

    /// <summary>
    /// Allocates the registers of one value.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <returns>The registers.</returns>
    private List<string> Registers(KernelType type)
    {
        var floating = KernelType.ScalarOf(type) == KernelScalar.Float;
        var count = KernelType.ComponentsOf(type);
        var registers = new List<string>(count);

        for (var index = 0; index < count; index++)
        {
            registers.Add(floating ? "%f" + m_Floats++ : "%r" + m_Registers++);
        }

        return registers;
    }




    /// <summary>
    /// Allocates one register.
    /// </summary>
    /// <returns>The register.</returns>
    private string Register() => "%r" + m_Registers++;

    /// <summary>
    /// Allocates one floating point register.
    /// </summary>
    /// <returns>The register.</returns>
    private string Float() => "%f" + m_Floats++;

    /// <summary>
    /// Writes the address of an element at a constant offset.
    /// </summary>
    /// <param name="address">The address of the array.</param>
    /// <param name="offset">The offset, in bytes.</param>
    /// <returns>The address.</returns>
    private string AddressOf(string address, int offset) => Add(address, offset);

    /// <summary>
    /// Allocates one address register.
    /// </summary>
    /// <returns>The register.</returns>
    private string Pointer(string instruction)
    {
        var register = "%rd" + m_Pointers++;
        m_Body.AppendLine("    " + instruction.Replace("%rd", register));

        return register;
    }

    /// <summary>
    /// Allocates one predicate.
    /// </summary>
    /// <returns>The predicate name, without its percent.</returns>
    

    /// <summary>
    /// Declares a local of the function being written.
    /// </summary>
    /// <param name="name">The name the body reads it by.</param>
    /// <param name="type">The type it holds.</param>
    /// <returns>The slot.</returns>
    private PtxSlot Declare(string name, KernelType type)
    {
        var storage = "__local" + m_Locals_Count++;
        m_Locals.AppendLine($"    .local .align 4 .b8 {storage}[{Stride(type)}];");

        var address = "%rd" + m_Pointers++;
        m_Body.AppendLine($"    mov.u64 {address}, {storage};");

        var slot = new PtxSlot(address, type);
        m_Scopes[m_Scopes.Count - 1][name] = slot;

        return slot;
    }

    /// <summary>
    /// Reads a local of the function being written.
    /// </summary>
    /// <param name="name">The name the body reads it by.</param>
    /// <returns>The slot.</returns>
    private PtxSlot Slot(string name)
    {
        for (var index = m_Scopes.Count - 1; index >= 0; index--)
        {
            if (m_Scopes[index].TryGetValue(name, out var slot))
            {
                return slot;
            }
        }

        throw new KernelTranslationException($"'{name}' is read by the device and was not declared.");
    }

    /// <summary>
    /// Writes the address of an element of a buffer.
    /// </summary>
    /// <param name="address">The address of the buffer.</param>
    /// <param name="index">The element index.</param>
    /// <param name="stride">The distance one element is from the next.</param>
    /// <returns>The address of the element.</returns>
    private string AddressOf(string address, KernelExpression index, int stride)
    {
        var offset = Mul(index, stride);
        var result = "%rd" + m_Pointers++;
        Instruction($"add.u64 {result}, {address}, {offset.Registers[0]};");

        return result;
    }



    /// <summary>
    /// Adds a constant offset to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="offset">The offset, in bytes.</param>
    /// <returns>The address.</returns>
    private string Offset(string address, int offset) => Add(address, offset);

    /// <summary>
    /// Adds a constant offset to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="offset">The offset, in bytes.</param>
    /// <returns>The address.</returns>
    private string Add(string address, int offset)
    {
        if (offset == 0)
        {
            return address;
        }

        var result = "%rd" + m_Pointers++;
        Instruction($"add.u64 {result}, {address}, {offset};");

        return result;
    }



    /// <summary>
    /// Writes the byte offset of an element of a shared array.
    /// </summary>
    /// <param name="element">The element type.</param>
    /// <param name="index">The element index.</param>
    /// <returns>The offset.</returns>
    private PtxValue ElementOffset(KernelType element, KernelExpression index) => Mul(index, Stride(element));

    /// <summary>
    /// Multiplies a value by a constant.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="factor">The factor.</param>
    /// <returns>The product, as an address.</returns>
    private PtxValue Mul(KernelExpression value, int factor)
    {
        var index = Value(value).Registers[0];
        var register = "%rd" + m_Pointers++;

        Instruction($"mul.wide.u32 {register}, {index}, {factor};");

        return PtxValue.Single(register, ScalarType.Int);
    }

    /// <summary>
    /// Reads the offset of a field of a structure.
    /// </summary>
    /// <param name="type">The structure.</param>
    /// <param name="name">The field.</param>
    /// <returns>The offset, in bytes.</returns>
    private int MemberOffset(KernelType type, string name)
    {
        var declaration = m_Program.Structs.First(candidate => candidate.Name == type.TypeName);
        var offset = 0;

        foreach (var field in declaration.Fields)
        {
            offset = Align(offset, AlignOf(field.Type));

            if (field.Name == name)
            {
                return offset;
            }

            offset += SizeOf(field.Type);
        }

        throw new KernelTranslationException($"'{name}' is not a field of '{type.TypeName}'.");
    }

    /// <summary>
    /// Reads the size a value takes in memory.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The size, in bytes.</returns>
    private int SizeOf(KernelType type) => type switch
    {
        ScalarType => 4,
        VectorType vector => vector.Components * 4,
        StructType structure => StructSize(structure),
        ArrayType array => Stride(array.Element) * array.Length,
        _ => 4
    };

    /// <summary>
    /// Reads the distance one element of an array is from the next.
    /// </summary>
    /// <param name="type">The element type.</param>
    /// <returns>The stride, in bytes.</returns>
    private int Stride(KernelType type) => Align(SizeOf(type), AlignOf(type));

    /// <summary>
    /// Reads the alignment of a value.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The alignment, in bytes.</returns>
    private int AlignOf(KernelType type) => type switch
    {
        VectorType vector when vector.Components == 2 => 8,
        VectorType vector when vector.Components >= 3 => 16,
        StructType structure => StructAlign(structure),
        _ => 4
    };

    private int StructSize(StructType structure)
    {
        var declaration = m_Program.Structs.First(candidate => candidate.Name == structure.Name);
        var offset = 0;

        foreach (var field in declaration.Fields)
        {
            offset = Align(offset, AlignOf(field.Type));
            offset += SizeOf(field.Type);
        }

        return Align(offset, StructAlign(structure));
    }

    private int StructAlign(StructType structure)
    {
        var declaration = m_Program.Structs.First(candidate => candidate.Name == structure.Name);

        return declaration.Fields.Count == 0 ? 4 : declaration.Fields.Max(field => AlignOf(field.Type));
    }

    private static int Align(int offset, int alignment) => (offset + alignment - 1) / alignment * alignment;

    /// <summary>
    /// Writes one instruction.
    /// </summary>
    /// <param name="instruction">The instruction.</param>
    private void Instruction(string instruction) => m_Body.AppendLine("    " + instruction);

    /// <summary>
    /// Reads the next label of the function.
    /// </summary>
    /// <returns>The label.</returns>
    private string Label() => "$L" + m_Labels++;

    /// <summary>
    /// One local of the function being written.
    /// </summary>
    /// <param name="Address">The register that holds its address.</param>
    /// <param name="Type">The type it holds.</param>
    /// <param name="Direct">Whether the register holds the value rather than its address.</param>
    private sealed record PtxSlot(string Address, KernelType Type);

    /// <summary>
    /// The registers of one value, or the address it lives at.
    /// </summary>
    /// <param name="Type">Its type.</param>
    /// <param name="Registers">Its registers, one per component.</param>
    /// <param name="Address">Its address, when it lives in memory.</param>
    private sealed record PtxValue(KernelType Type, IReadOnlyList<string> Registers, string? Address)
    {
        internal static PtxValue Single(string register, KernelType type) => new(type, new[] { register }, null);

        internal static PtxValue Word(string register) => new(ScalarType.UInt, new[] { register }, null);

        internal static PtxValue Many(IReadOnlyList<string> registers, KernelType type) => new(type, registers, null);

        internal static PtxValue At(string address, KernelType type) => new(type, Array.Empty<string>(), address);
    }
}
