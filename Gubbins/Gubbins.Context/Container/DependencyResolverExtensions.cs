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
        public T? Resolve<T>(string key)
            => (T?) resolver.Resolve(null, key);

        /// <summary>
        /// Resolve the fields and properties with InjectAttribute.
        /// </summary>
        /// <param name="instance">Resolve target.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inject(object instance)
        {
            if (instance is IResolvePreprocessing preprocessing)
            {
                preprocessing.BeforeResolve();
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
        }
    }
}