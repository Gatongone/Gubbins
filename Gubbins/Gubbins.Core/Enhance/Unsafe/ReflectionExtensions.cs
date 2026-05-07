using System.Reflection;

namespace Gubbins.Enhance;

public static class ReflectionExtensions
{
    private const string SYSTEM_FUNC_NAME = "System.Func";
    private const string SYSTEM_ACTION_NAME = "System.Action";

    public static Delegate? CreateSetDelegate(this PropertyInfo property)
    {
        return CreateGetSetDelegate(property.SetMethod);
    }

    public static Delegate? CreateGetDelegate(this PropertyInfo property)
    {
        return CreateGetSetDelegate(property.GetMethod);
    }

    private static Delegate? CreateGetSetDelegate(MethodInfo method)
    {
        var sourceType = method.DeclaringType;

        if (method.ContainsGenericParameters)
            throw new ArgumentException("Can't not create delegate from the method with generic parameters.");

        var sourceTypeChecker = sourceType!.CheckType();
        var isValueType = sourceType is {IsValueType: true};
        var isVoidReturn = method.ReturnType == typeof(void);
        var isStatic = sourceTypeChecker.IsStatic || method.IsStatic;
        var genericTypes =  Pool<List<Type>>.Default.Spawn();
        string typeName;

        //Add source type
        if (!isStatic)
        {
            genericTypes.Add(sourceType!);
            if (!isValueType)
                typeName = isVoidReturn ? MethodWithoutReturnValue.CLASS_METHOD_NAME : MethodWithReturnValue.CLASS_METHOD_NAME;
            else
                typeName = isVoidReturn ? MethodWithoutReturnValue.STRUCT_METHOD_NAME : MethodWithReturnValue.STRUCT_METHOD_NAME;
        }
        else
        {
            typeName = isVoidReturn ? SYSTEM_ACTION_NAME : SYSTEM_FUNC_NAME;
        }

        //Add parameters
        genericTypes.AddRange(method.GetParameters().Select(static parameter => parameter.ParameterType));

        //Add return
        if (!isVoidReturn)
            genericTypes.Add(method.ReturnType);

        var delegateType = Reflection.GetType(typeName, genericTypes.ToArray());
        Pool<List<Type>>.Default.Recycle(genericTypes);
        return delegateType != null ? method.CreateDelegate(delegateType) : null;
    }
}