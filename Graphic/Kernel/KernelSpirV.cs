using System.Globalization;
using System.Text;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Writes one program as the SPIR-V binary Vulkan loads.
/// </summary>
/// <remarks>
/// <para>
/// The binary is written here, in C#, with no compiler in between: the module declares the buffers as storage buffers
/// bound at the index every backend binds them at, and the body is the operations the kernel lowered to. It targets
/// SPIR-V 1.5, which every Vulkan 1.2 device accepts.
/// </para>
/// <para>
/// The module keeps every local in a variable of the function and loads and stores it, which is the shape a source
/// compiler produces before it builds static single assignment. A module does not have to be in that form, and building it
/// here would only add a pass that can be wrong.
/// </para>
/// </remarks>
public sealed class KernelSpirV
{
    /// <summary>
    /// Writes one program as a SPIR-V module.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns>The module.</returns>
    public byte[] Emit(KernelProgram program)
    {
        if (program is null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        if ((program.Textures?.Count ?? 0) > 0)
        {
            return System.Array.Empty<byte>();
        }

        return new SpirVWriter(program).Write();
    }
}

/// <summary>
/// Writes the module of one program: the declarations first, then the functions in the order a call reaches them.
/// </summary>
internal sealed class SpirVWriter
{
    private const uint OP_EXT_INST_IMPORT = 11;
    private const uint OP_EXT_INST = 12;
    private const uint OP_MEMORY_MODEL = 14;
    private const uint OP_ENTRY_POINT = 15;
    private const uint OP_EXECUTION_MODE = 16;
    private const uint OP_CAPABILITY = 17;
    private const uint OP_TYPE_VOID = 19;
    private const uint OP_TYPE_BOOL = 20;
    private const uint OP_TYPE_INT = 21;
    private const uint OP_TYPE_FLOAT = 22;
    private const uint OP_TYPE_VECTOR = 23;
    private const uint OP_TYPE_ARRAY = 28;
    private const uint OP_TYPE_RUNTIME_ARRAY = 29;
    private const uint OP_TYPE_STRUCT = 30;
    private const uint OP_TYPE_POINTER = 32;
    private const uint OP_TYPE_FUNCTION = 33;
    private const uint OP_CONSTANT_TRUE = 41;
    private const uint OP_CONSTANT_FALSE = 42;
    private const uint OP_CONSTANT = 43;
    private const uint OP_FUNCTION = 54;
    private const uint OP_FUNCTION_PARAMETER = 55;
    private const uint OP_FUNCTION_END = 56;
    private const uint OP_FUNCTION_CALL = 57;
    private const uint OP_VARIABLE = 59;
    private const uint OP_LOAD = 61;
    private const uint OP_STORE = 62;
    private const uint OP_COPY_LOGICAL = 400;
    private const uint OP_ACCESS_CHAIN = 65;
    private const uint OP_DECORATE = 71;
    private const uint OP_MEMBER_DECORATE = 72;
    private const uint OP_COMPOSITE_EXTRACT = 81;
    private const uint OP_CONVERT_F_TO_S = 109;
    private const uint OP_CONVERT_F_TO_U = 110;
    private const uint OP_CONVERT_S_TO_F = 111;
    private const uint OP_CONVERT_U_TO_F = 112;
    private const uint OP_BITCAST = 124;
    private const uint OP_S_NEGATE = 126;
    private const uint OP_F_NEGATE = 127;
    private const uint OP_I_ADD = 128;
    private const uint OP_F_ADD = 129;
    private const uint OP_I_SUB = 130;
    private const uint OP_F_SUB = 131;
    private const uint OP_I_MUL = 132;
    private const uint OP_F_MUL = 133;
    private const uint OP_U_DIV = 134;
    private const uint OP_S_DIV = 135;
    private const uint OP_F_DIV = 136;
    private const uint OP_U_MOD = 137;
    private const uint OP_S_REM = 138;
    private const uint OP_F_MOD = 141;
    private const uint OP_LOGICAL_OR = 166;
    private const uint OP_LOGICAL_AND = 167;
    private const uint OP_LOGICAL_NOT = 168;
    private const uint OP_SELECT = 169;
    private const uint OP_I_EQUAL = 170;
    private const uint OP_I_NOT_EQUAL = 171;
    private const uint OP_U_GREATER_THAN = 172;
    private const uint OP_S_GREATER_THAN = 173;
    private const uint OP_U_GREATER_THAN_EQUAL = 174;
    private const uint OP_S_GREATER_THAN_EQUAL = 175;
    private const uint OP_U_LESS_THAN = 176;
    private const uint OP_S_LESS_THAN = 177;
    private const uint OP_U_LESS_THAN_EQUAL = 178;
    private const uint OP_S_LESS_THAN_EQUAL = 179;
    private const uint OP_F_ORD_EQUAL = 180;
    private const uint OP_F_ORD_NOT_EQUAL = 182;
    private const uint OP_F_ORD_LESS_THAN = 184;
    private const uint OP_F_ORD_GREATER_THAN = 186;
    private const uint OP_F_ORD_LESS_THAN_EQUAL = 188;
    private const uint OP_F_ORD_GREATER_THAN_EQUAL = 190;
    private const uint OP_SHIFT_RIGHT_LOGICAL = 194;
    private const uint OP_SHIFT_RIGHT_ARITHMETIC = 195;
    private const uint OP_SHIFT_LEFT_LOGICAL = 196;
    private const uint OP_BITWISE_OR = 197;
    private const uint OP_BITWISE_XOR = 198;
    private const uint OP_BITWISE_AND = 199;
    private const uint OP_NOT = 200;
    private const uint OP_CONTROL_BARRIER = 224;
    private const uint OP_ATOMIC_I_ADD = 234;
    private const uint OP_LOOP_MERGE = 246;
    private const uint OP_SELECTION_MERGE = 247;
    private const uint OP_LABEL = 248;
    private const uint OP_BRANCH = 249;
    private const uint OP_BRANCH_CONDITIONAL = 250;
    private const uint OP_RETURN = 253;
    private const uint OP_RETURN_VALUE = 254;
    private const uint OP_GROUP_NON_UNIFORM_ALL = 334;
    private const uint OP_GROUP_NON_UNIFORM_ANY = 335;
    private const uint OP_GROUP_NON_UNIFORM_I_ADD = 349;
    private const uint OP_GROUP_NON_UNIFORM_F_ADD = 350;
    private const uint OP_GROUP_NON_UNIFORM_I_MUL = 351;
    private const uint OP_GROUP_NON_UNIFORM_F_MUL = 352;
    private const uint OP_GROUP_NON_UNIFORM_S_MIN = 353;
    private const uint OP_GROUP_NON_UNIFORM_U_MIN = 354;
    private const uint OP_GROUP_NON_UNIFORM_F_MIN = 355;
    private const uint OP_GROUP_NON_UNIFORM_S_MAX = 356;
    private const uint OP_GROUP_NON_UNIFORM_U_MAX = 357;
    private const uint OP_GROUP_NON_UNIFORM_F_MAX = 358;

    private const uint CAPABILITY_SHADER = 1;
    private const uint CAPABILITY_GROUP_NON_UNIFORM = 61;
    private const uint CAPABILITY_GROUP_NON_UNIFORM_VOTE = 62;
    private const uint CAPABILITY_GROUP_NON_UNIFORM_ARITHMETIC = 63;

    private const uint EXECUTION_MODEL_GL_COMPUTE = 5;
    private const uint EXECUTION_MODE_LOCAL_SIZE = 17;

    private const uint STORAGE_CLASS_INPUT = 1;
    private const uint STORAGE_CLASS_WORKGROUP = 4;
    private const uint STORAGE_CLASS_FUNCTION = 7;
    private const uint STORAGE_CLASS_STORAGE_BUFFER = 12;

