using System.Numerics;
using Gubbins.Span;

namespace Gubbins.Entities.Tests;

[TestFixture]
public sealed class ComponentsParallelTests
{
    private const int CHUNK_SIZE = 256;
    private const int ENTITY_COUNT = 1_000;
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

    private static EntityRepository CreateRepository<T>(T value) where T : unmanaged
    {
        var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        for (var i = 0; i < ENTITY_COUNT; i++)
        {
            repository.Insert(value);
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
    public void Add_ShouldAddOperand()
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.Add(IterationMode.Serial, 2);
        }

        AssertAllValues(repository, 3);
    }

    [Test]
    public void ApplyParallel_ShouldSupportTheNumberOperationEnum()
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.ApplyParallel(IterationMode.Simd, NumberOperation.Multiply, 3);
            components.ApplyParallel(IterationMode.Serial, NumberOperation.Subtract, 1);
            components.ApplyParallel(IterationMode.Serial, NumberOperation.Modulo, 2);
        }

        // (1 * 3 - 1) % 2 = 0
        AssertAllValues(repository, 0);
    }

    [TestCase(IterationMode.Serial)]
    [TestCase(IterationMode.Simd)]
    [TestCase(IterationMode.Parallel)]
    [TestCase(IterationMode.Parallel | IterationMode.Simd)]
    [TestCase(IterationMode.Auto)]
    public void ApplyParallel_ShouldSupportEveryNumberOperation(IterationMode mask)
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.ApplyParallel(mask, NumberOperation.Add, 4);       // 5
            components.ApplyParallel(mask, NumberOperation.Multiply, 4);  // 20
            components.ApplyParallel(mask, NumberOperation.Subtract, 11); // 9
            components.ApplyParallel(mask, NumberOperation.Divide, 3);    // 3
            components.ApplyParallel(mask, NumberOperation.Modulo, 2);    // 1
        }

        AssertAllValues(repository, 1);
    }

    [TestCase(IterationMode.Serial)]
    [TestCase(IterationMode.Simd)]
    [TestCase(IterationMode.Parallel)]
    [TestCase(IterationMode.Parallel | IterationMode.Simd)]
    [TestCase(IterationMode.Auto)]
    public void NamedOperationMethods_ShouldApplyEveryOperation(IterationMode mask)
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.Add(mask, 4);       // 5
            components.Multiply(mask, 4);  // 20
            components.Subtract(mask, 11); // 9
            components.Divide(mask, 3);    // 3
            components.Modulo(mask, 2);    // 1
        }

        AssertAllValues(repository, 1);
    }

    [Test]
    public void ApplyParallel_ShouldSupportNonAddOperations()
    {
        using var repository = CreateRepository();
        var operation = SpanOperations.GetOperation<int, ISpanNumberOperation<int>>(IterationMode.Simd)!;

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.ApplyParallel(
                (Operation: operation, Operand: 3),
                static (Span<int> span, (ISpanNumberOperation<int> Operation, int Operand) state) =>
                    state.Operation.Multiply(span, state.Operand, span));
        }

        AssertAllValues(repository, 3);
    }

    [TestCase(IterationMode.Serial)]
    [TestCase(IterationMode.Simd)]
    [TestCase(IterationMode.Parallel)]
    [TestCase(IterationMode.Parallel | IterationMode.Simd)]
    [TestCase(IterationMode.Auto)]
    public void ApplyReal_ShouldApplySqrtInPlace(IterationMode mask)
    {
        using var repository = CreateRepository(4f);

        using (var chunks = repository.Search(new ComponentFilter().Include<float>()))
        using (var components = chunks.GetComponents<float>())
        {
            components.ApplyReal(mask, 0, static (operation, span, _) => operation.Sqrt(span, span));

            var elements = components.Elements;
            for (var i = 0; i < elements.Count; i++)
            {
                Assert.That(elements[i], Is.EqualTo(2f).Within(1e-6f));
            }
        }
    }

    [Test]
    public void ApplyReal_ShouldPassScalarParametersThroughState()
    {
        using var repository = CreateRepository(3f);

        using (var chunks = repository.Search(new ComponentFilter().Include<float>()))
        using (var components = chunks.GetComponents<float>())
        {
            components.ApplyReal(
                IterationMode.Simd,
                (Min: 1f, Max: 2f),
                static (operation, span, range) => operation.Clamp(span, range.Min, range.Max, span));

            var elements = components.Elements;
            for (var i = 0; i < elements.Count; i++)
            {
                Assert.That(elements[i], Is.EqualTo(2f));
            }
        }
    }

    [Test]
    public void ApplyVector_ShouldNormalizeInPlace()
    {
        using var repository = CreateRepository(new Vector2(3f, 4f));

        using (var chunks = repository.Search(new ComponentFilter().Include<Vector2>()))
        using (var components = chunks.GetComponents<Vector2>())
        {
            components.ApplyVector(
                IterationMode.Serial,
                Vector2.Zero,
                static (operation, span, _) => operation.Normalize(span, span));

            var elements = components.Elements;
            for (var i = 0; i < elements.Count; i++)
            {
                Assert.That(elements[i].X, Is.EqualTo(0.6f).Within(1e-6f));
                Assert.That(elements[i].Y, Is.EqualTo(0.8f).Within(1e-6f));
            }
        }
    }

    [TestCase(IterationMode.Serial)]
    [TestCase(IterationMode.Simd)]
    [TestCase(IterationMode.Parallel)]
    [TestCase(IterationMode.Parallel | IterationMode.Simd)]
    [TestCase(IterationMode.Auto)]
    public void ApplyShift_ShouldShiftEveryElement(IterationMode mask)
    {
        using var repository = CreateRepository(1);

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.ApplyShift(mask, 3, static (operation, span, count) => operation.ShiftLeft(span, count, span));
        }

        AssertAllValues(repository, 8);
    }

    [Test]
    public void ApplyNumber_ShouldReachOperationsMissingFromTheEnum()
    {
        using var repository = CreateRepository();

        using (var chunks = repository.Search(filter))
        using (var components = chunks.GetComponents<int>())
        {
            components.ApplyNumber(
                IterationMode.Serial,
                5,
                static (operation, span, operand) => operation.Multiply(span, operand, span));
        }

        AssertAllValues(repository, 5);
    }
}
