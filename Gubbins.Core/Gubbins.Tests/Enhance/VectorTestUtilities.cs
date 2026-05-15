using System.Numerics;

namespace Gubbins.Unsafe.Tests;

internal static class ParallelVectorTestScalars
{
    internal static Vector2 RefractVector2(Vector2 incident, Vector2 normal, float eta)
    {
        var dot = Vector2.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector2.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    internal static float AngleVector2(Vector2 left, Vector2 right)
    {
        var denominator = left.Length() * right.Length();
        return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector2.Dot(left, right) / denominator, -1f, 1f));
    }

    internal static Vector2 MoveTowardsVector2(Vector2 current, Vector2 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f) return target;
        return current + toTarget / distance * maxDistanceDelta;
    }

    internal static Vector3 RefractVector3(Vector3 incident, Vector3 normal, float eta)
    {
        var dot = Vector3.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector3.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    internal static float AngleVector3(Vector3 left, Vector3 right)
    {
        var denominator = left.Length() * right.Length();
        return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(left, right) / denominator, -1f, 1f));
    }

    internal static Vector3 MoveTowardsVector3(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f) return target;
        return current + toTarget / distance * maxDistanceDelta;
    }

    internal static Vector4 CrossVector4(Vector4 left, Vector4 right) =>
        new(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X, 0f);

    internal static Vector4 ReflectVector4(Vector4 src, Vector4 normal) =>
        src - normal * (2f * Vector4.Dot(src, normal));

    internal static Vector4 RefractVector4(Vector4 incident, Vector4 normal, float eta)
    {
        var dot = Vector4.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector4.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    internal static float AngleVector4(Vector4 left, Vector4 right)
    {
        var denominator = left.Length() * right.Length();
        return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector4.Dot(left, right) / denominator, -1f, 1f));
    }

    internal static Vector4 MoveTowardsVector4(Vector4 current, Vector4 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f) return target;
        return current + toTarget / distance * maxDistanceDelta;
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