    private const uint DECORATION_BLOCK = 2;
    private const uint DECORATION_ARRAY_STRIDE = 6;
    private const uint DECORATION_BUILT_IN = 11;
    private const uint DECORATION_BINDING = 33;
    private const uint DECORATION_DESCRIPTOR_SET = 34;
    private const uint DECORATION_OFFSET = 35;

    private const uint BUILT_IN_GLOBAL_INVOCATION_ID = 28;
    private const uint SCOPE_DEVICE = 1;
    private const uint SCOPE_WORKGROUP = 2;
    private const uint SCOPE_SUBGROUP = 3;
    private const uint MEMORY_SEMANTICS_ACQUIRE_RELEASE = 0x8;
    private const uint MEMORY_SEMANTICS_WORKGROUP_MEMORY = 0x100;
    private const uint MEMORY_SEMANTICS_RELAXED = 0;

    private const uint GROUP_OPERATION_REDUCE = 0;
    private const uint GROUP_OPERATION_EXCLUSIVE_SCAN = 2;

    private const uint GLSL_F_ABS = 4;
    private const uint GLSL_S_ABS = 5;
    private const uint GLSL_FLOOR = 8;
    private const uint GLSL_CEIL = 9;
    private const uint GLSL_SIN = 13;
    private const uint GLSL_COS = 14;
    private const uint GLSL_POW = 26;
    private const uint GLSL_SQRT = 31;
    private const uint GLSL_F_MIN = 37;
    private const uint GLSL_U_MIN = 38;
    private const uint GLSL_S_MIN = 39;
    private const uint GLSL_F_MAX = 40;
    private const uint GLSL_U_MAX = 41;
    private const uint GLSL_S_MAX = 42;
    private const uint GLSL_F_CLAMP = 43;
    private const uint GLSL_U_CLAMP = 44;
    private const uint GLSL_S_CLAMP = 45;

    private const string THREAD_ID = "__gubbinsThreadId";

    private readonly KernelProgram m_Program;
    private readonly List<uint[]> m_Capabilities = new();
    private readonly List<uint[]> m_Imports = new();
    private readonly List<uint[]> m_EntryPoint = new();
    private readonly List<uint[]> m_Annotations = new();
    private readonly List<uint[]> m_Declarations = new();
    private readonly List<uint[]> m_Body = new();
    private readonly Dictionary<string, int> m_Types = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> m_Constants = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> m_Globals = new(StringComparer.Ordinal);
    private readonly Dictionary<KernelFunction, int> m_Functions = new();
    private readonly Stack<(int Merge, int Continue)> m_Loops = new();

    private int m_Glsl;
    private int m_Next = 1;
    private int m_Bound = 1;
    private int m_Void;
    private int m_Bool;
    private int m_Int;
    private int m_UInt;
    private int m_Float;
    private int m_UIntVector3;
    private bool m_UsesWaveArithmetic;
    private bool m_UsesWaveVote;
    private FunctionState? m_Function;
    private Block? m_Block;
    private int m_EntryId;

    internal SpirVWriter(KernelProgram program) => m_Program = program;

    /// <summary>
    /// Writes the module.
    /// </summary>
    /// <returns>The module.</returns>
    internal byte[] Write()
    {
        m_Void = Type(OP_TYPE_VOID, "void");
        m_Bool = Type(OP_TYPE_BOOL, "bool");
        m_Int = Type(OP_TYPE_INT, "int", 32u, 1u);
        m_UInt = Type(OP_TYPE_INT, "uint", 32u, 0u);
        m_Float = Type(OP_TYPE_FLOAT, "float", 32u);
        m_UIntVector3 = Type(OP_TYPE_VECTOR, "uvec3", (uint)m_UInt, 3u);
        m_Glsl = Id();
        m_Imports.Add(Instruction(new uint[] { OP_EXT_INST_IMPORT, (uint)m_Glsl }.Concat(Literal("GLSL.std.450"))));

        m_Capabilities.Add(Instruction(OP_CAPABILITY, CAPABILITY_SHADER));

        foreach (var buffer in m_Program.Buffers)
        {
            Buffer(buffer);
        }

        if (m_Program.Parameters is not null)
        {
            Buffer(m_Program.Parameters);
        }

        Lengths();
        InvocationId();

        m_EntryId = Id();
        m_Functions[m_Program.EntryPoint] = m_EntryId;

        var ordered = Order(m_Program.EntryPoint);

        foreach (var function in ordered)
        {
            if (function != m_Program.EntryPoint)
            {
                m_Functions[function] = Id();
            }
        }

        foreach (var function in ordered)
        {
            Function(function, m_Functions[function]);
        }

        Wave();

        var interfaceVariables = m_Globals.Values.Distinct().Select(id => (uint)id).ToArray();
        var entry = new List<uint> { OP_ENTRY_POINT, EXECUTION_MODEL_GL_COMPUTE, (uint)m_EntryId };
        entry.AddRange(Literal(KernelTranspiler.ENTRY_POINT));
        entry.AddRange(interfaceVariables);
        m_EntryPoint.Add(Instruction(entry));

        m_EntryPoint.Add(Instruction(
            OP_EXECUTION_MODE,
            (uint)m_EntryId,
            EXECUTION_MODE_LOCAL_SIZE,
            (uint)m_Program.ThreadsPerGroup,
            1u,
            1u));

        return Module();
    }

    /// <summary>
    /// Declares the capabilities a call over the lanes of a wave needs.
    /// </summary>
    private void Wave()
    {
        if (!m_UsesWaveArithmetic && !m_UsesWaveVote)
        {
            return;
        }

        m_Capabilities.Add(Instruction(OP_CAPABILITY, CAPABILITY_GROUP_NON_UNIFORM));

        if (m_UsesWaveVote)
        {
            m_Capabilities.Add(Instruction(OP_CAPABILITY, CAPABILITY_GROUP_NON_UNIFORM_VOTE));
        }

        if (m_UsesWaveArithmetic)
        {
            m_Capabilities.Add(Instruction(OP_CAPABILITY, CAPABILITY_GROUP_NON_UNIFORM_ARITHMETIC));
        }
    }

    /// <summary>
    /// Writes the words of the module.
    /// </summary>
    /// <returns>The module.</returns>
    private byte[] Module()
    {
        var words = new List<uint> { 0x07230203, 0x00010500, 0, (uint)m_Bound, 0 };

        foreach (var instruction in m_Capabilities.Concat(m_Imports))
        {
            words.AddRange(instruction);
        }

        words.AddRange(Instruction(OP_MEMORY_MODEL, 0, 1));

        foreach (var instruction in m_EntryPoint.Concat(m_Annotations).Concat(m_Declarations).Concat(m_Body))
        {
            words.AddRange(instruction);
        }

        var bytes = new byte[words.Count * 4];

        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];
            bytes[index * 4] = (byte)word;
            bytes[index * 4 + 1] = (byte)(word >> 8);
            bytes[index * 4 + 2] = (byte)(word >> 16);
            bytes[index * 4 + 3] = (byte)(word >> 24);
        }

