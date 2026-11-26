using System.Globalization;
using System.Text;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Writes one program as the source a backend compiles, with the shape of the language a subclass fills in.
/// </summary>
/// <remarks>
/// <para>
/// The languages a backend loads agree on what a statement, a block, a call and a loop look like; they differ in how a
/// type is named, how a buffer is bound, how a thread reads its position and how an intrinsic is spelled. This writes the
/// half they agree on from <see cref="KernelProgram"/>, and every difference is a hook a subclass answers.
/// </para>
/// <para>
/// An expression is written with the parentheses its precedence needs and no others, so that the source a backend is given
/// reads like the code the kernel was written as rather than like a serialization.
/// </para>
/// </remarks>
public abstract class KernelTextEmitter
{
    /// <summary>
    /// The indentation of one block.
    /// </summary>
    protected const string INDENT = "    ";


    /// <summary>
    /// Writes one program as the source of the backend.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The source.</returns>
    public string Emit(KernelProgram program)
    {
        if (program is null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        if ((program.Textures?.Count ?? 0) > 0 && !SupportsTextures)
        {
            return string.Empty;
        }

        Prepare(program);

        var text = new StringBuilder();
        var header = Header(program);

        if (header.Length > 0)
        {
            text.AppendLine(header);
        }

        foreach (var structure in program.Structs)
        {
            text.AppendLine($"struct {Identifier(structure.Name)}");
            text.AppendLine("{");

            foreach (var field in structure.Fields)
            {
                text.AppendLine(INDENT + Type(field.Type) + " " + Identifier(field.Name) + ";");
            }

            text.AppendLine("};");
            text.AppendLine();
        }

        if (!SharedInEntry)
        {
            foreach (var array in program.EntryPoint.SharedArrays)
            {
                text.AppendLine(Shared(array));
            }

            if (program.EntryPoint.SharedArrays.Count > 0)
            {
                text.AppendLine();
            }
        }

        // The languages declare a function before the one that calls it, so every function is named before any of them is
        // written.
        foreach (var function in program.Functions)
        {
            if (!function.IsEntryPoint)
            {
                text.AppendLine($"{Type(function.ReturnType)} {Identifier(function.Name)}({Parameters(function)});");
            }
        }

        if (program.Functions.Count(function => !function.IsEntryPoint) > 0)
        {
            text.AppendLine();
        }

        foreach (var function in program.Functions)
        {
            if (!function.IsEntryPoint)
            {
                Function(function, text);
            }
        }

        Bindings(program, text);

        Entry(program, text);

        return text.ToString();
    }

    /// <summary>
    /// Gives a backend the program before it writes anything, so a language that has to rename a name can decide once.
    /// </summary>
    /// <param name="program">The kernel.</param>
    protected virtual void Prepare(KernelProgram program) { }

    /// <summary>
    /// Writes one name the program declares.
    /// </summary>
    /// <param name="name">The name as the kernel declares it.</param>
    /// <returns>The name as the language takes it.</returns>
    protected virtual string Identifier(string name) => name;

    /// <summary>
    /// Gets a value indicating whether this language can emit a kernel that samples a texture.
    /// </summary>
    protected virtual bool SupportsTextures => false;

    /// <summary>
    /// Writes a sampled read of a texture.
    /// </summary>
    /// <param name="sample">The read.</param>
    /// <returns>The expression.</returns>
    protected virtual string TextureSample(KernelTextureSample sample)
        => throw new KernelTranslationException($"'{GetType().Name}' has no texture sampling.");

    /// <summary>
    /// Writes the functions of a program that are not its entry point.
    /// </summary>
    /// <param name="function">The function.</param>
    /// <param name="text">The source being written.</param>
    private void Function(KernelFunction function, StringBuilder text)
    {
        text.AppendLine($"{Type(function.ReturnType)} {Identifier(function.Name)}({Parameters(function)})");
        Block(function.Body, text, 0);
        text.AppendLine();
    }

    /// <summary>
    /// Writes the entry point of a program.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <param name="text">The source being written.</param>
    private void Entry(KernelProgram program, StringBuilder text)
    {
        foreach (var line in EntrySignature(program))
        {
            text.AppendLine(line);
        }

        text.AppendLine("{");

        // Group memory a kernel declares belongs inside the function, after its signature.
        if (SharedInEntry)
        {
            foreach (var array in program.EntryPoint.SharedArrays)
            {
                text.AppendLine(INDENT + Shared(array));
            }

            if (program.EntryPoint.SharedArrays.Count > 0)
            {
                text.AppendLine();
            }
        }

        var prologue = EntryPrologue(program);

        if (prologue.Length > 0)
        {
            text.AppendLine(INDENT + prologue);
        }

        foreach (var statement in program.EntryPoint.Body.Statements)
        {
            Statement(statement, text, 1);
        }

        text.AppendLine("}");
    }

    /// <summary>
    /// Writes a block of statements.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="text">The source being written.</param>
    /// <param name="depth">How many levels deep the block is nested.</param>
    private void Block(KernelBlock block, StringBuilder text, int depth)
    {
        var padding = Padding(depth);

        text.AppendLine(padding + "{");

        foreach (var statement in block.Statements)
        {
            Statement(statement, text, depth + 1);
        }

        text.AppendLine(padding + "}");
    }

    /// <summary>
    /// Writes one statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <param name="text">The source being written.</param>
    /// <param name="depth">How many levels deep the statement is nested.</param>
    private void Statement(KernelStatement statement, StringBuilder text, int depth)
    {
        var padding = Padding(depth);

        switch (statement)
        {
            case KernelBlock block:
                Block(block, text, depth);
                return;

            case KernelDeclaration declaration:
                text.AppendLine(padding + Inline(declaration) + ";");
                return;

            case KernelAssignment assignment:
                text.AppendLine(padding + Inline(assignment) + ";");
                return;

            case KernelExpressionStatement expression:
                text.AppendLine(padding + Value(expression.Expression) + ";");
                return;

            case KernelIf branch:
            {
                text.AppendLine(padding + "if (" + Value(branch.Condition) + ")");
                Block(branch.Then, text, depth);

                if (branch.Else is not null)
                {
                    text.AppendLine(padding + "else");
                    Block(branch.Else, text, depth);
                }

                return;
            }

            case KernelWhile loop:
                text.AppendLine(padding + "while (" + Value(loop.Condition) + ")");
                Block(loop.Body, text, depth);
                return;

            case KernelDoWhile loop:
                text.AppendLine(padding + "do");
                Block(loop.Body, text, depth);
                text.AppendLine(padding + "while (" + Value(loop.Condition) + ");");
                return;

            case KernelFor loop:
                text.AppendLine(padding + "for (" + Inline(loop) + ")");
                Block(loop.Body, text, depth);
                return;

            case KernelBreak:
                text.AppendLine(padding + "break;");
                return;

            case KernelContinue:
                text.AppendLine(padding + "continue;");
                return;

            case KernelReturn returned:
                text.AppendLine(padding + "return" + (returned.Value is null ? ";" : " " + Value(returned.Value) + ";"));
                return;

            case KernelBarrier:
                text.AppendLine(padding + Barrier());
                return;

            case KernelAtomicAdd atomic:
                text.AppendLine(padding + AtomicAdd(Value(atomic.Target), Value(atomic.Value)));
                return;

            default:
                throw new InvalidOperationException($"'{statement.GetType().Name}' is not a statement a kernel lowers to.");
        }
    }

    /// <summary>
    /// Writes a statement where the language takes one without a semicolon, which is the head of a <c>for</c> loop.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns>The statement.</returns>
    private string Inline(KernelStatement? statement)
    {
        switch (statement)
        {
            case null:
                return string.Empty;

            case KernelDeclaration declaration:
                return Inline(declaration);

            case KernelAssignment assignment:
                return Inline(assignment);

            case KernelExpressionStatement expression:
                return Value(expression.Expression);

            case KernelBlock block:
                return string.Join(", ", block.Statements.Select(Inline));

            default:
                throw new InvalidOperationException($"'{statement.GetType().Name}' is not a statement a loop head takes.");
        }
    }

    /// <summary>
    /// Writes a declaration.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <returns>The declaration, without its semicolon.</returns>
    private string Inline(KernelDeclaration declaration)
    {
        var text = declaration.Type is ArrayType array
            ? $"{Type(array.Element)} {Identifier(declaration.Name)}[{array.Length}]"
            : $"{Type(declaration.Type)} {Identifier(declaration.Name)}";

        return declaration.Initializer is null ? text : text + " = " + Value(declaration.Initializer);
    }

    /// <summary>
    /// Writes an assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <returns>The assignment, without its semicolon.</returns>
    private string Inline(KernelAssignment assignment) => $"{Value(assignment.Target)} = {Value(assignment.Value)}";

    /// <summary>
    /// Writes a <c>for</c> loop head.
    /// </summary>
    /// <param name="loop">The loop.</param>
    /// <returns>The three parts of the head, separated by semicolons.</returns>
    private string Inline(KernelFor loop)
    {
        var run = Inline(loop.Run);
        var condition = loop.Condition is null ? string.Empty : Value(loop.Condition);
        var next = Inline(loop.Next);

        return run + "; " + condition + "; " + next;
    }

    /// <summary>
    /// Writes the parameters of a function.
    /// </summary>
    /// <param name="function">The function.</param>
    /// <returns>The parameters, separated by commas.</returns>
    private string Parameters(KernelFunction function)
        => string.Join(", ", function.Parameters.Select(Parameter));

    /// <summary>
    /// Writes the padding of one level of nesting.
    /// </summary>
    /// <param name="depth">How many levels deep a statement is.</param>
    /// <returns>The indentation.</returns>
    private static string Padding(int depth)
    {
        var text = new StringBuilder();

        for (var level = 0; level < depth; level++)
        {
            text.Append(INDENT);
        }

        return text.ToString();
    }

    /// <summary>
    /// Writes one value, with the parentheses its precedence needs.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <param name="parent">The precedence of the expression that holds it.</param>
    /// <returns>The value.</returns>
    protected string Value(KernelExpression expression, int parent = 0)
    {
        var (text, precedence) = Write(expression);

        return precedence < parent ? "(" + text + ")" : text;
    }

    /// <summary>
    /// Writes one value and reads the precedence it binds at.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>The text of the value, and the precedence it binds at.</returns>
    private (string Text, int Precedence) Write(KernelExpression expression)
    {
        switch (expression)
        {
            case KernelLiteral literal:
                return (Literal(literal), Precedence.Primary);

            case KernelLocal local:
                return (Identifier(local.Name), Precedence.Primary);

            case KernelBufferElement element:
                return (Identifier(element.Buffer) + "[" + Value(element.Index) + "]", Precedence.Primary);

            case KernelArrayElement element:
                return (Identifier(element.Array) + "[" + Value(element.Index) + "]", Precedence.Primary);

            case KernelSharedElement element:
                return (Identifier(element.Array) + "[" + Value(element.Index) + "]", Precedence.Primary);

            case KernelFieldAccess field:
                return (Value(field.Target, Precedence.Primary) + "." + Identifier(field.Field), Precedence.Primary);

            case KernelThreadIndex:
                return (ThreadIndex(), Precedence.Primary);

            case KernelLengthsElement lengths:
                return (LengthsElement(lengths.Index), Precedence.Primary);

            case KernelCall call:
                return (Identifier(call.Function.Name) + "(" + string.Join(", ", call.Arguments.Select(argument => Value(argument))) + ")", Precedence.Primary);

            case KernelIntrinsicCall intrinsic:
                return (Intrinsic(intrinsic.Intrinsic, intrinsic.Arguments.Select(argument => Value(argument)).ToList()), Precedence.Primary);

            case KernelTextureSample sample:
                return (TextureSample(sample), Precedence.Primary);

            case KernelConvert convert:
                // A conversion is written as the constructor of the type, which is the form every one of these languages
                // has: a C style cast is not GLSL.
                return (Type(convert.Target) + "(" + Value(convert.Operand) + ")", Precedence.Primary);

            case KernelUnary unary:
                return (Unary(unary), Precedence.Unary);

            case KernelBinary binary:
                return Binary(binary);

            case KernelConditional conditional:
                return (Value(conditional.Condition, Precedence.Conditional + 1) + " ? " +
                        Value(conditional.WhenTrue, Precedence.Conditional + 1) + " : " +
                        Value(conditional.WhenFalse, Precedence.Conditional), Precedence.Conditional);

            default:
                throw new InvalidOperationException($"'{expression.GetType().Name}' is not a value a kernel lowers to.");
        }
    }

    /// <summary>
    /// Writes a binary expression.
    /// </summary>
    /// <param name="binary">The expression.</param>
    /// <returns>The text of the value, and the precedence it binds at.</returns>
    private (string Text, int Precedence) Binary(KernelBinary binary)
    {
        var text = Operator(binary.Operator);
        var precedence = Precedence.Of(binary.Operator);

        return (Value(binary.Left, precedence) + " " + text + " " + Value(binary.Right, precedence + 1), precedence);
    }

    /// <summary>
    /// Writes a unary expression.
    /// </summary>
    /// <param name="unary">The expression.</param>
    /// <returns>The text.</returns>
    private string Unary(KernelUnary unary) => unary.Operator switch
    {
        KernelUnaryOperator.Negate => "-" + Value(unary.Operand, Precedence.Unary),
        KernelUnaryOperator.Complement => "~" + Value(unary.Operand, Precedence.Unary),
        _ => "!" + Value(unary.Operand, Precedence.Unary)
    };

    /// <summary>
    /// Writes a literal.
    /// </summary>
    /// <param name="literal">The literal.</param>
    /// <returns>The text.</returns>
    private static string Literal(KernelLiteral literal) => literal.Value switch
    {
        bool value => value ? "true" : "false",
        uint value => value.ToString(CultureInfo.InvariantCulture) + "u",
        int value => value.ToString(CultureInfo.InvariantCulture),
        float value => Float(value),
        _ => throw new InvalidOperationException($"'{literal.Value.GetType().Name}' is not a literal a kernel lowers to.")
    };

    /// <summary>
    /// Writes a single precision number so that the language reads it as one.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The literal.</returns>
    private static string Float(float value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);

        if (text.IndexOf('.') < 0 && text.IndexOf('E') < 0 && text.IndexOf('e') < 0)
        {
            text += ".0";
        }

        // The suffix is what every one of these languages reads as single precision, which is the only kind a kernel has.
        return text + "f";
    }

