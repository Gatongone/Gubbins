using System.Collections.Concurrent;
using System.Reflection;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Context;

/// <summary>
/// A installer works by scanning assemblies.
/// </summary>
/// <param name="scanWithParallel">Foreach assembly with parallel.</param>
/// <param name="assemblies">Given assemblies.</param>
public class AssemblyInstaller(bool scanWithParallel, params Assembly[] assemblies) : IDependenciesInstaller
{
    public void Install(IDependenciesRegistry registry) => registry.Register(ScanAssemblies());

    /// <summary>
    /// Create InstallInfos from given assemblies.
    /// </summary>
    /// <returns>The InstallInfos from given assemblies.</returns>
    private IEnumerable<InstallInfo> ScanAssemblies()
    {
        if (scanWithParallel)
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

            return results;
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

            return results;
        }


        static bool TryCreateInstallInfo(Type type, out InstallInfo installInfo)
        {
            installInfo = new InstallInfo();
            if (type is not {IsClass: true, IsAbstract: false, IsInterface: false} || !type.TryGetAttribute<DependencyAttribute>(out var dependency))
            {
                return false;
            }

            installInfo.Type  = type;
            installInfo.Key   = string.IsNullOrEmpty(dependency!.Key) ? type.ToString() : dependency.Key;
            installInfo.Scope = Scope.Singleton;
            var bakeCount = 0;

            // Add scope.
            foreach (var attribute in type.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(SingletonAttribute))
                {
                    bakeCount = (bool) attribute.ConstructorArguments[0].Value! ? 1 : 0;
                }
                else if (attribute.AttributeType == typeof(MultitonAttribute))
                {
                    installInfo.Scope = Scope.Multiton;
                    bakeCount         = (int) attribute.ConstructorArguments[0].Value!;
                }
                else if (attribute.AttributeType == typeof(CustomAttribute))
                {
                    installInfo.Scope = Scope.Custom;
                    bakeCount         = (bool) attribute.ConstructorArguments[1].Value! ? 1 : 0;
                    installInfo.Controller = Activator.CreateInstance((Type) attribute.ConstructorArguments[0].Value!) as IScopeController
                        ?? throw new ArgumentException($"Invalid controller for type: \"{type}\"");
                }

                if (attribute.AttributeType == typeof(BindingAttribute))
                {
                    installInfo.Bindings = [..((IEnumerable<CustomAttributeTypedArgument>) attribute.ConstructorArguments[0].Value!).Select(static arg => (Type) arg.Value)];
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
                installInfo.Spawner = newer!.ToSpawner();
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
    public static bool TryGetAttribute<TAttribute>(this Type type, out TAttribute? attribute) where TAttribute : Attribute
    {
        attribute = type.GetCustomAttribute<TAttribute>();
        return attribute is not null;
    }
}