using System.Numerics;
using static Gubbins.Enhance.Tests.SimdVectorAssertions;

namespace Gubbins.Enhance.Tests;

[TestFixture]
public class SimdVectorOperationTests
{
    private ISpanVectorOperations<Vector2> m_Operation = null!;

    [SetUp]
    public void SetUp()
    {
        m_Operation = CreateOperation();
    }

    [Test]
    public void Dot_Cross_Length_And_Distance_ShouldMatchScalarImplementation()
    {
        var src = CreateSourceVectors();
        var other = CreateOtherVectors();

        var dotResult = new Vector2[src.Length];
        var crossResult = new Vector2[src.Length];
        var lengthResult = new float[src.Length];
        var lengthSquaredResult = new float[src.Length];
        var distanceResult = new float[src.Length];
        var distanceSquaredResult = new float[src.Length];

        m_Operation.Dot(src, other, dotResult);
        m_Operation.Cross(src, other, crossResult);
        m_Operation.Length(src, lengthResult);
        m_Operation.LengthSquared(src, lengthSquaredResult);
        m_Operation.Distance(src, other, distanceResult);
        m_Operation.DistanceSquared(src, other, distanceSquaredResult);

        var expectedDot = src.Select((value, index) =>
        {
            var dot = Vector2.Dot(value, other[index]);
            return new Vector2(dot, dot);
        }).ToArray();
        var expectedCross = src.Select((value, index) =>
        {
            var otherValue = other[index];
            var cross = value.X * otherValue.Y - value.Y * otherValue.X;
            return new Vector2(cross, cross);
        }).ToArray();
        var expectedLength = src.Select(static value => value.Length()).ToArray();
        var expectedLengthSquared = src.Select(static value => value.LengthSquared()).ToArray();
        var expectedDistance = src.Select((value, index) => Vector2.Distance(value, other[index])).ToArray();
        var expectedDistanceSquared = src.Select((value, index) => Vector2.DistanceSquared(value, other[index])).ToArray();

        Assert.Multiple(() =>
        {
            AssertVector2SpanEqual(expectedDot, dotResult);
            AssertVector2SpanEqual(expectedCross, crossResult);
            AssertFloatSpanEqual(expectedLength, lengthResult);
            AssertFloatSpanEqual(expectedLengthSquared, lengthSquaredResult);
            AssertFloatSpanEqual(expectedDistance, distanceResult);
            AssertFloatSpanEqual(expectedDistanceSquared, distanceSquaredResult);
        });
    }

    [Test]
    public void Normalize_Reflect_Refract_And_FaceForward_ShouldMatchScalarImplementation()
    {
        var src = CreateSourceVectors();
        var normals = CreateNormalVectors();
        var incidents = CreateIncidentValues(src.Length);
        var eta = new Vector2(0.65f, 123f);

        var normalizeResult = new Vector2[src.Length];
        var reflectResult = new Vector2[src.Length];
        var refractResult = new Vector2[src.Length];
        var faceForwardIncidentResult = new Vector2[src.Length];
        var faceForwardNormalResult = new Vector2[src.Length];

        m_Operation.Normalize(src, normalizeResult);
        m_Operation.Reflect(src, normals, reflectResult);
        m_Operation.Refract(src, normals, eta, refractResult);
        m_Operation.FaceForward(src, normals, incidents, faceForwardIncidentResult);
        m_Operation.FaceForward(src, normals, faceForwardNormalResult);

        var expectedNormalize = src.Select(static value => Vector2.Normalize(value)).ToArray();
        var expectedReflect = src.Select((value, index) => Vector2.Reflect(value, normals[index])).ToArray();
        var expectedRefract = src.Select((value, index) => RefractScalar(value, normals[index], eta.X)).ToArray();
        var expectedFaceForwardIncident = src.Select((value, index) => incidents[index] < 0f ? value : -value).ToArray();
        var expectedFaceForwardNormal = src.Select((value, index) => Vector2.Dot(normals[index], value) < 0f ? value : -value).ToArray();

        Assert.Multiple(() =>
        {
            AssertVector2SpanEqual(expectedNormalize, normalizeResult);
            AssertVector2SpanEqual(expectedReflect, reflectResult);
            AssertVector2SpanEqual(expectedRefract, refractResult);
            AssertVector2SpanEqual(expectedFaceForwardIncident, faceForwardIncidentResult);
            AssertVector2SpanEqual(expectedFaceForwardNormal, faceForwardNormalResult);
        });
    }

