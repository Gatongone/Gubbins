namespace Gubbins.Async.Tests;

[TestFixture]
public sealed class AsynchronousBuilderTests
{
    private static async Asynchronous CompleteAsync()
    {
        await Task.Yield();
    }

    private static async Asynchronous ThrowAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }

    private static async IAwaitable CompleteInterfaceAsync()
    {
        await Task.Yield();
    }

    private static async Asynchronous InvokeAsync(Action action)
    {
        await Task.Yield();
        action();
    }

    private static async Asynchronous<int> ComputeAsync()
    {
        await Task.Yield();
        return 42;
    }

    private static async Asynchronous<string> CompleteSynchronouslyAsync()
    {
        await Task.CompletedTask;
        return "value";
    }

    private static async Asynchronous<int> MultiAwaitAsync()
    {
        await Task.Yield();
        await Task.Yield();
        return 7;
    }

    private static async Asynchronous<int> ThrowIntAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("int boom");
    }

    private static async IAwaitable<int> InterfaceResultAsync()
    {
        await Task.Yield();
        return 5;
    }

    [Test]
    public async Task AsyncAsynchronous_ShouldBeAwaitable()
    {
        await CompleteAsync();
    }

    [Test]
    public async Task AsyncAsynchronous_ShouldSurfaceExceptions()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await ThrowAsync());
        Assert.That(exception!.Message, Is.EqualTo("boom"));
    }

    [Test]
    public async Task AsyncAsynchronous_ShouldResumeAfterSuspension()
    {
        var invoked = false;

        await InvokeAsync(() => invoked = true);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public async Task AsyncIAwaitable_ShouldBeAwaitable()
    {
        await CompleteInterfaceAsync();
    }

    [Test]
    public async Task AsyncAsynchronousOfT_ShouldReturnResult()
    {
        var result = await ComputeAsync();
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task AsyncAsynchronousOfT_SynchronousCompletion_ShouldReturnResult()
    {
        var result = await CompleteSynchronouslyAsync();
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public async Task AsyncAsynchronousOfT_ShouldResumeAfterMultipleAwaits()
    {
        var result = await MultiAwaitAsync();
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void AsyncAsynchronousOfT_ShouldSurfaceExceptions()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ThrowIntAsync();
        });
        Assert.That(exception!.Message, Is.EqualTo("int boom"));
    }

    [Test]
    public async Task AsyncAsynchronousOfT_AsTask_ShouldReturnResult()
    {
        var result = await ComputeAsync().AsTask();
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task AsyncIAwaitableOfT_ShouldReturnResult()
    {
        var result = await InterfaceResultAsync();
        Assert.That(result, Is.EqualTo(5));
    }
}
