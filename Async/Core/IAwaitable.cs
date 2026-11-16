using System.Runtime.CompilerServices;

namespace Gubbins.Async;

/// <summary>
/// Interface that supports explicit implementation of the async-await keyword for awaitables that produce a result.
/// It is also an async method return type: <c>async IAwaitable&lt;TResult&gt;</c> methods return a value.
/// </summary>
/// <typeparam name="TResult">The type of the awaited result.</typeparam>
[AsyncMethodBuilder(typeof(IAwaitableMethodBuilder<>))]
public interface IAwaitable<out TResult> : IAwaitable
{
    /// <summary>
    /// Gets an awaiter for this awaitable.
    /// </summary>
    /// <returns>An awaiter that produces a value of type <typeparamref name="TResult"/>.</returns>
    new IAwaiter<TResult> GetAwaiter();
}

/// <summary>
/// Interface that supports explicit implementation of the async-await keyword. It is also an async method return
/// type: <c>async IAwaitable</c> methods complete without a result value.
/// </summary>
[AsyncMethodBuilder(typeof(IAwaitableMethodBuilder))]
public interface IAwaitable
{
    /// <summary>
    /// Gets an awaiter for this awaitable.
    /// </summary>
    /// <returns>The awaiter.</returns>
    IAwaiter GetAwaiter();
}