    [Test]
    public void Angle_And_MoveTowards_ShouldMatchScalarImplementation()
    {
        var src = CreateSourceVectors();
        var other = CreateOtherVectors();
        var maxDistanceDelta = CreateMaxDistanceDelta(src.Length);

        var angleResult = new float[src.Length];
        var moveTowardsResult = new Vector2[src.Length];

        m_Operation.Angle(src, other, angleResult);
        m_Operation.MoveTowards(src, other, maxDistanceDelta, moveTowardsResult);

        var expectedAngle = src.Select((value, index) => AngleScalar(value, other[index])).ToArray();
        var expectedMoveTowards = src.Select((value, index) => MoveTowardsScalar(value, other[index], maxDistanceDelta[index])).ToArray();

        Assert.Multiple(() =>
        {
            AssertFloatSpanEqual(expectedAngle, angleResult);
            AssertVector2SpanEqual(expectedMoveTowards, moveTowardsResult);
        });
    }

    private static Vector2[] CreateSourceVectors()
    {
        var length = Vector<float>.Count / 2 + 5;
        var values = new Vector2[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                1 => Vector2.Zero,
                3 => new Vector2(-3.5f, 0f),
                5 => new Vector2(0.25f, -7.5f),
                _ => new Vector2(i - 2.75f, i % 2 == 0 ? i * 0.5f - 1.25f : -i * 0.75f + 0.5f)
            };
        }

        return values;
    }

    private static Vector2[] CreateOtherVectors()
    {
        var length = Vector<float>.Count / 2 + 5;
        var values = new Vector2[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                2 => Vector2.Zero,
                4 => new Vector2(-1.5f, 2.5f),
                _ => new Vector2(2.25f - i * 0.4f, i % 3 == 0 ? -1.75f + i : 0.8f - i * 0.35f)
            };
        }

        return values;
    }

    private static Vector2[] CreateNormalVectors()
    {
        var length = Vector<float>.Count / 2 + 5;
        var values = new Vector2[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                0 => new Vector2(0f, 1f),
                2 => new Vector2(1f, 0f),
                4 => new Vector2(-0.6f, 0.8f),
                _ => Vector2.Normalize(new Vector2(0.35f * (i + 1), i % 2 == 0 ? -0.5f : 0.9f))
            };
        }

        return values;
    }

    private static float[] CreateIncidentValues(int length)
    {
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i % 3 switch
            {
                0 => -1f - i,
                1 => 0f,
                _ => 0.5f + i
            };
        }

        return values;
    }

    private static float[] CreateMaxDistanceDelta(int length)
    {
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i % 4 switch
            {
                0 => 0f,
                1 => 0.35f * (i + 1),
                2 => 10f,
                _ => -0.25f * (i + 1)
            };
        }

        return values;
    }

    private static float AngleScalar(Vector2 left, Vector2 right)
    {
        var denominator = left.Length() * right.Length();
        if (denominator <= 0f)
        {
            return 0f;
        }

        var dot = Vector2.Dot(left, right) / denominator;
        return MathF.Acos(Math.Clamp(dot, -1f, 1f));
    }

    private static Vector2 RefractScalar(Vector2 incident, Vector2 normal, float eta)
    {
        var dot = Vector2.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f
            ? Vector2.Zero
            : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    private static Vector2 MoveTowardsScalar(Vector2 current, Vector2 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f)
        {
            return target;
        }

        return current + toTarget / distance * maxDistanceDelta;
    }

    private static ISpanVectorOperations<Vector2> CreateOperation()
    {
        var type = typeof(ISpanVectorOperations<Vector2>).Assembly.GetType("Gubbins.Enhance.SimdVectorOperation", throwOnError: true)!;
        return (ISpanVectorOperations<Vector2>) Activator.CreateInstance(type, nonPublic: true)!;
    }

    private static void AssertVector2SpanEqual(IReadOnlyList<Vector2> expected, IReadOnlyList<Vector2> actual, float tolerance = 1e-5f)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.That(FloatEquals(expected[i].X, actual[i].X, tolerance), Is.True, $"X mismatch at index {i}: expected {expected[i].X}, actual {actual[i].X}");
            Assert.That(FloatEquals(expected[i].Y, actual[i].Y, tolerance), Is.True, $"Y mismatch at index {i}: expected {expected[i].Y}, actual {actual[i].Y}");
        }
    }

    private static void AssertFloatSpanEqual(IReadOnlyList<float> expected, IReadOnlyList<float> actual, float tolerance = 1e-5f)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.That(FloatEquals(expected[i], actual[i], tolerance), Is.True, $"Value mismatch at index {i}: expected {expected[i]}, actual {actual[i]}");
        }
    }

    private static bool FloatEquals(float expected, float actual, float tolerance)
    {
        if (float.IsNaN(expected) || float.IsNaN(actual))
        {
            return float.IsNaN(expected) && float.IsNaN(actual);
        }

        if (float.IsInfinity(expected) || float.IsInfinity(actual))
        {
            return expected.Equals(actual);
        }

        return MathF.Abs(expected - actual) <= tolerance;
    }
}

