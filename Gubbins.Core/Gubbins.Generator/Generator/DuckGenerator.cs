using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

[Generator]
public class DuckGenerator : ISourceGenerator
{
    private const string FILE_NAME = "Duck.g.cs";

    private const string INTERFACE_BODY =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        using System.Linq;
        using System.Collections.Concurrent;
        using System.Reflection;
        using Gubbins.Spawner;
       
        namespace Gubbins.Generated
        {
            /// <summary>
            /// Auto generated.
            /// </summary>
            [global::System.Runtime.CompilerServices.CompilerGenerated]
            internal class Duck_{DisplayName} : Duck<{Interface}>
            {
                /// <summary>
                /// Check whether the target can be converted to a duck type for <see cref="{Interface}"/> type.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                public override bool Like(object obj, out {Interface} result)
                {
                    return Like(obj, out result, out _);
                }

                /// <summary>
                /// Check whether the target can be converted to a duck type for <see cref="{Interface}"/> type.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                public override bool Like(object obj, out {Interface} result, out PooledHandle handle)
                {
                    if (obj == null)
                    {
                        result = null;
                        handle = default;
                        return false;
                    }
                    if (obj is {Interface} target)
                    {
                        result = target;
                        handle = default;
                        return true;
                    }
                
                    var type = obj.GetType();
                    if (UnmatchedCache.TryGetValue(type, out var unmatchedTypes) && unmatchedTypes.Contains(type))
                    {
                        result = null;
                        handle = default;
                        return false;
                    }
                
                    if (!MatchedCache.TryGetValue(type, out var ducks))
                    {
                        ducks = new Dictionary<Type, ISpawner<IProxy>>();
                        MatchedCache.TryAdd(type, ducks);
                    }
                    if (!ducks.TryGetValue(typeof({Interface}), out var pool))
                    {
                        pool = Pool<Proxy_{DisplayName}>.Default;
                        ducks.Add(typeof({Interface}), pool);
                    }
                
                    var pooledObj = pool.Spawn();
                    if (!pooledObj.TryInit(obj))
                    {
                        if (unmatchedTypes == null)
                        {
                            unmatchedTypes = [];
                            UnmatchedCache.TryAdd(type, unmatchedTypes);
                        }
                        unmatchedTypes.Add(type);
                        result = null;
                        handle = default;
                        return false;
                    }
                    result = ({Interface}) pooledObj;
                    handle = new PooledHandle((IPool) pool, pooledObj);
                    return result != null;
                }

