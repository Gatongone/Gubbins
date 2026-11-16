using System.Numerics;
using System.Runtime.InteropServices;
using Gubbins.Span;

namespace Gubbins.Game.Components;

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct ScaleX
{
    public float Value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct ScaleY
{
    public float Value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanAs(typeof(ISpanRealOperation<float>))]
public partial struct ScaleZ
{
    public float Value;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>))]
public partial struct ScaleXY(float X, float Y)
{
    public float X;
    public float Y;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Z:Y"])]
public partial struct ScaleXZ
{
    public float X;
    public float Z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector2>), Lane = ["Y:X", "Z:Y"])]
public partial struct ScaleYZ
{
    public float Y;
    public float Z;
}

[Serializable, StructLayout(LayoutKind.Sequential), SpanTo(typeof(ISpanVectorOperation<Vector3>))]
public partial struct Scale
{
    public float X;
    public float Y;
    public float Z;
}