[TestFixture]
public class SimdVector3OperationTests
{
    private ISpanVectorOperations<Vector3> m_Operation = null!;

    [SetUp]
    public void SetUp()
    {
        m_Operation = CreateOperation();
    }

    [Test]
    public void Dot_Cross_Length_And_Distance_ShouldMatchScalarImplementation()
    {
        var src = CreateVector3Source();
        var other = CreateVector3Other();

        var dotResult = new Vector3[src.Length];
        var crossResult = new Vector3[src.Length];
        var lengthResult = new float[src.Length];
        var lengthSquaredResult = new float[src.Length];
        var distanceResult = new float[src.Length];
        var distanceSquaredResult = new float[src.Length];

        m_Operation.Dot(src, other, dotResult);
        m_Operation.Cross(src, other, crossResult);
        m_Operation.Length(src, lengthResult);
        m_Operation.LengthSquared(src, lengthSquaredResult);
        m_Operation.Distance(src, other, distanceResult);
        m_Operation.DistanceSquared(src, other, distanceSquaredResult);

        var expectedDot = src.Select((value, index) =>
        {
            var dot = Vector3.Dot(value, other[index]);
            return new Vector3(dot, dot, dot);
        }).ToArray();
        var expectedCross = src.Select((value, index) => Vector3.Cross(value, other[index])).ToArray();
        var expectedLength = src.Select(static value => value.Length()).ToArray();
        var expectedLengthSquared = src.Select(static value => value.LengthSquared()).ToArray();
        var expectedDistance = src.Select((value, index) => Vector3.Distance(value, other[index])).ToArray();
        var expectedDistanceSquared = src.Select((value, index) => Vector3.DistanceSquared(value, other[index])).ToArray();

        Assert.Multiple(() =>
        {
            AssertVector3SpanEqual(expectedDot, dotResult);
            AssertVector3SpanEqual(expectedCross, crossResult);
            AssertFloatSpanEqual(expectedLength, lengthResult);
            AssertFloatSpanEqual(expectedLengthSquared, lengthSquaredResult);
            AssertFloatSpanEqual(expectedDistance, distanceResult);
            AssertFloatSpanEqual(expectedDistanceSquared, distanceSquaredResult);
        });
    }

