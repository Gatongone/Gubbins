using System.Reflection;

namespace Gubbins.Enhance;

public partial class ValuableMember
{
    private readonly bool m_IsField;
    private readonly int m_Offset;
    private readonly uint m_Size;

    internal ValuableMember(FieldInfo fieldInfo)
    {
        m_SetMethod = null!;
        m_GetMethod = null!;
        m_StructValueSetWrapper = null!;
        m_StructValueGetWrapper = null!;
        MemberType = fieldInfo.FieldType;
        m_Offset = (int) Native.GetFieldOffset(fieldInfo);
        m_Size = Native.GetStackSize(MemberType);
        m_IsField = true;
        m_IsValueType = MemberType.CheckType().IsValueType;
        m_TypeCode = Type.GetTypeCode(MemberType);
    }
}