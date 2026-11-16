namespace Gubbins.Span;

/// <summary>
/// Flags that describe which span operation implementation strategy should be used.
/// </summary>
[Flags]
public enum IterationMode
{
    /// <summary>
    /// Automatically select the best available implementation.
    /// The order of preference is SIMD + Parallel, then SIMD, then Parallel, then Serial.
    /// </summary>
    Auto = -1,

    /// <summary>
    /// Indicates that the operation should run on a single thread.
    /// </summary>
    Serial = 1,

    /// <summary>
    /// Indicates that the operation can use SIMD acceleration.
    /// </summary>
    Simd = 2,

    /// <summary>
    /// Indicates that the operation can run in parallel.
    /// </summary>
    Parallel = 4
}

/// <summary>
/// Selects one scalar number operation from an <see cref="ISpanNumberOperation{T}"/> implementation.
/// </summary>
public enum NumberOperation
{
    /// <summary>
    /// Adds the operand to every element.
    /// </summary>
    Add,

    /// <summary>
    /// Subtracts the operand from every element.
    /// </summary>
    Subtract,

    /// <summary>
    /// Multiplies every element by the operand.
    /// </summary>
    Multiply,

    /// <summary>
    /// Divides every element by the operand.
    /// </summary>
    Divide,

    /// <summary>
    /// Replaces every element with its remainder modulo the operand.
    /// </summary>
    Modulo
}

/// <summary>
/// Declares span operations for a component whose storage <b>is</b> the bridged value: the component contains exactly
/// one instance field of the operation's value type, and that field is all the operation touches.
/// </summary>
/// <remarks>
/// <c>Span&lt;Component&gt;</c> is reinterpreted as <c>Span&lt;V&gt;</c> with <c>MemoryMarshal.Cast</c>, so one component
/// maps to exactly one <c>V</c>. The generator requires "single instance field, and it is the bridged member" because
/// that is the only shape whose layout it can prove. Use <see cref="SpanToAttribute"/> when the component has no such
/// member but its overall layout is equivalent to one <c>V</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class SpanAsAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the operation interfaces to generate.
    /// </summary>
    /// <param name="operations">Operation interface types that should be generated for the annotated struct.</param>
    public SpanAsAttribute(params Type[] operations) { }

    /// <summary>
    /// Backward-compatible overload for existing call sites that bind generation metadata explicitly.
    /// </summary>
    /// <param name="type">Legacy operation type argument retained for compatibility.</param>
    /// <param name="member">The component member to bind generated operations to.</param>
    /// <param name="method">A custom generated extension method name.</param>
    /// <param name="overwrite">A method name that should be overwritten/replaced by generation.</param>
    public SpanAsAttribute(Type type, string member, string method, string overwrite)
    {
        Member = member;
        Method = method;
        Overwrite = overwrite;
    }

    /// <summary>
    /// Optional component member name that listed operations should bind to.
    /// </summary>
    public string Member { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom generated extension method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Optional generated method name to overwrite.
    /// </summary>
    public string Overwrite { get; set; } = string.Empty;
}

/// <summary>
/// Declares span operations for a component whose <b>layout</b> is equivalent to the operation's value type: the
/// component has no such member, but its fields taken together are one <c>V</c> (four <c>float</c> fields forming a
/// <c>Vector4</c>, for example).
/// </summary>
/// <remarks>
/// <para>
/// <c>Span&lt;Component&gt;</c> is still reinterpreted as <c>Span&lt;V&gt;</c> with <c>MemoryMarshal.Cast</c>, so one
/// component maps to exactly one <c>V</c>; the difference from <see cref="SpanAsAttribute"/> is that the operation covers
/// <b>every</b> field of the component instead of a single member.
/// </para>
/// <para>
/// The generator verifies the layout rather than a member: every instance field must have a known size, the sequential
/// layout (with natural alignment) must have no padding, and the total size must equal the value type's size. Explicit
/// layouts, packed layouts and fixed-size buffers are rejected.
/// </para>
/// <para>
/// This is a layout assertion, not a member contract, so keep field order aligned with the target type's semantics:
/// <c>struct Orientation { float X, Y, Z, W; }</c> maps to <c>Vector4(X, Y, Z, W)</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class SpanToAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the operation interfaces to generate.
    /// </summary>
    /// <param name="operations">Operation interface types that should be generated for the annotated struct.</param>
    public SpanToAttribute(params Type[] operations) { }

    /// <summary>
    /// Partial field-to-lane mapping, one <c>"Field:Lane"</c> entry per listed field, in any order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lists the fields whose lane assignment is worth stating. Unlisted fields keep the positional mapping (the n-th
    /// instance field feeds the n-th lane), which is what the zero-copy reinterpret always does.
    /// </para>
    /// <para>
    /// The mapping is a checked assertion, not a rearrangement: because the bridge reinterprets the component's memory,
    /// a listed field's lane must be its own physical position, otherwise the declaration is rejected (GUB3010) instead
    /// of silently producing wrong results.
    /// </para>
    /// <para>
    /// Attribute arguments do not accept tuples, so pairs are encoded as strings; <c>"Y:X"</c> and <c>"Y=X"</c> are both
    /// accepted, and the lane may be a name (<c>"X"</c>) or a zero-based index (<c>"0"</c>).
    /// </para>
    /// <para>
    /// <c>[SpanTo(typeof(ISpanVectorOperation&lt;Vector2&gt;), Lane = new[] { "Z:Y" })]</c> on
    /// <c>struct RotationXZ { public float X; public float Z; }</c> states that <c>Z</c> is the <c>Y</c> lane (and
    /// <c>X</c> is <c>X</c> by position).
    /// </para>
    /// </remarks>
    public string[] Lane { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional custom generated extension method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Optional generated method name to overwrite, paired with <see cref="Method"/>.
    /// </summary>
    public string Overwrite { get; set; } = string.Empty;
}
/// <summary>
/// Registers and resolves span operation implementations for component types.