    [Test]
    public void Normalize_Reflect_Refract_FaceForward_Angle_And_MoveTowards_ShouldMatchScalarImplementation()
    {
        var src = CreateVector3Source();
        var normals = CreateVector3Normals();
        var target = CreateVector3Other();
        var incidents = CreateIncidentValues(src.Length);
        var maxDistanceDelta = CreateMaxDistanceDelta(src.Length);
        var eta = new Vector3(0.6f, 123f, -999f);

        var normalizeResult = new Vector3[src.Length];
        var reflectResult = new Vector3[src.Length];
        var refractResult = new Vector3[src.Length];
        var faceForwardIncidentResult = new Vector3[src.Length];
        var faceForwardNormalResult = new Vector3[src.Length];
        var angleResult = new float[src.Length];
        var moveTowardsResult = new Vector3[src.Length];

        m_Operation.Normalize(src, normalizeResult);
        m_Operation.Reflect(src, normals, reflectResult);
        m_Operation.Refract(src, normals, eta, refractResult);
        m_Operation.FaceForward(src, normals, incidents, faceForwardIncidentResult);
        m_Operation.FaceForward(src, normals, faceForwardNormalResult);
        m_Operation.Angle(src, target, angleResult);
        m_Operation.MoveTowards(src, target, maxDistanceDelta, moveTowardsResult);

        var expectedNormalize = src.Select(static value => Vector3.Normalize(value)).ToArray();
        var expectedReflect = src.Select((value, index) => Vector3.Reflect(value, normals[index])).ToArray();
        var expectedRefract = src.Select((value, index) => RefractVector3(value, normals[index], eta.X)).ToArray();
        var expectedFaceForwardIncident = src.Select((value, index) => incidents[index] < 0f ? value : -value).ToArray();
        var expectedFaceForwardNormal = src.Select((value, index) => Vector3.Dot(normals[index], value) < 0f ? value : -value).ToArray();
        var expectedAngle = src.Select((value, index) => AngleVector3(value, target[index])).ToArray();
        var expectedMoveTowards = src.Select((value, index) => MoveTowardsVector3(value, target[index], maxDistanceDelta[index])).ToArray();

        Assert.Multiple(() =>
        {
            AssertVector3SpanEqual(expectedNormalize, normalizeResult);
            AssertVector3SpanEqual(expectedReflect, reflectResult);
            AssertVector3SpanEqual(expectedRefract, refractResult);
            AssertVector3SpanEqual(expectedFaceForwardIncident, faceForwardIncidentResult);
            AssertVector3SpanEqual(expectedFaceForwardNormal, faceForwardNormalResult);
            AssertFloatSpanEqual(expectedAngle, angleResult);
            AssertVector3SpanEqual(expectedMoveTowards, moveTowardsResult);
        });
    }

    private static Vector3[] CreateVector3Source()
    {
        var length = Vector<float>.Count + 3;
        var values = new Vector3[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                1 => Vector3.Zero,
                4 => new Vector3(-2f, 3f, -4f),
                7 => new Vector3(0.5f, -7.5f, 2.25f),
                _ => new Vector3(i - 3.25f, i % 2 == 0 ? i * 0.6f - 1f : -i * 0.4f + 0.75f, i % 3 == 0 ? -i * 0.3f : i * 0.2f - 0.5f)
            };
        }

        return values;
    }

    private static Vector3[] CreateVector3Other()
    {
        var length = Vector<float>.Count + 3;
        var values = new Vector3[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                2 => Vector3.Zero,
                5 => new Vector3(3f, -1f, 2f),
                _ => new Vector3(2.5f - i * 0.35f, i % 2 == 0 ? 1.5f - i * 0.2f : -1.25f + i * 0.45f, i % 4 == 0 ? -0.75f : 0.5f + i * 0.15f)
            };
        }

        return values;
    }

    private static Vector3[] CreateVector3Normals()
    {
        var length = Vector<float>.Count + 3;
        var values = new Vector3[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                0 => Vector3.UnitY,
                2 => Vector3.UnitX,
                4 => Vector3.Normalize(new Vector3(-0.4f, 0.8f, 0.45f)),
                _ => Vector3.Normalize(new Vector3(0.2f * (i + 1), i % 2 == 0 ? -0.5f : 0.75f, i % 3 == 0 ? 0.35f : -0.6f))
            };
        }

        return values;
    }

    private static float[] CreateIncidentValues(int length)
    {
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i % 3 switch {0 => -1f - i, 1 => 0f, _ => 0.5f + i};
        }

        return values;
    }

    private static float[] CreateMaxDistanceDelta(int length)
    {
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i % 4 switch {0 => 0f, 1 => 0.25f * (i + 1), 2 => 10f, _ => -0.15f * (i + 1)};
        }

        return values;
    }

    private static float AngleVector3(Vector3 left, Vector3 right)
    {
        var denominator = left.Length() * right.Length();
        return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(left, right) / denominator, -1f, 1f));
    }

    private static Vector3 RefractVector3(Vector3 incident, Vector3 normal, float eta)
    {
        var dot = Vector3.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector3.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    private static Vector3 MoveTowardsVector3(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f) return target;
        return current + toTarget / distance * maxDistanceDelta;
    }

    private static ISpanVectorOperations<Vector3> CreateOperation()
    {
        var type = typeof(ISpanVectorOperations<Vector3>).Assembly.GetType("Gubbins.Enhance.SimdVector3Operation", throwOnError: true)!;
        return (ISpanVectorOperations<Vector3>) Activator.CreateInstance(type, nonPublic: true)!;
    }
}

