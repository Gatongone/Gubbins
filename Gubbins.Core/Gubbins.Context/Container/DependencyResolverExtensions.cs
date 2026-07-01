using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

namespace Gubbins.Context;

/// <summary>
/// Extensions for <c>IDependenciesResolver</c>.
/// </summary>
public static class DependencyResolverExtensions
{
    /// <param name="resolver">Dependencies resolver.</param>
    extension(IDependenciesResolver resolver)
    {
        /// <summary>
        /// Resolve all the dependency of <c>type</c>.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <returns>The dependence instances of target <c>type</c>.</returns>
        public T[] ResolveAll<T>() => resolver.ResolveAll(typeof(T)).Cast<T>().ToArray();

        /// <summary>
        /// Resolve the dependence key and get the dependency.
        /// </summary>
        /// <param name="key">Dependency key.</param>
        /// <returns>The dependence prototype of target key.</returns>
        public object? Resolve(string key) => resolver.Resolve(null!, key);

        /// <summary>
        /// Resolve the type and get the dependency.
        /// </summary>
        /// <param name="type">Resolve type.</param>
        /// <returns>The dependence prototype of target type.</returns>
        public object? Resolve(Type? type)
        {
            if (type!.CheckType().IsValueType)
            {
                throw new InvalidOperationException();
            }

            return resolver.Resolve(type, null);
        }

        /// <summary>
        /// Resolve the type and get the dependency.
        /// </summary>
        /// <typeparam name="T">Resolve type.</typeparam>
        /// <returns>The dependence prototype of target type.</returns>
        public T? Resolve<T>()
            => (T?) resolver.Resolve(typeof(T));

        /// <summary>
        /// Resolve the dependence key and get the dependency.
        /// </summary>
        /// <param name="key">Dependency key.</param>
        /// <typeparam name="T">Resolve type.</typeparam>
        /// <returns>The dependence prototype of target key.</returns>
        public T? Resolve<T>(string key) => (T?) resolver.Resolve(null, key);

        /// <summary>
        /// Resolve the constructor with <see cref="InjectAttribute"/> and its dependencies,
        /// then create an instance of the type and resolve its members with InjectAttribute.
        /// </summary>
        /// <typeparam name="T">Resolve type which has a constructor with <see cref="InjectAttribute"/>.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown when no constructor with <see cref="InjectAttribute"/> is found for the specified type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InjectByCtor<T>() where T : class
        {
            return (T) resolver.InjectByCtor(typeof(T));
        }

        /// <summary>
        /// Resolve the constructor with <see cref="InjectAttribute"/> and its dependencies,
        /// then create an instance of the type and resolve its members with InjectAttribute.
        /// </summary>
        /// <param name="type">Resolve type which has a constructor with <see cref="InjectAttribute"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown when no constructor with <see cref="InjectAttribute"/> is found for the specified type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object InjectByCtor(Type type)
        {
            var ctor = InjectCache.GetInjectConstructor(type);
            if (ctor == null)
            {
                throw new InvalidOperationException($"No constructor with InjectAttribute found for type '{type}'.");
            }

            var instance = ctor.Constructor.Invoke(resolver.ResolveParameters(ctor.Parameters));
            resolver.Inject(instance);
            return instance;
        }

        /// <summary>
        /// Resolve the fields, properties and methods with <see cref="InjectAttribute"/> in the type and its base types.
        /// </summary>
        /// <param name="instance">Resolve target.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inject(object instance)
        {
            if (instance is IResolvePreprocessing preprocessing)
            {
                preprocessing.BeforeResolve();
            }

            var injectMethods = InjectCache.GetInjectMethods(instance.GetType());
            for (var index = 0; index < injectMethods.Length; index++)
            {
                var method = injectMethods[index];
                method.Method.Invoke(instance, resolver.ResolveParameters(method.Parameters));
            }

            resolver.InjectMembers(instance, instance.GetType());

            if (instance is IResolvePostprocessing postprocessing)
            {
                postprocessing.AfterResolve();
            }
        }

        /// <summary>
        /// Recursively resolve the fields and properties with InjectAttribute in the type and its base types.
        /// </summary>
        /// <param name="instance">Resolve target.</param>
        /// <param name="type">Current resolve type.</param>
        private void InjectMembers(object instance, Type? type)
        {
            if (type == null || type == typeof(object)) return;
            InjectMembers(resolver, instance, type.BaseType);
            var members = InjectCache.GetInjectMembers(type);
            for (var index = 0; index < members.Length; index++)
            {
                var member = members[index];
                var memberType = member.Member.MemberType;
                var value = resolver.Resolve(memberType, member.Key);
                if (value == null)
                {
                    throw new ArgumentException("Got a null member.");
                }

                if (!memberType.IsInstanceOfType(value))
                {
                    throw new InvalidCastException($"Cannot set the value of member '{member.Member.MemberType}' to '{value.GetType()}'.");
                }

                member.Member.SetValue(instance, value);
            }

            var methods = InjectCache.GetInjectMethods(type);
            for (var index = 0; index < methods.Length; index++)
            {
                var method = methods[index];
                method.Method.Invoke(instance, resolver.ResolveParameters(method.Parameters));
            }
        }

        /// <summary>
        /// Resolve the arguments for an injected constructor or method.
        /// </summary>
        /// <param name="parameters">The parameter descriptors to resolve.</param>
        /// <returns>The resolved arguments, positionally aligned with <paramref name="parameters"/>.</returns>
        private object[] ResolveParameters(InjectCache.InjectParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                return [];
            }

            var args = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                var value = resolver.Resolve(parameter.Type, parameter.Key);
                if (value == null)
                {
                    throw new ArgumentException($"Got a null argument for parameter of type '{parameter.Type}'.");
                }

                if (!parameter.Type.IsInstanceOfType(value))
                {
                    throw new InvalidCastException($"Cannot set the argument of type '{parameter.Type}' to '{value.GetType()}'.");
                }

                args[index] = value;
            }

            return args;
        }
    }
}