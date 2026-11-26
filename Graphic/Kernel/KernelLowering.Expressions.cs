using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// The half of the translator that reads expressions and the statements that are written as one.
/// </summary>
internal sealed partial class KernelLowering
{
    /// <summary>
    /// The calls a kernel makes and the operation each one becomes.
    /// </summary>
    private static readonly Dictionary<string, KernelIntrinsic> INTRINSICS = new(StringComparer.Ordinal)
    {
        ["Math.Abs"] = KernelIntrinsic.Abs,
        ["MathF.Abs"] = KernelIntrinsic.Abs,
        ["Math.Min"] = KernelIntrinsic.Min,
        ["MathF.Min"] = KernelIntrinsic.Min,
        ["Math.Max"] = KernelIntrinsic.Max,
        ["MathF.Max"] = KernelIntrinsic.Max,
        ["Math.Sqrt"] = KernelIntrinsic.Sqrt,
        ["MathF.Sqrt"] = KernelIntrinsic.Sqrt,
        ["Math.Floor"] = KernelIntrinsic.Floor,
        ["MathF.Floor"] = KernelIntrinsic.Floor,
        ["Math.Ceiling"] = KernelIntrinsic.Ceiling,
        ["MathF.Ceiling"] = KernelIntrinsic.Ceiling,
        ["Math.Pow"] = KernelIntrinsic.Pow,
        ["MathF.Pow"] = KernelIntrinsic.Pow,
        ["Math.Clamp"] = KernelIntrinsic.Clamp,
        ["MathF.Clamp"] = KernelIntrinsic.Clamp,
        ["Math.Sin"] = KernelIntrinsic.Sin,
        ["MathF.Sin"] = KernelIntrinsic.Sin,
        ["Math.Cos"] = KernelIntrinsic.Cos,
        ["MathF.Cos"] = KernelIntrinsic.Cos,
        ["GpuWave.Sum"] = KernelIntrinsic.WaveSum,
        ["GpuWave.Min"] = KernelIntrinsic.WaveMin,
        ["GpuWave.Max"] = KernelIntrinsic.WaveMax,
        ["GpuWave.Product"] = KernelIntrinsic.WaveProduct,
        ["GpuWave.PrefixSum"] = KernelIntrinsic.WavePrefixSum,
        ["GpuWave.Any"] = KernelIntrinsic.WaveAny,
        ["GpuWave.All"] = KernelIntrinsic.WaveAll,
    };