[TestFixture]
public class SimdVector4OperationTests
{
    private ISpanVectorOperations<Vector4> m_Operation = null!;

    [SetUp]
    public void SetUp()
    {
        m_Operation = CreateOperation();
    }

    [Test]
    public void Dot_Cross_Length_And_Distance_ShouldMatchScalarImplementation()
    {
        var src = CreateVector4Source();
        var other = CreateVector4Other();

        var dotResult = new Vector4[src.Length];
        var crossResult = new Vector4[src.Length];
        var lengthResult = new float[src.Length];
        var lengthSquaredResult = new float[src.Length];
        var distanceResult = new float[src.Length];
        var distanceSquaredResult = new float[src.Length];

        m_Operation.Dot(src, other, dotResult);
        m_Operation.Cross(src, other, crossResult);
        m_Operation.Length(src, lengthResult);
        m_Operation.LengthSquared(src, lengthSquaredResult);
        m_Operation.Distance(src, other, distanceResult);
        m_Operation.DistanceSquared(src, other, distanceSquaredResult);

        var expectedDot = src.Select((value, index) =>
        {
            var dot = Vector4.Dot(value, other[index]);
            return new Vector4(dot, dot, dot, dot);
        }).ToArray();
        var expectedCross = src.Select((value, index) => CrossVector4(value, other[index])).ToArray();
        var expectedLength = src.Select(static value => value.Length()).ToArray();
        var expectedLengthSquared = src.Select(static value => value.LengthSquared()).ToArray();
        var expectedDistance = src.Select((value, index) => Vector4.Distance(value, other[index])).ToArray();
        var expectedDistanceSquared = src.Select((value, index) => Vector4.DistanceSquared(value, other[index])).ToArray();

        Assert.Multiple(() =>
        {
            AssertVector4SpanEqual(expectedDot, dotResult);
            AssertVector4SpanEqual(expectedCross, crossResult);
            AssertFloatSpanEqual(expectedLength, lengthResult);
            AssertFloatSpanEqual(expectedLengthSquared, lengthSquaredResult);
            AssertFloatSpanEqual(expectedDistance, distanceResult);
            AssertFloatSpanEqual(expectedDistanceSquared, distanceSquaredResult);
        });
    }