                /// <summary>
                /// Auto generated.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                private class Proxy_{DisplayName} : IProxy, {Interface}
                {
        {Delegates}
        {Fields}
                    /// <summary>
                    /// Auto generated.
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    private Type m_Type;
                
                    /// <summary>
                    /// Auto generated.
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    private object m_Proxy;

                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    void IResetable.Reset() => m_Proxy = null;

                    /// <summary>
                    /// Auto generated.
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    private void ClearAllCache()
                    {
                        m_Proxy = null;
                        m_Type = null;
                        {Reset}
                    }

                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    public override bool Equals(object obj)
                    {
                        return m_Proxy == null ? false : m_Proxy.Equals(obj);
                    }
                
                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    public override int GetHashCode()
                    {
                        return m_Proxy == null ? 0 : m_Proxy.GetHashCode();
                    }

                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    public bool TryInit(object obj)
                    {
                        if (obj.GetType() == m_Type)
                        {
                            m_Proxy = obj;
                            return true;
                        }
                        ClearAllCache();
                        m_Proxy = obj;
                        m_Type = obj.GetType();
        {MemberInit}
                        return true;
                    }
        {Methods}
                }
            }
        }
        """;

    private const string DELEGATE_BODY =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}
        
        using System.Linq;
        using System.Collections.Concurrent;
        using System.Reflection;
        using Gubbins.Enhance;
        
        namespace Gubbins.Generated
        {
            /// <summary>
            /// Auto generated.
            /// </summary>
            [global::System.Runtime.CompilerServices.CompilerGenerated]
            public class Duck_{DisplayName} : Duck<{Delegate}>
            {
                /// <summary>
                /// Check whether the target can be converted to a duck type for <see cref="{Delegate}"/> type.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                public override bool Like(object obj, out {Delegate} result)
                {
                    return Like(obj, out result, out _);
                }
        
                /// <summary>
                /// Check whether the target can be converted to a duck type for <see cref="{Delegate}"/> type.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                public override bool Like(object obj, out {Delegate} result, out PooledHandle handle)
                {
                    if (obj == null)
                    {
                        result = null;
                        handle = default;
                        return false;
                    }
                    if (obj is {Delegate} target)
                    {
                        result = target;
                        handle = default;
                        return true;
                    }
                
                    var type = obj.GetType();
                    if (UnmatchedCache.TryGetValue(type, out var unmatchedTypes) && unmatchedTypes.Contains(type))
                    {
                        result = null;
                        handle = default;
                        return false;
                    }
                
                    if (!MatchedCache.TryGetValue(type, out var ducks))
                    {
                        ducks = new Dictionary<Type, ISpawner<IProxy>>();
                        MatchedCache.TryAdd(type, ducks);
                    }
                    if (!ducks.TryGetValue(typeof({Delegate}), out var pool))
                    {
                        pool = Pool<Proxy_{DisplayName}>.Default;
                        ducks.Add(typeof({Delegate}), pool);
                    }
                
                    var pooledObj = pool.Spawn();
                    if (!pooledObj.TryInit(obj))
                    {
                        if (unmatchedTypes == null)
                        {
                            unmatchedTypes = [];
                            UnmatchedCache.TryAdd(type, unmatchedTypes);
                        }
                        unmatchedTypes.Add(type);
                        result = null;
                        handle = default;
                        return false;
                    }
                    result = ((Proxy_{DisplayName}) pooledObj).{MethodName};
                    handle = new PooledHandle((IPool) pool, pooledObj);
                    return result != null;
                }

                /// <summary>
                /// Auto generated.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                private class Proxy_{DisplayName} : IProxy
                {
        {Delegates}
        {Fields}
                    /// <summary>
                    /// Auto generated.
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    private Type m_Type;

                    /// <summary>
                    /// Auto generated.
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    private object m_Proxy;
        
                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    void IResetable.Reset() => m_Proxy = null;
                    
                    /// <summary>
                    /// Auto generated.
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    private void ClearAllCache()
                    {
                        m_Proxy = null;
                        m_Type = null;
        {Reset}
                    }
        
                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    public override bool Equals(object obj)
                    {
                        return m_Proxy == null ? false : m_Proxy.Equals(obj);
                    }
                
                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    public override int GetHashCode()
                    {
                        return m_Proxy == null ? 0 : m_Proxy.GetHashCode();
                    }

                    /// <inheritdoc />
                    [global::System.Runtime.CompilerServices.CompilerGenerated]
                    public bool TryInit(object obj)
                    {
                        if (obj.GetType() == m_Type)
                        {
                            m_Proxy = obj;
                            return true;
                        }
                        ClearAllCache();
                        m_Proxy = obj;
                        m_Type = obj.GetType();
        {MemberInit}
                        return true;
                    }
        {Methods}
                }
            }
        }
        """;