    /// <summary>
    /// Reads the token a binary operation is written with.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns>The token.</returns>
    private static string Operator(KernelBinaryOperator operation) => operation switch
    {
        KernelBinaryOperator.Add => "+",
        KernelBinaryOperator.Subtract => "-",
        KernelBinaryOperator.Multiply => "*",
        KernelBinaryOperator.Divide => "/",
        KernelBinaryOperator.Remainder => "%",
        KernelBinaryOperator.And => "&",
        KernelBinaryOperator.Or => "|",
        KernelBinaryOperator.Xor => "^",
        KernelBinaryOperator.ShiftLeft => "<<",
        KernelBinaryOperator.ShiftRight => ">>",
        KernelBinaryOperator.Equal => "==",
        KernelBinaryOperator.NotEqual => "!=",
        KernelBinaryOperator.Less => "<",
        KernelBinaryOperator.LessOrEqual => "<=",
        KernelBinaryOperator.Greater => ">",
        KernelBinaryOperator.GreaterOrEqual => ">=",
        KernelBinaryOperator.LogicalAnd => "&&",
        _ => "||"
    };

    /// <summary>
    /// Gets the line that opens the source of the backend, which may be nothing.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The header, without a trailing newline.</returns>
    protected virtual string Header(KernelProgram program) => string.Empty;

