// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/18-03:22:46
// Github: https://github.com/Gatongone

using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

internal static class ErrorDiagnostic
{
    public static class Id
    {
        // General:
        internal const string TYPE_DOES_NOT_EXIST_PUBLIC_EMPTY_CTOR = "GUB0001";

        // Domains:
        internal const string MODULE_IS_VALUE_TYPE = "GUB1001";
        internal const string MODULE_BASE_TYPE_INVALID = "GUB1002";
        internal const string CATEGORY_TYPE_ERROR = "GUB1003";

        // Events:
        internal const string PARAMETERS_NOT_MATCH = "GUB2001";
        internal const string TYPE_IS_NOT_EVENT_BUS = "GUB2002";

        // Span Operations:
        internal const string SPAN_OPERATION_TYPE_INVALID = "GUB3001";
        internal const string SPAN_OPERATION_TYPE_NOT_GENERIC = "GUB3002";
        internal const string SPAN_OPERATION_MEMBER_NOT_FOUND = "GUB3003";
        internal const string SPAN_OPERATION_MEMBER_AMBIGUOUS = "GUB3004";
        internal const string SPAN_OPERATION_METHOD_NOT_SUPPORTED = "GUB3005";
        internal const string SPAN_OPERATION_COMPONENT_RETURN_NOT_WRITABLE = "GUB3006";
        internal const string SPAN_OPERATION_CONFLICT = "GUB3007";
    }

    public static class Descriptor
    {
        // General:
        public static readonly DiagnosticDescriptor TypeDoesNotExistPublicEmptyCtor = new(
            id: Id.TYPE_DOES_NOT_EXIST_PUBLIC_EMPTY_CTOR,
            title: "Non public empty constructor.",
            messageFormat: "Type must contains a public empty constructor method. Type: {0}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // Entities:
        public static readonly DiagnosticDescriptor ModuleIsValueType = new(
            id: Id.MODULE_IS_VALUE_TYPE,
            title: "Can't register Module.",
            messageFormat: "Module can't be value type. Module: {0}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ModuleBaseTypeInvalid = new(
            id: Id.MODULE_BASE_TYPE_INVALID,
            title: "Not supported Module type.",
            messageFormat: "Only the 'UnityEngine.ScriptableObject' and 'UnityEngine.Component' two types implementations of `UnityEngine.Object` type Module are supported.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CategoryTypeError = new(
            id: Id.CATEGORY_TYPE_ERROR,
            title: "Category type error.",
            messageFormat: "Type must be assignable from Gubbins.Entities.ICategory. Type: {0}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // Message:
        public static readonly DiagnosticDescriptor ParametersNotMatch = new(
            id: Id.PARAMETERS_NOT_MATCH,
            title: "Parameters not match.",
            messageFormat: "Event parameter can't be matched event \"{0}\" with parameters: ({1}).",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TypeIsNotEventBus = new(
            id: Id.TYPE_IS_NOT_EVENT_BUS,
            title: "Type is not event bus.",
            messageFormat: "Type must be assignable from Gubbins.Message.IEventBus<TNotification>. Type: {0}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // Span Operations:
        public static readonly DiagnosticDescriptor SpanOperationTypeInvalid = new(
            id: Id.SPAN_OPERATION_TYPE_INVALID,
            title: "Invalid span operation interface.",
            messageFormat: "Type must be an interface implementing Gubbins.Enhance.ISpanOperation. Type: {0}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SpanOperationTypeNotGeneric = new(
            id: Id.SPAN_OPERATION_TYPE_NOT_GENERIC,
            title: "Span operation interface generic arguments are invalid.",
            messageFormat: "Span operation interface must be a generic interface with at least one type argument. Type: {0}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SpanOperationMemberNotFound = new(
            id: Id.SPAN_OPERATION_MEMBER_NOT_FOUND,
            title: "No component member matches span operation value type.",
            messageFormat: "Component '{0}' does not contain a readable field/property of type '{1}' for operation '{2}'.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SpanOperationMemberAmbiguous = new(
            id: Id.SPAN_OPERATION_MEMBER_AMBIGUOUS,
            title: "Multiple component members match span operation value type.",
            messageFormat: "Component '{0}' has multiple readable fields/properties of type '{1}' for operation '{2}': {3}.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SpanOperationMethodNotSupported = new(
            id: Id.SPAN_OPERATION_METHOD_NOT_SUPPORTED,
            title: "Span operation method shape is not supported.",
            messageFormat: "Method '{0}' on operation '{1}' contains unsupported parameter or return type for component proxy generation.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SpanOperationComponentReturnNotWritable = new(
            id: Id.SPAN_OPERATION_COMPONENT_RETURN_NOT_WRITABLE,
            title: "Component return value cannot be materialized.",
            messageFormat: "Method '{0}' on operation '{1}' returns component '{2}', but member '{3}' is not writable in object initializer.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SpanOperationConflict = new(
            id: Id.SPAN_OPERATION_CONFLICT,
            title: "Span operation generation conflict.",
            messageFormat: "Component '{0}' cannot generate both operations '{1}' and '{2}' because they collapse to the same generated surface '{3}'.",
            category: "DiagnosticsGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}