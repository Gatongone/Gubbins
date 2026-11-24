using System.Numerics;
using System.Runtime.InteropServices;
using Gubbins.Span;

namespace Gubbins.Game;

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct ScaleX(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct ScaleY(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct ScaleZ(float value)
{
    public float Value = value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>))]
public partial struct ScaleXY(float x, float y)
{
    public float X = x;
    public float Y = y;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Z:Y"])]
public partial struct ScaleXZ(float x, float z)
{
    public float X = x;
    public float Z = z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Y:X", "Z:Y"])]
public partial struct ScaleYZ(float y, float z)
{
    public float Y = y;
    public float Z = z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector3>))]
public partial struct Scale(float x, float y, float z)
{
    public float x;
    public float y;
    public float z;
}