    [Test]
    public void Normalize_Reflect_Refract_FaceForward_Angle_And_MoveTowards_ShouldMatchScalarImplementation()
    {
        var src = CreateVector4Source();
        var normals = CreateVector4Normals();
        var target = CreateVector4Other();
        var incidents = CreateIncidentValues(src.Length);
        var maxDistanceDelta = CreateMaxDistanceDelta(src.Length);
        var eta = new Vector4(0.7f, 99f, -3f, 11f);

        var normalizeResult = new Vector4[src.Length];
        var reflectResult = new Vector4[src.Length];
        var refractResult = new Vector4[src.Length];
        var faceForwardIncidentResult = new Vector4[src.Length];
        var faceForwardNormalResult = new Vector4[src.Length];
        var angleResult = new float[src.Length];
        var moveTowardsResult = new Vector4[src.Length];

        m_Operation.Normalize(src, normalizeResult);
        m_Operation.Reflect(src, normals, reflectResult);
        m_Operation.Refract(src, normals, eta, refractResult);
        m_Operation.FaceForward(src, normals, incidents, faceForwardIncidentResult);
        m_Operation.FaceForward(src, normals, faceForwardNormalResult);
        m_Operation.Angle(src, target, angleResult);
        m_Operation.MoveTowards(src, target, maxDistanceDelta, moveTowardsResult);

        var expectedNormalize = src.Select(static value => Vector4.Normalize(value)).ToArray();
        var expectedReflect = src.Select((value, index) => ReflectVector4(value, normals[index])).ToArray();
        var expectedRefract = src.Select((value, index) => RefractVector4(value, normals[index], eta.X)).ToArray();
        var expectedFaceForwardIncident = src.Select((value, index) => incidents[index] < 0f ? value : -value).ToArray();
        var expectedFaceForwardNormal = src.Select((value, index) => Vector4.Dot(normals[index], value) < 0f ? value : -value).ToArray();
        var expectedAngle = src.Select((value, index) => AngleVector4(value, target[index])).ToArray();
        var expectedMoveTowards = src.Select((value, index) => MoveTowardsVector4(value, target[index], maxDistanceDelta[index])).ToArray();

        Assert.Multiple(() =>
        {
            AssertVector4SpanEqual(expectedNormalize, normalizeResult);
            AssertVector4SpanEqual(expectedReflect, reflectResult);
            AssertVector4SpanEqual(expectedRefract, refractResult);
            AssertVector4SpanEqual(expectedFaceForwardIncident, faceForwardIncidentResult);
            AssertVector4SpanEqual(expectedFaceForwardNormal, faceForwardNormalResult);
            AssertFloatSpanEqual(expectedAngle, angleResult);
            AssertVector4SpanEqual(expectedMoveTowards, moveTowardsResult);
        });
    }

    private static Vector4[] CreateVector4Source()
    {
        var length = Vector<float>.Count / 4 + 5;
        var values = new Vector4[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                1 => Vector4.Zero,
                3 => new Vector4(-2f, 3f, -4f, 1f),
                6 => new Vector4(0.5f, -7.5f, 2.25f, -0.75f),
                _ => new Vector4(i - 2.25f, i % 2 == 0 ? i * 0.4f - 1f : -i * 0.3f + 0.5f, i % 3 == 0 ? -i * 0.2f : i * 0.15f - 0.25f, i % 4 == 0 ? 1f : -0.5f * i)
            };
        }

        return values;
    }

    private static Vector4[] CreateVector4Other()
    {
        var length = Vector<float>.Count / 4 + 5;
        var values = new Vector4[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                2 => Vector4.Zero,
                4 => new Vector4(3f, -1f, 2f, 4f),
                _ => new Vector4(2.75f - i * 0.25f, i % 2 == 0 ? 1.25f - i * 0.15f : -1.5f + i * 0.35f, i % 3 == 0 ? -0.6f : 0.25f + i * 0.2f, i % 2 == 0 ? -0.75f * i : 0.5f * i)
            };
        }

        return values;
    }

    private static Vector4[] CreateVector4Normals()
    {
        var length = Vector<float>.Count / 4 + 5;
        var values = new Vector4[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i switch
            {
                0 => Vector4.UnitY,
                2 => Vector4.UnitX,
                4 => Vector4.Normalize(new Vector4(-0.4f, 0.8f, 0.45f, -0.1f)),
                _ => Vector4.Normalize(new Vector4(0.2f * (i + 1), i % 2 == 0 ? -0.4f : 0.7f, i % 3 == 0 ? 0.3f : -0.55f, i % 4 == 0 ? 0.25f : -0.35f))
            };
        }

        return values;
    }

    private static float[] CreateIncidentValues(int length)
    {
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i % 3 switch {0 => -1f - i, 1 => 0f, _ => 0.5f + i};
        }

        return values;
    }

    private static float[] CreateMaxDistanceDelta(int length)
    {
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            values[i] = i % 4 switch {0 => 0f, 1 => 0.25f * (i + 1), 2 => 10f, _ => -0.15f * (i + 1)};
        }

        return values;
    }

    private static Vector4 CrossVector4(Vector4 left, Vector4 right) =>
        new(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X, 0f);

    private static Vector4 ReflectVector4(Vector4 src, Vector4 normal) =>
        src - normal * (2f * Vector4.Dot(src, normal));

    private static Vector4 RefractVector4(Vector4 incident, Vector4 normal, float eta)
    {
        var dot = Vector4.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector4.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    private static float AngleVector4(Vector4 left, Vector4 right)
    {
        var denominator = left.Length() * right.Length();
        return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector4.Dot(left, right) / denominator, -1f, 1f));
    }

    private static Vector4 MoveTowardsVector4(Vector4 current, Vector4 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f) return target;
        return current + toTarget / distance * maxDistanceDelta;
    }

    private static ISpanVectorOperations<Vector4> CreateOperation()
    {
        var type = typeof(ISpanVectorOperations<Vector4>).Assembly.GetType("Gubbins.Enhance.SimdVector4Operation", throwOnError: true)!;
        return (ISpanVectorOperations<Vector4>) Activator.CreateInstance(type, nonPublic: true)!;
    }
}

