using Gubbins.Enhance;

namespace Gubbins.Events;

public static class ReactiveEventExtensions
{
    extension<TNotification>(IEventSubscribable<TNotification> source)
    {
        /// <summary>
        /// Forwards only the notifications that satisfy <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate that decides whether a notification is forwarded.</param>
        /// <returns>A source that only forwards matching notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Where(Func<TNotification, bool> predicate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new FilterHandler<TNotification>(handler, predicate));
        }

        /// <summary>
        /// Projects every notification with <paramref name="selector"/>.
        /// </summary>
        /// <param name="selector">The projection applied to each notification.</param>
        /// <typeparam name="TResult">The notification type of the projection.</typeparam>
        /// <returns>A source of projected notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TResult> Select<TResult>(Func<TNotification, TResult> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            return new ReactiveEvent<TNotification, TResult>(
                source,
                handler => new ProjectHandler<TNotification, TResult>(handler, selector));
        }

        /// <summary>
        /// Forwards at most <paramref name="count"/> notifications, then completes and detaches.
        /// </summary>
        /// <param name="count">The maximum number of notifications to forward.</param>
        /// <returns>A source that completes after <paramref name="count"/> notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
        public IEventSubscribable<TNotification> Take(int count)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "The count must not be negative.");

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new TakeHandler<TNotification>(source, handler, count),
                completeOnSubscribe: count == 0);
        }

        /// <summary>
        /// Forwards only the notifications that differ from the previously forwarded one.
        /// </summary>
        /// <param name="comparer">The comparer used to detect duplicates.</param>
        /// <returns>A source without consecutive duplicates.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Distinct(IEqualityComparer<TNotification> comparer)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DistinctByHandler<TNotification, TNotification>(handler, static value => value, comparer));
        }

        /// <summary>
        /// Forwards only the notifications that differ from the previously forwarded one.
        /// </summary>
        /// <returns>A source without consecutive duplicates.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Distinct() => Distinct(source, EqualityComparer<TNotification>.Default);

        /// <summary>
        /// Emits only the last notification when the source completes.
        /// </summary>
        /// <returns>A source that emits the last notification on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Last()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new LastHandler<TNotification>(handler));
        }

        /// <summary>
        /// Emits the first notification and then detaches and completes.
        /// </summary>
        /// <returns>A source that emits the first notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> First()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new FirstHandler<TNotification>(source, handler));
        }

        /// <summary>
        /// Emits the number of notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the notification count on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<int> Count()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, int>(
                source,
                handler => new AggregateHandler<TNotification, int>(handler, 0, static (count, _) => count + 1));
        }

        /// <summary>
        /// Skips the first <paramref name="count"/> notifications.
        /// </summary>
        /// <param name="count">The number of notifications to skip.</param>
        /// <returns>A source without the first notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
        public IEventSubscribable<TNotification> Skip(int count)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "The count must not be negative.");

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new SkipHandler<TNotification>(handler, count));
        }

        /// <summary>
        /// Skips notifications while <paramref name="predicate"/> holds.
        /// </summary>
        /// <param name="predicate">The predicate that decides whether a notification is skipped.</param>
        /// <returns>A source without the leading matching notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> SkipWhile(Func<TNotification, bool> predicate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new SkipWhileHandler<TNotification>(handler, predicate));
        }

        /// <summary>
        /// Forwards notifications while <paramref name="predicate"/> holds.
        /// </summary>
        /// <param name="predicate">The predicate that decides whether a notification is forwarded.</param>
        /// <returns>A source that completes at the first non-matching notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> TakeWhile(Func<TNotification, bool> predicate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new TakeWhileHandler<TNotification>(source, handler, predicate));
        }

        /// <summary>
        /// Emits the running aggregate of every notification.
        /// </summary>
        /// <param name="accumulate">The accumulator applied to the running value and the notification.</param>
        /// <returns>A source of running aggregates.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Scan(Func<TNotification, TNotification, TNotification> accumulate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (accumulate is null) throw new ArgumentNullException(nameof(accumulate));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ScanHandler<TNotification>(handler, accumulate));
        }

        /// <summary>
        /// Emits the running aggregate of every notification, starting from <paramref name="seed"/>.
        /// </summary>
        /// <param name="seed">The initial running value.</param>
        /// <param name="accumulate">The accumulator applied to the running value and the notification.</param>
        /// <returns>A source of running aggregates.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Scan(
            TNotification seed,
            Func<TNotification, TNotification, TNotification> accumulate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (accumulate is null) throw new ArgumentNullException(nameof(accumulate));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ScanSeedHandler<TNotification>(handler, seed, accumulate));
        }

        /// <summary>
        /// Emits every notification together with the previous one.
        /// </summary>
        /// <returns>A source of consecutive pairs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<(TNotification, TNotification)> Pairwise()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, (TNotification, TNotification)>(
                source,
                handler => new PairwiseHandler<TNotification>(handler));
        }

        /// <summary>
        /// Emits notifications in fixed-size arrays.
        /// </summary>
        /// <param name="count">The maximum size of a batch.</param>
        /// <returns>A source of batches.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is not positive.</exception>
        public IEventSubscribable<TNotification[]> Buffer(int count)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "The count must be positive.");

            return new ReactiveEvent<TNotification, TNotification[]>(
                source,
                handler => new BufferHandler<TNotification>(handler, count));
        }

        /// <summary>
        /// Emits <paramref name="defaultValue"/> when the source completes without a notification.
        /// </summary>
        /// <param name="defaultValue">The value emitted for an empty source.</param>
        /// <returns>A source that never completes empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> DefaultIfEmpty(TNotification defaultValue)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DefaultIfEmptyHandler<TNotification>(handler, defaultValue));
        }

        /// <summary>
        /// Suppresses every notification and forwards only the completion.
        /// </summary>
        /// <returns>A source without notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> IgnoreElements()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new IgnoreElementsHandler<TNotification>(handler));
        }

        /// <summary>
        /// Folds every notification into a value emitted when the source completes.
        /// </summary>
        /// <param name="seed">The initial aggregate.</param>
        /// <param name="accumulate">The accumulator applied to the aggregate and the notification.</param>
        /// <typeparam name="TResult">The aggregate type.</typeparam>
        /// <returns>A source that emits the aggregate on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TResult> Aggregate<TResult>(
            TResult seed,
            Func<TResult, TNotification, TResult> accumulate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (accumulate is null) throw new ArgumentNullException(nameof(accumulate));

            return new ReactiveEvent<TNotification, TResult>(
                source,
                handler => new AggregateHandler<TNotification, TResult>(handler, seed, accumulate));
        }

        /// <summary>
        /// Emits whether the source has any notification.
        /// </summary>
        /// <returns>A source that emits the answer on completion or at the first notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<bool> Any()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, bool>(
                source,
                handler => new AnyHandler<TNotification>(source, handler, static _ => true));
        }

        /// <summary>
        /// Emits whether any notification satisfies <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to test.</param>
        /// <returns>A source that emits the answer on completion or at the first match.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<bool> Any(Func<TNotification, bool> predicate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            return new ReactiveEvent<TNotification, bool>(
                source,
                handler => new AnyHandler<TNotification>(source, handler, predicate));
        }

        /// <summary>
        /// Emits whether every notification satisfies <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to test.</param>
        /// <returns>A source that emits the answer on completion or at the first mismatch.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<bool> All(Func<TNotification, bool> predicate)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            return new ReactiveEvent<TNotification, bool>(
                source,
                handler => new AllHandler<TNotification>(source, handler, predicate));
        }

        /// <summary>
        /// Emits whether the source contains <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to look for.</param>
        /// <returns>A source that emits the answer on completion or at the first match.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<bool> Contains(TNotification value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, bool>(
                source,
                handler => new AnyHandler<TNotification>(
                    source,
                    handler,
                    notification => EqualityComparer<TNotification>.Default.Equals(notification, value)));
        }

        /// <summary>
        /// Collects every notification and emits the list when the source completes.
        /// </summary>
        /// <returns>A source that emits all notifications as a list.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<IReadOnlyList<TNotification>> ToList()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, IReadOnlyList<TNotification>>(
                source,
                handler => new ToListHandler<TNotification>(handler));
        }

        /// <summary>
        /// Collects every notification and emits the array when the source completes.
        /// </summary>
        /// <returns>A source that emits all notifications as an array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification[]> ToArray()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification[]>(
                source,
                handler => new ToArrayHandler<TNotification>(handler));
        }

        /// <summary>
        /// Subscribes <paramref name="second"/> after the source completes successfully.
        /// </summary>
        /// <param name="second">The source subscribed after the first one.</param>
        /// <returns>A source that forwards both sources in sequence.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Concat(IEventSubscribable<TNotification> second)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (second is null) throw new ArgumentNullException(nameof(second));

            return new ConcatEvent<TNotification>(source, second);
        }

        /// <summary>
        /// Merges the source with <paramref name="other"/>, completing when both complete.
        /// </summary>
        /// <param name="other">The source merged with this one.</param>
        /// <returns>A source that forwards both inputs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Merge(IEventSubscribable<TNotification> other)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (other is null) throw new ArgumentNullException(nameof(other));

            return new MergeEvent<TNotification>(source, other);
        }

        /// <summary>
        /// Invokes <paramref name="onFinally"/> once when the stream completes or is detached.
        /// </summary>
        /// <param name="onFinally">The action invoked on completion or detachment.</param>
        /// <returns>A source that invokes the action on termination.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Finally(Action onFinally)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onFinally is null) throw new ArgumentNullException(nameof(onFinally));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new FinallyHandler<TNotification>(handler, onFinally));
        }

        /// <summary>
        /// Switches to <paramref name="fallback"/> when the source terminates with an error.
        /// </summary>
        /// <param name="fallback">The source subscribed after an error.</param>
        /// <returns>A source that recovers from errors with the fallback.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Catch(IEventSubscribable<TNotification> fallback)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (fallback is null) throw new ArgumentNullException(nameof(fallback));

            return new CatchEvent<TNotification>(source, _ => fallback);
        }

        /// <summary>
        /// Switches to the source produced by <paramref name="fallbackFactory"/> when the source errors.
        /// </summary>
        /// <param name="fallbackFactory">The factory that builds the fallback from the error.</param>
        /// <returns>A source that recovers from errors with the produced fallback.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Catch(Func<Exception, IEventSubscribable<TNotification>> fallbackFactory)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (fallbackFactory is null) throw new ArgumentNullException(nameof(fallbackFactory));

            return new CatchEvent<TNotification>(source, error => fallbackFactory(error));
        }

        /// <summary>
        /// Ignores the error and completes successfully.
        /// </summary>
        /// <returns>A source that never terminates with an error.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> CatchAndIgnore()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new CatchEvent<TNotification>(source, _ => null);
        }

        /// <summary>
        /// Resubscribes the source up to <paramref name="retryCount"/> times after an error.
        /// </summary>
        /// <param name="retryCount">The number of retries after the first subscription.</param>
        /// <returns>A source that retries on error.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retryCount"/> is negative.</exception>
        public IEventSubscribable<TNotification> Retry(int retryCount)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "The retry count must not be negative.");

            return new ResubscribeEvent<TNotification>(source, retryCount, false);
        }

        /// <summary>
        /// Resubscribes the source up to <paramref name="repeatCount"/> times after a successful completion.
        /// </summary>
        /// <param name="repeatCount">The number of repeats after the first subscription.</param>
        /// <returns>A source that repeats on successful completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="repeatCount"/> is negative.</exception>
        public IEventSubscribable<TNotification> Repeat(int repeatCount)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (repeatCount < 0) throw new ArgumentOutOfRangeException(nameof(repeatCount), repeatCount, "The repeat count must not be negative.");

            return new ResubscribeEvent<TNotification>(source, repeatCount, true);
        }

        /// <summary>
        /// Completes when <paramref name="trigger"/> emits or completes.
        /// </summary>
        /// <param name="trigger">The trigger source.</param>
        /// <typeparam name="TTrigger">The trigger notification type.</typeparam>
        /// <returns>A source that completes when the trigger fires.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> TakeUntil<TTrigger>(IEventSubscribable<TTrigger> trigger)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (trigger is null) throw new ArgumentNullException(nameof(trigger));

            return new TakeUntilEvent<TNotification, TTrigger>(source, trigger);
        }

        /// <summary>
        /// Completes when <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A source that completes when the token is cancelled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> TakeUntil(CancellationToken cancellationToken)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new TakeUntilTokenEvent<TNotification>(source, cancellationToken);
        }

        /// <summary>
        /// Forwards only the notifications whose key was not seen before.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source without duplicate keys.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DistinctBy<TKey>(Func<TNotification, TKey> keySelector)
            => DistinctBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Forwards only the notifications whose key was not seen before.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="comparer">The key comparer.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source without duplicate keys.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DistinctBy<TKey>(
            Func<TNotification, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DistinctByHandler<TNotification, TKey>(handler, keySelector, comparer));
        }

        /// <summary>
        /// Emits only the last <paramref name="count"/> notifications on completion.
        /// </summary>
        /// <param name="count">The number of trailing notifications.</param>
        /// <returns>A source of the trailing notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
        public IEventSubscribable<TNotification> TakeLast(int count)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "The count must not be negative.");

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new TakeLastHandler<TNotification>(handler, count));
        }

        /// <summary>
        /// Drops the last <paramref name="count"/> notifications.
        /// </summary>
        /// <param name="count">The number of trailing notifications to drop.</param>
        /// <returns>A source without the trailing notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
        public IEventSubscribable<TNotification> SkipLast(int count)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "The count must not be negative.");

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new SkipLastHandler<TNotification>(handler, count));
        }

        /// <summary>
        /// Skips notifications until <paramref name="trigger"/> emits.
        /// </summary>
        /// <param name="trigger">The trigger source.</param>
        /// <typeparam name="TTrigger">The trigger notification type.</typeparam>
        /// <returns>A source that starts forwarding after the trigger fires.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> SkipUntil<TTrigger>(IEventSubscribable<TTrigger> trigger)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (trigger is null) throw new ArgumentNullException(nameof(trigger));

            return new SkipUntilEvent<TNotification, TTrigger>(source, trigger);
        }

        /// <summary>
        /// Skips notifications until <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="cancellationToken">The gate token.</param>
        /// <returns>A source that starts forwarding after cancellation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> SkipUntil(CancellationToken cancellationToken)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new SkipUntilTokenEvent<TNotification>(source, cancellationToken);
        }

        /// <summary>
        /// Combines every notification with the latest notification of <paramref name="second"/>.
        /// </summary>
        /// <param name="second">The source that provides the latest value.</param>
        /// <param name="selector">The combination projection.</param>
        /// <typeparam name="TSecond">The second notification type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>A source of the latest combinations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TResult> WithLatestFrom<TSecond, TResult>(
            IEventSubscribable<TSecond> second,
            Func<TNotification, TSecond, TResult> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            return new WithLatestFromEvent<TNotification, TSecond, TResult>(source, second, selector);
        }

        /// <summary>
        /// Creates a connectable event that forwards the source after <see cref="IConnectableEvent{TNotification}.Connect"/>.
        /// </summary>
        /// <returns>A connectable event.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IConnectableEvent<TNotification> Multicast()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ConnectableEvent<TNotification>(source);
        }

        /// <summary>
        /// Shares one source subscription among all subscribers.
        /// </summary>
        /// <returns>A shared event.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Share()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new SharedEvent<TNotification>(source);
        }

        /// <summary>
        /// Emits <paramref name="value"/> before forwarding the source.
        /// </summary>
        /// <param name="value">The value emitted first.</param>
        /// <returns>A source with the extra leading value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Prepend(TNotification value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new PrependEvent<TNotification>(source, value);
        }

        /// <summary>
        /// Emits <paramref name="value"/> after the source completes successfully.
        /// </summary>
        /// <param name="value">The value emitted last.</param>
        /// <returns>A source with the extra trailing value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Append(TNotification value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new AppendHandler<TNotification>(handler, value));
        }

        /// <summary>
        /// Flattens the inner sources produced by <paramref name="selector"/>.
        /// </summary>
        /// <param name="selector">The selector that produces an inner source for each notification.</param>
        /// <typeparam name="TResult">The notification type of the inner sources.</typeparam>
        /// <returns>A source of the flattened notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TResult> SelectMany<TResult>(Func<TNotification, IEventSubscribable<TResult>> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            return new SelectManyEvent<TNotification, TResult>(source, selector);
        }

        /// <summary>
        /// Invokes <paramref name="onNext"/> before forwarding each notification.
        /// </summary>
        /// <param name="onNext">The side effect for each notification.</param>
        /// <returns>A source that forwards the notifications unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Do(Action<TNotification> onNext)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onNext is null) throw new ArgumentNullException(nameof(onNext));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DoHandler<TNotification>(handler, onNext));
        }

        /// <summary>
        /// Invokes <paramref name="onSubscribe"/> when the source is subscribed.
        /// </summary>
        /// <param name="onSubscribe">The subscription side effect.</param>
        /// <returns>A source that forwards the notifications unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DoOnSubscribe(Action onSubscribe)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onSubscribe is null) throw new ArgumentNullException(nameof(onSubscribe));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler =>
                {
                    onSubscribe();
                    return new IdentityHandler<TNotification>(handler);
                });
        }

        /// <summary>
        /// Invokes <paramref name="onComplete"/> when the source completes.
        /// </summary>
        /// <param name="onComplete">The completion side effect.</param>
        /// <returns>A source that forwards the notifications unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DoOnComplete(Action<Result<TNotification, Exception>> onComplete)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onComplete is null) throw new ArgumentNullException(nameof(onComplete));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DoOnCompleteHandler<TNotification>(handler, onComplete));
        }

        /// <summary>
        /// Invokes <paramref name="onDispose"/> when the subscription is detached.
        /// </summary>
        /// <param name="onDispose">The detach side effect.</param>
        /// <returns>A source that forwards the notifications unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DoOnDispose(Action onDispose)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onDispose is null) throw new ArgumentNullException(nameof(onDispose));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DoOnDisposeHandler<TNotification>(handler, onDispose));
        }

        /// <summary>
        /// Invokes <paramref name="onError"/> when the source fails.
        /// </summary>
        /// <param name="onError">The error side effect.</param>
        /// <returns>A source that forwards the error unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DoOnError(Action<Exception> onError)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onError is null) throw new ArgumentNullException(nameof(onError));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DoOnErrorHandler<TNotification>(handler, onError));
        }

        /// <summary>
        /// Emits each notification together with its zero-based index.
        /// </summary>
        /// <returns>A source of indexed notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<(int Index, TNotification Value)> Index()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, (int, TNotification)>(
                source,
                handler => new IndexHandler<TNotification>(handler));
        }

        /// <summary>
        /// Emits the notification with the smallest key on completion.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source of the minimum notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> MinBy<TKey>(Func<TNotification, TKey> keySelector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ExtremumByHandler<TNotification, TKey>(handler, keySelector, false));
        }

        /// <summary>
        /// Emits the notification with the largest key on completion.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source of the maximum notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> MaxBy<TKey>(Func<TNotification, TKey> keySelector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ExtremumByHandler<TNotification, TKey>(handler, keySelector, true));
        }

        /// <summary>
        /// Emits notifications in fixed-size arrays.
        /// </summary>
        /// <param name="count">The maximum size of a batch.</param>
        /// <returns>A source of batches.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is not positive.</exception>
        public IEventSubscribable<TNotification[]> Chunk(int count) => source.Buffer(count);

        /// <summary>
        /// Converts the source to a task that completes with the last notification when the source completes.
        /// </summary>
        ///     /// <param name="cancellationToken">The token that cancels the wait and detaches the subscription.</param>
        /// <typeparam name="TNotification">The notification type.</typeparam>
        /// <returns>A task that completes with the last notification, faults on error or cancels on request.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public Task<TNotification> ToTask(
            CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var completion = new TaskCompletionSource<TNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
                return completion.Task;
            }

            var handler = new TaskHandler<TNotification>(completion);
            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(
                    static state =>
                    {
                        var (eventSource, taskHandler, taskCompletion) = ((IEventSubscribable<TNotification>, TaskHandler<TNotification>, TaskCompletionSource<TNotification>))state!;
                        eventSource.Unsubscribe(taskHandler);
                        taskCompletion.TrySetCanceled();
                    },
                    (source, handler, completion));

                _ = completion.Task.ContinueWith(
                    static (_, state) => ((CancellationTokenRegistration)state!).Dispose(),
                    registration,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            source.Subscribe(handler);
            return completion.Task;
        }

        /// <summary>
        /// Pairs the notifications of two sources by index.
        /// </summary>
        /// <param name="second">The second source.</param>
        /// <param name="selector">The pair projection.</param>
        /// <typeparam name="TSecond">The second notification type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>A source of paired notifications.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TResult> Zip<TSecond, TResult>(
            IEventSubscribable<TSecond> second,
            Func<TNotification, TSecond, TResult> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            return new ZipEvent<TNotification, TSecond, TResult>(source, second, selector);
        }

        /// <summary>
        /// Emits the latest combination of two sources.
        /// </summary>
        /// <param name="second">The second source.</param>
        /// <param name="selector">The combination projection.</param>
        /// <typeparam name="TSecond">The second notification type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>A source of the latest combinations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TResult> CombineLatest<TSecond, TResult>(
            IEventSubscribable<TSecond> second,
            Func<TNotification, TSecond, TResult> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            return new CombineLatestEvent<TNotification, TSecond, TResult>(source, second, selector);
        }

        /// <summary>
        /// Emits the notification at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>A source that emits the indexed notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
        public IEventSubscribable<TNotification> ElementAt(int index)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "The index must not be negative.");

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ElementAtHandler<TNotification>(source, handler, index, default, false));
        }

        /// <summary>
        /// Emits the notification at <paramref name="index"/>, or <paramref name="defaultValue"/> when the source is shorter.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <param name="defaultValue">The value emitted when the index is out of range.</param>
        /// <returns>A source that emits the indexed notification or the default.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
        public IEventSubscribable<TNotification> ElementAtOrDefault(int index, TNotification defaultValue)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "The index must not be negative.");

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ElementAtHandler<TNotification>(source, handler, index, defaultValue, true));
        }

        /// <summary>
        /// Emits the only notification, failing when there is not exactly one.
        /// </summary>
        /// <returns>A source that emits the single notification.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Single()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new SingleHandler<TNotification>(source, handler, default, false));
        }

        /// <summary>
        /// Emits the only notification, or <paramref name="defaultValue"/> when the source is empty.
        /// </summary>
        /// <param name="defaultValue">The value emitted when the source is empty.</param>
        /// <returns>A source that emits the single notification or the default.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> SingleOrDefault(TNotification defaultValue)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new SingleHandler<TNotification>(source, handler, defaultValue, true));
        }

        /// <summary>
        /// Emits the first notification, or <paramref name="defaultValue"/> when the source is empty.
        /// </summary>
        /// <param name="defaultValue">The value emitted when the source is empty.</param>
        /// <returns>A source that emits the first notification or the default.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> FirstOrDefault(TNotification defaultValue)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new FirstOrDefaultHandler<TNotification>(source, handler, defaultValue));
        }

        /// <summary>
        /// Emits the last notification, or <paramref name="defaultValue"/> when the source is empty.
        /// </summary>
        /// <param name="defaultValue">The value emitted when the source is empty.</param>
        /// <returns>A source that emits the last notification or the default.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> LastOrDefault(TNotification defaultValue)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new LastOrDefaultHandler<TNotification>(handler, defaultValue));
        }

        /// <summary>
        /// Collects every notification into a set on completion.
        /// </summary>
        /// <returns>A source that emits the set on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<HashSet<TNotification>> ToHashSet()
            => ToHashSet(source, EqualityComparer<TNotification>.Default);

        /// <summary>
        /// Collects every notification into a set on completion.
        /// </summary>
        /// <param name="comparer">The set comparer.</param>
        /// <returns>A source that emits the set on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<HashSet<TNotification>> ToHashSet(IEqualityComparer<TNotification> comparer)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new ReactiveEvent<TNotification, HashSet<TNotification>>(
                source,
                handler => new ToHashSetHandler<TNotification>(handler, comparer));
        }

        /// <summary>
        /// Runs <paramref name="onNext"/> for every notification and completes with the source.
        /// </summary>
        /// <param name="onNext">The side effect for each notification.</param>
        /// <param name="cancellationToken">The token that cancels the wait and detaches the subscription.</param>
        /// <returns>A task that completes with the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public Task ForEachAsync(Action<TNotification> onNext, CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (onNext is null) throw new ArgumentNullException(nameof(onNext));

            var completion = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
                return completion.Task;
            }

            var handler = new ForEachHandler<TNotification>(source, onNext, completion);
            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(
                    static state =>
                    {
                        var (eventSource, taskHandler, taskCompletion) = ((IEventSubscribable<TNotification>, ForEachHandler<TNotification>, TaskCompletionSource<Unit>))state!;
                        eventSource.Unsubscribe(taskHandler);
                        taskCompletion.TrySetCanceled();
                    },
                    (source, handler, completion));

                _ = completion.Task.ContinueWith(
                    static (_, state) => ((CancellationTokenRegistration)state!).Dispose(),
                    registration,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            source.Subscribe(handler);
            return completion.Task;
        }

        /// <summary>
        /// Collects every notification into a dictionary on completion.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source that emits the dictionary on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<Dictionary<TKey, TNotification>> ToDictionary<TKey>(Func<TNotification, TKey> keySelector)
            where TKey : notnull
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

            return new ReactiveEvent<TNotification, Dictionary<TKey, TNotification>>(
                source,
                handler => new ToDictionaryHandler<TNotification, TKey, TNotification>(
                    handler,
                    keySelector,
                    static value => value,
                    EqualityComparer<TKey>.Default));
        }

        /// <summary>
        /// Collects every notification into a dictionary on completion.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="elementSelector">The element selector.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <returns>A source that emits the dictionary on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<Dictionary<TKey, TElement>> ToDictionary<TKey, TElement>(
            Func<TNotification, TKey> keySelector,
            Func<TNotification, TElement> elementSelector)
            where TKey : notnull
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null) throw new ArgumentNullException(nameof(elementSelector));

            return new ReactiveEvent<TNotification, Dictionary<TKey, TElement>>(
                source,
                handler => new ToDictionaryHandler<TNotification, TKey, TElement>(
                    handler,
                    keySelector,
                    elementSelector,
                    EqualityComparer<TKey>.Default));
        }

        /// <summary>
        /// Collects every notification into a dictionary on completion.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="elementSelector">The element selector.</param>
        /// <param name="comparer">The key comparer.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <returns>A source that emits the dictionary on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<Dictionary<TKey, TElement>> ToDictionary<TKey, TElement>(
            Func<TNotification, TKey> keySelector,
            Func<TNotification, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
            where TKey : notnull
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null) throw new ArgumentNullException(nameof(elementSelector));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new ReactiveEvent<TNotification, Dictionary<TKey, TElement>>(
                source,
                handler => new ToDictionaryHandler<TNotification, TKey, TElement>(
                    handler,
                    keySelector,
                    elementSelector,
                    comparer));
        }

        /// <summary>
        /// Forwards only the notifications whose selected key differs from the previous one.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source without consecutive duplicate keys.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DistinctUntilChangedBy<TKey>(Func<TNotification, TKey> keySelector)
            => DistinctUntilChangedBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Forwards only the notifications whose selected key differs from the previous one.
        /// </summary>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="comparer">The key comparer.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <returns>A source without consecutive duplicate keys.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DistinctUntilChangedBy<TKey>(
            Func<TNotification, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DistinctUntilChangedByHandler<TNotification, TKey>(handler, keySelector, comparer));
        }

        /// <summary>
        /// Compares two sources element by element and emits the answer when both complete.
        /// </summary>
        /// <param name="other">The source to compare with.</param>
        /// <returns>A source that emits whether the sequences are equal.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<bool> SequenceEqual(IEventSubscribable<TNotification> other)
            => SequenceEqual(source, other, EqualityComparer<TNotification>.Default);

        /// <summary>
        /// Compares two sources element by element and emits the answer when both complete.
        /// </summary>
        /// <param name="other">The source to compare with.</param>
        /// <param name="comparer">The element comparer.</param>
        /// <returns>A source that emits whether the sequences are equal.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<bool> SequenceEqual(
            IEventSubscribable<TNotification> other,
            IEqualityComparer<TNotification> comparer)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (other is null) throw new ArgumentNullException(nameof(other));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new SequenceEqualEvent<TNotification>(source, other, comparer);
        }

        /// <summary>
        /// Forwards the first notification and suppresses the following ones inside <paramref name="window"/>.
        /// </summary>
        /// <param name="window">The suppression window after a forwarded notification.</param>
        /// <returns>A throttled source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> ThrottleFirst(TimeSpan window)
            => ThrottleFirst(source, window, SystemTimeProvider.Default);

        /// <summary>
        /// Forwards the first notification and suppresses the following ones inside <paramref name="window"/>.
        /// </summary>
        /// <param name="window">The suppression window after a forwarded notification.</param>
        /// <param name="timeProvider">The clock used to measure the window.</param>
        /// <returns>A throttled source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> ThrottleFirst(TimeSpan window, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ThrottleFirstHandler<TNotification>(timeProvider, window, handler));
        }

        /// <summary>
        /// Delays every notification and the completion by <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The delay applied to each notification.</param>
        /// <returns>A delayed source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Delay(TimeSpan dueTime)
            => Delay(source, dueTime, SystemTimeProvider.Default);

        /// <summary>
        /// Delays every notification and the completion by <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The delay applied to each notification.</param>
        /// <param name="timeProvider">The scheduler used to delay.</param>
        /// <returns>A delayed source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Delay(TimeSpan dueTime, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DelayHandler<TNotification>(timeProvider, dueTime, handler));
        }

        /// <summary>
        /// Emits a notification only after the source stays quiet for <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The quiet period required before emitting.</param>
        /// <returns>A debounced source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Debounce(TimeSpan dueTime)
            => Debounce(source, dueTime, SystemTimeProvider.Default);

        /// <summary>
        /// Emits a notification only after the source stays quiet for <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The quiet period required before emitting.</param>
        /// <param name="timeProvider">The scheduler used to measure the quiet period.</param>
        /// <returns>A debounced source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Debounce(TimeSpan dueTime, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DebounceHandler<TNotification>(timeProvider, dueTime, handler));
        }

        /// <summary>
        /// Emits the latest notification on every <paramref name="period"/>.
        /// </summary>
        /// <param name="period">The sampling period.</param>
        /// <returns>A sampled source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Sample(TimeSpan period)
            => Sample(source, period, SystemTimeProvider.Default);

        /// <summary>
        /// Emits the latest notification on every <paramref name="period"/>.
        /// </summary>
        /// <param name="period">The sampling period.</param>
        /// <param name="timeProvider">The scheduler used to sample.</param>
        /// <returns>A sampled source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Sample(TimeSpan period, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new SampleHandler<TNotification>(timeProvider, period, handler));
        }

        /// <summary>
        /// Fails when the source does not emit within <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The maximum silence before failing.</param>
        /// <returns>A source that fails on timeout.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Timeout(TimeSpan dueTime)
            => Timeout(source, dueTime, SystemTimeProvider.Default);

        /// <summary>
        /// Fails when the source does not emit within <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The maximum silence before failing.</param>
        /// <param name="timeProvider">The scheduler used to measure the timeout.</param>
        /// <returns>A source that fails on timeout.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> Timeout(TimeSpan dueTime, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler =>
                {
                    var timeout = new TimeoutHandler<TNotification>(source, timeProvider, dueTime, handler);
                    timeout.Start();
                    return timeout;
                });
        }

        /// <summary>
        /// Emits the latest notification at the end of each <paramref name="window"/>.
        /// </summary>
        /// <param name="window">The window length.</param>
        /// <returns>A throttled source that emits the last value per window.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> ThrottleLast(TimeSpan window)
            => ThrottleLast(source, window, SystemTimeProvider.Default);

        /// <summary>
        /// Emits the latest notification at the end of each <paramref name="window"/>.
        /// </summary>
        /// <param name="window">The window length.</param>
        /// <param name="timeProvider">The scheduler used to measure the window.</param>
        /// <returns>A throttled source that emits the last value per window.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> ThrottleLast(TimeSpan window, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ThrottleLastHandler<TNotification>(timeProvider, window, handler));
        }

        /// <summary>
        /// Subscribes to the source after <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The subscription delay.</param>
        /// <returns>A source that subscribes late.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> DelaySubscription(TimeSpan dueTime)
            => DelaySubscription(source, dueTime, SystemTimeProvider.Default);

        /// <summary>
        /// Subscribes to the source after <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The subscription delay.</param>
        /// <param name="timeProvider">The scheduler used to delay the subscription.</param>
        /// <returns>A source that subscribes late.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DelaySubscription(TimeSpan dueTime, ITimeProvider timeProvider)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (timeProvider is null) throw new ArgumentNullException(nameof(timeProvider));

            return new DelaySubscriptionEvent<TNotification>(source, timeProvider, dueTime);
        }

        /// <summary>
        /// Forwards only the notifications that differ from the previous one.
        /// </summary>
        /// <returns>A source without consecutive duplicates.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> DistinctUntilChanged()
            => DistinctUntilChanged(source, EqualityComparer<TNotification>.Default);

        /// <summary>
        /// Forwards only the notifications that differ from the previous one.
        /// </summary>
        /// <param name="comparer">The comparer used to detect duplicates.</param>
        /// <returns>A source without consecutive duplicates.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public IEventSubscribable<TNotification> DistinctUntilChanged(IEqualityComparer<TNotification> comparer)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (comparer is null) throw new ArgumentNullException(nameof(comparer));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new DistinctHandler<TNotification>(handler, comparer));
        }
    }

    extension(IEventSubscribable<int> source)
    {
        /// <summary>
        /// Emits the sum of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the sum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<int> Sum()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<int, int>(
                source,
                handler => new AggregateHandler<int, int>(handler, 0, static (sum, value) => sum + value));
        }

        /// <summary>
        /// Emits the average of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the average on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<double> Average()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<int, double>(
                source,
                handler => new AverageHandler<int>(handler, static value => value));
        }
    }

    extension(IEventSubscribable<long> source)
    {
        /// <summary>
        /// Emits the sum of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the sum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<long> Sum()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<long, long>(
                source,
                handler => new AggregateHandler<long, long>(handler, 0, static (sum, value) => sum + value));
        }

        /// <summary>
        /// Emits the average of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the average on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<double> Average()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<long, double>(
                source,
                handler => new AverageHandler<long>(handler, static value => value));
        }
    }

    extension(IEventSubscribable<float> source)
    {
        /// <summary>
        /// Emits the sum of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the sum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<float> Sum()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<float, float>(
                source,
                handler => new AggregateHandler<float, float>(handler, 0, static (sum, value) => sum + value));
        }

        /// <summary>
        /// Emits the average of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the average on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<double> Average()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<float, double>(
                source,
                handler => new AverageHandler<float>(handler, static value => value));
        }
    }

    extension(IEventSubscribable<double> source)
    {
        /// <summary>
        /// Emits the sum of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the sum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<double> Sum()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<double, double>(
                source,
                handler => new AggregateHandler<double, double>(handler, 0, static (sum, value) => sum + value));
        }

        /// <summary>
        /// Emits the average of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the average on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<double> Average()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<double, double>(
                source,
                handler => new AverageHandler<double>(handler, static value => value));
        }
    }

    extension(IEventSubscribable<decimal> source)
    {
        /// <summary>
        /// Emits the sum of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the sum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<decimal> Sum()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<decimal, decimal>(
                source,
                handler => new AggregateHandler<decimal, decimal>(handler, 0, static (sum, value) => sum + value));
        }

        /// <summary>
        /// Emits the average of the notifications when the source completes.
        /// </summary>
        /// <returns>A source that emits the average on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<double> Average()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<decimal, double>(
                source,
                handler => new AverageHandler<decimal>(handler, static value => (double)value));
        }
    }

    extension<TNotification>(IEventSubscribable<IEventSubscribable<TNotification>> source)
    {
        /// <summary>
        /// Switches to the latest inner source emitted by the outer source.
        /// </summary>
        /// <returns>A source that forwards the latest inner source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Switch()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new SwitchEvent<TNotification>(source);
        }
    }

    extension<TNotification>(IEventSubscribable<TNotification> source) where TNotification : IComparable<TNotification>
    {
        /// <summary>
        /// Emits the smallest notification when the source completes.
        /// </summary>
        /// <returns>A source that emits the minimum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Min()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ExtremumHandler<TNotification>(handler, false));
        }

        /// <summary>
        /// Emits the largest notification when the source completes.
        /// </summary>
        /// <returns>A source that emits the maximum on completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public IEventSubscribable<TNotification> Max()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ReactiveEvent<TNotification, TNotification>(
                source,
                handler => new ExtremumHandler<TNotification>(handler, true));
        }
    }
}