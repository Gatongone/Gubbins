using Gubbins.Enhance;

namespace Gubbins.Events.Tests;

[TestFixture]
public class EventReactiveTests
{
    [Test]
    public void Where_ForwardsOnlyMatchingNotifications()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Where(static value => value % 2 == 0).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.Broadcast(4);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2, 4 }));
    }

    [Test]
    public void Select_ProjectsNotifications()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<string>();
        bus.Select(static value => value.ToString()).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { "1", "2" }));
    }

    [Test]
    public void WhereThenSelect_Composes()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Where(static value => value % 2 == 0).Select(static value => value * 10).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public void Distinct_RemovesAllDuplicates()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Distinct().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(2);
        bus.Broadcast(1);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Take_ForwardsCountThenCompletes()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Take(2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void TakeZero_CompletesOnSubscribe()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Take(0).Subscribe(handler);

        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void BusCompletion_ForwardsToStreamSubscriber()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Where(static _ => true).Subscribe(handler);

        bus.OnComplete(default);

        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(handler.Result.Valid, Is.True);
    }

    [Test]
    public void BusError_ForwardsTheSameError()
    {
        var bus = new Event<int>();
        var error = new InvalidOperationException("boom");
        var handler = new RecordingHandler<int>();
        bus.Where(static _ => true).Subscribe(handler);

        bus.OnComplete(error);

        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(GetError(handler.Result), Is.SameAs(error));
    }

    [Test]
    public void Select_MapsTheCompletionValue()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<string>();
        bus.Select(static value => value.ToString()).Subscribe(handler);

        bus.OnComplete(Result<int, Exception>.Success(7));

        Assert.That(handler.Result.TryGetValue(out var value, out _), Is.True);
        Assert.That(value, Is.EqualTo("7"));
    }

    [Test]
    public void Unsubscribe_DetachesTheSubscriber()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        var stream = bus.Where(static _ => true);
        stream.Subscribe(handler);

        bus.Broadcast(1);
        stream.Unsubscribe(handler);
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Last_EmitsLastNotificationOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Last().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 3 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void First_EmitsFirstNotificationThenCompletes()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.First().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Count_EmitsCountOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Count().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void Sum_EmitsSumOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Sum().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 6 }));
    }

    [Test]
    public void Average_EmitsAverageOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<double>();
        bus.Average().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2.0 }));
    }

    [Test]
    public void MinAndMax_EmitExtremumOnCompletion()
    {
        var bus = new Event<int>();
        var min = new RecordingHandler<int>();
        var max = new RecordingHandler<int>();
        bus.Min().Subscribe(min);
        bus.Max().Subscribe(max);

        bus.Broadcast(3);
        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        Assert.That(min.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(max.Values, Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void EmptyLast_Fails()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Last().Subscribe(handler);

        bus.OnComplete(default);

        Assert.That(handler.Values, Is.Empty);
        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(GetError(handler.Result), Is.Not.Null);
    }

    [Test]
    public void Skip_SkipsLeadingNotifications()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Skip(2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.Broadcast(4);

        Assert.That(handler.Values, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void SkipWhile_SkipsWhilePredicateHolds()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.SkipWhile(static value => value < 3).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.Broadcast(1);

        Assert.That(handler.Values, Is.EqualTo(new[] { 3, 1 }));
    }

    [Test]
    public void TakeWhile_StopsAtFirstMismatch()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.TakeWhile(static value => value < 3).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.Broadcast(1);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Scan_EmitsRunningAggregate()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Scan(static (state, value) => state + value).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Pairwise_EmitsConsecutivePairs()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<(int, int)>();
        bus.Pairwise().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { (1, 2), (2, 3) }));
    }

    [Test]
    public void Buffer_EmitsFixedSizeBatchesAndRemainder()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int[]>();
        bus.Buffer(2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(handler.Values.Select(static value => string.Join(",", value)), Is.EqualTo(new[] { "1,2", "3" }));
    }

    [Test]
    public void DefaultIfEmpty_EmitsDefaultWhenEmpty()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.DefaultIfEmpty(42).Subscribe(handler);

        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public void IgnoreElements_ForwardsOnlyCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.IgnoreElements().Subscribe(handler);

        bus.Broadcast(1);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.Empty);
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Aggregate_EmitsFoldedValueOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Aggregate(10, static (sum, value) => sum + value).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 13 }));
    }

    [Test]
    public void AnyAllContains_EmitBooleanAnswers()
    {
        var bus = new Event<int>();
        var any = new RecordingHandler<bool>();
        var all = new RecordingHandler<bool>();
        var contains = new RecordingHandler<bool>();
        bus.Any(static value => value > 2).Subscribe(any);
        bus.All(static value => value > 0).Subscribe(all);
        bus.Contains(2).Subscribe(contains);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(any.Values, Is.EqualTo(new[] { true }));
        Assert.That(all.Values, Is.EqualTo(new[] { true }));
        Assert.That(contains.Values, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void ToListAndToArray_CollectOnCompletion()
    {
        var bus = new Event<int>();
        var list = new RecordingHandler<IReadOnlyList<int>>();
        var array = new RecordingHandler<int[]>();
        bus.ToList().Subscribe(list);
        bus.ToArray().Subscribe(array);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        Assert.That(list.Values[0], Is.EqualTo(new[] { 1, 2 }));
        Assert.That(array.Values[0], Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Concat_SubscribesSecondAfterFirstCompletes()
    {
        var first = new Event<int>();
        var second = new Event<int>();
        var handler = new RecordingHandler<int>();
        first.Concat(second).Subscribe(handler);

        first.Broadcast(1);
        second.Broadcast(99);
        first.OnComplete(default);
        second.Broadcast(2);
        second.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Merge_ForwardsBothAndCompletesWhenBothComplete()
    {
        var first = new Event<int>();
        var second = new Event<int>();
        var handler = new RecordingHandler<int>();
        first.Merge(second).Subscribe(handler);

        first.Broadcast(1);
        second.Broadcast(2);
        first.OnComplete(default);

        Assert.That(handler.Completions, Is.Zero);

        second.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Switch_ForwardsOnlyTheLatestInnerSource()
    {
        var outer = new Event<IEventSubscribable<int>>();
        var handler = new RecordingHandler<int>();
        outer.Switch().Subscribe(handler);

        var first = new Event<int>();
        var second = new Event<int>();
        outer.Broadcast(first);
        first.Broadcast(1);
        outer.Broadcast(second);
        first.Broadcast(2);
        second.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 3 }));

        outer.OnComplete(default);
        Assert.That(handler.Completions, Is.Zero);

        second.OnComplete(default);
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Finally_InvokesOnceOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        var calls = 0;
        bus.Finally(() => calls++).Subscribe(handler);

        bus.Broadcast(1);
        bus.OnComplete(default);

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Finally_InvokesOnceOnUnsubscribe()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        var calls = 0;
        var stream = bus.Finally(() => calls++);
        stream.Subscribe(handler);

        stream.Unsubscribe(handler);

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void ToTask_CompletesWithLastNotification()
    {
        var bus = new Event<int>();
        var task = bus.ToTask();

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        Assert.That(task.GetAwaiter().GetResult(), Is.EqualTo(2));
    }

    [Test]
    public void ToTask_FaultsOnError()
    {
        var bus = new Event<int>();
        var task = bus.ToTask();

        bus.OnComplete(new InvalidOperationException("boom"));

        Assert.That(task.IsFaulted, Is.True);
        Assert.That(task.Exception?.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ToTask_CancelsAndDetaches()
    {
        var bus = new Event<int>();
        using var cancellation = new CancellationTokenSource();
        var task = bus.ToTask(cancellation.Token);

        cancellation.Cancel();

        Assert.That(task.IsCanceled, Is.True);
    }

    [Test]
    public void Catch_UsesFallbackOnError()
    {
        var bus = new Event<int>();
        var fallback = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Catch(fallback).Subscribe(handler);

        bus.Broadcast(1);
        bus.OnComplete(new InvalidOperationException("boom"));
        fallback.Broadcast(2);
        fallback.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(handler.Result.Valid, Is.True);
    }

    [Test]
    public void CatchAndIgnore_CompletesSuccessfullyOnError()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.CatchAndIgnore().Subscribe(handler);

        bus.Broadcast(1);
        bus.OnComplete(new InvalidOperationException("boom"));

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(handler.Result.Valid, Is.True);
    }

    [Test]
    public void Retry_ResubscribesOnError()
    {
        var source = new ScriptedSource<int>();
        source.Add(handler =>
        {
            handler.Handle(1);
            handler.OnComplete(new InvalidOperationException("boom"));
        });
        source.Add(handler =>
        {
            handler.Handle(2);
            handler.OnComplete(default);
        });

        var recorder = new RecordingHandler<int>();
        source.Retry(1).Subscribe(recorder);

        Assert.That(recorder.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(recorder.Completions, Is.EqualTo(1));
        Assert.That(recorder.Result.Valid, Is.True);
    }

    [Test]
    public void Repeat_ResubscribesOnSuccess()
    {
        var source = new ScriptedSource<int>();
        source.Add(handler =>
        {
            handler.Handle(1);
            handler.OnComplete(default);
        });
        source.Add(handler =>
        {
            handler.Handle(2);
            handler.OnComplete(default);
        });

        var recorder = new RecordingHandler<int>();
        source.Repeat(1).Subscribe(recorder);

        Assert.That(recorder.Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(recorder.Completions, Is.EqualTo(1));
    }

    [Test]
    public void ObservableReturn_EmitsValueAndCompletes()
    {
        var handler = new RecordingHandler<int>();
        Subscribable.Return(5).Subscribe(handler);

        Assert.That(handler.Values, Is.EqualTo(new[] { 5 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void ObservableRange_EmitsAllValues()
    {
        var handler = new RecordingHandler<int>();
        Subscribable.Range(1, 3).Subscribe(handler);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void ObservableThrow_Faults()
    {
        var error = new InvalidOperationException("boom");
        var handler = new RecordingHandler<int>();
        Subscribable.Throw<int>(error).Subscribe(handler);

        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(GetError(handler.Result), Is.SameAs(error));
    }

    [Test]
    public void Latest_ReplaysCurrentValue()
    {
        var subject = new LatestEvent<int>(10);
        var first = new RecordingHandler<int>();
        subject.Subscribe(first);
        subject.OnNext(20);

        var second = new RecordingHandler<int>();
        subject.Subscribe(second);

        Assert.That(first.Values, Is.EqualTo(new[] { 10, 20 }));
        Assert.That(second.Values, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public void State_DropsEqualValues()
    {
        var property = new StateEvent<int>(1);
        var handler = new RecordingHandler<int>();
        property.Subscribe(handler);

        property.Value = 1;
        property.Value = 2;

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Replay_ReplaysBufferedValues()
    {
        var subject = new ReplayEvent<int>();
        subject.OnNext(1);
        subject.OnNext(2);

        var handler = new RecordingHandler<int>();
        subject.Subscribe(handler);
        subject.OnNext(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Zip_PairsByIndex()
    {
        var first = new Event<int>();
        var second = new Event<int>();
        var handler = new RecordingHandler<int>();
        first.Zip(second, static (left, right) => left + right).Subscribe(handler);

        first.Broadcast(1);
        first.Broadcast(2);
        second.Broadcast(10);
        second.Broadcast(20);

        Assert.That(handler.Values, Is.EqualTo(new[] { 11, 22 }));
    }

    [Test]
    public void CombineLatest_EmitsLatestCombination()
    {
        var first = new Event<int>();
        var second = new Event<int>();
        var handler = new RecordingHandler<int>();
        first.CombineLatest(second, static (left, right) => left + right).Subscribe(handler);

        first.Broadcast(1);
        second.Broadcast(10);
        first.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 11, 12 }));
    }

    [Test]
    public void TakeUntil_TriggerCompletesTheStream()
    {
        var bus = new Event<int>();
        var trigger = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.TakeUntil(trigger).Subscribe(handler);

        bus.Broadcast(1);
        trigger.Broadcast(0);
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void TakeUntil_CancellationTokenCompletesTheStream()
    {
        var bus = new Event<int>();
        using var cancellation = new CancellationTokenSource();
        var handler = new RecordingHandler<int>();
        bus.TakeUntil(cancellation.Token).Subscribe(handler);

        bus.Broadcast(1);
        cancellation.Cancel();
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void DistinctBy_FiltersByKey()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.DistinctBy(static value => value % 2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.Broadcast(4);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void TakeLast_EmitsTrailingNotifications()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.TakeLast(2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void SkipLast_DropsTrailingNotifications()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.SkipLast(2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void SkipUntil_OpensWhenTriggerFires()
    {
        var bus = new Event<int>();
        var trigger = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.SkipUntil(trigger).Subscribe(handler);

        bus.Broadcast(1);
        trigger.Broadcast(0);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void WithLatestFrom_CombinesWithLatestValue()
    {
        var main = new Event<int>();
        var other = new Event<int>();
        var handler = new RecordingHandler<int>();
        main.WithLatestFrom(other, static (left, right) => left + right).Subscribe(handler);

        main.Broadcast(1);
        other.Broadcast(10);
        main.Broadcast(2);
        main.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 12, 13 }));
    }

    [Test]
    public void Multicast_ForwardsOnlyAfterConnect()
    {
        var bus = new Event<int>();
        var connectable = bus.Multicast();
        var handler = new RecordingHandler<int>();
        connectable.Subscribe(handler);

        bus.Broadcast(1);
        var connection = connectable.Connect();
        bus.Broadcast(2);
        connection.Dispose();
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Share_ForwardsToAllSubscribersAndDetachesIndividually()
    {
        var bus = new Event<int>();
        var shared = bus.Share();
        var first = new RecordingHandler<int>();
        var second = new RecordingHandler<int>();
        shared.Subscribe(first);
        shared.Subscribe(second);

        bus.Broadcast(1);
        shared.Unsubscribe(first);
        bus.Broadcast(2);

        Assert.That(first.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(second.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Prepend_EmitsValueFirst()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Prepend(0).Subscribe(handler);

        bus.Broadcast(1);

        Assert.That(handler.Values, Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void Append_EmitsValueOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Append(9).Subscribe(handler);

        bus.Broadcast(1);
        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 9 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void SelectMany_FlattensInnerSources()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.SelectMany(static value => Subscribable.Return(value * 10)).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 10, 20 }));
    }

    [Test]
    public void Do_InvokesSideEffects()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        var seen = new List<int>();
        var completed = 0;
        bus.Do(seen.Add).DoOnComplete(_ => completed++).Subscribe(handler);

        bus.Broadcast(1);
        bus.OnComplete(default);

        Assert.That(seen, Is.EqualTo(new[] { 1 }));
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void DoOnDispose_InvokedOnUnsubscribe()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        var disposed = 0;
        var stream = bus.DoOnDispose(() => disposed++);
        stream.Subscribe(handler);

        stream.Unsubscribe(handler);

        Assert.That(disposed, Is.EqualTo(1));
    }

    [Test]
    public void DoOnError_InvokedOnError()
    {
        var bus = new Event<int>();
        var error = new InvalidOperationException("boom");
        var handler = new RecordingHandler<int>();
        Exception? captured = null;
        bus.DoOnError(value => captured = value).Subscribe(handler);

        bus.OnComplete(error);

        Assert.That(captured, Is.SameAs(error));
    }

    [Test]
    public void ElementAt_EmitsIndexedNotification()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.ElementAt(1).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2 }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Single_ErrorsOnMultipleNotifications()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.Single().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(GetError(handler.Result), Is.Not.Null);
    }

    [Test]
    public void FirstOrDefault_EmitsFirstNotification()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.FirstOrDefault(9).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void LastOrDefault_EmitsDefaultOnEmpty()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.LastOrDefault(9).Subscribe(handler);

        bus.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { 9 }));
    }

    [Test]
    public void ToHashSet_CollectsUniqueValues()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<HashSet<int>>();
        bus.ToHashSet().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        Assert.That(handler.Values[0], Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public void ForEachAsync_RunsActionAndCompletes()
    {
        var bus = new Event<int>();
        var seen = new List<int>();
        var task = bus.ForEachAsync(seen.Add);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        task.GetAwaiter().GetResult();

        Assert.That(seen, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void ToDictionary_CollectsOnCompletion()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<Dictionary<int, string>>();
        bus.ToDictionary(static value => value, static value => value.ToString()).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.OnComplete(default);

        Assert.That(handler.Values[0].Count, Is.EqualTo(2));
        Assert.That(handler.Values[0][1], Is.EqualTo("1"));
        Assert.That(handler.Values[0][2], Is.EqualTo("2"));
    }

    [Test]
    public void DistinctUntilChangedBy_UsesSelectedKey()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.DistinctUntilChangedBy(static value => value % 2).Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(3);
        bus.Broadcast(2);
        bus.Broadcast(4);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void SequenceEqual_EmitsTrueForEqualSequences()
    {
        var first = new Event<int>();
        var second = new Event<int>();
        var handler = new RecordingHandler<bool>();
        first.SequenceEqual(second).Subscribe(handler);

        first.Broadcast(1);
        second.Broadcast(1);
        first.Broadcast(2);
        second.Broadcast(2);
        first.OnComplete(default);
        second.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { true }));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void SequenceEqual_EmitsFalseForDifferentSequences()
    {
        var first = new Event<int>();
        var second = new Event<int>();
        var handler = new RecordingHandler<bool>();
        first.SequenceEqual(second).Subscribe(handler);

        first.Broadcast(1);
        second.Broadcast(1);
        first.Broadcast(2);
        second.Broadcast(3);
        first.OnComplete(default);
        second.OnComplete(default);

        Assert.That(handler.Values, Is.EqualTo(new[] { false }));
    }

    [Test]
    public void ThrottleFirst_UsesTimeWindow()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.ThrottleFirst(TimeSpan.FromSeconds(10), time).Subscribe(handler);

        bus.Broadcast(1);
        time.Advance(TimeSpan.FromSeconds(5));
        bus.Broadcast(2);
        time.Advance(TimeSpan.FromSeconds(6));
        bus.Broadcast(3);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Delay_SchedulesNotifications()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.Delay(TimeSpan.FromSeconds(1), time).Subscribe(handler);

        bus.Broadcast(1);
        Assert.That(handler.Values, Is.Empty);

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.That(handler.Values, Is.EqualTo(new[] { 1 }));

        bus.OnComplete(default);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.That(handler.Completions, Is.EqualTo(1));
    }

    [Test]
    public void Debounce_EmitsAfterQuietPeriod()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.Debounce(TimeSpan.FromSeconds(1), time).Subscribe(handler);

        bus.Broadcast(1);
        time.Advance(TimeSpan.FromMilliseconds(500));
        bus.Broadcast(2);
        time.Advance(TimeSpan.FromSeconds(1));

        Assert.That(handler.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Sample_EmitsLatestOnPeriod()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.Sample(TimeSpan.FromSeconds(1), time).Subscribe(handler);

        bus.Broadcast(1);
        time.Advance(TimeSpan.FromSeconds(1));
        bus.Broadcast(2);
        time.Advance(TimeSpan.FromSeconds(1));

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Timeout_FailsWhenSourceStaysSilent()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.Timeout(TimeSpan.FromSeconds(1), time).Subscribe(handler);

        time.Advance(TimeSpan.FromSeconds(1));

        Assert.That(handler.Completions, Is.EqualTo(1));
        Assert.That(GetError(handler.Result), Is.TypeOf<TimeoutException>());
    }

    [Test]
    public void ThrottleLast_EmitsLastValuePerWindow()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.ThrottleLast(TimeSpan.FromSeconds(10), time).Subscribe(handler);

        bus.Broadcast(1);
        time.Advance(TimeSpan.FromSeconds(5));
        bus.Broadcast(2);
        time.Advance(TimeSpan.FromSeconds(5));
        bus.Broadcast(3);
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.That(handler.Values, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void DelaySubscription_IgnoresValuesBeforeDelay()
    {
        var bus = new Event<int>();
        var time = new ManualTimeProvider();
        var handler = new RecordingHandler<int>();
        bus.DelaySubscription(TimeSpan.FromSeconds(1), time).Subscribe(handler);

        bus.Broadcast(1);
        time.Advance(TimeSpan.FromSeconds(1));
        bus.Broadcast(2);

        Assert.That(handler.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void DistinctUntilChanged_UsesConsecutiveComparison()
    {
        var bus = new Event<int>();
        var handler = new RecordingHandler<int>();
        bus.DistinctUntilChanged().Subscribe(handler);

        bus.Broadcast(1);
        bus.Broadcast(1);
        bus.Broadcast(2);
        bus.Broadcast(1);

        Assert.That(handler.Values, Is.EqualTo(new[] { 1, 2, 1 }));
    }

    private static Exception? GetError<TNotification>(Result<TNotification, Exception> result)
    {
        result.TryGetValue(out _, out var error);
        return error;
    }

    private sealed class ScriptedSource<TNotification> : IEventSubscribable<TNotification>
    {
        private readonly Queue<Action<IEventHandler<TNotification>>> m_Scripts = new();

        public void Add(Action<IEventHandler<TNotification>> script) => m_Scripts.Enqueue(script);

        public void Subscribe(IEventHandler<TNotification> handler)
        {
            if (m_Scripts.Count > 0) m_Scripts.Dequeue()(handler);
        }

        public bool Unsubscribe(IEventHandler<TNotification> handler) => true;

        public void Clear() => m_Scripts.Clear();
    }

    private sealed class ManualTimeProvider : ITimeProvider
    {
        private DateTimeOffset m_Now = DateTimeOffset.UnixEpoch;
        private readonly List<Entry> m_Entries = [];

        public DateTimeOffset UtcNow => m_Now;

        public IDisposable Schedule(TimeSpan dueTime, Action callback)
        {
            var entry = new Entry(m_Now + dueTime, null, callback);
            m_Entries.Add(entry);
            return new Cancel(m_Entries, entry);
        }

        public IDisposable SchedulePeriodic(TimeSpan period, Action callback)
        {
            var entry = new Entry(m_Now + period, period, callback);
            m_Entries.Add(entry);
            return new Cancel(m_Entries, entry);
        }

        public void Advance(TimeSpan delta)
        {
            var target = m_Now + delta;
            while (true)
            {
                Entry? due = null;
                foreach (var entry in m_Entries)
                {
                    if (entry.Due > target) continue;
                    if (due is null || entry.Due < due.Due) due = entry;
                }

                if (due is null) break;

                m_Now = due.Due;
                due.Run();

                if (due.Period is { } period) due.Due += period;
                else m_Entries.Remove(due);
            }

            m_Now = target;
        }

        private sealed class Entry(DateTimeOffset due, TimeSpan? period, Action callback)
        {
            public DateTimeOffset Due { get; set; } = due;

            public TimeSpan? Period { get; } = period;

            public void Run() => callback();
        }

        private sealed class Cancel(List<Entry> entries, Entry entry) : IDisposable
        {
            public void Dispose() => entries.Remove(entry);
        }
    }
    private sealed class RecordingHandler<TNotification> : IEventHandler<TNotification>
    {
        public readonly List<TNotification> Values = [];
        public int Completions;
        public Result<TNotification, Exception> Result;

        public void Handle(TNotification notification) => Values.Add(notification);

        public void OnComplete(Result<TNotification, Exception> result)
        {
            Completions++;
            Result = result;
        }
    }
}