/// </summary>
public static class SpanOperations
{
    /// <summary>
    /// Internal storage for registered span operation implementations, organized by component type and execution strategy.
    /// </summary>
    private static class Simd<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support parallel execution, organized by component type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private static class Serial<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support both SIMD and parallel execution, organized by component type.
    /// </summary>
    private static class Parallel<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support both SIMD and parallel execution, organized by component type.
    /// </summary>
    private static class SimdParallel<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    static SpanOperations()
    {
        RegisterSpanOperation(new SerialNumberOperation<int>(), IterationMode.Serial);
        RegisterSpanOperation(new SerialNumberOperation<uint>(), IterationMode.Serial);
        RegisterSpanOperation(new SerialNumberOperation<long>(), IterationMode.Serial);
        RegisterSpanOperation(new SerialNumberOperation<ulong>(), IterationMode.Serial);
        RegisterSpanOperation(new SerialNumberOperation<float>(), IterationMode.Serial);
        RegisterSpanOperation(new SerialNumberOperation<double>(), IterationMode.Serial);
        RegisterSpanOperation(new SerialIntOperation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialUintOperation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialLongOperation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialUlongOperation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialFloatOperation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialDoubleOperation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialVector2Operation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialVector3Operation(), IterationMode.Serial);
        RegisterSpanOperation(new SerialVector4Operation(), IterationMode.Serial);

