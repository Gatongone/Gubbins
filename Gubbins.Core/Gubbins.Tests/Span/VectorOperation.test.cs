using System.Numerics;
using static Gubbins.Span.Tests.ParallelVectorTestScalars;
using static Gubbins.Span.Tests.SimdVectorAssertions;

namespace Gubbins.Span.Tests;

public abstract class Vector2OperationTestsBase
{
    protected abstract ISpanVectorOperation<Vector2> CreateOperation();

    private static Vector2[] CreateSource() =>
    [
        new(1f, 2f),
        Vector2.Zero,
        new(-2f, 5f),
        new(3f, -4f),
        new(-1f, -1f)
    ];

    private static Vector2[] CreateOther() =>
    [
        new(-2f, 1f),
        new(0.5f, -3f),
        Vector2.Zero,
        new(2f, 2f),
        new(1f, 0f)
    ];

    private static Vector2[] CreateNormal() =>
    [
        Vector2.UnitY,
        Vector2.UnitX,
        Vector2.Normalize(new Vector2(0.3f, -0.8f)),
        new(-1f, 0f),
        Vector2.UnitY
    ];

    private static float[] CreateIncident() => [-1f, 0f, 2f, -0.5f, 1f];
    private static float[] CreateMaxDelta() => [0f, 0.5f, 100f, 0.1f, -0.5f];

    [Test]
    public void Dot_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new Vector2[src.Length];
        op.Dot(src, other, result);

        var expected = new Vector2[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            var dot = Vector2.Dot(src[i], other[i]);
            expected[i] = new Vector2(dot, dot);
        }

        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void Cross_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new Vector2[src.Length];
        op.Cross(src, other, result);

        var expected = new Vector2[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            var cross = src[i].X * other[i].Y - src[i].Y * other[i].X;
            expected[i] = new Vector2(cross, cross);
        }

        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void Normalize_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new Vector2[src.Length];
        op.Normalize(src, result);

        var expected = src.Select(static v => Vector2.Normalize(v)).ToArray();
        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void Reflect_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var result = new Vector2[src.Length];
        op.Reflect(src, normal, result);

        var expected = src.Select((v, i) => Vector2.Reflect(v, normal[i])).ToArray();
        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void Refract_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var eta = new Vector2(0.7f, 99f);
        var result = new Vector2[src.Length];
        op.Refract(src, normal, eta, result);

        var expected = src.Select((v, i) => RefractVector2(v, normal[i], eta.X)).ToArray();
        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void FaceForward_WithIncident_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var incident = CreateIncident();
        var result = new Vector2[src.Length];
        op.FaceForward(src, normal, incident, result);

        var expected = src.Select((v, i) => incident[i] < 0f ? v : -v).ToArray();
        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void FaceForward_WithNormal_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var result = new Vector2[src.Length];
        op.FaceForward(src, normal, result);

        var expected = src.Select((v, i) => Vector2.Dot(normal[i], v) < 0f ? v : -v).ToArray();
        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void MoveTowards_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var maxDelta = CreateMaxDelta();
        var result = new Vector2[src.Length];
        op.MoveTowards(src, other, maxDelta, result);

