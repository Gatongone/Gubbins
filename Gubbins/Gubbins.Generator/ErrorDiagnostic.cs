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
    }
}