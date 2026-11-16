using System.Runtime.CompilerServices;

namespace Gubbins.Async;

/// <summary>
/// Marked a async-task is completed.
/// </summary>
public interface IAsyncTask : INotifyCompletion
{
    /// <summary>
    /// Identifies the completion of the async operation.(Don't fucking to try to modify it to isCompleted, this is the .NET specification)
    /// </summary>
    bool IsCompleted { get; }
}

/// <summary>
/// Used to identify asynchronous processing objects.
/// </summary>
/// <typeparam name="TResult"></typeparam>
public interface IAwaiter<out TResult> : IAwaiter
{
    /// <summary>
    /// Wait for the completion of the asynchronous operation and return a value.
    /// </summary>
    /// <returns>Awaiter result</returns>
    new TResult GetResult();
}

/// <summary>
/// Used to identify asynchronous processing objects.
/// </summary>
public interface IAwaiter : IAsyncTask
{
    /// <summary>
    /// Wait for the completion of the asynchronous operation.
    /// </summary>
    void GetResult();
}