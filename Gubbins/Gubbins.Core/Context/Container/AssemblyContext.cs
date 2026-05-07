using System.Collections.Concurrent;
using System.Reflection;
using Gubbins.Enhance;

namespace Gubbins.Context;

/// <summary>
/// An application context that was configured from scanning assemblies.
/// </summary>
/// <param name="scanWithParallel">Foreach assembly with parallel.</param>
/// <param name="assemblies">Given assemblies.</param>
public class AssemblyContext(bool scanWithParallel, params Assembly[] assemblies) : IContext, IDependenciesRegistry
{
    /// <summary>
    /// The proxy target.
    /// </summary>
    private readonly ApplicationContext m_ApplicationContext = new(ScanAssemblies(scanWithParallel, assemblies));

    /// <inheritdoc />
    public IContext? Parent => m_ApplicationContext.Parent;

    /// <inheritdoc />
    public void Dispose() => m_ApplicationContext.Dispose();

    /// <inheritdoc />
    public object? Resolve(Type? type, string? key) => m_ApplicationContext.Resolve(type, key);

    /// <inheritdoc />
    public object?[] ResolveAll(Type type) => m_ApplicationContext.ResolveAll(type);

    /// <summary>
    /// Create InstallInfos from given assemblies.
    /// </summary>
    /// <param name="useParallel">Foreach assembly with parallel.</param>
    /// <param name="assemblies">Given assemblies.</param>
    /// <returns>The InstallInfos from given assemblies.</returns>
    private static InstallInfo[] ScanAssemblies(bool useParallel, Assembly[] assemblies)
    {
        if (useParallel)
        {
            var results = new ConcurrentBag<InstallInfo>();

            foreach (var assembly in assemblies)
            {
                Parallel.ForEach(assembly.GetTypes(), type =>
                {
                    if (TryCreateInstallInfo(type, out var installInfo))
                    {
                        results.Add(installInfo);
                    }
                });
            }

            return results.ToArray();
        }
        else
        {
            var results = new List<InstallInfo>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (TryCreateInstallInfo(type, out var installInfo))
                    {
                        results.Add(installInfo);
                    }
                }
            }

            return results.ToArray();
        }


        static bool TryCreateInstallInfo(Type type, out InstallInfo installInfo)
        {
            installInfo = new InstallInfo();
            if (type is not {IsClass: true, IsAbstract: false, IsInterface: false} || !type.TryGetAttribute<DependencyAttribute>(out var dependency))
            {
                return false;
            }

            installInfo.Type  = type;
            installInfo.Key   = string.IsNullOrEmpty(dependency.Key) ? type.ToString() : dependency.Key;
            installInfo.Scope = Scope.Singleton;
            var bakeCount = 0;

            // Add scope.
            foreach (var attribute in type.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(SingletonAttribute))
                {
                    bakeCount = (bool) attribute.ConstructorArguments[0].Value ? 1 : 0;
                }
                else if (attribute.AttributeType == typeof(MultitonAttribute))
                {
                    installInfo.Scope = Scope.Multiton;
                    bakeCount         = (int) attribute.ConstructorArguments[0].Value;
                }
                else if (attribute.AttributeType == typeof(CustomAttribute))
                {
                    installInfo.Scope = Scope.Custom;
                    bakeCount         = (bool) attribute.ConstructorArguments[1].Value ? 1 : 0;
                    installInfo.Controller = Activator.CreateInstance((Type) attribute.ConstructorArguments[0].Value) as IScopeController
                        ?? throw new ArgumentException($"Invalid controller for type: \"{type}\"");
                }

                if (attribute.AttributeType == typeof(BindingAttribute))
                {
                    installInfo.Bindings = [..((IEnumerable<CustomAttributeTypedArgument>) attribute.ConstructorArguments[0].Value).Select(static arg => (Type) arg.Value)];
                }
            }

            // Add spawner.
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<SpawnerAttribute>() is not null)
                {
                    installInfo.Spawner = new FuncSpawner(method);
                }
            }

            if (installInfo.Spawner == null && type.IsNewable(out var newer))
            {
                installInfo.Spawner = newer.ToSpawner();
            }

            if (installInfo.Spawner == null)
            {
                throw new ArgumentException($"Non spawner for type: \"{type}\"");
            }

            // Bake instances.
            if (bakeCount > 0)
            {
                if (installInfo.Scope == Scope.Multiton)
                {
                    var instances = new object[bakeCount];
                    for (var i = 0; i < bakeCount; i++)
                    {
                        instances[i] = installInfo.Spawner.Spawn()!;
                    }

                    installInfo.Instances = instances;
                }
                else
                {
                    installInfo.Instance = installInfo.Spawner.Spawn();
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Spawner from method.
    /// </summary>
    /// <param name="method"></param>
    private class FuncSpawner(MethodInfo method) : ISpawner
    {
        /// <inheritdoc />
        public object? Spawn() => method.Invoke(null, []);
    }

    /// <inheritdoc />
    IBindingDecorator IDependenciesRegistry.Register(Type type) => ((IDependenciesRegistry) m_ApplicationContext).Register(type);

    /// <inheritdoc />
    INotMultitonBindingDecorator IDependenciesRegistry.Register(object instance) => ((IDependenciesRegistry) m_ApplicationContext).Register(instance);

    /// <inheritdoc />
    IMultitonBindingDecorator IDependenciesRegistry.Register(object[] instances) => ((IDependenciesRegistry) m_ApplicationContext).Register(instances);
}

/// <summary>
/// Attribute extensions.
/// </summary>
file static class AttributeExtensions
{
    /// <summary>
    /// Try get attribute from type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="attribute">Target attribute.</param>
    /// <typeparam name="TAttribute">Attribute type.</typeparam>
    /// <returns>Attribute instance.</returns>
    public static bool TryGetAttribute<TAttribute>(this Type type, out TAttribute attribute) where TAttribute : Attribute
    {
        attribute = type.GetCustomAttribute<TAttribute>();
        return attribute is not null;
    }
}