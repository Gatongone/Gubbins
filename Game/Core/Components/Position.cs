using System.Numerics;
using System.Runtime.InteropServices;
using Gubbins.Span;

namespace Gubbins.Game.Components;

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct PositionX(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct PositionY(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct PositionZ(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>))]
public partial struct PositionXY(float x, float y)
{
    public float X = x;
    public float Y = y;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Z:Y"])]
public partial struct PositionXZ(float x, float z)
{
    public float X = x;
    public float Z = z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Y:X", "Z:Y"])]
public partial struct PositionYZ(float y, float z)
{
    public float Y = y;
    public float Z = z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector3>))]
public partial struct Position(float x, float y, float z)
{
    public float x = x;
    public float y = y;
    public float z = z;
}