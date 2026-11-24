namespace Gubbins.Entities.Tests;

[TestFixture]
public sealed class ReadOnlyForeachAsyncTests
{
    private const int CHUNK_SIZE = 256;
    private const int ENTITY_COUNT = 1000;

    private static readonly ComponentFilter filter = new ComponentFilter().Include<int>();

    private static EntityRepository CreateRepository()
    {
        var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        for (var i = 0; i < ENTITY_COUNT; i++)
        {
            repository.Insert(1, 1.2f);
        }

        return repository;
    }

    [Test]
    public async Task ForEachAsync_ShouldInvokeEveryElementExactlyOnce()
    {
        using var repository = CreateRepository();

        var sum = 0;
        {
            var chunks = repository.Query(filter);
            await chunks.ForEachAsync((in Entity _, in int value) => Interlocked.Add(ref sum, value));
        }

        Assert.That(sum, Is.EqualTo(ENTITY_COUNT));
    }

    [Test]
    public async Task ForEachAsync_ShouldReadEveryComponentOfTheArity()
    {
        using var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        for (var i = 0; i < 100; i++)
        {
            repository.Insert('a', 5, 1.5f);
        }

        var matches = 0;
        {
            var chunks = repository.Query(new ComponentFilter().Include<char>().Include<int>().Include<float>());
            await chunks.ForEachAsync(
                (in Entity _, in char c, in int i, in float f) =>
                {
                    if (c == 'a' && i == 5 && Math.Abs(f - 1.5f) < float.Epsilon)
                    {
                        Interlocked.Increment(ref matches);
                    }
                });
        }

        Assert.That(matches, Is.EqualTo(100));
    }

    [Test]
    public async Task ForEachAsync_ShouldNotStampTheChunksItVisits()
    {
        using var repository = CreateRepository();
        var before = repository.Version;

        {

            var chunks = repository.Query(filter);
            await chunks.ForEachAsync(static (in Entity _, in int _) => { });
        }

        Assert.That(
            repository.Query(filter, before).Count,
            Is.EqualTo(0),
            "A read-only view must not make a chunk hot.");
        {
            var chunks = repository.Command(filter);
            await chunks.ForEachAsync(static (in Entity _, ref int value) => value += 1);
        }

        Assert.That(repository.Query(filter, before).Count, Is.GreaterThan(0), "Write iteration must make its chunks hot.");
    }

    [Test]
    public async Task ForEachAsync_EmptyQuery_ShouldCompleteWithoutInvokingAction()
    {
        using var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        repository.Insert(1.2f);

        var invoked = 0;
        var chunks = repository.Query(new ComponentFilter().Include<int>());
        var pending = chunks.ForEachAsync((in Entity _, in int _) => invoked++);

        Assert.That(pending.IsCompleted, Is.True);
        await pending;
        Assert.That(invoked, Is.Zero);
    }

    [Test]
    public async Task ForEachAsync_AlreadyCanceledToken_ShouldCompleteWithoutInvokingAction()
    {
        using var repository = CreateRepository();
        using var source = new CancellationTokenSource();
        source.Cancel();

        var invoked = 0;
        var chunks = repository.Query(filter);
        var pending = chunks.ForEachAsync((in Entity _, in int _) => invoked++, source.Token);

        Assert.That(pending.IsCompleted, Is.True);
        await pending;
        Assert.That(invoked, Is.Zero);
    }

    [Test]
    public async Task ForEachAsync_ActionThrows_ShouldSurfaceAggregateExceptionAndRemainUsable()
    {
        using var repository = CreateRepository();

        {

            var chunks = repository.Query(filter);
            var exception = Assert.ThrowsAsync<AggregateException>(async () =>
                await chunks.ForEachAsync(static (in Entity _, in int _) =>
                    throw new InvalidOperationException("boom")));

            Assert.That(exception!.InnerExceptions.Count, Is.GreaterThanOrEqualTo(1));
        }

        var sum = 0;
        {
            var chunks = repository.Query(filter);
            await chunks.ForEachAsync((in Entity _, in int value) => Interlocked.Add(ref sum, value));
        }

        Assert.That(sum, Is.EqualTo(ENTITY_COUNT));
    }

    [Test]
    public async Task ForEachAsync_RepeatedCalls_ShouldReuseOperationsWithoutStateLeaks()
    {
        using var repository = CreateRepository();

        var sum = 0;
        for (var i = 0; i < 3; i++)
        {
            var chunks = repository.Query(filter);
            await chunks.ForEachAsync((in Entity _, in int value) => Interlocked.Add(ref sum, value));
        }

        Assert.Multiple(() =>
        {
            Assert.That(sum, Is.EqualTo(3 * ENTITY_COUNT));
            Assert.That(repository.Query(filter, repository.Version).Count, Is.EqualTo(0));
        });
    }
}