internal static class SimdVectorAssertions
{
    internal static void AssertVector2SpanEqual(IReadOnlyList<Vector2> expected, IReadOnlyList<Vector2> actual, float tolerance = 1e-5f)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.That(FloatEquals(expected[i].X, actual[i].X, tolerance), Is.True, $"X mismatch at index {i}: expected {expected[i].X}, actual {actual[i].X}");
            Assert.That(FloatEquals(expected[i].Y, actual[i].Y, tolerance), Is.True, $"Y mismatch at index {i}: expected {expected[i].Y}, actual {actual[i].Y}");
        }
    }

    internal static void AssertVector3SpanEqual(IReadOnlyList<Vector3> expected, IReadOnlyList<Vector3> actual, float tolerance = 1e-5f)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.That(FloatEquals(expected[i].X, actual[i].X, tolerance), Is.True, $"X mismatch at index {i}: expected {expected[i].X}, actual {actual[i].X}");
            Assert.That(FloatEquals(expected[i].Y, actual[i].Y, tolerance), Is.True, $"Y mismatch at index {i}: expected {expected[i].Y}, actual {actual[i].Y}");
            Assert.That(FloatEquals(expected[i].Z, actual[i].Z, tolerance), Is.True, $"Z mismatch at index {i}: expected {expected[i].Z}, actual {actual[i].Z}");
        }
    }

    internal static void AssertVector4SpanEqual(IReadOnlyList<Vector4> expected, IReadOnlyList<Vector4> actual, float tolerance = 1e-5f)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.That(FloatEquals(expected[i].X, actual[i].X, tolerance), Is.True, $"X mismatch at index {i}: expected {expected[i].X}, actual {actual[i].X}");
            Assert.That(FloatEquals(expected[i].Y, actual[i].Y, tolerance), Is.True, $"Y mismatch at index {i}: expected {expected[i].Y}, actual {actual[i].Y}");
            Assert.That(FloatEquals(expected[i].Z, actual[i].Z, tolerance), Is.True, $"Z mismatch at index {i}: expected {expected[i].Z}, actual {actual[i].Z}");
            Assert.That(FloatEquals(expected[i].W, actual[i].W, tolerance), Is.True, $"W mismatch at index {i}: expected {expected[i].W}, actual {actual[i].W}");
        }
    }

    internal static void AssertFloatSpanEqual(IReadOnlyList<float> expected, IReadOnlyList<float> actual, float tolerance = 1e-5f)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.That(FloatEquals(expected[i], actual[i], tolerance), Is.True, $"Value mismatch at index {i}: expected {expected[i]}, actual {actual[i]}");
        }
    }

    internal static bool FloatEquals(float expected, float actual, float tolerance)
    {
        if (float.IsNaN(expected) || float.IsNaN(actual))
            return float.IsNaN(expected) && float.IsNaN(actual);
        if (float.IsInfinity(expected) || float.IsInfinity(actual))
            return expected.Equals(actual);
        return MathF.Abs(expected - actual) <= tolerance;
    }
}