        RegisterSpanOperation(new ParallelNumberOperation<int>(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<uint>(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<long>(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<ulong>(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<float>(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<double>(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelIntOperation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelUintOperation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelLongOperation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelUlongOperation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelFloatOperation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelDoubleOperation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelVector2Operation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelVector3Operation(), IterationMode.Parallel);
        RegisterSpanOperation(new ParallelVector4Operation(), IterationMode.Parallel);

        RegisterSpanOperation(new SimdNumberOperation<int>(), IterationMode.Simd);
        RegisterSpanOperation(new SimdNumberOperation<uint>(), IterationMode.Simd);
        RegisterSpanOperation(new SimdNumberOperation<long>(), IterationMode.Simd);
        RegisterSpanOperation(new SimdNumberOperation<ulong>(), IterationMode.Simd);
        RegisterSpanOperation(new SimdNumberOperation<float>(), IterationMode.Simd);
        RegisterSpanOperation(new SimdNumberOperation<double>(), IterationMode.Simd);
#if NET7_0_OR_GREATER
        RegisterSpanOperation(new SimdIntOperation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdUintOperation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdLongOperation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdUlongOperation(), IterationMode.Simd);
#endif
        RegisterSpanOperation(new SimdFloatOperation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdDoubleOperation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdVector2Operation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdVector3Operation(), IterationMode.Simd);
        RegisterSpanOperation(new SimdVector4Operation(), IterationMode.Simd);

        RegisterSpanOperation(new ParallelSimdNumberOperation<int>(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<uint>(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<long>(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<ulong>(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<float>(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<double>(), IterationMode.Parallel | IterationMode.Simd);
#if NET7_0_OR_GREATER
        RegisterSpanOperation(new ParallelSimdIntOperation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdUintOperation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdLongOperation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdUlongOperation(), IterationMode.Parallel | IterationMode.Simd);
#endif
        RegisterSpanOperation(new ParallelSimdFloatOperation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdDoubleOperation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdVector2Operation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdVector3Operation(), IterationMode.Parallel | IterationMode.Simd);
        RegisterSpanOperation(new ParallelSimdVector4Operation(), IterationMode.Parallel | IterationMode.Simd);
    }

    /// <summary>
    /// Registers a span operation implementation for a component type and execution mask.
    /// </summary>
    /// <typeparam name="T">The component type the operation applies to.</typeparam>
    /// <param name="operation">The operation implementation to register.</param>
    /// <param name="mask">
    /// The strategy flags that determine which internal queue receives the operation.
    /// If no explicit SIMD/Parallel match is found, registration falls back to the serial queue.
    /// </param>
    public static void RegisterSpanOperation<T>(ISpanOperation<T> operation, IterationMode mask)
    {
        if ((mask & IterationMode.Simd) == IterationMode.Simd)
        {
            if ((mask & IterationMode.Parallel) == IterationMode.Parallel)
            {
                SimdParallel<T>.Operations.Enqueue(operation);
            }
            else
            {
                Simd<T>.Operations.Enqueue(operation);
            }
        }
        else if (mask == IterationMode.Serial)
        {
            Serial<T>.Operations.Enqueue(operation);
        }
        else if ((mask & IterationMode.Parallel) == IterationMode.Parallel)
        {
            Parallel<T>.Operations.Enqueue(operation);
        }
        else
        {
            Serial<T>.Operations.Enqueue(operation);
        }
    }

    /// <summary>
    /// Gets the first supported operation implementation that matches the requested mask.
    /// </summary>
    /// <typeparam name="T">The component type the operation applies to.</typeparam>
    /// <typeparam name="TOperation">The specific operation interface/class to resolve.</typeparam>
    /// <param name="mask">The strategy flags used to choose candidate queues. Defaults to <see cref="IterationMode.Auto"/>.</param>
    /// <returns>
    /// The first supported operation of type <typeparamref name="TOperation"/>, or <see langword="null"/> when no matching supported operation is available.
    /// </returns>
    /// <remarks>
    /// Candidates are inspected without being removed from their registration queues.
    /// With <see cref="IterationMode.Auto"/>, lookup preference is SIMD + Parallel, then SIMD, then Parallel, then Serial.
    /// The component's type initializer is forced first, because that is where a generated adapter registers itself.
    /// </remarks>
    public static TOperation? GetOperation<T, TOperation>(IterationMode mask = IterationMode.Auto) where TOperation : class, ISpanOperation<T>
    {
        // A struct's static constructor only runs on first static access, and naming T as a type argument does not
        // count as one: without this the generated adapter would be missing on the very first lookup.
        _ = ComponentRegistration<T>.Initialized;

        if ((mask & IterationMode.Auto) == IterationMode.Auto)
        {
            foreach (var operation in SimdParallel<T>.Operations)
            {
                if (operation is TOperation { Supported: true } result) return result;
            }

            foreach (var operation in Simd<T>.Operations)
            {
                if (operation is TOperation { Supported: true } result) return result;
            }

            foreach (var operation in Parallel<T>.Operations)
            {
                if (operation is TOperation { Supported: true } result) return result;
            }

            foreach (var operation in Serial<T>.Operations)
            {
                if (operation is TOperation { Supported: true } result) return result;
            }
        }

        if ((mask & IterationMode.Simd) == IterationMode.Simd)
        {
            if ((mask & IterationMode.Parallel) == IterationMode.Parallel)
            {
                foreach (var operation in SimdParallel<T>.Operations)
                {
                    if (operation is TOperation { Supported: true } result) return result;
                }
            }
            else
            {
                foreach (var operation in Simd<T>.Operations)
                {
                    if (operation is TOperation { Supported: true } result) return result;
                }
            }
        }
        else if ((mask & IterationMode.Parallel) == IterationMode.Parallel)
        {
            foreach (var operation in Parallel<T>.Operations)
            {
                if (operation is TOperation { Supported: true } result) return result;
            }
        }

        foreach (var operation in Serial<T>.Operations)
        {
            if (operation is TOperation { Supported: true } result) return result;
        }

        return null;
    }

    /// <summary>
    /// Runs the type initializer of <typeparamref name="T"/> exactly once, on first read.
    /// </summary>
    /// <typeparam name="T">The component type whose adapter should be registered.</typeparam>
    private static class ComponentRegistration<T>
    {
        /// <summary>Reading this field is what forces <typeparamref name="T"/>'s static constructor to run.</summary>
        public static readonly bool Initialized = Initialize();

        private static bool Initialize()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
            return true;
        }
    }
}
