using System.Numerics;
using System.Runtime.InteropServices;
using Gubbins.Span;

namespace Gubbins.Game.Components;

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct RotationX
{
    public float Value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct RotationY
{
    public float Value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct RotationZ
{
    public float Value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>))]
public partial struct RotationXY(float X, float Y)
{
    public float X;
    public float Y;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Z:Y"])]
public partial struct RotationXZ
{
    public float X;
    public float Z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Y:X", "Z:Y"])]
public partial struct RotationYZ
{
    public float Y;
    public float Z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector3>))]
public partial struct Rotation
{
    public float X;
    public float Y;
    public float Z;
}

[Serializable, SpanTo(typeof(ISpanVectorOperation<Vector4>))]
public partial struct Orientation
{
    public float X;
    public float Y;
    public float Z;
    public float W;
}