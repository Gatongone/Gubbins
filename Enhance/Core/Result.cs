#pragma warning disable CS0618 // Type or member is obsolete
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Gubbins.Enhance;

/// <summary>
/// Result type that carries no value
/// </summary>
/// <typeparam name="TError">Error enum type</typeparam>
#if NET11_0_OR_GREATER
[System.Runtime.CompilerServices.Union]
#endif
public readonly struct Result<TError> : Result<TError>.IUnionMembers
{
    /// <summary>
    /// Holds the error info when failed (always present). Null when succeeded
    /// </summary>
    private readonly TError? m_Error;

    /// <summary>
    /// State of the Result, succeeded or failed. When failed, error is always non-null
    /// </summary>
    public bool Valid => m_Error == null; // Zero-constructible struct: no Valid field, so illegal states cannot exist

    object? IUnionMembers.Value => Valid ? new Succeed() : m_Error;

    /// <summary>Tries to get the error; returns true only when the Result failed</summary>
    /// <param name="value">Receives the error when the Result failed</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetValue([NotNullWhen(true)] out TError? value)
    {
        value = m_Error;
        return !Valid;
    }

    /// <summary>Tries to get the success marker; returns true only when the Result succeeded</summary>
    /// <param name="value">Receives the success marker when the Result succeeded</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetValue([NotNullWhen(true)] out Succeed? value)
    {
        value = Valid ? new Succeed() : null;
        return Valid;
    }

    private Result(TError? error)
    {
        if (error is null)
        {
            return;
        }

        m_Error = error;
    }

    /// <summary>Creates a successful Result</summary>
    public static Result<TError> Success() => new();

    /// <summary>Creates a failed Result</summary>
    public static Result<TError> Fail(TError err) => new(err);

    // Err => Result<T>
    /// <summary>Treats an error directly as a failed Result</summary>
    /// <remarks>The Result takes ownership of the error: disposing it returns the error to the pool</remarks>
    public static implicit operator Result<TError>(TError err) => Fail(err);

    public void Unwrap()
    {
        if (!Valid)
        {
            throw m_Error is Exception ex ? throw ex : new PanicException(m_Error!);
        }
    }

    /// <summary>
    /// Runs one of the handlers depending on the state of the Result and returns its result
    /// </summary>
    /// <typeparam name="TResult">Type produced by the handlers</typeparam>
    /// <param name="ok">Invoked when the Result succeeded</param>
    /// <param name="fail">Invoked when the Result failed; receives the error</param>
    /// <returns>The result of the invoked handler</returns>
    public TResult Match<TResult>(Func<TResult> ok, Func<TError, TResult> fail) =>
        Valid ? ok.Invoke() : fail.Invoke(m_Error!);

    /// <summary>
    /// Runs one of the handlers depending on the state of the Result
    /// </summary>
    /// <param name="ok">Invoked when the Result succeeded</param>
    /// <param name="fail">Invoked when the Result failed; receives the error</param>
    public void Match(Action ok, Action<TError> fail)
    {
        if (Valid) ok.Invoke();
        else fail.Invoke(m_Error!);
    }

    /// <summary>Formats the state of the Result for output</summary>
    public override string ToString() => Valid
        ? $"Result<{typeof(TError).Name}>.Success()"
        : $"Result<{typeof(TError).Name}>.Fail({m_Error?.ToString() ?? "null"})";

    /// <summary>Members that the compiler-generated union pattern matching relies on</summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a successful Result from the success marker</summary>
        static Result<TError> Create(Succeed? _) => new();

        /// <summary>Creates a failed Result from an error</summary>
        static Result<TError> Create(TError? value) => new(value);

        /// <summary>Current state: the success marker or the error</summary>
        object? Value { get; }

        /// <summary>Tries to get the error; returns true only when the Result failed</summary>
        bool TryGetValue([NotNullWhen(true)] out TError? value);

        /// <summary>Tries to get the success marker; returns true only when the Result succeeded</summary>
        bool TryGetValue([NotNullWhen(true)] out Succeed? value);
    }
}