    /// <summary>
    /// Writes the name a type is declared with.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The name.</returns>
    protected abstract string Type(KernelType type);

    /// <summary>
    /// Writes the declarations that bind the buffers of the kernel and the block that carries their counts.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <param name="text">The source being written.</param>
    /// <remarks>
    /// A language that binds a buffer by an attribute on a function argument writes nothing here: its entry point carries
    /// the bindings in its signature.
    /// </remarks>
    protected virtual void Bindings(KernelProgram program, StringBuilder text)
    {
        foreach (var buffer in program.Buffers)
        {
            text.AppendLine(Buffer(program, buffer));
        }

        if (program.Parameters is not null)
        {
            text.AppendLine(Buffer(program, program.Parameters));
        }

        text.AppendLine(Lengths(program));
        text.AppendLine();
    }

    /// <summary>
    /// Reads the binding the block that carries the element counts is bound at, which is the one after every buffer of the
    /// kernel and after the block of the structure it takes as one value, when it takes one.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The binding.</returns>
    protected static int LengthsBinding(KernelProgram program) => program.Buffers.Count + (program.Parameters is null ? 0 : 1);

    /// <summary>
    /// Writes the declaration of one buffer of the kernel.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <param name="buffer">The buffer.</param>
    /// <returns>The declaration.</returns>
    protected abstract string Buffer(KernelProgram program, KernelBuffer buffer);

