namespace Gubbins.Context;

/// <summary>
/// Extension methods for <see cref="IDependenciesRegistry"/> to register dependencies based on <see cref="InstallInfo"/> collections.
/// </summary>
public static class DependencyRegistryExtensions
{
    /// <param name="registry">Dependencies registry.</param>
    extension(IDependenciesRegistry registry)
    {
        /// <summary>
        /// Register dependency type.
        /// </summary>
        /// <typeparam name="T">Dependency type.</typeparam>
        /// <returns>Register options.</returns>
        public IBindingDecorator Register<T>()
            where T : class
            => registry.Register(typeof(T));

        /// <summary>
        /// Register dependency type and bind to target type.
        /// </summary>
        /// <typeparam name="TOrigin">Dependency type.</typeparam>
        /// <typeparam name="TBind">Binding type.</typeparam>
        /// <returns>Register options.</returns>
        public IBindingDecorator Register<TOrigin, TBind>()
            where TOrigin : class, TBind
            where TBind : class
            => registry.Register<TOrigin>().BindTo<TBind>();

        /// <summary>
        /// Register all info to the dependencies container.
        /// </summary>
        /// <param name="installInfos">Install info collections.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throw when scope is invalid.</exception>
        public void RegisterAll(IEnumerable<InstallInfo> installInfos)
        {
            foreach (var info in installInfos)
            {
                if (info.Type == null && info.Instance == null)
                {
                    throw new ArgumentException("Can't resolve InstallInfo Type.");
                }

                if (info.Scope == Scope.Multiton)
                {
                    if (info.Instances is {Length: > 0})
                    {
                        registry.RegisterMultitonWithInstance(info);
                    }
                    else
                    {
                        registry.RegisterMultitonWithType(info);
                    }
                }
                else
                {
                    //Resolve prototype
                    if (info.Instance != null)
                    {
                        registry.RegisterSingletonOrCustomWithInstance(info);
                    }
                    else
                    {
                        registry.RegisterSingletonOrCustomWithType(info);
                    }
                }
            }
        }

        /// <summary>
        /// Register instance with Singleton or Custom scope.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throw </exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void RegisterSingletonOrCustomWithType(InstallInfo info)
        {
            var decorator = registry.Register(info.Type);

            if (info.Bindings != null)
            {
                decorator.BindTo(info.Bindings.ToArray());
            }

            if (!string.IsNullOrEmpty(info.Key))
            {
                decorator.WithKey(info.Key);
            }

            var scopeDecorator = info.Scope switch
            {
                Scope.Singleton => decorator.AsSingleton(),
                Scope.Custom    => info.Controller != null ? decorator.AsCustom(info.Controller) : throw new ArgumentNullException(nameof(info.Controller)),
                _               => throw new ArgumentOutOfRangeException()
            };

            if (info.Spawner != null)
            {
                scopeDecorator.WithSpawner(info.Spawner);
            }

            if (info.BakeCount > 0)
            {
                scopeDecorator.Bake();
            }
        }

        /// <summary>
        /// Register instance with Singleton or Custom scope.
        /// </summary>
        /// <param name="info">Install info.</param>
        /// <exception cref="ArgumentException">Throw when InstallInfo controller is null when it is Custom scope.</exception>
        private void RegisterSingletonOrCustomWithInstance(InstallInfo info)
        {
            var decorator = registry.Register(info.Instance!);

            if (!string.IsNullOrEmpty(info.Key))
            {
                decorator.WithKey(info.Key);
            }

            if (info.Bindings != null)
            {
                decorator.BindTo(info.Bindings.ToArray());
            }

            if (info.Scope == Scope.Singleton)
            {
                decorator.AsSingleton();
            }
            else if (info.Scope == Scope.Custom)
            {
                if (info.Controller == null)
                {
                    throw new ArgumentException("Install info controller cannot be null when it is Custom scope.");
                }

                var spawnerDecorator = decorator.AsCustom(info.Controller!);
                if (info.Spawner != null)
                {
                    spawnerDecorator.WithSpawner(info.Spawner);
                }
            }
        }

        /// <summary>
        /// Register instance with Multiton scope.
        /// </summary>
        /// <param name="info">Install info.</param>
        private void RegisterMultitonWithType(InstallInfo info)
        {
            var decorator = registry.Register(info.Type);

            if (info.Bindings != null)
            {
                decorator.BindTo(info.Bindings.ToArray());
            }

            if (!string.IsNullOrEmpty(info.Key))
            {
                decorator.WithKey(info.Key);
            }

            var scopeDecorator = decorator.AsMultiton();

            if (info.Spawner != null)
            {
                scopeDecorator.WithSpawner(info.Spawner);
            }

            if (info.BakeCount > 0)
            {
                scopeDecorator.Bake(info.BakeCount);
            }
        }

        /// <summary>
        /// Register instance with Multiton scope.
        /// </summary>
        /// <param name="info">Install info.</param>
        private void RegisterMultitonWithInstance(InstallInfo info)
        {
            var decorator = registry.Register(info.Instances!);

            if (info.Bindings != null)
            {
                decorator.BindTo(info.Bindings.ToArray());
            }

            if (!string.IsNullOrEmpty(info.Key))
            {
                decorator.WithKey(info.Key);
            }

            if (info.Spawner != null)
            {
                decorator.WithSpawner(info.Spawner);
            }

            if (info.BakeCount > 0)
            {
                decorator.Bake(info.BakeCount);
            }
        }
    }
}