        var expected = src.Select((v, i) => MoveTowardsVector2(v, other[i], maxDelta[i])).ToArray();
        AssertVector2SpanEqual(expected, result);
    }

    [Test]
    public void Angle_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.Angle(src, other, result);

        var expected = src.Select((v, i) => AngleVector2(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void Length_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new float[src.Length];
        op.Length(src, result);

        var expected = src.Select(static v => v.Length()).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void LengthSquared_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new float[src.Length];
        op.LengthSquared(src, result);

        var expected = src.Select(static v => v.LengthSquared()).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void Distance_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.Distance(src, other, result);

        var expected = src.Select((v, i) => Vector2.Distance(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void DistanceSquared_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.DistanceSquared(src, other, result);

        var expected = src.Select((v, i) => Vector2.DistanceSquared(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }
}

public abstract class Vector3OperationTestsBase
{
    protected abstract ISpanVectorOperation<Vector3> CreateOperation();

    private static Vector3[] CreateSource() =>
    [
        new(1f, 2f, 3f),
        Vector3.Zero,
        new(-2f, 5f, 0.5f),
        new(3f, -4f, 1f),
        new(-1f, -1f, -1f)
    ];

    private static Vector3[] CreateOther() =>
    [
        new(-2f, 1f, 4f),
        new(0.5f, -3f, 2f),
        Vector3.Zero,
        new(2f, 2f, -1f),
        new(1f, 0f, 3f)
    ];

    private static Vector3[] CreateNormal() =>
    [
        Vector3.UnitY,
        Vector3.UnitX,
        Vector3.Normalize(new Vector3(0.3f, -0.8f, 0.2f)),
        new(-1f, 0f, 0f),
        Vector3.UnitZ
    ];

    private static float[] CreateIncident() => [-1f, 0f, 2f, -0.5f, 1f];
    private static float[] CreateMaxDelta() => [0f, 0.5f, 100f, 0.1f, -0.5f];

    [Test]
    public void Dot_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new Vector3[src.Length];
        op.Dot(src, other, result);

        for (var i = 0; i < src.Length; i++)
        {
            var dot = Vector3.Dot(src[i], other[i]);
            Assert.That(FloatEquals(dot, result[i].X, 1e-5f));
            Assert.That(FloatEquals(dot, result[i].Y, 1e-5f));
            Assert.That(FloatEquals(dot, result[i].Z, 1e-5f));
        }
    }

    [Test]
    public void Cross_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new Vector3[src.Length];
        op.Cross(src, other, result);

        var expected = src.Select((v, i) => Vector3.Cross(v, other[i])).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void Normalize_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new Vector3[src.Length];
        op.Normalize(src, result);

        var expected = src.Select(static v => Vector3.Normalize(v)).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void Reflect_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var result = new Vector3[src.Length];
        op.Reflect(src, normal, result);

        var expected = src.Select((v, i) => Vector3.Reflect(v, normal[i])).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void Refract_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var eta = new Vector3(0.7f, 99f, -3f);
        var result = new Vector3[src.Length];
        op.Refract(src, normal, eta, result);

        var expected = src.Select((v, i) => RefractVector3(v, normal[i], eta.X)).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void FaceForward_WithIncident_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var incident = CreateIncident();
        var result = new Vector3[src.Length];
        op.FaceForward(src, normal, incident, result);

        var expected = src.Select((v, i) => incident[i] < 0f ? v : -v).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void FaceForward_WithNormal_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var result = new Vector3[src.Length];
        op.FaceForward(src, normal, result);

        var expected = src.Select((v, i) => Vector3.Dot(normal[i], v) < 0f ? v : -v).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void MoveTowards_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var maxDelta = CreateMaxDelta();
        var result = new Vector3[src.Length];
        op.MoveTowards(src, other, maxDelta, result);

        var expected = src.Select((v, i) => MoveTowardsVector3(v, other[i], maxDelta[i])).ToArray();
        AssertVector3SpanEqual(expected, result);
    }

    [Test]
    public void Angle_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.Angle(src, other, result);

        var expected = src.Select((v, i) => AngleVector3(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void Length_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new float[src.Length];
        op.Length(src, result);

        var expected = src.Select(static v => v.Length()).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void LengthSquared_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new float[src.Length];
        op.LengthSquared(src, result);

        var expected = src.Select(static v => v.LengthSquared()).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void Distance_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.Distance(src, other, result);

        var expected = src.Select((v, i) => Vector3.Distance(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void DistanceSquared_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.DistanceSquared(src, other, result);

        var expected = src.Select((v, i) => Vector3.DistanceSquared(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }
}

public abstract class Vector4OperationTestsBase
{
    protected abstract ISpanVectorOperation<Vector4> CreateOperation();

    private static Vector4[] CreateSource() =>
    [
        new(1f, 2f, 3f, 4f),
        Vector4.Zero,
        new(-2f, 5f, 0.5f, -3f),
        new(3f, -4f, 1f, 2f),
        new(-1f, -1f, -1f, 0.25f)
    ];

    private static Vector4[] CreateOther() =>
    [
        new(-2f, 1f, 4f, -1f),
        new(0.5f, -3f, 2f, 0.5f),
        Vector4.Zero,
        new(2f, 2f, -1f, 4f),
        new(1f, 0f, 3f, -2f)
    ];

    private static Vector4[] CreateNormal() =>
    [
        Vector4.UnitY,
        Vector4.UnitX,
        Vector4.Normalize(new Vector4(0.3f, -0.8f, 0.2f, -0.1f)),
        new(-1f, 0f, 0f, 0f),
        Vector4.UnitZ
    ];

    private static float[] CreateIncident() => [-1f, 0f, 2f, -0.5f, 1f];
    private static float[] CreateMaxDelta() => [0f, 0.5f, 100f, 0.1f, -0.5f];

    [Test]
    public void Dot_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new Vector4[src.Length];
        op.Dot(src, other, result);

        for (var i = 0; i < src.Length; i++)
        {
            var dot = Vector4.Dot(src[i], other[i]);
            Assert.That(FloatEquals(dot, result[i].X, 1e-5f));
            Assert.That(FloatEquals(dot, result[i].Y, 1e-5f));
            Assert.That(FloatEquals(dot, result[i].Z, 1e-5f));
            Assert.That(FloatEquals(dot, result[i].W, 1e-5f));
        }
    }

    [Test]
    public void Cross_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new Vector4[src.Length];
        op.Cross(src, other, result);

        var expected = src.Select((v, i) => CrossVector4(v, other[i])).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void Normalize_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new Vector4[src.Length];
        op.Normalize(src, result);

        var expected = src.Select(static v => Vector4.Normalize(v)).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void Reflect_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var result = new Vector4[src.Length];
        op.Reflect(src, normal, result);

        var expected = src.Select((v, i) => ReflectVector4(v, normal[i])).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void Refract_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var eta = new Vector4(0.7f, 99f, -3f, 11f);
        var result = new Vector4[src.Length];
        op.Refract(src, normal, eta, result);

        var expected = src.Select((v, i) => RefractVector4(v, normal[i], eta.X)).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void FaceForward_WithIncident_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var incident = CreateIncident();
        var result = new Vector4[src.Length];
        op.FaceForward(src, normal, incident, result);

        var expected = src.Select((v, i) => incident[i] < 0f ? v : -v).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void FaceForward_WithNormal_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var normal = CreateNormal();
        var result = new Vector4[src.Length];
        op.FaceForward(src, normal, result);

        var expected = src.Select((v, i) => Vector4.Dot(normal[i], v) < 0f ? v : -v).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void MoveTowards_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var maxDelta = CreateMaxDelta();
        var result = new Vector4[src.Length];
        op.MoveTowards(src, other, maxDelta, result);

        var expected = src.Select((v, i) => MoveTowardsVector4(v, other[i], maxDelta[i])).ToArray();
        AssertVector4SpanEqual(expected, result);
    }

    [Test]
    public void Angle_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.Angle(src, other, result);

        var expected = src.Select((v, i) => AngleVector4(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void Length_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new float[src.Length];
        op.Length(src, result);

        var expected = src.Select(static v => v.Length()).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void LengthSquared_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var result = new float[src.Length];
        op.LengthSquared(src, result);

        var expected = src.Select(static v => v.LengthSquared()).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void Distance_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.Distance(src, other, result);

        var expected = src.Select((v, i) => Vector4.Distance(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }

    [Test]
    public void DistanceSquared_ShouldMatchScalar()
    {
        var op = CreateOperation();
        var src = CreateSource();
        var other = CreateOther();
        var result = new float[src.Length];
        op.DistanceSquared(src, other, result);

        var expected = src.Select((v, i) => Vector4.DistanceSquared(v, other[i])).ToArray();
        AssertFloatSpanEqual(expected, result);
    }
}

[TestFixture]
public class SimdVectorOperationTests : Vector2OperationTestsBase
{
    protected override ISpanVectorOperation<Vector2> CreateOperation() => new SimdVector2Operation();
}

[TestFixture]
public class SimdVector3OperationTests : Vector3OperationTestsBase
{
    protected override ISpanVectorOperation<Vector3> CreateOperation() => new SimdVector3Operation();
}

[TestFixture]
public class SimdVector4OperationTests : Vector4OperationTestsBase
{
    protected override ISpanVectorOperation<Vector4> CreateOperation() => new SimdVector4Operation();
}

[TestFixture]
public class ParallelVector2OperationTests : Vector2OperationTestsBase
{
    protected override ISpanVectorOperation<Vector2> CreateOperation() => new ParallelVector2Operation();
}

[TestFixture]
public class ParallelVector3OperationTests : Vector3OperationTestsBase
{
    protected override ISpanVectorOperation<Vector3> CreateOperation() => new ParallelVector3Operation();
}

[TestFixture]
public class ParallelVector4OperationTests : Vector4OperationTestsBase
{
    protected override ISpanVectorOperation<Vector4> CreateOperation() => new ParallelVector4Operation();
}

[TestFixture]
public class ParallelSimdVectorOperationTests : Vector2OperationTestsBase
{
    protected override ISpanVectorOperation<Vector2> CreateOperation() => new ParallelSimdVector2Operation();
}

[TestFixture]
public class ParallelSimdVector3OperationTests : Vector3OperationTestsBase
{
    protected override ISpanVectorOperation<Vector3> CreateOperation() => new ParallelSimdVector3Operation();
}

[TestFixture]
public class ParallelSimdVector4OperationTests : Vector4OperationTestsBase
{
    protected override ISpanVectorOperation<Vector4> CreateOperation() => new ParallelSimdVector4Operation();
}

[TestFixture]
public class SerialVector2OperationTests : Vector2OperationTestsBase
{
    protected override ISpanVectorOperation<Vector2> CreateOperation() => new SerialVector2Operation();
}

[TestFixture]
public class SerialVector3OperationTests : Vector3OperationTestsBase
{
    protected override ISpanVectorOperation<Vector3> CreateOperation() => new SerialVector3Operation();
}

[TestFixture]
public class SerialVector4OperationTests : Vector4OperationTestsBase
{
    protected override ISpanVectorOperation<Vector4> CreateOperation() => new SerialVector4Operation();
}