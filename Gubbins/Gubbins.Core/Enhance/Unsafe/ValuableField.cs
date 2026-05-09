using System.Reflection;

namespace Gubbins.Enhance;

/// <summary>
/// Represents a valuable member, which can be either a field or a property, and provides information about its type, offset, size, and other characteristics.
/// </summary>
public partial class ValuableMember
{
    /// <summary>
    /// Indicates whether the member is a field or a property. This is determined based on the type of MemberInfo passed to the constructor.
    /// </summary>
    private readonly bool m_IsField;

    /// <summary>
    /// Field offset, which is the byte offset of the field within its containing type.
    /// This is calculated using the Native.GetFieldOffset method and is used for direct memory access to the field's value.
    /// </summary>
    private readonly int m_Offset;

    /// <summary>
    /// Field size, which is the size in bytes of the field's type.
    /// This is used for memory management and optimization when accessing the field's value.
    /// </summary>
    private readonly uint m_Size;

    /// <summary>
    /// Indicates whether the member's type is a value type (struct) or a reference type (class). This is determined by checking the IsValueType property of the member's type.
    /// </summary>
    /// <param name="fieldInfo">FieldInfo object representing the field for which the ValuableMember is being created. This parameter is used to extract information about the field's type, offset, and size.</param>
    internal ValuableMember(FieldInfo fieldInfo)
    {
        m_SetMethod             = null!;
        m_GetMethod             = null!;
        m_StructValueSetWrapper = null!;
        m_StructValueGetWrapper = null!;
        MemberType              = fieldInfo.FieldType;
        m_Offset                = (int) Native.GetFieldOffset(fieldInfo);
        m_Size                  = Native.GetStackSize(MemberType);
        m_IsField               = true;
        m_IsValueType           = MemberType.CheckType().IsValueType;
        m_TypeCode              = Type.GetTypeCode(MemberType);
    }
}