/// <summary>Result type that carries a value</summary>
/// <typeparam name="T">Type of the value on success</typeparam>
/// <typeparam name="TError">Error enum type</typeparam>
#if NET11_0_OR_GREATER
[System.Runtime.CompilerServices.Union]
#endif
public readonly struct Result<T, TError> : Result<T, TError>.IUnionMembers
{
    /// <summary>
    /// Holds the value on success, otherwise default
    /// </summary>
    private readonly T? m_Value;

    /// <summary>
    /// Holds the error info when failed (always present). Null when succeeded
    /// </summary>
    private readonly TError? m_Error;

    /// <summary>State of the Result, succeeded or failed. When failed, error is always non-null</summary>
    // Structs are always zero-constructible, so no separate Valid field is used, to prevent illegal states
    public bool Valid => m_Error == null;

    object? IUnionMembers.Value => Valid ? m_Value : m_Error;

    /// <summary>Tries to get the value; returns true only when the Result succeeded</summary>
    /// <param name="result">Receives the value when the Result succeeded (it may be null)</param>
    /// <param name="error">Receives the error when the Result failed</param>
    public bool TryGetValue(out T? result, [NotNullWhen(false)] out TError? error)
    {
        result = m_Value;
        error = m_Error;
        return Valid;
    }

    /// <summary>Tries to get the value; returns true only when the Result succeeded</summary>
    /// <param name="result">Receives the value when the Result succeeded (it may be null)</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetValue(out T? result)
    {
        result = m_Value;
        return Valid;
    }

    /// <summary>Tries to get the error; returns true only when the Result failed</summary>
    /// <param name="error">Receives the error when the Result failed</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetValue([NotNullWhen(true)] out TError? error)
    {
        error = m_Error;
        return !Valid;
    }

    private Result(T? value)
    {
        // Even a null success value is a successful Result, so the state is recorded unconditionally.
        m_Value = value;
    }

    private Result(TError? error)
    {
        if (error is not null)
        {
            m_Value = default;
            m_Error = error;
        }
    }

    /// <summary>Creates a successful Result</summary>
    public static Result<T, TError> Success(T value) => new(value);

    /// <summary>Creates a failed Result</summary>
    public static Result<T, TError> Fail(TError err) => new(err);

    // T => Result<T>
    /// <summary>Treats a success value directly as a successful Result</summary>
    public static implicit operator Result<T, TError>(T value) => Success(value);

    // Err => Result<T>
    /// <summary>Treats an error directly as a failed Result</summary>
    /// <remarks>The Result takes ownership of the error: disposing it returns the error to the pool</remarks>
    public static implicit operator Result<T, TError>(TError err) => Fail(err);

    /// <summary>Formats the state of the Result for output</summary>
    // m_Value is interpolated directly: a null success value must not throw from ToString()
    public override string ToString() => Valid
        ? $"Result<{typeof(T).Name}, {typeof(TError).Name}>.Success({m_Value})"
        : $"Result<{typeof(T).Name}, {typeof(TError).Name}>.Fail({m_Error?.ToString() ?? "null"})";

    public T Unwrap()
    {
        if (!Valid)
        {
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
            throw m_Error! is Exception ex ? throw ex : new PanicException(m_Error!);
#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
        }

        return m_Value!;
    }

    /// <summary>
    /// Returns the value when the Result succeeded, otherwise returns the default value
    /// </summary>
    /// <param name="default">Value used when the Result failed</param>
    /// <remarks>
    /// This call takes the Result over: it is disposed on both paths, and a failed Result's error is
    /// dropped here instead of being handed on.
    /// </remarks>
    public T? ValueOrDefault(T? @default = default)
    {
        return Valid ? m_Value : @default;
    }

    /// <summary>
    /// Returns the value when the Result succeeded; otherwise calls the given function to handle
    /// the error and produce a value
    /// </summary>
    public T ValueOr(Func<TError, T> whenFailed) => Valid ? m_Value! : whenFailed.Invoke(m_Error!);

    /// <summary>
    /// Runs one of the handlers depending on the state of the Result and returns its result
    /// </summary>
    /// <typeparam name="TResult">Type produced by the handlers</typeparam>
    /// <param name="ok">Invoked when the Result succeeded; receives the value</param>
    /// <param name="fail">Invoked when the Result failed; receives the error</param>
    /// <returns>The result of the invoked handler</returns>
    public TResult Match<TResult>(
        Func<T, TResult> ok,
        Func<TError, TResult> fail)
        => Valid ? ok.Invoke(m_Value!) : fail.Invoke(m_Error!);

    /// <summary>
    /// Runs one of the handlers depending on the state of the Result
    /// </summary>
    /// <param name="ok">Invoked when the Result succeeded; receives the value</param>
    /// <param name="fail">Invoked when the Result failed; receives the error</param>
    public void Match(Action<T> ok, Action<TError> fail)
    {
        if (Valid) ok.Invoke(m_Value!);
        else fail.Invoke(m_Error!);
    }

    /// <summary>
    /// Casts Result&lt;T, TError> to Result&lt;TR, TError&gt;
    /// </summary>
    /// <remarks>
    /// On failure the returned Result takes over the error of this one, so this Result must not be
    /// consumed afterwards — the same error would be handed back to the pool twice.
    /// </remarks>
    public Result<TR, TError> Cast<TR>() where TR : T => Valid
        ? Result<TR, TError>.Success((TR)m_Value!)
        : Result<TR, TError>.Fail(m_Error!);

    /// <summary>
    /// Converts Result&lt;T, TError> to Result&lt;TR, TError&gt;
    /// </summary>
    /// <remarks>
    /// On failure the returned Result takes over the error of this one, so this Result must not be
    /// consumed afterwards — the same error would be handed back to the pool twice.
    /// </remarks>
    public Result<TR, TError> Map<TR>(Func<T, TR> mapFunc) => Valid
        ? Result<TR, TError>.Success(mapFunc.Invoke(m_Value))
        : Result<TR, TError>.Fail(m_Error!);

    /// <summary>Members that the compiler-generated union pattern matching relies on</summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a successful Result from the value</summary>
        static Result<T, TError> Create(T? value) => new(value);

        /// <summary>Creates a failed Result from an error</summary>
        static Result<T, TError> Create(TError? value) => new(value);

        /// <summary>Current state: the value or the error</summary>
        object? Value { get; }

        /// <summary>Tries to get the value; returns true only when the Result succeeded</summary>
        bool TryGetValue(out T? result);

        /// <summary>Tries to get the error; returns true only when the Result failed</summary>
        bool TryGetValue([NotNullWhen(true)] out TError? error);
    }
}

public readonly record struct Succeed;

public class PanicException(object obj) : Exception($"Error emitted manually. Error: {obj}");