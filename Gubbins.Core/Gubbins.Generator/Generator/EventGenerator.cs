using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

[Generator]
public class EventGenerator : ISourceGenerator
{
    private const string EVENT_NAMESPACE     = "Gubbins.Context";
    private const string EVENT_BUS_NAMESPACE = "Gubbins.Events";
    private const string FILE_NAME           = "EventListener.g.cs";

    private const string CODE_BODY_WITH_NAMESPACE =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        using Gubbins.Events;
        using Gubbins.Context;

        namespace {Namespace}
        {
        {TypeStart}
            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}void IEventListener.Listen(IDependenciesResolver resolver, IDependenciesRegistry registry)
            {Indentation}{
        {ListenEvent}
            {Indentation}}

            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}void IEventListener.Clear(IDependenciesResolver resolver)
            {Indentation}{
        {ClearEvent}
            {Indentation}}
        {TypeEnd}
        }
        """;

    private const string CODE_BODY_WITHOUT_NAMESPACE =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        using Gubbins.Events;
        using Gubbins.Context;

        {TypeStart}
            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}void IEventListener.Listen(IDependenciesResolver resolver, IDependenciesRegistry registry)
            {Indentation}{
        {ListenEvent}
            {Indentation}}

            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}void IEventListener.Clear(IDependenciesResolver resolver)
            {Indentation}{
        {ClearEvent}
            {Indentation}}
        {TypeEnd}
        """;

    private const string CLEAR_BODY = "{Indentation}resolver.Resolve<{EventName}>(nameof({EventName})).Clear();";

    private const string LISTEN_BODY =
        """
        {Indentation}var event{Index} = resolver.Resolve<{EventName}>(nameof({EventName}));
        {Indentation}if (event{Index} == null)
        {Indentation}{
        {Indentation}    event{Index} = new {EventName}();
        {Indentation}    registry.Register(event{Index})
        {Indentation}            .BindTo<{Bindings}>()
        {Indentation}            .WithKey(nameof({EventName}))
        {Indentation}            .AsSingleton();
        {Indentation}}
        {Indentation}event{Index}.{Subscribe}({MethodName});
        """;

    public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(static () => new SyntaxContextReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxReceiver) return;

        foreach (var info in syntaxReceiver.Infos)
        {
            ProcessInfo(context, info);
        }
    }

    private void ProcessInfo(GeneratorExecutionContext context, TypeInfo typeInfo)
    {
        var containsNamespace = !string.IsNullOrEmpty(typeInfo.Namespace) && !typeInfo.Namespace.Equals("<global namespace>");
        var bodyTemplate = containsNamespace ? CODE_BODY_WITH_NAMESPACE : CODE_BODY_WITHOUT_NAMESPACE;
        var indentationLevel = containsNamespace ? typeInfo.NestedTypes.Length : typeInfo.NestedTypes.Length - 1;
        var indentation = new string('\t', indentationLevel);

        var listenBody = new StringBuilder();
        var clearBody = new StringBuilder();
        var index = 0;

        foreach (var methodInfo in typeInfo.Methods)
        {
            ProcessMethodInfo(context, methodInfo, listenBody, clearBody, ref index, containsNamespace, typeInfo);
        }

        var body = FormatBody(bodyTemplate, typeInfo, containsNamespace, indentation, listenBody, clearBody);
        context.AddSource($"{typeInfo.FullName}.{FILE_NAME}", body);
    }

    private void ProcessMethodInfo(GeneratorExecutionContext context, MethodInfo methodInfo, StringBuilder listenBody, StringBuilder clearBody, ref int index, bool containsNamespace, TypeInfo typeInfo)
    {
        var registeredEvent = new HashSet<string>();
        foreach (var eventInfo in methodInfo.Events)
        {
            if (!ValidateEventInfo(context, eventInfo, methodInfo)) continue;
            var eventName = eventInfo.Symbol.ToDisplayString();
            listenBody.AppendLine(FormatListenBody(eventInfo, methodInfo, typeInfo, index, containsNamespace));
            if (registeredEvent.Add(eventName))
            {
                clearBody.AppendLine(FormatClearBody(eventInfo, typeInfo, containsNamespace));
            }

            index++;
        }
    }

    private static bool ValidateEventInfo(GeneratorExecutionContext context, EventInfo eventInfo, MethodInfo methodInfo)
    {
        if (!eventInfo.CanBeSubscribed)
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.TypeCantBeSubscribed, methodInfo.Location, eventInfo.Symbol.ToDisplayString());
            return false;
        }


        if (!(methodInfo.ParameterTypes.Length == 0
            && eventInfo.ParameterTypes.Length == 1
            && eventInfo.ParameterTypes[0].Equals("Gubbins.Enhance.Unit")))
        {
            if (!eventInfo.ParameterTypes.SequenceEqual(methodInfo.ParameterTypes))
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.ParametersNotMatch, methodInfo.Location, eventInfo.Symbol.ToDisplayString(), string.Join(", ", eventInfo.ParameterTypes));
                return false;
            }
        }

        if (!eventInfo.Symbol.IsNewable())
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.TypeDoesNotExistPublicEmptyCtor, eventInfo.Location, eventInfo.Symbol.ToDisplayString());
            return false;
        }

        return true;
    }

    private string FormatClearBody(EventInfo eventInfo, TypeInfo typeInfo, bool containsNamespace)
    {
        var clearBody = CLEAR_BODY;
        var indentation = containsNamespace ? typeInfo.NestedTypes.Length : typeInfo.NestedTypes.Length - 1;
        var eventName = eventInfo.Symbol.ToDisplayString();

        // ReSharper disable InconsistentNaming
        return clearBody.Format
        (
            Indentation => new string('\t', indentation + 2),
            EventName => eventName
        );
    }

    private string FormatListenBody(EventInfo eventInfo, MethodInfo methodInfo, TypeInfo info, int index, bool containsNamespace)
    {
        var indentation = containsNamespace ? info.NestedTypes.Length : info.NestedTypes.Length - 1;
        var eventName = eventInfo.Symbol.ToDisplayString();

        // ReSharper disable InconsistentNaming
        return LISTEN_BODY.Format
        (
            Indentation => new string('\t', indentation + 2),
            Index => index.ToString(),
            EventName => eventName,
            MethodName => methodInfo.Name,
            Bindings => BuildBindings(eventInfo.Symbol, eventInfo.Notification),
            // Prefer strong subscription; fall back to weak when the event is weak-only.
            Subscribe => Implements(eventInfo.Symbol, "IEventSubscriable", generic: true) ? "Subscribe" : "SubscribeWeakly"
        );
    }

    /// <summary>
    /// Builds the <c>BindTo&lt;...&gt;</c> type-argument list from the event interfaces the target type
    /// actually implements, so e.g. a subscribe-only event is not bound to <c>IEventBroadcastable&lt;T&gt;</c>.
    /// </summary>
    private static string BuildBindings(ITypeSymbol type, string notification)
    {
        var bindings = new List<string>();

        // Generic interfaces carrying the notification type.
        if (Implements(type, "IEvent", generic: true))                bindings.Add($"IEvent<{notification}>");
        if (Implements(type, "IEventBroadcastable", generic: true))   bindings.Add($"IEventBroadcastable<{notification}>");
        if (Implements(type, "IEventSubscriable", generic: true))     bindings.Add($"IEventSubscriable<{notification}>");
        if (Implements(type, "IWeakEventSubscriable", generic: true)) bindings.Add($"IWeakEventSubscriable<{notification}>");

        // Non-generic (Unit) interfaces, only present on parameterless events.
        if (Implements(type, "IEvent", generic: false))                bindings.Add("IEvent");
        if (Implements(type, "IEventBroadcastable", generic: false))   bindings.Add("IEventBroadcastable");
        if (Implements(type, "IEventSubscriable", generic: false))     bindings.Add("IEventSubscriable");
        if (Implements(type, "IWeakEventSubscriable", generic: false)) bindings.Add("IWeakEventSubscriable");

        return string.Join(", ", bindings);
    }

    /// <summary>
    /// Whether <paramref name="type"/> implements the given <c>Gubbins.Events</c> interface,
    /// distinguishing the generic (<c>I...&lt;T&gt;</c>) and non-generic (Unit) variants by arity.
    /// </summary>
    private static bool Implements(ITypeSymbol type, string interfaceName, bool generic) =>
        type.AllInterfaces.Any(symbol =>
            symbol.ContainingNamespace.ToDisplayString().Equals(EVENT_BUS_NAMESPACE) &&
            symbol.Name.Equals(interfaceName) &&
            symbol.TypeArguments.Length == (generic ? 1 : 0));

    // ReSharper disable InconsistentNaming
    private string FormatBody(string bodyTemplate, TypeInfo typeInfo, bool containsNamespace, string indentation, StringBuilder listenBody, StringBuilder clearBody) => bodyTemplate.Format
    (
        Generator => nameof(EventGenerator),
        TypeStart => typeInfo.NestedTypes.GetTypeStringBegin(containsNamespace ? 0 : -1),
        TypeEnd => typeInfo.NestedTypes.GetTypeStringEnd(containsNamespace ? 0 : -1),
        CreateTime => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        Namespace => typeInfo.Namespace,
        Indentation => indentation,
        ListenEvent => listenBody.ToString(),
        ClearEvent => clearBody.ToString()
    );

    private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
    {
        public readonly List<TypeInfo> Infos = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (!TryGetTypeSymbol(context, out var typeSymbol))
            {
                return;
            }

            var methods = new List<MethodInfo>();
            foreach (var methodInfo in typeSymbol!.GetMembers().OfType<IMethodSymbol>().Select(ProcessMethod))
            {
                if (methodInfo != null)
                {
                    methods.Add(methodInfo.Value);
                }
            }

            var info = new TypeInfo
            {
                NestedTypes = typeSymbol.GetNestedTypeName(),
                Namespace   = typeSymbol.ContainingNamespace.ToDisplayString(),
                FullName    = typeSymbol.ToDisplayString(),
                Methods     = methods.ToArray()
            };
            Infos.Add(info);
        }

        private static bool TryGetTypeSymbol(GeneratorSyntaxContext context, out ITypeSymbol? typeSymbol)
        {
            typeSymbol = context.SemanticModel.GetDeclaredSymbol(context.Node) as ITypeSymbol;
            return typeSymbol is
            {
                IsAbstract: false,
                IsStatic  : false
            } && typeSymbol.ContainsInterface($"{EVENT_NAMESPACE}.IEventListener");
        }

        private static MethodInfo? ProcessMethod(IMethodSymbol method)
        {
            var attributeData = method.GetAttributes().Where(MatchEventAttribute).ToArray();

            if (!attributeData.Any()) return null;

            var events = new List<EventInfo>();

            foreach (var eventInfo in attributeData.Select(ProcessAttribute))
            {
                if (eventInfo != null)
                {
                    events.Add(eventInfo.Value);
                }
            }

            return new MethodInfo
            {
                ParameterTypes = method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray(),
                Events         = events.ToArray(),
                Name           = method.Name,
                Location       = method.Locations.First()
            };

            bool MatchEventAttribute(AttributeData data)
            {
                var namespaceMatch = data.AttributeClass!.ContainingNamespace.ToDisplayString() == EVENT_NAMESPACE;
                var nameMatch = data.AttributeClass!.Name == "EventAttribute" || data.AttributeClass!.Name == "Event";
                return namespaceMatch && nameMatch;
            }
        }

        private static EventInfo? ProcessAttribute(AttributeData attribute)
        {
            ITypeSymbol type;
            switch (attribute.ConstructorArguments.Length)
            {
                case 0:
                    type = attribute.AttributeClass!.TypeArguments.First();
                    break;
                case 1:
                    type = (ITypeSymbol) attribute.ConstructorArguments[0].Value!;
                    break;
                default: return null;
            }

            var eventInfo = new EventInfo
            {
                Symbol         = type,
                Location       = type.Locations.First(),
                CanBeSubscribed     = TryGetEventBusInfo(type, out var notification, out var parameterTypes),
                Notification   = notification,
                ParameterTypes = parameterTypes
            };

            return eventInfo;
        }

        private static bool TryGetEventBusInfo(ITypeSymbol type, out string notification, out string[] parameterTypes)
        {
            notification   = string.Empty;
            parameterTypes = [];

            if (!type.TryGetInterface(MatchEventBus, out var interfaceSymbol))
            {
                return false;
            }

            var genericArg = (INamedTypeSymbol) interfaceSymbol!.TypeArguments.First();
            notification   = genericArg.ToDisplayString();
            parameterTypes = DetermineParameterTypes(genericArg);
            return true;

            bool MatchEventBus(INamedTypeSymbol symbol) =>
                symbol.ContainingNamespace.ToDisplayString().Equals(EVENT_BUS_NAMESPACE) &&
                (symbol.Name.Equals("IEventSubscriable") || symbol.Name.Equals("IWeakEventSubscriable")) &&
                symbol.TypeArguments.Length == 1;
        }

        private static string[] DetermineParameterTypes(INamedTypeSymbol genericArg)
        {
            if (genericArg.ContainingNamespace.ToDisplayString().Equals($"{nameof(Gubbins)}.Enhance") && genericArg.Name.Equals("Void"))
            {
                return [];
            }

            if (genericArg.ContainingNamespace.ToDisplayString().Equals(nameof(System)) && genericArg.Name.Equals(nameof(ValueTuple)))
            {
                return genericArg.TypeArguments.Select(t => t.ToDisplayString()).ToArray();
            }

            return [genericArg.ToDisplayString()];
        }
    }

    private struct TypeInfo
    {
        public (string TypeName, string TypeKind)[] NestedTypes;
        public string                               Namespace;
        public string                               FullName;
        public MethodInfo[]                         Methods;
    }

    private struct MethodInfo
    {
        public string      Name;
        public EventInfo[] Events;
        public string[]    ParameterTypes;
        public Location    Location;
    }

    private struct EventInfo
    {
        public ITypeSymbol Symbol;
        public string[]    ParameterTypes;
        public string      Notification;
        public bool        CanBeSubscribed;
        public Location    Location;
    }
}