        return bytes;
    }

    /// <summary>
    /// Reads the functions of one program in the order a call reaches them, so that a call is written after the function
    /// it reaches.
    /// </summary>
    /// <param name="entry">The entry point.</param>
    /// <returns>The functions.</returns>
    private List<KernelFunction> Order(KernelFunction entry)
    {
        var ordered = new List<KernelFunction>();
        var visited = new HashSet<KernelFunction>();
        var visiting = new HashSet<KernelFunction>();

        void Visit(KernelFunction function)
        {
            if (!visited.Add(function))
            {
                return;
            }

            if (!visiting.Add(function))
            {
                throw new KernelTranslationException($"'{function.Name}' calls itself, which a function of a kernel does not do.");
            }

            foreach (var called in Calls(function))
            {
                Visit(called);
            }

            visiting.Remove(function);
            ordered.Add(function);
        }

        Visit(entry);

        return ordered;
    }

    /// <summary>
    /// Reads the functions one function calls.
    /// </summary>
    /// <param name="function">The function.</param>
    /// <returns>The functions its body calls.</returns>
    private static IEnumerable<KernelFunction> Calls(KernelFunction function)
    {
        var called = new List<KernelFunction>();

        foreach (var statement in function.Body.Statements)
        {
            Statements(statement, called);
        }

        return called;
    }

    /// <summary>
    /// Collects the functions a statement calls.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <param name="called">The functions collected so far.</param>
    private static void Statements(KernelStatement statement, List<KernelFunction> called)
    {
        switch (statement)
        {
            case KernelBlock block:
                foreach (var inner in block.Statements)
                {
                    Statements(inner, called);
                }

                break;

            case KernelDeclaration declaration:
                if (declaration.Initializer is not null)
                {
                    Values(declaration.Initializer, called);
                }

                break;

            case KernelAssignment assignment:
                Values(assignment.Target, called);
                Values(assignment.Value, called);
                break;

            case KernelExpressionStatement expression:
                Values(expression.Expression, called);
                break;

            case KernelIf branch:
                Values(branch.Condition, called);
                Statements(branch.Then, called);

                if (branch.Else is not null)
                {
                    Statements(branch.Else, called);
                }

                break;

            case KernelWhile loop:
                Values(loop.Condition, called);
                Statements(loop.Body, called);
                break;

            case KernelDoWhile loop:
                Values(loop.Condition, called);
                Statements(loop.Body, called);
                break;

            case KernelFor loop:
                if (loop.Run is not null)
                {
                    Statements(loop.Run, called);
                }

                if (loop.Condition is not null)
                {
                    Values(loop.Condition, called);
                }

                if (loop.Next is not null)
                {
                    Statements(loop.Next, called);
                }

                Statements(loop.Body, called);
                break;

            case KernelReturn returned:
                if (returned.Value is not null)
                {
                    Values(returned.Value, called);
                }

                break;

            case KernelAtomicAdd atomic:
                Values(atomic.Target, called);
                Values(atomic.Value, called);
                break;
        }
    }

    /// <summary>
    /// Collects the functions a value calls.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <param name="called">The functions collected so far.</param>
    private static void Values(KernelExpression expression, List<KernelFunction> called)
    {
        switch (expression)
        {
            case KernelCall call:
                called.Add(call.Function);

                foreach (var argument in call.Arguments)
                {
                    Values(argument, called);
                }

                break;

            case KernelIntrinsicCall intrinsic:
                foreach (var argument in intrinsic.Arguments)
                {
                    Values(argument, called);
                }

                break;

            case KernelBinary binary:
                Values(binary.Left, called);
                Values(binary.Right, called);
                break;

            case KernelUnary unary:
                Values(unary.Operand, called);
                break;

            case KernelConvert convert:
                Values(convert.Operand, called);
                break;

            case KernelConditional conditional:
                Values(conditional.Condition, called);
                Values(conditional.WhenTrue, called);
                Values(conditional.WhenFalse, called);
                break;

            case KernelBufferElement element:
                Values(element.Index, called);
                break;

            case KernelArrayElement element:
                Values(element.Index, called);
                break;

            case KernelSharedElement element:
                Values(element.Index, called);
                break;

            case KernelFieldAccess field:
                Values(field.Target, called);
                break;
        }
    }

    /// <summary>
    /// Writes one function.
    /// </summary>
    /// <param name="function">The function.</param>
    /// <param name="id">Its id.</param>
    private void Function(KernelFunction function, int id)
    {
        var returnType = function.ReturnType is VoidType ? m_Void : LocalType(function.ReturnType);
        var parameters = function.Parameters.Select(ParameterType).ToList();
        var type = Type(OP_TYPE_FUNCTION, "function" + id, new[] { returnType }.Concat(parameters).Select(operand => (uint)operand).ToArray());

        m_Body.Add(Instruction(OP_FUNCTION, (uint)returnType, (uint)id, 0, (uint)type));

        m_Function = new FunctionState();
        m_Block = null;

        foreach (var parameter in function.Parameters)
        {
            var parameterType = ParameterType(parameter);
            var parameterId = Id();
            m_Body.Add(Instruction(OP_FUNCTION_PARAMETER, (uint)parameterType, (uint)parameterId));
            m_Function.Locals[parameter.Name] = new Cell(parameterId, parameter.Type, parameter.Mode != KernelParameterMode.In);
        }

        var start = NewBlock();
        Start(start);

        foreach (var shared in function.SharedArrays)
        {
            Shared(shared);
        }

        Declarations(function.Body);

        foreach (var parameter in function.Parameters)
        {
            if (parameter.Mode != KernelParameterMode.In)
            {
                continue;
            }

            var slot = m_Function.Locals[parameter.Name];
            var variable = Variable(slot.Type);
            m_Body.Add(Instruction(OP_STORE, (uint)variable, (uint)slot.Value));
            m_Function.Locals[parameter.Name] = new Cell(variable, slot.Type, true);
        }

        Statement(function.Body);
        Finish();

        m_Function = null;
        m_Block = null;
    }

    /// <summary>
    /// Reads the type a parameter is passed with.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>The type.</returns>
    private int ParameterType(KernelFunctionParameter parameter) => parameter.Mode == KernelParameterMode.In
        ? LocalType(parameter.Type)
        : Type(OP_TYPE_POINTER, "parameter" + parameter.Name, (uint)STORAGE_CLASS_FUNCTION, (uint)LocalType(parameter.Type));

    /// <summary>
    /// Writes the variables of a body, which a function keeps in its first block.
    /// </summary>
    /// <param name="statement">The body.</param>
    private void Declarations(KernelStatement statement)
    {
        switch (statement)
        {
            case KernelBlock block:
                foreach (var inner in block.Statements)
                {
                    Declarations(inner);
                }

                break;

            case KernelDeclaration declaration:
                if (!m_Function!.Locals.ContainsKey(declaration.Name))
                {
                    m_Function.Locals[declaration.Name] = new Cell(Variable(declaration.Type), declaration.Type, true);
                }

                break;

            case KernelIf branch:
                Declarations(branch.Then);

                if (branch.Else is not null)
                {
                    Declarations(branch.Else);
                }

                break;

            case KernelWhile loop:
                Declarations(loop.Body);
                break;

            case KernelDoWhile loop:
                Declarations(loop.Body);
                break;

            case KernelFor loop:
                if (loop.Run is not null)
                {
                    Declarations(loop.Run);
                }

                Declarations(loop.Body);
                break;
        }
    }

    /// <summary>
    /// Writes one statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    private void Statement(KernelStatement statement)
    {
        if (m_Block is not null && m_Block.Ended && statement is not KernelBlock)
        {
            Start(NewBlock());
        }

        switch (statement)
        {
            case KernelBlock block:
                foreach (var inner in block.Statements)
                {
                    Statement(inner);
                }

                return;

            case KernelDeclaration declaration:
                if (declaration.Initializer is not null)
                {
                    var slot = Slot(declaration.Name);
                    var value = Value(declaration.Initializer);

                    // A structure that comes from a buffer carries the explicit layout of that buffer, which a variable of
                    // the function may not have; the logical copy converts it to the type the function uses.
                    if (declaration.Type is StructType)
                    {
                        value = Result(OP_COPY_LOGICAL, LocalType(declaration.Type), (uint)value);
                    }

                    Op(OP_STORE, (uint)slot.Value, (uint)value);
                }

                return;

            case KernelAssignment assignment:
                Op(OP_STORE, (uint)Address(assignment.Target), (uint)Value(assignment.Value));
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
                Jump(m_Loops.Peek().Merge);
                return;

            case KernelContinue:
                Jump(m_Loops.Peek().Continue);
                return;

            case KernelReturn returned:
                if (returned.Value is null)
                {
                    Op(OP_RETURN);
                }
                else
                {
                    Op(OP_RETURN_VALUE, (uint)Value(returned.Value));
                }

                End();
                return;

            case KernelBarrier:
                Op(OP_CONTROL_BARRIER, (uint)ConstantUInt32(SCOPE_WORKGROUP), (uint)ConstantUInt32(SCOPE_WORKGROUP), (uint)ConstantUInt32(MEMORY_SEMANTICS_ACQUIRE_RELEASE | MEMORY_SEMANTICS_WORKGROUP_MEMORY));
                return;

            case KernelAtomicAdd atomic:
                Result(OP_ATOMIC_I_ADD, TypeOf(atomic.Target.ResultType), (uint)Address(atomic.Target), (uint)ConstantUInt32(SCOPE_DEVICE), (uint)ConstantUInt32(MEMORY_SEMANTICS_RELAXED), (uint)Value(atomic.Value));
                return;

            default:
                throw new InvalidOperationException($"'{statement.GetType().Name}' is not a statement a kernel lowers to.");
        }
    }

    /// <summary>
    /// Writes an <c>if</c>, with the block the condition runs and the block it joins.
    /// </summary>
    /// <param name="branch">The statement.</param>
    private void If(KernelIf branch)
    {
        var then = NewBlock();
        var otherwise = branch.Else is null ? null : NewBlock();
        var merge = NewBlock();
        var condition = Value(branch.Condition);

        Op(OP_SELECTION_MERGE, (uint)merge.Id, 0);
        Op(OP_BRANCH_CONDITIONAL, (uint)condition, (uint)then.Id, (uint)(otherwise ?? merge).Id);
        End();

        Start(then);
        Statement(branch.Then);
        Jump(merge.Id);

        if (otherwise is not null)
        {
            Start(otherwise);
            Statement(branch.Else!);
            Jump(merge.Id);
        }

        Start(merge);
    }

    /// <summary>
    /// Writes a loop that tests its condition before each pass.
    /// </summary>
    /// <param name="loop">The statement.</param>
    private void While(KernelWhile loop)
    {
        var header = NewBlock();
        var body = NewBlock();
        var next = NewBlock();
        var merge = NewBlock();

        Jump(header.Id);
        Start(header);
        var condition = Value(loop.Condition);
        Op(OP_LOOP_MERGE, (uint)merge.Id, (uint)next.Id, 0);
        Op(OP_BRANCH_CONDITIONAL, (uint)condition, (uint)body.Id, (uint)merge.Id);
        End();

        Start(body);
        m_Loops.Push((merge.Id, next.Id));
        Statement(loop.Body);
        m_Loops.Pop();
        Jump(next.Id);

        Start(next);
        Jump(header.Id);

        Start(merge);
    }

    /// <summary>
    /// Writes a loop that tests its condition after each pass.
    /// </summary>
    /// <param name="loop">The statement.</param>
    private void DoWhile(KernelDoWhile loop)
    {
        var header = NewBlock();
        var body = NewBlock();
        var next = NewBlock();
        var merge = NewBlock();

        Jump(header.Id);
        Start(header);
        Op(OP_LOOP_MERGE, (uint)merge.Id, (uint)next.Id, 0);
        Op(OP_BRANCH, (uint)body.Id);
        End();

        Start(body);
        m_Loops.Push((merge.Id, next.Id));
        Statement(loop.Body);
        m_Loops.Pop();
        Jump(next.Id);

        Start(next);
        Op(OP_BRANCH_CONDITIONAL, (uint)Value(loop.Condition), (uint)body.Id, (uint)merge.Id);
        End();

        Start(merge);
    }

    /// <summary>
    /// Writes a loop the kernel wrote as a <c>for</c>.
    /// </summary>
    /// <param name="loop">The statement.</param>
    private void For(KernelFor loop)
    {
        var header = NewBlock();
        var body = NewBlock();
        var next = NewBlock();
        var merge = NewBlock();

        if (loop.Run is not null)
        {
            Statement(loop.Run);
        }

        Jump(header.Id);
        Start(header);
        var condition = loop.Condition is null ? (int?)null : Value(loop.Condition);
        Op(OP_LOOP_MERGE, (uint)merge.Id, (uint)next.Id, 0);

        if (condition is null)
        {
            Op(OP_BRANCH, (uint)body.Id);
        }
        else
        {
            Op(OP_BRANCH_CONDITIONAL, (uint)condition.Value, (uint)body.Id, (uint)merge.Id);
        }

        End();

        Start(body);
        m_Loops.Push((merge.Id, next.Id));
        Statement(loop.Body);
        m_Loops.Pop();
        Jump(next.Id);

        Start(next);

        if (loop.Next is not null)
        {
            Statement(loop.Next);
        }

        Jump(header.Id);

        Start(merge);
    }

    /// <summary>
    /// Writes one value.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>The id of the value.</returns>
    private int Value(KernelExpression expression)
    {
        switch (expression)
        {
            case KernelLiteral literal:
                return Constant(literal);

            case KernelLocal local:
            {
                var slot = Slot(local.Name);

                return slot.Address
                    ? Result(OP_LOAD, LocalType(local.ResultType), (uint)slot.Value)
                    : slot.Value;
            }

            case KernelThreadIndex:
                return ThreadIndex();

            case KernelLengthsElement lengths:
            {
                var pointer = Result(OP_ACCESS_CHAIN, Pointer((int)STORAGE_CLASS_STORAGE_BUFFER, m_UInt), (uint)m_Globals[KernelTranspiler.LENGTHS_BUFFER], (uint)ConstantInt(0), (uint)ConstantInt(lengths.Index));

                return Result(OP_LOAD, m_UInt, (uint)pointer);
            }

            case KernelCall call:
                return Call(call);

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
                return Result(OP_LOAD, LoadedType(expression), (uint)Address(expression));
        }
    }

    /// <summary>
    /// Writes one of the two values a condition chooses, which a value the device cannot select is written as a branch.
    /// </summary>
    /// <param name="conditional">The expression.</param>
    /// <returns>The id of the value.</returns>
    private int Conditional(KernelConditional conditional)
    {
        if (conditional.ResultType is StructType or ArrayType)
        {
            throw new KernelTranslationException($"'{conditional}' chooses between values the device selects only one of at a time.");
        }

        return Result(OP_SELECT, TypeOf(conditional.ResultType), (uint)Value(conditional.Condition), (uint)Value(conditional.WhenTrue), (uint)Value(conditional.WhenFalse));
    }

    /// <summary>
    /// Writes the address of a value that lives in the function or in a buffer.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>The id of the pointer to it.</returns>
    private int Address(KernelExpression expression)
    {
        switch (expression)
        {
            case KernelLocal local:
            {
                var slot = Slot(local.Name);

                return slot.Address ? slot.Value : throw new KernelTranslationException($"'{local.Name}' is a value a function reads and does not write.");
            }

            case KernelBufferElement element:
                return Result(OP_ACCESS_CHAIN, Pointer((int)STORAGE_CLASS_STORAGE_BUFFER, TypeOf(element.ResultType)), (uint)m_Globals[element.Buffer], (uint)ConstantInt(0), (uint)Value(element.Index));

            case KernelArrayElement array:
                return Result(OP_ACCESS_CHAIN, Pointer(LocalType(array.ResultType)), (uint)Slot(array.Array).Value, (uint)Value(array.Index));

            case KernelSharedElement shared:
                return Result(OP_ACCESS_CHAIN, Pointer((int)STORAGE_CLASS_WORKGROUP, TypeOf(shared.ResultType)), (uint)m_Globals[shared.Array], (uint)Value(shared.Index));

            case KernelFieldAccess field:
            {
                var storage = StorageClassOf(field.Target);
                var type = storage == (int)STORAGE_CLASS_FUNCTION ? LocalType(field.ResultType) : TypeOf(field.ResultType);

                return Result(OP_ACCESS_CHAIN, Pointer(storage, type), (uint)Address(field.Target), (uint)ConstantInt(MemberIndex(field.Target.ResultType, field.Field)));
            }


            default:
                throw new KernelTranslationException($"'{expression}' is not a value the device addresses.");
        }
    }

    /// <summary>
    /// Reads the storage class a value that can be addressed lives in.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>The storage class of its pointer.</returns>
    private static int StorageClassOf(KernelExpression expression) => expression switch
    {
        KernelBufferElement => (int)STORAGE_CLASS_STORAGE_BUFFER,
        KernelSharedElement => (int)STORAGE_CLASS_WORKGROUP,
        KernelFieldAccess field => StorageClassOf(field.Target),
        _ => (int)STORAGE_CLASS_FUNCTION
    };

    /// <summary>
    /// Writes a call of a function of the program.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The id of the value it computes.</returns>
    private int Call(KernelCall call)
    {
        var id = Id();
        var words = new List<uint> { OP_FUNCTION_CALL, (uint)LocalType(call.Function.ReturnType), (uint)id, (uint)m_Functions[call.Function] };

        for (var index = 0; index < call.Arguments.Count; index++)
        {
            var parameter = call.Function.Parameters[index];

            words.Add(parameter.Mode == KernelParameterMode.In
                ? (uint)Value(call.Arguments[index])
                : (uint)Address(call.Arguments[index]));
        }

        m_Body.Add(Instruction(words));

        return id;
    }

    /// <summary>
    /// Writes one of the calls the device has as an operation.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The id of the value it computes.</returns>
    private int Intrinsic(KernelIntrinsicCall call)
    {
        switch (call.Intrinsic)
        {
            case KernelIntrinsic.WaveAny:
                m_UsesWaveVote = true;
                m_UsesWaveArithmetic = true;
                return Result(OP_GROUP_NON_UNIFORM_ANY, m_Bool, (uint)ConstantUInt32(SCOPE_SUBGROUP), (uint)Value(call.Arguments[0]));

            case KernelIntrinsic.WaveAll:
                m_UsesWaveVote = true;
                m_UsesWaveArithmetic = true;
                return Result(OP_GROUP_NON_UNIFORM_ALL, m_Bool, (uint)ConstantUInt32(SCOPE_SUBGROUP), (uint)Value(call.Arguments[0]));

            case KernelIntrinsic.WaveSum:
            case KernelIntrinsic.WaveMin:
            case KernelIntrinsic.WaveMax:
            case KernelIntrinsic.WaveProduct:
            case KernelIntrinsic.WavePrefixSum:
                m_UsesWaveArithmetic = true;
                return Wave(call);

            default:
                return Arithmetic(call);
        }
    }

    /// <summary>
    /// Writes a call over the lanes of a wave.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The id of the value it computes.</returns>
    private int Wave(KernelIntrinsicCall call)
    {
        var scalar = KernelType.ScalarOf(call.ResultType);
        var operation = call.Intrinsic == KernelIntrinsic.WavePrefixSum ? GROUP_OPERATION_EXCLUSIVE_SCAN : GROUP_OPERATION_REDUCE;

        var opcode = (call.Intrinsic, scalar) switch
        {
            (KernelIntrinsic.WaveSum, KernelScalar.Float) => OP_GROUP_NON_UNIFORM_F_ADD,
            (KernelIntrinsic.WaveSum, _) => OP_GROUP_NON_UNIFORM_I_ADD,
            (KernelIntrinsic.WaveProduct, KernelScalar.Float) => OP_GROUP_NON_UNIFORM_F_MUL,
            (KernelIntrinsic.WaveProduct, _) => OP_GROUP_NON_UNIFORM_I_MUL,
            (KernelIntrinsic.WaveMin, KernelScalar.Float) => OP_GROUP_NON_UNIFORM_F_MIN,
            (KernelIntrinsic.WaveMin, KernelScalar.UInt) => OP_GROUP_NON_UNIFORM_U_MIN,
            (KernelIntrinsic.WaveMin, _) => OP_GROUP_NON_UNIFORM_S_MIN,
            (KernelIntrinsic.WaveMax, KernelScalar.Float) => OP_GROUP_NON_UNIFORM_F_MAX,
            (KernelIntrinsic.WaveMax, KernelScalar.UInt) => OP_GROUP_NON_UNIFORM_U_MAX,
            _ => OP_GROUP_NON_UNIFORM_S_MAX
        };

        return Result(opcode, TypeOf(call.ResultType), (uint)ConstantUInt32(SCOPE_SUBGROUP), (uint)Value(call.Arguments[0]), operation, 0);
    }

    /// <summary>
    /// Writes a call of one of the maths functions, which is an instruction of the standard library.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>The id of the value it computes.</returns>
    private int Arithmetic(KernelIntrinsicCall call)
    {
        var scalar = KernelType.ScalarOf(call.ResultType);

        uint instruction;

        if (call.Intrinsic == KernelIntrinsic.Abs)
        {
            instruction = scalar == KernelScalar.Float ? GLSL_F_ABS : GLSL_S_ABS;
        }
        else
        {
            var (min, max, clamp) = scalar switch
            {
                KernelScalar.Float => (GLSL_F_MIN, GLSL_F_MAX, GLSL_F_CLAMP),
                KernelScalar.UInt => (GLSL_U_MIN, GLSL_U_MAX, GLSL_U_CLAMP),
                _ => (GLSL_S_MIN, GLSL_S_MAX, GLSL_S_CLAMP)
            };

            instruction = call.Intrinsic switch
            {
                KernelIntrinsic.Min => min,
                KernelIntrinsic.Max => max,
                KernelIntrinsic.Clamp => clamp,
                KernelIntrinsic.Sqrt => GLSL_SQRT,
                KernelIntrinsic.Floor => GLSL_FLOOR,
                KernelIntrinsic.Ceiling => GLSL_CEIL,
                KernelIntrinsic.Pow => GLSL_POW,
                KernelIntrinsic.Sin => GLSL_SIN,
                _ => GLSL_COS
            };
        }

        var id = Id();
        var words = new List<uint> { OP_EXT_INST, (uint)TypeOf(call.ResultType), (uint)id, (uint)m_Glsl, instruction };
        words.AddRange(call.Arguments.Select(argument => (uint)Value(argument)));
        m_Body.Add(Instruction(words));

        return id;
    }

    /// <summary>
    /// Writes a binary operation.
    /// </summary>
    /// <param name="binary">The operation.</param>
    /// <returns>The id of the value it computes.</returns>
    private int Binary(KernelBinary binary)
    {
        var scalar = KernelType.ScalarOf(binary.Left.ResultType);
        var unsigned = scalar == KernelScalar.UInt;
        var floating = scalar == KernelScalar.Float;

        var opcode = binary.Operator switch
        {
            KernelBinaryOperator.Add => floating ? OP_F_ADD : OP_I_ADD,
            KernelBinaryOperator.Subtract => floating ? OP_F_SUB : OP_I_SUB,
            KernelBinaryOperator.Multiply => floating ? OP_F_MUL : OP_I_MUL,
            KernelBinaryOperator.Divide => floating ? OP_F_DIV : unsigned ? OP_U_DIV : OP_S_DIV,
            KernelBinaryOperator.Remainder => floating ? OP_F_MOD : unsigned ? OP_U_MOD : OP_S_REM,
            KernelBinaryOperator.And => OP_BITWISE_AND,
            KernelBinaryOperator.Or => OP_BITWISE_OR,
            KernelBinaryOperator.Xor => OP_BITWISE_XOR,
            KernelBinaryOperator.ShiftLeft => OP_SHIFT_LEFT_LOGICAL,
            KernelBinaryOperator.ShiftRight => unsigned ? OP_SHIFT_RIGHT_LOGICAL : OP_SHIFT_RIGHT_ARITHMETIC,
            KernelBinaryOperator.Equal => floating ? OP_F_ORD_EQUAL : OP_I_EQUAL,
            KernelBinaryOperator.NotEqual => floating ? OP_F_ORD_NOT_EQUAL : OP_I_NOT_EQUAL,
            KernelBinaryOperator.Less => floating ? OP_F_ORD_LESS_THAN : unsigned ? OP_U_LESS_THAN : OP_S_LESS_THAN,
            KernelBinaryOperator.LessOrEqual => floating ? OP_F_ORD_LESS_THAN_EQUAL : unsigned ? OP_U_LESS_THAN_EQUAL : OP_S_LESS_THAN_EQUAL,
            KernelBinaryOperator.Greater => floating ? OP_F_ORD_GREATER_THAN : unsigned ? OP_U_GREATER_THAN : OP_S_GREATER_THAN,
            KernelBinaryOperator.GreaterOrEqual => floating ? OP_F_ORD_GREATER_THAN_EQUAL : unsigned ? OP_U_GREATER_THAN_EQUAL : OP_S_GREATER_THAN_EQUAL,
            KernelBinaryOperator.LogicalAnd => OP_LOGICAL_AND,
            KernelBinaryOperator.LogicalOr => OP_LOGICAL_OR,
            _ => throw new InvalidOperationException($"'{binary.Operator}' is not an operation a kernel lowers to.")
        };

        return Result(opcode, TypeOf(binary.ResultType), (uint)Value(binary.Left), (uint)Value(binary.Right));
    }

    /// <summary>
    /// Writes a unary operation.
    /// </summary>
    /// <param name="unary">The operation.</param>
    /// <returns>The id of the value it computes.</returns>
    private int Unary(KernelUnary unary)
    {
        var opcode = unary.Operator switch
        {
            KernelUnaryOperator.Negate => KernelType.ScalarOf(unary.ResultType) == KernelScalar.Float ? OP_F_NEGATE : OP_S_NEGATE,
            KernelUnaryOperator.Complement => OP_NOT,
            _ => OP_LOGICAL_NOT
        };

        return Result(opcode, TypeOf(unary.ResultType), (uint)Value(unary.Operand));
    }

    /// <summary>
    /// Writes a value read as another type.
    /// </summary>
    /// <param name="convert">The value.</param>
    /// <returns>The id of the converted value.</returns>
    private int Convert(KernelConvert convert)
    {
        var source = KernelType.ScalarOf(convert.Operand.ResultType);
        var wanted = KernelType.ScalarOf(convert.Target);

        var opcode = (source, wanted) switch
        {
            (KernelScalar.Float, KernelScalar.Int) => OP_CONVERT_F_TO_S,
            (KernelScalar.Float, KernelScalar.UInt) => OP_CONVERT_F_TO_U,
            (KernelScalar.Int, KernelScalar.Float) => OP_CONVERT_S_TO_F,
            (KernelScalar.UInt, KernelScalar.Float) => OP_CONVERT_U_TO_F,
            _ => OP_BITCAST
        };

        return Result(opcode, TypeOf(convert.Target), (uint)Value(convert.Operand));
    }

    /// <summary>
    /// Writes the value the entry point reads the thread position from.
    /// </summary>
    /// <returns>The id of the value, as an integer.</returns>
    private int ThreadIndex()
    {
        var loaded = Result(OP_LOAD, m_UIntVector3, (uint)m_Globals[THREAD_ID]);
        var x = Result(OP_COMPOSITE_EXTRACT, m_UInt, (uint)loaded, 0);

        return Result(OP_BITCAST, m_Int, (uint)x);
    }

    /// <summary>
    /// Declares the block that carries the element counts of the buffers.
    /// </summary>
    private void Lengths()
    {
        var array = Type(OP_TYPE_RUNTIME_ARRAY, "lengths.array", (uint)m_UInt);
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)array, DECORATION_ARRAY_STRIDE, 4u));

        var block = Type(OP_TYPE_STRUCT, "lengths.block", (uint)array);
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)block, DECORATION_BLOCK));
        m_Annotations.Add(Instruction(OP_MEMBER_DECORATE, (uint)block, 0u, DECORATION_OFFSET, 0u));

        var pointer = Type(OP_TYPE_POINTER, "lengths.pointer", (uint)STORAGE_CLASS_STORAGE_BUFFER, (uint)block);
        var variable = Id();

        m_Declarations.Add(Instruction(OP_VARIABLE, (uint)pointer, (uint)variable, STORAGE_CLASS_STORAGE_BUFFER));
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)variable, DECORATION_DESCRIPTOR_SET, 0u));
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)variable, DECORATION_BINDING, (uint)(m_Program.Buffers.Count + (m_Program.Parameters is null ? 0 : 1))));

        m_Globals[KernelTranspiler.LENGTHS_BUFFER] = variable;
    }

    /// <summary>
    /// Declares one buffer of the kernel.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    private void Buffer(KernelBuffer buffer)
    {
        var element = TypeOf(buffer.ElementType);
        var array = Type(OP_TYPE_RUNTIME_ARRAY, "array" + buffer.Binding, (uint)element);
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)array, DECORATION_ARRAY_STRIDE, (uint)Stride(buffer.ElementType)));

        var block = Type(OP_TYPE_STRUCT, "block" + buffer.Binding, (uint)array);
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)block, DECORATION_BLOCK));
        m_Annotations.Add(Instruction(OP_MEMBER_DECORATE, (uint)block, 0u, DECORATION_OFFSET, 0u));

        var pointer = Type(OP_TYPE_POINTER, "buffer" + buffer.Binding, (uint)STORAGE_CLASS_STORAGE_BUFFER, (uint)block);
        var variable = Id();

        m_Declarations.Add(Instruction(OP_VARIABLE, (uint)pointer, (uint)variable, STORAGE_CLASS_STORAGE_BUFFER));
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)variable, DECORATION_DESCRIPTOR_SET, 0u));
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)variable, DECORATION_BINDING, (uint)buffer.Binding));

        m_Globals[buffer.Name] = variable;
    }

    /// <summary>
    /// Declares one array the group shares.
    /// </summary>
    /// <param name="array">The array.</param>
    private void Shared(KernelSharedArray array)
    {
        var type = Type(OP_TYPE_ARRAY, "shared" + array.Name, (uint)TypeOf(array.Element), (uint)ConstantInt(array.Length));

        var pointer = Type(OP_TYPE_POINTER, "shared.pointer" + array.Name, (uint)STORAGE_CLASS_WORKGROUP, (uint)type);
        var variable = Id();

        m_Declarations.Add(Instruction(OP_VARIABLE, (uint)pointer, (uint)variable, STORAGE_CLASS_WORKGROUP));
        m_Globals[array.Name] = variable;
    }

    /// <summary>
    /// Declares the variable the thread position is read from.
    /// </summary>
    private void InvocationId()
    {
        var pointer = Type(OP_TYPE_POINTER, "input.pointer", (uint)STORAGE_CLASS_INPUT, (uint)m_UIntVector3);
        var variable = Id();

        m_Declarations.Add(Instruction(OP_VARIABLE, (uint)pointer, (uint)variable, STORAGE_CLASS_INPUT));
        m_Annotations.Add(Instruction(OP_DECORATE, (uint)variable, DECORATION_BUILT_IN, BUILT_IN_GLOBAL_INVOCATION_ID));

        m_Globals[THREAD_ID] = variable;
    }

    /// <summary>
    /// Reads the field a name is, in the order the structure declares it.
    /// </summary>
    /// <param name="type">The structure.</param>
    /// <param name="name">The field.</param>
    /// <returns>Its index.</returns>
    private int MemberIndex(KernelType type, string name)
    {
        var structure = m_Program.Structs.First(candidate => candidate.Name == type.TypeName);
        var index = structure.Fields.ToList().FindIndex(field => field.Name == name);

        return index >= 0 ? index : throw new KernelTranslationException($"'{name}' is not a field of '{structure.Name}'.");
    }

    /// <summary>
    /// Reads a literal as a constant of the module.
    /// </summary>
    /// <param name="literal">The literal.</param>
    /// <returns>The id of the constant.</returns>
    private int Constant(KernelLiteral literal) => literal.Value switch
    {
        bool value => ConstantBool(value),
        int value => Constant("int:" + value, m_Int, (uint)value),
        uint value => Constant("uint:" + value, m_UInt, value),
        float value => Constant("float:" + value.ToString("R", CultureInfo.InvariantCulture), m_Float, BitConverter.ToUInt32(BitConverter.GetBytes(value), 0)),
        _ => throw new InvalidOperationException($"'{literal.Value.GetType().Name}' is not a literal a kernel lowers to.")
    };

    /// <summary>
    /// Reads an unsigned integer as a constant of the module.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The id of the constant.</returns>
    private int ConstantUInt32(uint value) => Constant("uint:" + value, m_UInt, value);

    /// <summary>
    /// Reads a signed integer as a constant of the module.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The id of the constant.</returns>
    private int ConstantInt(int value) => Constant("int:" + value, m_Int, (uint)value);

    /// <summary>
    /// Reads a test as a constant of the module.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The id of the constant.</returns>
    private int ConstantBool(bool value)
    {
        var key = value ? "true" : "false";

        if (m_Constants.TryGetValue(key, out var known))
        {
            return known;
        }

        var id = Id();
        m_Declarations.Add(Instruction(value ? OP_CONSTANT_TRUE : OP_CONSTANT_FALSE, (uint)m_Bool, (uint)id));
        m_Constants[key] = id;

        return id;
    }

    /// <summary>
    /// Declares a constant of the module once.
    /// </summary>
    /// <param name="key">The key that makes it unique.</param>
    /// <param name="type">Its type.</param>
    /// <param name="value">Its value, as the word the module holds.</param>
    /// <returns>The id of the constant.</returns>
    private int Constant(string key, int type, uint value)
    {
        if (m_Constants.TryGetValue(key, out var known))
        {
            return known;
        }

        var id = Id();
        m_Declarations.Add(Instruction(OP_CONSTANT, (uint)type, (uint)id, value));
        m_Constants[key] = id;

        return id;
    }

    /// <summary>
    /// Reads the type of one value of the program.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The id of the type.</returns>
    private int TypeOf(KernelType type)
    {
        switch (type)
        {
            case ScalarType { Scalar: KernelScalar.Bool }:
                return m_Bool;

            case ScalarType { Scalar: KernelScalar.Int }:
                return m_Int;

            case ScalarType { Scalar: KernelScalar.UInt }:
                return m_UInt;

            case ScalarType { Scalar: KernelScalar.Float }:
                return m_Float;

            case VectorType vector:
                return Type(OP_TYPE_VECTOR, "vector" + vector.TypeName, (uint)TypeOf(new ScalarType(vector.Scalar)), (uint)vector.Components);

            case StructType structure:
                return Structure(structure);

            case ArrayType array:
                return Type(OP_TYPE_ARRAY, "array" + array.TypeName, (uint)TypeOf(array.Element), (uint)ConstantInt(array.Length));

            case VoidType:
                return m_Void;

            default:
                throw new KernelTranslationException($"'{type.TypeName}' has no representation in SPIR-V.");
        }
    }

    /// <summary>
    /// Reads the type a value of the function takes, which carries no explicit layout.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The id of the type.</returns>
    private int LocalType(KernelType type) => type switch
    {
        StructType structure => Structure(structure, false),
        ArrayType array => Type(OP_TYPE_ARRAY, "array.local." + array.TypeName, (uint)LocalType(array.Element), (uint)ConstantInt(array.Length)),
        _ => TypeOf(type)
    };

    /// <summary>
    /// Reads the type a value is loaded as, which depends on where it lives.
    /// </summary>
    /// <param name="expression">The value.</param>
    /// <returns>The id of the type.</returns>
    private int LoadedType(KernelExpression expression)
        => StorageClassOf(expression) == (int)STORAGE_CLASS_FUNCTION ? LocalType(expression.ResultType) : TypeOf(expression.ResultType);

    /// <summary>
    /// Declares one structure of the program.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <param name="layout">Whether the structure needs the explicit layout of a buffer.</param>
    /// <returns>The id of the type.</returns>
    private int Structure(StructType structure, bool layout = true)
    {
        var prefix = layout ? "struct:" : "struct.local:";
        var key = OP_TYPE_STRUCT + ":" + prefix + structure.Name;

        if (m_Types.TryGetValue(key, out var known))
        {
            return known;
        }

        var declaration = m_Program.Structs.First(candidate => candidate.Name == structure.Name);
        var fields = declaration.Fields.Select(field => layout ? TypeOf(field.Type) : LocalType(field.Type)).ToArray();
        var type = Type(OP_TYPE_STRUCT, prefix + structure.Name, fields.Select(field => (uint)field).ToArray());

        if (!layout)
        {
            return type;
        }

        var offset = 0;

        for (var index = 0; index < declaration.Fields.Count; index++)
        {
            var field = declaration.Fields[index];
            offset = Align(offset, AlignOf(field.Type));
            m_Annotations.Add(Instruction(OP_MEMBER_DECORATE, (uint)type, (uint)index, DECORATION_OFFSET, (uint)offset));
            offset += SizeOf(field.Type);
        }

        return type;
    }

    /// <summary>
    /// Reads the size a value takes in a buffer.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The size, in bytes.</returns>
    private int SizeOf(KernelType type) => type switch
    {
        ScalarType => 4,
        VectorType vector => vector.Components * 4,
        StructType structure => StructureSize(structure),
        _ => 4
    };

    /// <summary>
    /// Reads the distance one element of a runtime array is from the next, which is its size rounded to its alignment.
    /// </summary>
    /// <param name="type">The element type.</param>
    /// <returns>The stride, in bytes.</returns>
    private int Stride(KernelType type) => Align(SizeOf(type), AlignOf(type));

    /// <summary>
    /// Reads the alignment a value takes in a buffer, which is what the offsets of a structure follow.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The alignment, in bytes.</returns>
    private int AlignOf(KernelType type) => type switch
    {
        VectorType vector when vector.Components == 2 => 8,
        VectorType vector when vector.Components >= 3 => 16,
        StructType structure => StructureAlign(structure),
        _ => 4
    };

    /// <summary>
    /// Reads the size of a structure, which is its last field rounded to its alignment.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <returns>The size, in bytes.</returns>
    private int StructureSize(StructType structure)
    {
        var declaration = m_Program.Structs.First(candidate => candidate.Name == structure.Name);
        var offset = 0;

        foreach (var field in declaration.Fields)
        {
            offset = Align(offset, AlignOf(field.Type));
            offset += SizeOf(field.Type);
        }

        return Align(offset, StructureAlign(structure));
    }

    /// <summary>
    /// Reads the alignment of a structure, which is the largest alignment of its fields.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <returns>The alignment, in bytes.</returns>
    private int StructureAlign(StructType structure)
    {
        var declaration = m_Program.Structs.First(candidate => candidate.Name == structure.Name);

        return declaration.Fields.Count == 0 ? 4 : declaration.Fields.Max(field => AlignOf(field.Type));
    }

    /// <summary>
    /// Rounds an offset up to an alignment.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="alignment">The alignment.</param>
    /// <returns>The offset.</returns>
    private static int Align(int offset, int alignment) => (offset + alignment - 1) / alignment * alignment;

    /// <summary>
    /// Reads the pointer type of a value of the function.
    /// </summary>
    /// <param name="type">The type it points to.</param>
    /// <returns>The id of the pointer type.</returns>
    private int Pointer(int type) => Pointer((int)STORAGE_CLASS_FUNCTION, type);

    /// <summary>
    /// Writes a pointer type in the storage class the value it points at lives in.
    /// </summary>
    /// <param name="storageClass">The storage class.</param>
    /// <param name="type">The type pointed at.</param>
    /// <returns>The id of the pointer type.</returns>
    private int Pointer(int storageClass, int type) => Type(OP_TYPE_POINTER, "pointer" + storageClass + ":" + type, (uint)storageClass, (uint)type);


    /// <summary>
    /// Declares a type of the module once.
    /// </summary>
    /// <param name="opcode">The instruction that declares it.</param>
    /// <param name="key">The key that makes it unique.</param>
    /// <param name="operands">The operands after its id.</param>
    /// <returns>The id of the type.</returns>
    private int Type(uint opcode, string key, params uint[] operands)
    {
        var full = opcode + ":" + key;

        if (m_Types.TryGetValue(full, out var known))
        {
            return known;
        }

        var id = Id();
        var words = new List<uint> { opcode, (uint)id };
        words.AddRange(operands);
        m_Declarations.Add(Instruction(words));
        m_Types[full] = id;

        return id;
    }

    /// <summary>
    /// Declares a variable of the function being written.
    /// </summary>
    /// <param name="type">The type it holds.</param>
    /// <param name="address">Whether the body takes its address, which every local does.</param>
    /// <returns>The id of the variable.</returns>
    private int Variable(KernelType type)
    {
        var id = Id();
        m_Body.Add(Instruction(OP_VARIABLE, (uint)Pointer(LocalType(type)), (uint)id, STORAGE_CLASS_FUNCTION));

        return id;
    }

    /// <summary>
    /// Reads a variable of the function being written.
    /// </summary>
    /// <param name="name">The name the body uses.</param>
    /// <returns>The slot it holds.</returns>
    private Cell Slot(string name) => m_Function!.Locals.TryGetValue(name, out var slot)
        ? slot
        : throw new KernelTranslationException($"'{name}' is read by the device and was not declared.");

    /// <summary>
    /// Creates a block of the function being written.
    /// </summary>
    /// <returns>The block.</returns>
    private Block NewBlock() => new(Id());

    /// <summary>
    /// Starts a block, ending the one that was being written when it is not ended.
    /// </summary>
    /// <param name="block">The block.</param>
    private void Start(Block block)
    {
        if (m_Block is not null && !m_Block.Ended)
        {
            Op(OP_BRANCH, (uint)block.Id);
        }

        m_Body.Add(Instruction(OP_LABEL, (uint)block.Id));
        m_Block = block;
    }

    /// <summary>
    /// Ends the block being written by branching to another one.
    /// </summary>
    /// <param name="block">The block to branch to.</param>
    private void Jump(int target)
    {
        if (m_Block is null || m_Block.Ended)
        {
            return;
        }

        Op(OP_BRANCH, (uint)target);
        End();
    }

    /// <summary>
    /// Marks the block being written as ended.
    /// </summary>
    private void End()
    {
        if (m_Block is not null)
        {
            m_Block.Ended = true;
        }
    }

    /// <summary>
    /// Ends the function being written.
    /// </summary>
    private void Finish()
    {
        if (m_Block is not null && !m_Block.Ended)
        {
            Op(OP_RETURN);
            End();
        }

        m_Body.Add(Instruction(OP_FUNCTION_END));
    }

    /// <summary>
    /// Writes one instruction that computes a value.
    /// </summary>
    /// <param name="opcode">The instruction.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="operands">The operands after the type and the id.</param>
    /// <returns>The id of the value.</returns>
    private int Result(uint opcode, int type, params uint[] operands)
    {
        var id = Id();
        var words = new List<uint> { opcode, (uint)type, (uint)id };
        words.AddRange(operands);
        m_Body.Add(Instruction(words));

        return id;
    }

    /// <summary>
    /// Writes one instruction that has no result.
    /// </summary>
    /// <param name="opcode">The instruction.</param>
    /// <param name="operands">Its operands.</param>
    private void Op(uint opcode, params uint[] operands)
    {
        var words = new List<uint> { opcode };
        words.AddRange(operands);
        m_Body.Add(Instruction(words));
    }

    /// <summary>
    /// Reads the next id of the module.
    /// </summary>
    /// <returns>The id.</returns>
    private int Id()
    {
        var id = m_Next++;
        m_Bound = m_Next;

        return id;
    }

    /// <summary>
    /// Builds one instruction.
    /// </summary>
    /// <param name="opcode">The instruction.</param>
    /// <param name="operands">Its operands.</param>
    /// <returns>The words of the instruction, with its word count in front.</returns>
    private static uint[] Instruction(uint opcode, params uint[] operands) => Instruction(new[] { opcode }.Concat(operands));

    /// <summary>
    /// Builds one instruction.
    /// </summary>
    /// <param name="words">The words, starting with the opcode.</param>
    /// <returns>The words of the instruction, with its word count in front.</returns>
    private static uint[] Instruction(IEnumerable<uint> words)
    {
        var list = words.ToList();
        list[0] |= (uint)list.Count << 16;

        return list.ToArray();
    }

    /// <summary>
    /// Writes a string as the words of an instruction.
    /// </summary>
    /// <param name="text">The string.</param>
    /// <returns>The words, null terminated and padded.</returns>
    private static uint[] Literal(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\0");
        var words = new uint[(bytes.Length + 3) / 4];

        for (var index = 0; index < bytes.Length; index++)
        {
            words[index / 4] |= (uint)bytes[index] << (index % 4 * 8);
        }

        return words;
    }

    /// <summary>
    /// One local of a function: the id it lives in, its type, and whether that id is an address.
    /// </summary>
    /// <param name="Value">The id.</param>
    /// <param name="Type">The type of the value.</param>
    /// <param name="Address">Whether the id is a pointer to the value.</param>
    private sealed record Cell(int Value, KernelType Type, bool Address);

    /// <summary>
    /// One block of a function.
    /// </summary>
    private sealed class Block
    {
        internal Block(int id) => Id = id;

        internal int Id { get; }

        internal bool Ended { get; set; }
    }

    /// <summary>
    /// The locals of the function being written.
    /// </summary>
    private sealed class FunctionState
    {
        internal Dictionary<string, Cell> Locals { get; } = new(StringComparer.Ordinal);
    }
}
