using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// <inheritdoc cref="ILinkableEventHandler{TNotification, TResult}"/>
/// </summary>
public interface ILinkableEventHandler<TResult> : ILinkableEventHandler<Unit, TResult>;

/// <summary>
/// Linkable event handler interface, which can be used to link multiple handlers sequentially.
/// The result of the previous handler will be passed to the next handler as a parameter, and the final result will be returned to the caller.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ILinkableEventHandler<in TNotification, TResult>
{
    /// <summary>
    /// Handle the notification and return the result. The previous result will be passed as a parameter,
    /// and the final result will be returned to the caller.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="previousResult">The result of the previous handler, which will be passed to the current handler as a parameter.</param>
    /// <returns>The result of handling the notification, which will be passed to the next handler as a parameter.</returns>
    TResult Handle(TNotification notification, TResult previousResult);
}