    public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(static () => new SyntaxContextReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxReceiver) return;
        foreach (var info in syntaxReceiver.Infos)
        {
            var body = info.IsInterface ? GenerateInterfaceProxy(info) : GenerateDelegateProxy(info);
            context.AddSource($"{info.FullName}.{FILE_NAME}", body);
        }
    }

    private static string GenerateDelegateProxy(FeatureInfo info)
    {
        var method = info.Methods[0];
        var implements = GetMethodImplement(method, 0);
        var memberInit = GetMemberInitString(method, 0);
        var delegates = GetMethodDelegate(method, 0);
        var members = GetProxyField(method, 0);
        var resets = GetResetBody(method, 0);

        // ReSharper disable InconsistentNaming
        var body = DELEGATE_BODY.Format
        (
            Generator => nameof(DuckGenerator),
            CreateTime => DateTime.Now.ToString(CultureInfo.InvariantCulture),
            Delegate => info.FullName,
            DisplayName => info.DisplayName,
            Delegates => delegates,
            Fields => members,
            Methods => implements,
            MemberInit => memberInit,
            Reset => resets,
            MethodName => method.Name
        );
        return body;
    }

    private static string GenerateInterfaceProxy(FeatureInfo info)
    {
        var resets = new StringBuilder();
        var implements = new StringBuilder();
        var memberInit = new StringBuilder();
        var delegates = new StringBuilder();
        var members = new StringBuilder();
        for (var index = 0; index < info.Methods.Length; index++)
        {
            var method = info.Methods[index];
            resets.AppendLine(GetResetBody(method, index));
            delegates.AppendLine(GetMethodDelegate(method, index));
            members.AppendLine(GetProxyField(method, index));
            implements.AppendLine(GetMethodImplement(method, index));
            memberInit.AppendLine(GetMemberInitString(method, index));
        }

        // ReSharper disable InconsistentNaming
        var body = INTERFACE_BODY.Format
        (
            Generator => nameof(DuckGenerator),
            CreateTime => DateTime.Now.ToString(CultureInfo.InvariantCulture),
            Interface => info.FullName,
            DisplayName => info.DisplayName,
            Delegates => delegates.ToString(),
            Fields => members.ToString(),
            Methods => implements.ToString(),
            MemberInit => memberInit.ToString(),
            Reset => resets.ToString()
        );
        return body;
    }

    private static string GetResetBody(MethodInfo method, int index)
    {
        return method.HasGenericType
            ? $"                m_Func{index}Cache.Clear();"
            : string.Empty;
    }

    private static string GetProxyField(MethodInfo method, int index) =>
        method.HasGenericType
            ? $"""
                           /// <summary>
                           /// Auto generated.
                           /// </summary>
                           [global::System.Runtime.CompilerServices.CompilerGenerated]
                           private System.Reflection.MethodInfo m_Func{index};
               """
            : $"""
                           /// <summary>
                           /// Auto generated.
                           /// </summary>
                           [global::System.Runtime.CompilerServices.CompilerGenerated]
                           private Func{index}<object> m_Func{index};
               """;

    private static string GetMethodDelegate(MethodInfo method, int index) =>
        method.HasGenericType
            ? $"""
                           /// <summary>
                           /// Auto generated.
                           /// </summary>
                           [global::System.Runtime.CompilerServices.CompilerGenerated]
                           private System.Collections.Generic.Dictionary<System.Type, System.Delegate> m_Func{index}Cache = new();

                           /// <summary>
                           /// Auto generated.
                           /// </summary>
                           [global::System.Runtime.CompilerServices.CompilerGenerated]
                           private delegate {method.ReturnType.ToDisplayString()} Func{index}<_T_, {string.Join(", ", method.GenericNames)}>(_T_ _{(method.Args.Length == 0 ? string.Empty : ", " + string.Join(", ", method.Args.Select(static arg => $"{arg.Type.ToDisplayString()} {arg.Name}")))});
               """
            : $"""
                           /// <summary>
                           /// Auto generated.
                           /// </summary>
                           [global::System.Runtime.CompilerServices.CompilerGenerated]
                           private delegate {method.ReturnType.ToDisplayString()} Func{index}<T>(T _{(method.Args.Length == 0 ? string.Empty : ", " + string.Join(", ", method.Args.Select(static arg => $"{arg.Type.ToDisplayString()} {arg.Name}")))});
               """;

    private static string GetMemberInitString(MethodInfo method, int index) =>
        method.HasGenericType
            ? $$"""

                                var func{{index}} = m_Type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(static method =>
                                {
                                    if (method.Name != "{{method.Name}}") return false;
                                    var parameters = method.GetParameters();
                                    if (parameters.Length != {{method.Args.Length}}) return false;
                {{
                    string.Join("\n", method.Args.Select(static (arg, index) =>
                        arg.Type is ITypeParameterSymbol
                            ? $"                    if (!parameters[{index}].ParameterType.IsGenericParameter) return false;"
                            : $"                    if (parameters[{index}].ParameterType.FullName != \"{arg.Type.ContainingNamespace}.{arg.Type.MetadataName}\") return false;"))
                }}
                                    return true;
                                }).FirstOrDefault();
                                if (func{{index}} == null) return false;
                                m_Func{{index}} = func{{index}};
                """
            : $$"""

                                var func{{index}} = m_Type.GetMethod("{{method.Name}}", [{{string.Join(", ", method.Args.Select(static arg => $"typeof({arg.Type.ToDisplayString()})"))}}])?.CreateDelegate(typeof(Func{{index}}<>).MakeGenericType(m_Type));
                                if (func{{index}} == null) return false;
                                m_Func{{index}} = Native.Cast<Func{{index}}<object>>(func{{index}});
                """;

    private static string GetMethodImplement(MethodInfo method, int index)
    {
        var methodBody = method.HasGenericType
            ? $$"""
                                var _types = {{(method.GenericNames.Length == 1 ? $"typeof({string.Join(", ", method.GenericNames)})" : $"({string.Join(", ", method.GenericNames.Select(static arg => $"typeof({arg})"))}}};)")}};
                                if (!m_Func{{index}}Cache.TryGetValue(_types, out var _func))
                                {
                                    var _argTypes = new System.Type[{{method.GenericNames.Length + 1}}];
                                    _argTypes[0] = m_Type;
               {{
                   string.Join("\n", method.GenericNames.Select(static (arg, index) => $"                     _argTypes[{index + 1}] = typeof({arg});"))
               }}
                                    _func = m_Func{{index}}.MakeGenericMethod(System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray(_argTypes, Range.StartAt(1))).CreateDelegate(typeof(Func{{index}}<{{new string(',', method.GenericNames.Length)}}>).MakeGenericType(_argTypes));
                                    m_Func{{index}}Cache.Add(_types, _func);
                                }
                                var _castedFunc = Native.Cast<Func{{index}}<object, {{string.Join(", ", method.GenericNames)}}>>(_func);

                                {{(method.IsVoidReturn ? string.Empty : "return ")}}_castedFunc.Invoke(m_Proxy{{(method.Args.Length == 0 ? string.Empty : ", " + string.Join(",", method.Args.Select(static arg => arg.Name).ToArray()))}});
               """
            : $"                {(method.IsVoidReturn ? string.Empty : "return ")}m_Func{index}.Invoke(m_Proxy{(method.Args.Length == 0 ? string.Empty : ", " + string.Join(",", method.Args.Select(static arg => arg.Name).ToArray()))});";
        return $$"""
                 
                             /// <summary>
                             /// Auto generated.
                             /// </summary>
                             [global::System.Runtime.CompilerServices.CompilerGenerated]
                             public {{method.ReturnType}} {{method.Name}}{{(method.GenericNames.Length == 0 ? string.Empty : $"<{string.Join(", ", method.GenericNames)}>")}}({{string.Join(", ", method.Args.Select(static arg => $"{arg.Type.ToDisplayString()} {arg.Name}"))}}){{(string.IsNullOrEmpty(method.Constraints) ? string.Empty : " " + method.Constraints)}}
                             {
                                 if (m_Proxy == null) throw new System.ObjectDisposedException(nameof(m_Proxy));
                 {{methodBody}}
                             }
                 """;
    }

    private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
    {
        public readonly List<FeatureInfo> Infos = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            if (symbol is not INamedTypeSymbol {TypeKind: TypeKind.Interface or TypeKind.Delegate} typeSymbol
                || !typeSymbol.ContainsAttribute("Gubbins.Enhance", "DuckAttribute"))
            {
                return;
            }

            var info = typeSymbol.TypeKind == TypeKind.Delegate
                ? new FeatureInfo
                {
                    IsInterface = false,
                    FullName    = typeSymbol.ToDisplayString(),
                    DisplayName = typeSymbol.ToDisplayString().Replace(".", "_").Replace("+", "_").Replace("`", "_"), // Safe name
                    Methods =
                    [
                        new MethodInfo
                        {
                            Name           = typeSymbol.Name,
                            ReturnType     = typeSymbol.DelegateInvokeMethod!.ReturnType,
                            Args           = typeSymbol.DelegateInvokeMethod.Parameters.Select(static p => (p.Type, p.Name)).ToArray(),
                            HasGenericType = typeSymbol.DelegateInvokeMethod.IsGenericMethod,
                            GenericNames   = typeSymbol.DelegateInvokeMethod.TypeParameters.Select(static p => p.Name).ToArray(),
                            Constraints    = typeSymbol.DelegateInvokeMethod.GetConstraintsString()
                        }
                    ]
                }
                : new FeatureInfo
                {
                    IsInterface = true,
                    FullName    = typeSymbol.ToDisplayString(),
                    DisplayName = typeSymbol.ToDisplayString().Replace(".", "_").Replace("+", "_").Replace("`", "_"), // Safe name
                    Methods = typeSymbol.GetMethods().Select(m => new MethodInfo
                    {
                        Name           = m.Name,
                        ReturnType     = m.ReturnType,
                        Args           = m.Parameters.Select(static p => (p.Type, p.Name)).ToArray(),
                        HasGenericType = m.IsGenericMethod,
                        GenericNames   = m.TypeParameters.Select(static p => p.Name).ToArray(),
                        Constraints    = m.GetConstraintsString()
                    }).ToArray()
                };
            Infos.Add(info);
        }
    }

    private struct MethodInfo
    {
        public string                            Name;
        public (ITypeSymbol Type, string Name)[] Args;
        public ITypeSymbol                       ReturnType;
        public bool                              HasGenericType;
        public string[]                          GenericNames;
        public string                            Constraints;
        public bool IsVoidReturn => ReturnType.SpecialType == SpecialType.System_Void;
    }

    private struct FeatureInfo
    {
        public bool   IsInterface;
        public string FullName;
        public string DisplayName;

        public MethodInfo[] Methods;
    }
}