    /// <summary>
    /// Lowers one expression.
    /// </summary>
    /// <param name="node">The expression.</param>
    /// <param name="expected">The type it is read as, when the caller knows it.</param>
    /// <returns>The value of the device.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the expression is outside the supported subset.</exception>
    private KernelExpression Expression(ExpressionSyntax node, KernelType? expected = null)
    {
        switch (node)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return Expression(parenthesized.Expression, expected);

            case LiteralExpressionSyntax literal:
                return Literal(literal, expected);

            case IdentifierNameSyntax identifier:
                return Identifier(identifier);

            case MemberAccessExpressionSyntax access:
                return Member(access);

            case ElementAccessExpressionSyntax element:
                return ElementOf(element);

            case InvocationExpressionSyntax invocation:
                return Call(invocation);

            case ObjectCreationExpressionSyntax creation:
                return Create(creation);

            case BinaryExpressionSyntax binary:
                return Binary(binary);

            case PrefixUnaryExpressionSyntax prefix:
                return Prefix(prefix);

            case CastExpressionSyntax cast:
                return Coerce(Expression(cast.Expression), TypeOf(cast.Type));

            case ConditionalExpressionSyntax conditional:
            {
                var whenTrue = Expression(conditional.WhenTrue);
                var whenFalse = Expression(conditional.WhenFalse, whenTrue.ResultType);
                var type = Promote(whenTrue.ResultType, whenFalse.ResultType, conditional);

                return new KernelConditional(Expression(conditional.Condition), Coerce(whenTrue, type), Coerce(whenFalse, type), type);
            }

            case IsPatternExpressionSyntax pattern:
                return Pattern(pattern);

            case SwitchExpressionSyntax branch:
                return Switch(branch);

            case ThisExpressionSyntax when m_Self is not null:
                return new KernelLocal(SELF, m_Self);

            case CheckedExpressionSyntax checkedExpression:
                return Expression(checkedExpression.Expression, expected);

            default:
                throw new KernelTranslationException($"'{node}' has no representation on the device.");
        }
    }

    /// <summary>
    /// Lowers an expression the body evaluates for what it does: a write, an increment, a call or a barrier.
    /// </summary>
    /// <param name="node">The expression.</param>
    /// <param name="statements">The statements it adds to.</param>
    /// <exception cref="KernelTranslationException">Thrown when the expression is outside the supported subset.</exception>
    private void Lower(ExpressionSyntax node, List<KernelStatement> statements)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax assignment:
                statements.Add(Assign(assignment));
                return;

            case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression):
                statements.Add(Step(prefix.Operand, prefix.IsKind(SyntaxKind.PreIncrementExpression)));
                return;

            case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression):
                statements.Add(Step(postfix.Operand, postfix.IsKind(SyntaxKind.PostIncrementExpression)));
                return;

            case InvocationExpressionSyntax invocation:
                Invoke(invocation, statements);
                return;

            default:
            {
                var value = Expression(node);

                if (!KernelType.Same(value.ResultType, VoidType.Void))
                {
                    throw new KernelTranslationException($"'{node}' computes a value nothing reads, which the device does not run.");
                }

                statements.Add(new KernelExpressionStatement(value));
                return;
            }
        }
    }

    /// <summary>
    /// Lowers a call the body makes as a statement, which is where the calls that change something live.
    /// </summary>
    /// <param name="invocation">The call.</param>
    /// <param name="statements">The statements it adds to.</param>
    /// <exception cref="KernelTranslationException">Thrown when the call is outside the supported subset.</exception>
    private void Invoke(InvocationExpressionSyntax invocation, List<KernelStatement> statements)
    {
        var name = CallName(invocation.Expression);
        var qualified = Qualified(invocation.Expression);

        if (qualified.StartsWith("System.", StringComparison.Ordinal))
        {
            qualified = qualified.Substring("System.".Length);
        }

        if (name == KernelTranspiler.BARRIER)
        {
            if (invocation.ArgumentList.Arguments.Count != 0)
            {
                throw new KernelTranslationException("The barrier of a group takes no arguments.");
            }

            statements.Add(KernelBarrier.Value);
            return;
        }

        if (qualified == KernelTranspiler.INTERLOCKED)
        {
            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count != 2)
            {
                throw new KernelTranslationException($"'{KernelTranspiler.INTERLOCKED}' takes the element it adds to and the value it adds.");
            }

            var atomic = Target(arguments[0].Expression);
            statements.Add(new KernelAtomicAdd(atomic, Coerce(Expression(arguments[1].Expression), atomic.ResultType)));

            if (invocation.Parent is not ExpressionStatementSyntax)
            {
                throw new KernelTranslationException($"'{KernelTranspiler.INTERLOCKED}' is called as a statement: the value an atomic add returns is not part of the subset.");
            }

            return;
        }

        if (IsShared(invocation, out _))
        {
            throw new KernelTranslationException($"'{KernelTranspiler.SHARED}<T>(count)' declares an array and is not an expression.");
        }

        // A call that writes variables the caller declares in place of an argument declares them before the call, with the
        // type the call takes there.
        var declared = new List<KernelFunctionParameter>();

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is not DeclarationExpressionSyntax declaration || declaration.Designation is not SingleVariableDesignationSyntax designated)
            {
                continue;
            }

            if (declared.Count == 0 && TrySignature(invocation, out var signature))
            {
                declared.AddRange(signature);
            }

            var position = invocation.ArgumentList.Arguments.IndexOf(argument);
            var parameter = position < declared.Count ? declared[position] : null;
            var type = declaration.Type is null || KernelTranspiler.TypeName(declaration.Type) == "var"
                ? parameter?.Type
                : TypeOf(declaration.Type);

            if (type is null)
            {
                throw new KernelTranslationException($"'{invocation}' writes a variable it does not declare the type of, and the call is not one the kernel declares.");
            }

            m_Locals[designated.Identifier.ValueText] = type;
            statements.Add(new KernelDeclaration(designated.Identifier.ValueText, type, null));
        }

        var call = Expression(invocation);
        statements.Add(new KernelExpressionStatement(call));
    }

    /// <summary>
    /// Resolves a call to the helper it reaches, which may be declared in any class of the compilation.
    /// </summary>
    /// <param name="invocation">The call.</param>
    /// <param name="helper">When the method returns, the helper it reaches.</param>
    /// <returns><see langword="true"/> when the call is a helper of the program.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the name is one several helpers share and the call does not say which.</exception>
    private bool TryHelper(InvocationExpressionSyntax invocation, out HelperInfo helper)
    {
        helper = null!;

        if (m_Semantics is not null)
        {
            // The compiler has already decided which method the call reaches, including between two classes that declare
            // a method of the same name.
            if (m_Semantics.ModelFor(invocation.SyntaxTree).GetSymbolInfo(invocation).Symbol is not IMethodSymbol symbol)
            {
                return false;
            }

            return m_HelpersBySymbol.TryGetValue(symbol, out helper!);
        }

        var name = CallName(invocation.Expression);
        var owner = OwnerName(invocation.Expression);

        if (owner is not null)
        {
            if (m_HelpersByOwner.TryGetValue(owner + "." + name, out var qualified))
            {
                if (qualified.Count == 1)
                {
                    helper = qualified[0];
                    return true;
                }

                throw new KernelTranslationException($"'{invocation}' calls one of {qualified.Count} helpers named '{owner}.{name}': a helper of a kernel is a function with one body.");
            }

            // A name that is a structure of the kernel reaches the structure before any helper, and anything else that is
            // qualified with a name the program does not declare is not a helper either.
            return false;
        }

        if (!m_HelpersByName.TryGetValue(name, out var candidates))
        {
            return false;
        }

        if (candidates.Count == 1)
        {
            helper = candidates[0];
            return true;
        }

        var own = candidates.Where(candidate => candidate.Owner == m_KernelOwner).ToList();

        if (own.Count == 1)
        {
            helper = own[0];
            return true;
        }

        throw new KernelTranslationException($"'{invocation}' calls one of {candidates.Count} helpers named '{name}': name the class that declares the one it means.");
    }

    /// <summary>
    /// Reads the name of the type a call is qualified with, when it is qualified with one.
    /// </summary>
    /// <param name="expression">The expression the call is made through.</param>
    /// <returns>The name of the type, or <see langword="null"/> when the call is not qualified.</returns>
    private static string? OwnerName(ExpressionSyntax expression)
        => expression is MemberAccessExpressionSyntax access && access.Expression is IdentifierNameSyntax owner
            ? owner.Identifier.ValueText
            : null;

    /// <summary>
    /// Reads the signature of a call a kernel or a helper declares, so that a variable declared in place of an argument has
    /// a type where the compiler was not asked.
    /// </summary>
    /// <param name="invocation">The call.</param>
    /// <param name="parameters">When the method returns, the parameters of the call.</param>
    /// <returns><see langword="true"/> when the call is one of the program's.</returns>
    private bool TrySignature(InvocationExpressionSyntax invocation, out IReadOnlyList<KernelFunctionParameter> parameters)
    {
        parameters = System.Array.Empty<KernelFunctionParameter>();

        if (TryHelper(invocation, out var helper))
        {
            parameters = helper.Syntax.ParameterList.Parameters.Select(parameter => Parameter(parameter)).ToList();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Lowers an assignment, with the operation a compound assignment performs.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the operator is not one the device has.</exception>
    private KernelStatement Assign(AssignmentExpressionSyntax assignment)
    {
        var target = Target(assignment.Left);
        var value = Expression(assignment.Right, target.ResultType);

        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            return new KernelAssignment(target, Coerce(value, target.ResultType));
        }

        var operation = assignment.Kind() switch
        {
            SyntaxKind.AddAssignmentExpression => KernelBinaryOperator.Add,
            SyntaxKind.SubtractAssignmentExpression => KernelBinaryOperator.Subtract,
            SyntaxKind.MultiplyAssignmentExpression => KernelBinaryOperator.Multiply,
            SyntaxKind.DivideAssignmentExpression => KernelBinaryOperator.Divide,
            SyntaxKind.ModuloAssignmentExpression => KernelBinaryOperator.Remainder,
            SyntaxKind.AndAssignmentExpression => KernelBinaryOperator.And,
            SyntaxKind.OrAssignmentExpression => KernelBinaryOperator.Or,
            SyntaxKind.ExclusiveOrAssignmentExpression => KernelBinaryOperator.Xor,
            SyntaxKind.LeftShiftAssignmentExpression => KernelBinaryOperator.ShiftLeft,
            SyntaxKind.RightShiftAssignmentExpression => KernelBinaryOperator.ShiftRight,
            _ => throw new KernelTranslationException($"'{assignment.Kind()}' has no representation on the device.")
        };

        return new KernelAssignment(target, new KernelBinary(operation, target, Coerce(value, target.ResultType), target.ResultType));
    }

    /// <summary>
    /// Lowers an increment or a decrement to the addition or subtraction it is.
    /// </summary>
    /// <param name="operand">The value stepped.</param>
    /// <param name="up">Whether it is stepped up.</param>
    /// <returns>The statement.</returns>
    private KernelStatement Step(ExpressionSyntax operand, bool up)
    {
        var target = Target(operand);
        var one = Coerce(new KernelLiteral(1, ScalarType.Int), target.ResultType);
        var operation = up ? KernelBinaryOperator.Add : KernelBinaryOperator.Subtract;

        return new KernelAssignment(target, new KernelBinary(operation, target, one, target.ResultType));
    }

    /// <summary>
    /// Lowers the value an assignment writes.
    /// </summary>
    /// <param name="target">The expression written.</param>
    /// <returns>The place written.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the expression is not a place.</exception>
    private KernelExpression Target(ExpressionSyntax target) => Expression(target) switch
    {
        KernelLocal or KernelBufferElement or KernelArrayElement or KernelSharedElement or KernelFieldAccess => Expression(target),
        _ => throw new KernelTranslationException($"'{target}' is not a value the device writes: a local, an element of a buffer or a field of a value.")
    };

    /// <summary>
    /// Lowers a name.
    /// </summary>
    /// <param name="identifier">The name.</param>
    /// <returns>The value it stands for.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the name is not one the device reads.</exception>
    private KernelExpression Identifier(IdentifierNameSyntax identifier)
    {
        var name = identifier.Identifier.ValueText;

        if (name == m_IndexParameter)
        {
            return KernelThreadIndex.Value;
        }

        if (m_Locals.TryGetValue(name, out var local))
        {
            return new KernelLocal(name, local);
        }

        if (m_Self is not null && m_Structures.TryGetValue(m_Self.Name, out var self))
        {
            if (self.Fields.FirstOrDefault(field => field.Name == name) is { } own)
            {
                return new KernelFieldAccess(new KernelLocal(SELF, m_Self), name, own.Type);
            }

            var declaration = DeclarationOf(m_Self);

            foreach (var property in declaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (property.Identifier.ValueText == name)
                {
                    return new KernelCall(Member(m_Self, property, declaration, true, false), new[] { new KernelLocal(SELF, m_Self) });
                }
            }
        }

        if (m_Buffers.Any(buffer => buffer.Name == name))
        {
            throw new KernelTranslationException($"'{name}' is a buffer: the device reads an element of it or its Length.");
        }

        if (TryConstant(identifier, out var constant))
        {
            return constant;
        }

        throw new KernelTranslationException($"'{name}' is not a local, a parameter or a constant of the kernel, so the device has nothing to read.");
    }

    /// <summary>
    /// Lowers a member access: a field of a value, the length of a buffer, or the name of a member a call reaches.
    /// </summary>
    /// <param name="access">The access.</param>
    /// <returns>The value it stands for.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the member has no representation on the device.</exception>
    private KernelExpression Member(MemberAccessExpressionSyntax access)
    {
        var name = access.Name.Identifier.ValueText;

        if (name == "Length")
        {
            return Length(access);
        }

        if (TryConstant(access, out var constant))
        {
            return constant;
        }

        var target = Expression(access.Expression);

        if (target.ResultType is not StructType structure)
        {
            throw new KernelTranslationException($"'{access}' reads '{name}' of a value that is not a structure of the kernel.");
        }

        if (!m_Structures.TryGetValue(structure.Name, out var declared))
        {
            throw new KernelTranslationException($"'{access}' reads '{name}' of a value that has no declaration on the device.");
        }

        if (declared.Fields.FirstOrDefault(field => field.Name == name) is { } field)
        {
            return new KernelFieldAccess(target, name, field.Type);
        }

        // A property is the function it reads, called on the value it was read from.
        var declaration = DeclarationOf(structure);

        foreach (var property in declaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.Identifier.ValueText == name)
            {
                return new KernelCall(Member(structure, property, declaration, true, false), new[] { target });
            }
        }

        throw new KernelTranslationException($"'{name}' is not a field or a property of '{structure.Name}'.");
    }

    /// <summary>
    /// Lowers the length of a buffer, a slice of one, an array of the group or a local array.
    /// </summary>
    /// <param name="access">The access.</param>
    /// <returns>The value it stands for.</returns>
    /// <exception cref="KernelTranslationException">Thrown when nothing of that name is measured.</exception>
    private KernelExpression Length(MemberAccessExpressionSyntax access)
    {
        if (access.Expression is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.ValueText;
            var shared = m_Shared.FirstOrDefault(array => array.Name == name);

            if (shared is not null)
            {
                return new KernelLiteral(shared.Length, ScalarType.Int);
            }

            if (m_Arrays.TryGetValue(name, out var array))
            {
                return new KernelLiteral(array.Length, ScalarType.Int);
            }
        }

        if (TryResolve(access.Expression, out var view))
        {
            return view.Count;
        }

        throw new KernelTranslationException($"'{access}' measures something that is not a buffer or an array of the kernel.");
    }

    /// <summary>
    /// Lowers one element of a buffer, a slice of one, an array of the group or a local array.
    /// </summary>
    /// <param name="element">The access.</param>
    /// <returns>The element.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the collection is not one the device reads.</exception>
    private KernelExpression ElementOf(ElementAccessExpressionSyntax element)
    {
        if (element.ArgumentList.Arguments.Count != 1)
        {
            throw new KernelTranslationException($"'{element}' reads more than one index, which the device does not have.");
        }

        var argument = element.ArgumentList.Arguments[0].Expression;

        if (argument is RangeExpressionSyntax)
        {
            throw new KernelTranslationException("A range is read as an element or as a length: this kernel uses one where the device needs a value.");
        }

        if (TryIndexFromTheEnd(element, argument, out var fromEnd))
        {
            return fromEnd;
        }

        if (element.Expression is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.ValueText;
            var shared = m_Shared.FirstOrDefault(array => array.Name == name);

            if (shared is not null)
            {
                return new KernelSharedElement(name, Expression(argument), shared.Element);
            }

            if (m_Arrays.TryGetValue(name, out var array))
            {
                return new KernelArrayElement(name, Expression(argument), array.Element);
            }
        }

        if (TryResolve(element.Expression, out var view))
        {
            return Element(view, Expression(argument));
        }

        throw new KernelTranslationException($"'{element}' reads an element of something that is not a buffer or an array of the kernel.");
    }

    /// <summary>
    /// Lowers an index from the end of a buffer, an array of the group or a local array.
    /// </summary>
    /// <param name="element">The access.</param>
    /// <param name="argument">The index.</param>
    /// <param name="lowered">When the method returns, the element.</param>
    /// <returns><see langword="true"/> when the index is measured from the end.</returns>
    private bool TryIndexFromTheEnd(ElementAccessExpressionSyntax element, ExpressionSyntax argument, out KernelExpression lowered)
    {
        lowered = null!;

        if (argument is not PrefixUnaryExpressionSyntax behind || !behind.IsKind(SyntaxKind.IndexExpression))
        {
            return false;
        }

        var distance = Expression(behind.Operand);
        var length = TryResolve(element.Expression, out var view)
            ? view.Count
            : element.Expression is IdentifierNameSyntax identifier && m_Arrays.TryGetValue(identifier.Identifier.ValueText, out var array)
                ? new KernelLiteral(array.Length, ScalarType.Int)
                : throw new KernelTranslationException($"'{element}' counts from the end of something that has no length on the device.");

        var index = new KernelBinary(KernelBinaryOperator.Subtract, length, distance, ScalarType.Int);

        lowered = element.Expression is IdentifierNameSyntax name && m_Arrays.TryGetValue(name.Identifier.ValueText, out var local)
            ? new KernelArrayElement(local.Name, index, local.Element)
            : Element(view!, index);

        return true;
    }

    /// <summary>
    /// Lowers a call: one of the intrinsics the table maps, a helper of the kernel, or a member of a structure.
    /// </summary>
    /// <param name="invocation">The call.</param>
    /// <returns>The value the call computes.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the call is outside the supported subset.</exception>
    private KernelExpression Call(InvocationExpressionSyntax invocation)
    {
        var name = CallName(invocation.Expression);
        var qualified = Qualified(invocation.Expression);

        if (invocation.Expression is MemberAccessExpressionSyntax sample &&
            sample.Expression is IdentifierNameSyntax sampled &&
            sample.Name.Identifier.ValueText == "Sample" &&
            invocation.ArgumentList.Arguments.Count == 2)
        {
            var texture = m_Textures.FirstOrDefault(candidate => candidate.Name == sampled.Identifier.ValueText);

            if (texture is not null)
            {
                if (invocation.ArgumentList.Arguments[0].Expression is not IdentifierNameSyntax samplerName)
                {
                    throw new KernelTranslationException("A texture is sampled through a sampler state the kernel declares.");
                }

                var sampler = m_Samplers.FirstOrDefault(candidate => candidate.Name == samplerName.Identifier.ValueText)
                              ?? throw new KernelTranslationException($"'{samplerName.Identifier.ValueText}' is not a sampler state of the kernel.");

                return new KernelTextureSample(
                    texture,
                    sampler,
                    Expression(invocation.ArgumentList.Arguments[1].Expression, new VectorType(KernelScalar.Float, 2)));
            }
        }

        if (qualified.StartsWith("System.", StringComparison.Ordinal))
        {
            qualified = qualified.Substring("System.".Length);
        }

        if (INTRINSICS.TryGetValue(qualified, out var intrinsic))
        {
            var arguments = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)).ToList();
            var type = IntrinsicType(intrinsic, arguments);

            return new KernelIntrinsicCall(intrinsic, Coerce(arguments, intrinsic), type);
        }

        // A call made through a value is a method of the structure that value is, and a call made through a name is a
        // static one of the structure the name declares.
        if (invocation.Expression is MemberAccessExpressionSyntax access)
        {
            if (TryHelper(invocation, out var helper))
            {
                return Call(Helper(helper), invocation, null);
            }

            if (TryStruct(access.Expression, out var receiver))
            {
                return Call(Member(receiver, MemberOf(receiver, name), DeclarationOf(receiver), true, Writes(receiver, name)), invocation, null);
            }

            var value = Expression(access.Expression);

            if (value.ResultType is StructType structure)
            {
                var member = MemberOf(structure, name);

                return Call(Member(structure, member, DeclarationOf(structure), true, Mutates(member)), invocation, value);
            }
        }

        if (invocation.Expression is IdentifierNameSyntax && m_Self is not null)
        {
            var member = MemberOf(m_Self, name);

            return Call(Member(m_Self, member, DeclarationOf(m_Self), true, Mutates(member)), invocation, new KernelLocal(SELF, m_Self));
        }

        if (TryHelper(invocation, out var plain))
        {
            return Call(Helper(plain), invocation, null);
        }

        throw new KernelTranslationException($"A kernel body supports operators, indexing, control flow and the maths calls the subset maps; the call to '{invocation.Expression}' is not supported.");
    }

    /// <summary>
    /// Lowers the arguments of a call and passes the ones the function writes back to as the places they are.
    /// </summary>
    /// <param name="function">The function the call reaches.</param>
    /// <param name="invocation">The call.</param>
    /// <param name="receiver">The value a method of a structure is called on, when it is one.</param>
    /// <returns>The call.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the arguments do not fit the function.</exception>
    private KernelExpression Call(KernelFunction function, InvocationExpressionSyntax invocation, KernelExpression? receiver)
    {
        var arguments = new List<KernelExpression>();
        var offset = receiver is null ? 0 : 1;

        if (receiver is not null)
        {
            arguments.Add(receiver);
        }

        var given = invocation.ArgumentList.Arguments;

        if (given.Count != function.Parameters.Count - offset)
        {
            throw new KernelTranslationException($"'{invocation}' passes {given.Count} arguments and the call takes {function.Parameters.Count - offset}.");
        }

        for (var index = 0; index < given.Count; index++)
        {
            var parameter = function.Parameters[index + offset];
            var argument = given[index].Expression;

            if (parameter.Mode == KernelParameterMode.In)
            {
                arguments.Add(Coerce(Expression(argument), parameter.Type));
                continue;
            }

            if (argument is DeclarationExpressionSyntax declaration && declaration.Designation is SingleVariableDesignationSyntax designated)
            {
                arguments.Add(new KernelLocal(designated.Identifier.ValueText, parameter.Type));
                continue;
            }

            if (argument is PrefixUnaryExpressionSyntax keyword && keyword.IsKind(SyntaxKind.RefExpression))
            {
                throw new KernelTranslationException($"'{argument}' passes a value by reference, which the device only passes by name.");
            }

            arguments.Add(Target(argument));
        }

        return new KernelCall(function, arguments);
    }

    /// <summary>
    /// Lowers the call of one of the intrinsics a kernel makes directly, which the compiler resolves by symbol rather than
    /// by name.
    /// </summary>
    /// <param name="symbol">The method the call reaches.</param>
    /// <param name="invocation">The call.</param>
    /// <returns>The value the call computes, or <see langword="null"/> when the call is not one of the intrinsics.</returns>
    private KernelExpression? Intrinsic(Microsoft.CodeAnalysis.IMethodSymbol symbol, InvocationExpressionSyntax invocation)
    {
        var owner = symbol.ContainingType?.Name;

        if (owner is null || !INTRINSICS.TryGetValue(owner + "." + symbol.Name, out var intrinsic))
        {
            return null;
        }

        var arguments = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)).ToList();

        return new KernelIntrinsicCall(intrinsic, Coerce(arguments, intrinsic), IntrinsicType(intrinsic, arguments));
    }

    /// <summary>
    /// Reads the type an intrinsic call computes.
    /// </summary>
    /// <param name="intrinsic">The operation.</param>
    /// <param name="arguments">The values it takes.</param>
    /// <returns>The type of the result.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the operation has no result of a type the device has.</exception>
    private static KernelType IntrinsicType(KernelIntrinsic intrinsic, IReadOnlyList<KernelExpression> arguments)
    {
        switch (intrinsic)
        {
            case KernelIntrinsic.WaveAny or KernelIntrinsic.WaveAll:
                return ScalarType.Bool;

            default:
                return arguments.Count == 0
                    ? throw new KernelTranslationException("An intrinsic call takes a value.")
                    : arguments[0].ResultType;
        }
    }

    /// <summary>
    /// Reads the values a call passes to an intrinsic, converted to the type it computes with.
    /// </summary>
    /// <param name="arguments">The values passed.</param>
    /// <param name="intrinsic">The operation.</param>
    /// <returns>The values.</returns>
    private List<KernelExpression> Coerce(List<KernelExpression> arguments, KernelIntrinsic intrinsic)
    {
        if (arguments.Count == 0)
        {
            return arguments;
        }

        var type = arguments[0].ResultType;

        for (var index = 0; index < arguments.Count; index++)
        {
            arguments[index] = Coerce(arguments[index], type);
        }

        return arguments;
    }

    /// <summary>
    /// Lowers a value the kernel builds from one of its structures.
    /// </summary>
    /// <param name="creation">The construction.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the value is not one the device builds.</exception>
    private KernelExpression Create(ObjectCreationExpressionSyntax creation)
    {
        if (creation.Initializer is not null)
        {
            throw new KernelTranslationException($"'{creation}' starts a value with field values, which the device only does through a constructor.");
        }

        if (creation.ArgumentList is null || creation.ArgumentList.Arguments.Count == 0)
        {
            throw new KernelTranslationException($"A value of '{creation.Type}' with no arguments is declared as a local, not built inside an expression.");
        }

        if (TypeOf(creation.Type) is not StructType structure)
        {
            throw new KernelTranslationException($"'{creation.Type}' is not a structure the kernel declares, so a value of it cannot be built on the device.");
        }

        var constructor = MemberOf(structure, "ctor");
        var arguments = new List<KernelExpression>();

        foreach (var argument in creation.ArgumentList.Arguments)
        {
            arguments.Add(Expression(argument.Expression));
        }

        return new KernelCall(Member(structure, constructor, DeclarationOf(structure), false, false), arguments);
    }

    /// <summary>
    /// Lowers a binary expression, which is an operator of a structure when one of its sides is one.
    /// </summary>
    /// <param name="binary">The expression.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the operation has no representation on the device.</exception>
    private KernelExpression Binary(BinaryExpressionSyntax binary)
    {
        if (binary.IsKind(SyntaxKind.AsExpression) || binary.IsKind(SyntaxKind.IsExpression))
        {
            throw new KernelTranslationException($"'{binary}' casts between references: a kernel body has no objects to cast.");
        }

        var left = Expression(binary.Left);
        var right = Expression(binary.Right, left.ResultType);
        var operation = Operator(binary.Kind());

        if (Operate(left, right, binary.OperatorToken.Kind()) is { } structured)
        {
            return structured;
        }

        var comparison = operation is KernelBinaryOperator.Equal or KernelBinaryOperator.NotEqual or KernelBinaryOperator.Less
            or KernelBinaryOperator.LessOrEqual or KernelBinaryOperator.Greater or KernelBinaryOperator.GreaterOrEqual
            or KernelBinaryOperator.LogicalAnd or KernelBinaryOperator.LogicalOr;

        if (comparison)
        {
            var type = Promote(left.ResultType, right.ResultType, binary);

            return new KernelBinary(operation, Coerce(left, type), Coerce(right, type), ScalarType.Bool);
        }

        if (operation is KernelBinaryOperator.ShiftLeft or KernelBinaryOperator.ShiftRight)
        {
            return new KernelBinary(operation, Coerce(left, left.ResultType), Coerce(right, ScalarType.Int), left.ResultType);
        }

        var promoted = Promote(left.ResultType, right.ResultType, binary);

        return new KernelBinary(operation, Coerce(left, promoted), Coerce(right, promoted), promoted);
    }

    /// <summary>
    /// Lowers a binary expression whose sides are a structure that declares that operator.
    /// </summary>
    /// <param name="left">The value on the left.</param>
    /// <param name="right">The value on the right.</param>
    /// <param name="kind">The token of the operator.</param>
    /// <returns>The call, or <see langword="null"/> when the operation is one of the numbers.</returns>
    private KernelExpression? Operate(KernelExpression left, KernelExpression right, SyntaxKind kind)
    {
        var name = OperatorNameOf(kind);

        if (name is null)
        {
            return null;
        }

        foreach (var side in new[] { left.ResultType, right.ResultType })
        {
            if (side is not StructType structure || !m_Structures.TryGetValue(structure.Name, out var declared))
            {
                continue;
            }

            if (declared.Fields.Any(field => field.Name == name))
            {
                continue;
            }

            var operation = MemberOf(structure, name);

            if (operation is null)
            {
                continue;
            }

            return new KernelCall(Member(structure, operation, DeclarationOf(structure), false, false), new[] { left, right });
        }

        return null;
    }

    /// <summary>
    /// Lowers a unary expression.
    /// </summary>
    /// <param name="prefix">The expression.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the operation has no representation on the device.</exception>
    private KernelExpression Prefix(PrefixUnaryExpressionSyntax prefix)
    {
        var operand = Expression(prefix.Operand);

        return prefix.Kind() switch
        {
            SyntaxKind.UnaryMinusExpression => new KernelUnary(KernelUnaryOperator.Negate, operand, operand.ResultType),
            SyntaxKind.BitwiseNotExpression => new KernelUnary(KernelUnaryOperator.Complement, operand, operand.ResultType),
            SyntaxKind.LogicalNotExpression => new KernelUnary(KernelUnaryOperator.Not, operand, ScalarType.Bool),
            SyntaxKind.UnaryPlusExpression => operand,
            _ => throw new KernelTranslationException($"'{prefix}' has no representation on the device.")
        };
    }

    /// <summary>
    /// Lowers a test against a constant pattern, which the device has as a comparison.
    /// </summary>
    /// <param name="pattern">The expression.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the pattern is not a constant.</exception>
    private KernelExpression Pattern(IsPatternExpressionSyntax pattern)
    {
        var subject = Expression(pattern.Expression);

        if (pattern.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } && literal.IsKind(SyntaxKind.NullLiteralExpression))
        {
            throw new KernelTranslationException("A kernel has no nulls to test for: the buffers it is given are always there.");
        }

        if (pattern.Pattern is ConstantPatternSyntax constant)
        {
            return new KernelBinary(KernelBinaryOperator.Equal, subject, Coerce(Expression(constant.Expression), subject.ResultType), ScalarType.Bool);
        }

        if (pattern.Pattern is UnaryPatternSyntax negation && negation.Pattern is ConstantPatternSyntax different)
        {
            return new KernelBinary(KernelBinaryOperator.NotEqual, subject, Coerce(Expression(different.Expression), subject.ResultType), ScalarType.Bool);
        }

        throw new KernelTranslationException($"A kernel tests values with ==, != or a switch over constants: '{pattern.Pattern}' is not one.");
    }

    /// <summary>
    /// Lowers a switch expression into the chain of comparisons it is, because the device branches with comparisons.
    /// </summary>
    /// <param name="branch">The expression.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KernelTranslationException">Thrown when an arm is not a constant with a discard after it.</exception>
    private KernelExpression Switch(SwitchExpressionSyntax branch)
    {
        var subject = Expression(branch.GoverningExpression);
        var arms = branch.Arms;
        var fallback = arms.FirstOrDefault(arm => arm.Pattern is DiscardPatternSyntax)
                       ?? throw new KernelTranslationException("A switch expression ends with a discard ('_') arm: the device has no exception to throw when nothing matches.");

        var value = Coerce(Expression(fallback.Expression), subject.ResultType);

        foreach (var arm in arms.Reverse())
        {
            if (arm == fallback)
            {
                continue;
            }

            if (arm.Pattern is not ConstantPatternSyntax pattern || arm.WhenClause is not null)
            {
                throw new KernelTranslationException($"A switch expression of a kernel compares with constants: '{arm.Pattern}' is not one.");
            }

            var type = Promote(value.ResultType, Expression(arm.Expression).ResultType, arm.Expression);
            var compared = Coerce(Expression(pattern.Expression), subject.ResultType);

            value = new KernelConditional(
                new KernelBinary(KernelBinaryOperator.Equal, subject, compared, ScalarType.Bool),
                Coerce(Expression(arm.Expression), type),
                Coerce(value, type),
                type);
        }

        return value;
    }

    /// <summary>
    /// Reads a literal.
    /// </summary>
    /// <param name="literal">The literal.</param>
    /// <param name="expected">The type it is read as, when the caller knows it.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the literal has no representation on the device.</exception>
    private KernelLiteral Literal(LiteralExpressionSyntax literal, KernelType? expected)
    {
        switch (literal.Kind())
        {
            case SyntaxKind.NumericLiteralExpression:
            {
                var text = literal.Token.Text;
                var digits = text.Replace("_", string.Empty);

                if (digits.EndsWith("u", StringComparison.OrdinalIgnoreCase) || digits.EndsWith("ul", StringComparison.OrdinalIgnoreCase))
                {
                    return new KernelLiteral(Parse(text, digits, 'u'), ScalarType.UInt);
                }

                if (digits.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                {
                    return new KernelLiteral(Parse(text, digits, 'f'), ScalarType.Float);
                }

                if (digits.IndexOf('.') >= 0 || digits.IndexOf('e') >= 0 || digits.IndexOf('E') >= 0)
                {
                    // A number written with a decimal point is a double in C#, and the device has no double: it is read as
                    // the single it is written against, which is what a kernel that writes 0.5 means.
                    return new KernelLiteral((float)double.Parse(digits.TrimEnd('d', 'D'), CultureInfo.InvariantCulture), ScalarType.Float);
                }

                var value = int.Parse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture);

                return expected is { } wanted && KernelType.ScalarOf(wanted) == KernelScalar.UInt && value >= 0
                    ? new KernelLiteral((uint)value, ScalarType.UInt)
                    : new KernelLiteral(value, ScalarType.Int);
            }

            case SyntaxKind.TrueLiteralExpression:
                return new KernelLiteral(true, ScalarType.Bool);

            case SyntaxKind.FalseLiteralExpression:
                return new KernelLiteral(false, ScalarType.Bool);

            case SyntaxKind.StringLiteralExpression:
            case SyntaxKind.CharacterLiteralExpression:
                throw new KernelTranslationException("The device has no strings or characters.");

            case SyntaxKind.NullLiteralExpression:
                throw new KernelTranslationException("A kernel has no nulls: the buffers it is given are always there.");

            default:
                throw new KernelTranslationException($"'{literal}' has no representation on the device.");
        }
    }

    /// <summary>
    /// Reads the value of a number written with a suffix.
    /// </summary>
    /// <param name="text">The text the kernel wrote.</param>
    /// <param name="digits">The same text without its separators.</param>
    /// <param name="suffix">The suffix that makes it that number type.</param>
    /// <returns>The value.</returns>
    private static object Parse(string text, string digits, char suffix)
    {
        var trimmed = digits.TrimEnd(suffix, char.ToUpperInvariant(suffix));

        _ = text;

        return suffix == 'u'
            ? uint.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : (object)float.Parse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads the constant a name stands for, when the compiler resolved the source.
    /// </summary>
    /// <param name="node">The name or the access.</param>
    /// <param name="constant">When the method returns, the value.</param>
    /// <returns><see langword="true"/> when the name is a constant.</returns>
    private bool TryConstant(SyntaxNode node, out KernelLiteral constant)
    {
        constant = null!;

        if (m_Semantics is null || !m_Semantics.TryConstantValue(node, out var value))
        {
            return false;
        }

        constant = value switch
        {
            float number => new KernelLiteral(number, ScalarType.Float),
            int number => new KernelLiteral(number, ScalarType.Int),
            uint number => new KernelLiteral(number, ScalarType.UInt),
            bool number => new KernelLiteral(number, ScalarType.Bool),
            _ => throw new KernelTranslationException($"A constant of type '{value.GetType().Name}' has no literal on the device.")
        };

        return true;
    }

    /// <summary>
    /// Reads a value as another type, when the two are not the same.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="target">The type it is read as.</param>
    /// <returns>The value, as that type.</returns>
    private KernelExpression Coerce(KernelExpression value, KernelType target)
    {
        if (KernelType.Same(value.ResultType, target) || KernelType.Same(target, VoidType.Void))
        {
            return value;
        }

        if (value is KernelLiteral { Type: ScalarType { Scalar: KernelScalar.Int } } literal && target is ScalarType { Scalar: KernelScalar.UInt })
        {
            return new KernelLiteral((uint)(int)literal.Value, ScalarType.UInt);
        }

        if (value is KernelLiteral { Type: ScalarType { Scalar: KernelScalar.Int } } number && target is ScalarType { Scalar: KernelScalar.Float })
        {
            return new KernelLiteral((float)(int)number.Value, ScalarType.Float);
        }

        if (value is KernelLiteral { Type: ScalarType { Scalar: KernelScalar.Float } } fraction && target is ScalarType { Scalar: KernelScalar.Int })
        {
            return new KernelLiteral((int)(float)fraction.Value, ScalarType.Int);
        }

        return new KernelConvert(target, value);
    }

    /// <summary>
    /// Reads the type an operation of two values computes with, which is the type C# would compute it with.
    /// </summary>
    /// <param name="left">The type on the left.</param>
    /// <param name="right">The type on the right.</param>
    /// <param name="node">The expression, for the refusal.</param>
    /// <returns>The type of the result.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the two values have no common type on the device.</exception>
    private static KernelType Promote(KernelType left, KernelType right, SyntaxNode node)
    {
        if (KernelType.Same(left, right))
        {
            return left;
        }

        // A vector is combined with a vector of the same width: a value that is not a vector has nothing to combine with a
        // component, and the languages disagree about whether the single value is one component or all of them.
        if (left is VectorType != right is VectorType || KernelType.ComponentsOf(left) != KernelType.ComponentsOf(right))
        {
            throw new KernelTranslationException($"'{node}' combines values of different widths, which the device does not do.");
        }

        var one = KernelType.ScalarOf(left);
        var other = KernelType.ScalarOf(right);

        if (one == KernelScalar.Bool || other == KernelScalar.Bool)
        {
            throw new KernelTranslationException($"'{node}' combines a test with a number, which the device does not do.");
        }

        var scalar = one == KernelScalar.Float || other == KernelScalar.Float
            ? KernelScalar.Float
            : one == KernelScalar.UInt || other == KernelScalar.UInt ? KernelScalar.UInt : KernelScalar.Int;

        var components = Math.Max(KernelType.ComponentsOf(left), KernelType.ComponentsOf(right));

        return components > 1 ? new VectorType(scalar, components) : new ScalarType(scalar);
    }

    /// <summary>
    /// Reads the operation a binary token is.
    /// </summary>
    /// <param name="kind">The token.</param>
    /// <returns>The operation.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the token is not an operation of the subset.</exception>
    private static KernelBinaryOperator Operator(SyntaxKind kind) => kind switch
    {
        SyntaxKind.AddExpression => KernelBinaryOperator.Add,
        SyntaxKind.SubtractExpression => KernelBinaryOperator.Subtract,
        SyntaxKind.MultiplyExpression => KernelBinaryOperator.Multiply,
        SyntaxKind.DivideExpression => KernelBinaryOperator.Divide,
        SyntaxKind.ModuloExpression => KernelBinaryOperator.Remainder,
        SyntaxKind.BitwiseAndExpression => KernelBinaryOperator.And,
        SyntaxKind.BitwiseOrExpression => KernelBinaryOperator.Or,
        SyntaxKind.ExclusiveOrExpression => KernelBinaryOperator.Xor,
        SyntaxKind.LeftShiftExpression => KernelBinaryOperator.ShiftLeft,
        SyntaxKind.RightShiftExpression => KernelBinaryOperator.ShiftRight,
        SyntaxKind.EqualsExpression => KernelBinaryOperator.Equal,
        SyntaxKind.NotEqualsExpression => KernelBinaryOperator.NotEqual,
        SyntaxKind.LessThanExpression => KernelBinaryOperator.Less,
        SyntaxKind.LessThanOrEqualExpression => KernelBinaryOperator.LessOrEqual,
        SyntaxKind.GreaterThanExpression => KernelBinaryOperator.Greater,
        SyntaxKind.GreaterThanOrEqualExpression => KernelBinaryOperator.GreaterOrEqual,
        SyntaxKind.LogicalAndExpression => KernelBinaryOperator.LogicalAnd,
        SyntaxKind.LogicalOrExpression => KernelBinaryOperator.LogicalOr,
        _ => throw new KernelTranslationException($"'{kind}' has no representation on the device.")
    };

    /// <summary>
    /// Reads the name a call is made through: the two last parts of the name, without its namespace.
    /// </summary>
    /// <param name="expression">The expression the call is made through.</param>
    /// <returns>The name.</returns>
    private static string CallName(ExpressionSyntax expression)
    {
        return expression switch
        {
            MemberAccessExpressionSyntax access => CallName(access.Name),
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => expression.ToString()
        };
    }

    /// <summary>
    /// Reads the two last parts of a called name, which is what the intrinsics table is keyed by.
    /// </summary>
    /// <param name="expression">The expression the call is made through.</param>
    /// <returns>The owner and the name, or just the name.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the call is made through something the device has no name for.</exception>
    private static string Qualified(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access => Qualified(access.Expression) + "." + access.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => throw new KernelTranslationException($"'{expression}' is not a name the device calls.")
    };

    /// <summary>
    /// Reports whether a call declares an array every thread of the group addresses.
    /// </summary>
    /// <param name="invocation">The call.</param>
    /// <param name="declaration">When the method returns, what the call declares.</param>
    /// <returns><see langword="true"/> when the call declares one.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the count is not a literal.</exception>
    private bool IsShared(InvocationExpressionSyntax invocation, out SharedDeclaration declaration)
    {
        declaration = null!;

        if (invocation.Expression is not GenericNameSyntax generic || generic.Identifier.ValueText != KernelTranspiler.SHARED)
        {
            return false;
        }

        if (generic.TypeArgumentList.Arguments.Count != 1 || invocation.ArgumentList.Arguments.Count != 1)
        {
            throw new KernelTranslationException($"'{KernelTranspiler.SHARED}<T>(count)' takes the type of an element and the number of them.");
        }

        var count = invocation.ArgumentList.Arguments[0].Expression.ToString();

        if (!int.TryParse(count, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) || length <= 0)
        {
            throw new KernelTranslationException($"'{KernelTranspiler.SHARED}<{generic.TypeArgumentList.Arguments[0]}>' takes the number of elements as a literal, which is what the device sizes the array with.");
        }

        declaration = new SharedDeclaration(TypeOf(generic.TypeArgumentList.Arguments[0]), length);
        return true;
    }

    /// <summary>
    /// Resolves a span expression into the buffer it is a window on and the numbers the device reads it through.
    /// </summary>
    /// <param name="expression">The expression to resolve.</param>
    /// <param name="view">When the method returns, the span it stands for.</param>
    /// <returns><see langword="true"/> when the expression names a buffer or a span of one.</returns>
    /// <exception cref="KernelTranslationException">Thrown when a slice is not one the device reads.</exception>
    private bool TryResolve(ExpressionSyntax expression, out SpanView view)
    {
        view = null!;

        if (expression is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.ValueText;

            if (m_Views.TryGetValue(name, out var known))
            {
                view = known;
                return true;
            }

            var buffer = m_Buffers.FirstOrDefault(candidate => candidate.Name == name);

            if (buffer is not null)
            {
                view = new SpanView(name, new KernelLiteral(0, ScalarType.Int), BufferLength(buffer), buffer.ElementType);
                return true;
            }

            return false;
        }

        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            return TryResolve(parenthesized.Expression, out view);
        }

        if (expression is ElementAccessExpressionSyntax ranged &&
            ranged.ArgumentList.Arguments.Count == 1 &&
            ranged.ArgumentList.Arguments[0].Expression is RangeExpressionSyntax range &&
            TryResolve(ranged.Expression, out var source))
        {
            var from = range.LeftOperand is null ? new KernelLiteral(0, ScalarType.Int) : Expression(range.LeftOperand);
            var offset = Combine(source.Offset, from);
            var count = range.RightOperand is null
                ? new KernelBinary(KernelBinaryOperator.Subtract, source.Count, from, ScalarType.Int)
                : new KernelBinary(KernelBinaryOperator.Subtract, Expression(range.RightOperand), from, ScalarType.Int);

            view = new SpanView(source.Buffer, offset, count, source.Element);
            return true;
        }

        if (expression is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax access ||
            access.Name.Identifier.ValueText != KernelTranspiler.SLICE)
        {
            return false;
        }

        if (!TryResolve(access.Expression, out var inner))
        {
            throw new KernelTranslationException($"'{access.Expression}' is not one of the kernel's buffers, so a slice of it has no representation on the device.");
        }

        var arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count is not (1 or 2))
        {
            throw new KernelTranslationException($"'{KernelTranspiler.SLICE}' takes a start and an optional count; {arguments.Count} arguments were given.");
        }

        var start = Expression(arguments[0].Expression);
        var window = Combine(inner.Offset, start);
        var length = arguments.Count == 2
            ? Expression(arguments[1].Expression)
            : new KernelBinary(KernelBinaryOperator.Subtract, inner.Count, start, ScalarType.Int);

        view = new SpanView(inner.Buffer, window, length, inner.Element);
        return true;
    }

    /// <summary>
    /// Reads the local that carries the element count of a buffer.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <returns>The value.</returns>
    private static KernelExpression BufferLength(KernelBuffer buffer) => new KernelLocal(LENGTH_PREFIX + buffer.Name, ScalarType.Int);

    /// <summary>
    /// Adds two offsets, leaving out the one that is zero so that a window of a whole buffer keeps its offset.
    /// </summary>
    /// <param name="left">The offset the window started at.</param>
    /// <param name="right">The offset the slice adds to it.</param>
    /// <returns>The value of the sum.</returns>
    private static KernelExpression Combine(KernelExpression left, KernelExpression right)
    {
        if (IsZero(right))
        {
            return left;
        }

        return IsZero(left) ? right : new KernelBinary(KernelBinaryOperator.Add, left, right, ScalarType.Int);
    }

    /// <summary>
    /// Reports whether a value is the number zero.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    private static bool IsZero(KernelExpression value) => value is KernelLiteral literal && literal.Value is 0;

    /// <summary>
    /// Reports whether an expression names one of the kernel's structures rather than a value.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="structure">When the method returns, the structure, instantiated.</param>
    /// <returns><see langword="true"/> when the name is a structure.</returns>
    private bool TryStruct(ExpressionSyntax expression, out StructType structure)
    {
        structure = null!;

        if (expression is not (IdentifierNameSyntax or GenericNameSyntax))
        {
            return false;
        }

        // A name that is a local, a buffer or a field is a value, not the structure of that name.
        if (expression is IdentifierNameSyntax identifier &&
            (m_Locals.ContainsKey(identifier.Identifier.ValueText) || m_Buffers.Any(buffer => buffer.Name == identifier.Identifier.ValueText)))
        {
            return false;
        }

        if (TypeOf(expression as TypeSyntax ?? SyntaxFactory.ParseTypeName(expression.ToString())) is not StructType declared)
        {
            return false;
        }

        structure = declared;
        return true;
    }

    /// <summary>
    /// Reads the declaration of a structure.
    /// </summary>
    /// <param name="structure">The structure, instantiated.</param>
    /// <returns>Its declaration.</returns>
    private StructDeclarationSyntax DeclarationOf(StructType structure)
    {
        return m_Declared[TemplateOf(structure)];
    }

    /// <summary>
    /// Reads the name of the structure a type is an instantiation of.
    /// </summary>
    /// <param name="structure">The structure, with its arguments.</param>
    /// <returns>The name the source declares it with.</returns>
    private static string TemplateOf(StructType structure)
        => structure.Arguments.Count == 0
            ? structure.Name
            : structure.Name.Substring(0, structure.Name.Length - MangledArguments(structure).Length);

    /// <summary>
    /// Reads the part of a structure's name that carries its arguments.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <returns>The arguments, as they are written in the name.</returns>
    private static string MangledArguments(StructType structure)
        => structure.Arguments.Count == 0 ? string.Empty : "_" + string.Join("_", structure.Arguments.Select(Mangle));

    /// <summary>
    /// Reads the member of a structure a call names.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <param name="name">The name of the member, or the operator name the call is a use of.</param>
    /// <returns>The member.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the structure has no such member.</exception>
    private MemberDeclarationSyntax MemberOf(StructType structure, string name)
    {
        var declaration = DeclarationOf(structure);

        foreach (var member in declaration.Members)
        {
            var memberName = member switch
            {
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                ConstructorDeclarationSyntax => "ctor",
                OperatorDeclarationSyntax operation => OperatorName(operation.OperatorToken.Kind()),
                _ => null
            };

            if (memberName == name)
            {
                return member;
            }
        }

        throw new KernelTranslationException($"'{structure.Name}' has no member named '{name}'.");
    }

    /// <summary>
    /// Reports whether a call of a method of a structure writes the value it is called on.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private bool Writes(StructType structure, string name)
        => MemberOf(structure, name) is MethodDeclarationSyntax method && Mutates(method);

    /// <summary>
    /// Reports whether a value is a test that a condition reads.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when it is a boolean.</returns>
    private static bool IsTest(KernelExpression value) => KernelType.ScalarOf(value.ResultType) == KernelScalar.Bool;
}