    /// <summary>
    /// Writes the declaration of the block that carries the element counts.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The declaration.</returns>
    protected abstract string Lengths(KernelProgram program);

    /// <summary>
    /// Writes the lines that open the entry point.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The lines, without the brace.</returns>
    protected abstract IReadOnlyList<string> EntrySignature(KernelProgram program);

    /// <summary>
    /// Writes the first line of the entry point's body, where the backend needs one.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The line, or an empty string.</returns>
    protected virtual string EntryPrologue(KernelProgram program) => string.Empty;

    /// <summary>
    /// Writes the declaration of one array the group shares.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <returns>The declaration.</returns>
    protected abstract string Shared(KernelSharedArray array);

    /// <summary>
    /// Gets a value indicating whether the arrays a group shares are declared inside the entry point, which a language
    /// with no module scope for them does.
    /// </summary>
    protected virtual bool SharedInEntry => false;

    /// <summary>
    /// Writes one parameter of a function.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>The parameter.</returns>
    protected abstract string Parameter(KernelFunctionParameter parameter);

    /// <summary>
    /// Writes one of the calls the device has as an operation.
    /// </summary>
    /// <param name="intrinsic">The operation.</param>
    /// <param name="arguments">The values it takes, written.</param>
    /// <returns>The call.</returns>
    protected abstract string Intrinsic(KernelIntrinsic intrinsic, IReadOnlyList<string> arguments);

