namespace Gubbins.Entities.Tests;

[TestFixture]
public sealed class ChunksForeachAsyncTests
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

    private static void AssertAllValues(EntityRepository repository, int expected)
    {
        using var chunks = repository.Search(filter);
        using var components = chunks.GetComponents<int>();
        var elements = components.Elements;

        Assert.That(elements.Count, Is.EqualTo(ENTITY_COUNT));
        for (var i = 0; i < elements.Count; i++)
        {
            Assert.That(elements[i], Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task ForEachAsync_ShouldInvokeEveryElementExactlyOnce()
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        {
            await chunks.ForEachAsync(static (in Entity _, ref int value) => value += 1);
        }

        AssertAllValues(repository, 2);
    }

    [Test]
    public async Task ForEachAsync_RepeatedCalls_ShouldReuseOperationsWithoutStateLeaks()
    {
        using var repository = CreateRepository();

        for (var i = 0; i < 3; i++)
        {
            using var chunks = repository.Search(filter);
            await chunks.ForEachAsync(static (in Entity _, ref int value) => value += 1);
        }

        AssertAllValues(repository, 4);
    }

    [Test]
    public async Task ForEachAsync_EmptyQuery_ShouldCompleteWithoutInvokingAction()
    {
        using var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        repository.Insert(1.2f);

        var invoked = 0;
        var filter = new ComponentFilter().Include<int>();
        using var chunks = repository.Search(filter);
        var pending = chunks.ForEachAsync((in Entity _, ref int _) => invoked++);

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
        using var chunks = repository.Search(filter);
        var pending = chunks.ForEachAsync((in Entity _, ref int _) => invoked++, source.Token);

        Assert.That(pending.IsCompleted, Is.True);
        await pending;
        Assert.That(invoked, Is.Zero);
    }

    [Test]
    public async Task ForEachAsync_CanceledDuringRun_ShouldStopEarlyWithoutThrowing()
    {
        using var repository = CreateRepository();
        using var source = new CancellationTokenSource();
        var invoked = 0;

        using (var chunks = repository.Search(filter))
        {
            await chunks.ForEachAsync((in Entity _, ref int _) =>
            {
                Interlocked.Increment(ref invoked);
                source.Cancel();
            }, source.Token);
        }

        Assert.That(invoked, Is.LessThan(ENTITY_COUNT));
    }

    [Test]
    public async Task ForEachAsync_ActionThrows_ShouldSurfaceAggregateExceptionAndRemainUsable()
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        {
            var exception = Assert.ThrowsAsync<AggregateException>(async () =>
                await chunks.ForEachAsync(static (in Entity _, ref int _) =>
                    throw new InvalidOperationException("boom")));

            Assert.That(exception!.InnerExceptions.Count, Is.GreaterThanOrEqualTo(1));
        }

        using (var chunks = repository.Search(filter))
        {
            await chunks.ForEachAsync(static (in Entity _, ref int value) => value += 1);
        }

        AssertAllValues(repository, 2);
    }

    [Test]
    public async Task ForEachAsync_OneComponentArity_ShouldUpdateComponent()
    {
        using var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        for (var i = 0; i < 100; i++)
        {
            repository.Insert(5);
        }

        var filter = new ComponentFilter().Include<int>();
        using (var chunks = repository.Search(filter))
        {
            await chunks.ForEachAsync(static (in Entity _, ref int value) => value += 1);
        }

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            var elements = components.Elements;
            Assert.That(elements.Count, Is.EqualTo(100));
            for (var i = 0; i < elements.Count; i++)
            {
                Assert.That(elements[i], Is.EqualTo(6));
            }
        }
    }

    [Test]
    public async Task ForEachAsync_ThreeComponentArity_ShouldUpdateAllComponents()
    {
        using var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        for (var i = 0; i < 100; i++)
        {
            repository.Insert('a', 1, 1.2f);
        }

        using (var chunks = repository.Search(filter))
        {
            await chunks.ForEachAsync(static (in Entity _, ref char c, ref int i, ref float f) =>
            {
                c = 'b';
                i += 1;
                f += 1;
            });
        }

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            var elements = components.Elements;
            Assert.That(elements.Count, Is.EqualTo(100));
            for (var i = 0; i < elements.Count; i++)
            {
                Assert.That(elements[i], Is.EqualTo(2));
            }
        }
    }

    [Test]
    public async Task ForEachAsync_ConcurrentCalls_ShouldProcessAllElements()
    {
        using var repository = CreateRepository();
        const int callers = 4;

        var tasks = new Task[callers];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using var chunks = repository.Search(filter);
                await chunks.ForEachAsync(static (in Entity _, ref int value) => Interlocked.Increment(ref value));
            });
        }

        await Task.WhenAll(tasks);
        AssertAllValues(repository, 1 + callers);
    }
}
