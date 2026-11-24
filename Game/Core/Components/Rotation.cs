using System.Numerics;
using System.Runtime.InteropServices;
using Gubbins.Span;

namespace Gubbins.Game;

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct RotationX(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct RotationY(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct RotationZ(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>))]
public partial struct RotationXY(float x, float y)
{
    public float X = x;
    public float Y = y;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Z:Y"])]
public partial struct RotationXZ(float x, float z)
{
    public float X = x;
    public float Z = z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Y:X", "Z:Y"])]
public partial struct RotationYZ(float y, float z)
{
    public float Y = y;
    public float Z = z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector3>))]
public partial struct Rotation(float x, float y, float z)
{
    public float x = x;
    public float y = y;
    public float z = z;
}

[Serializable, SpanTo(typeof(ISpanVectorOperation<Vector4>))]
public partial struct Orientation(float x, float y, float z, float w)
{
    public float X = x;
    public float Y = y;
    public float Z = z;
    public float W = w;
}