    /// <summary>
    /// Writes the call that makes the threads of a group wait for each other.
    /// </summary>
    /// <returns>The call.</returns>
    protected abstract string Barrier();

    /// <summary>
    /// Writes the addition of a value to an element several threads write at once.
    /// </summary>
    /// <param name="target">The element, written.</param>
    /// <param name="value">The value, written.</param>
    /// <returns>The statement.</returns>
    protected abstract string AtomicAdd(string target, string value);

    /// <summary>
    /// Writes the value the entry point reads the thread position from.
    /// </summary>
    /// <returns>The value, as an integer.</returns>
    protected abstract string ThreadIndex();

    /// <summary>
    /// Writes the value of one element of the block that carries the element counts.
    /// </summary>
    /// <param name="index">The buffer the count belongs to.</param>
    /// <returns>The value, as an unsigned integer.</returns>
    protected abstract string LengthsElement(int index);

    /// <summary>
    /// Reports whether a program makes a call over the lanes of a wave, which is what needs the extension of a backend.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    protected static bool Wave(KernelProgram program)
    {
        foreach (var function in program.Functions)
        {
            foreach (var statement in function.Body.Statements)
            {
                if (StatementUsesWave(statement))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Reports whether a statement makes a call over the lanes of a wave.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool StatementUsesWave(KernelStatement statement) => statement switch
    {
        KernelBlock block => block.Statements.Any(StatementUsesWave),
        KernelDeclaration declaration => declaration.Initializer is not null && ValueUsesWave(declaration.Initializer),
        KernelAssignment assignment => ValueUsesWave(assignment.Target) || ValueUsesWave(assignment.Value),
        KernelExpressionStatement expression => ValueUsesWave(expression.Expression),
        KernelIf branch => ValueUsesWave(branch.Condition) || StatementUsesWave(branch.Then) || (branch.Else is not null && StatementUsesWave(branch.Else)),
        KernelWhile loop => ValueUsesWave(loop.Condition) || StatementUsesWave(loop.Body),
        KernelDoWhile loop => ValueUsesWave(loop.Condition) || StatementUsesWave(loop.Body),
        KernelFor loop => StatementUsesWave(loop.Body) ||
                          (loop.Run is not null && StatementUsesWave(loop.Run)) ||
                          (loop.Next is not null && StatementUsesWave(loop.Next)) ||
                          (loop.Condition is not null && ValueUsesWave(loop.Condition)),
        KernelReturn returned => returned.Value is not null && ValueUsesWave(returned.Value),
        KernelAtomicAdd atomic => ValueUsesWave(atomic.Target) || ValueUsesWave(atomic.Value),
        _ => false
    };

    /// <summary>
    /// Reports whether a value makes a call over the lanes of a wave.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool ValueUsesWave(KernelExpression expression) => expression switch
    {
        KernelIntrinsicCall call => call.Intrinsic >= KernelIntrinsic.WaveSum || call.Arguments.Any(ValueUsesWave),
        KernelBinary binary => ValueUsesWave(binary.Left) || ValueUsesWave(binary.Right),
        KernelUnary unary => ValueUsesWave(unary.Operand),
        KernelConvert convert => ValueUsesWave(convert.Operand),
        KernelConditional conditional => ValueUsesWave(conditional.Condition) || ValueUsesWave(conditional.WhenTrue) || ValueUsesWave(conditional.WhenFalse),
        KernelCall call => call.Arguments.Any(ValueUsesWave),
        KernelBufferElement element => ValueUsesWave(element.Index),
        KernelArrayElement element => ValueUsesWave(element.Index),
        KernelSharedElement element => ValueUsesWave(element.Index),
        KernelFieldAccess field => ValueUsesWave(field.Target),
        _ => false
    };

    /// <summary>
    /// The precedence an expression binds at, which is what decides where a parenthesis is needed.
    /// </summary>
    protected static class Precedence
    {
        /// <summary>
        /// The lowest: a conditional.
        /// </summary>
        public const int Conditional = 1;

        /// <summary>
        /// A logical or.
        /// </summary>
        public const int Or = 2;

        /// <summary>
        /// A logical and.
        /// </summary>
        public const int And = 3;

        /// <summary>
        /// A bitwise or.
        /// </summary>
        public const int BitwiseOr = 4;

        /// <summary>
        /// A bitwise exclusive or.
        /// </summary>
        public const int BitwiseXor = 5;

        /// <summary>
        /// A bitwise and.
        /// </summary>
        public const int BitwiseAnd = 6;

        /// <summary>
        /// An equality.
        /// </summary>
        public const int Equality = 7;

        /// <summary>
        /// A relational comparison.
        /// </summary>
        public const int Relational = 8;

        /// <summary>
        /// A shift.
        /// </summary>
        public const int Shift = 9;

        /// <summary>
        /// An addition.
        /// </summary>
        public const int Additive = 10;

        /// <summary>
        /// A multiplication.
        /// </summary>
        public const int Multiplicative = 11;

        /// <summary>
        /// A unary operation.
        /// </summary>
        public const int Unary = 12;

        /// <summary>
        /// A name, a literal, an element or a call.
        /// </summary>
        public const int Primary = 13;

        /// <summary>
        /// Reads the precedence a binary operation binds at.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <returns>The precedence.</returns>
        public static int Of(KernelBinaryOperator operation) => operation switch
        {
            KernelBinaryOperator.LogicalOr => Or,
            KernelBinaryOperator.LogicalAnd => And,
            KernelBinaryOperator.Or => BitwiseOr,
            KernelBinaryOperator.Xor => BitwiseXor,
            KernelBinaryOperator.And => BitwiseAnd,
            KernelBinaryOperator.Equal or KernelBinaryOperator.NotEqual => Equality,
            KernelBinaryOperator.Less or KernelBinaryOperator.LessOrEqual or KernelBinaryOperator.Greater or KernelBinaryOperator.GreaterOrEqual => Relational,
            KernelBinaryOperator.ShiftLeft or KernelBinaryOperator.ShiftRight => Shift,
            KernelBinaryOperator.Add or KernelBinaryOperator.Subtract => Additive,
            _ => Multiplicative
        };
    }
}
