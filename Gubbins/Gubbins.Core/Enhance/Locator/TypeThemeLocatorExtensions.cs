namespace Gubbins.Enhance;

public static class TypeThemeLocatorExtensions
{
    /// <param name="locator">Type theme locator.</param>
    extension(Locator<Type> locator)
    {
        /// <summary>
        /// <inheritdoc cref="ILocator{TTheme}.Locate{T}(TTheme)"/>
        /// </summary>
        /// <typeparam name="T">Theme type.</typeparam>
        /// <returns><inheritdoc cref="ILocator{TTheme}.Locate{T}(TTheme)"/></returns>
        public T? Locate<T>() where T : class
        {
            return locator.Locate<T>(typeof(T));
        }

        /// <summary>
        /// <inheritdoc cref="ILocator{TTheme}.TryGetInstance"/>
        /// </summary>
        /// <param name="instance"><inheritdoc cref="ILocator{TTheme}.TryGetInstance"/></param>
        /// <typeparam name="T">Theme type.</typeparam>
        /// <returns><inheritdoc cref="ILocator{TTheme}.TryGetInstance"/></returns>
        public bool TryGetInstance<T>(out T? instance) where T : class
        {
            if (!locator.TryGetInstance(typeof(T), out var obj))
            {
                instance = null;
                return false;
            }

            instance = obj as T;
            return instance != null;
        }

        /// <summary>
        /// <inheritdoc cref="ILocator{TTheme}.Contains(TTheme)"/>
        /// </summary>
        /// <typeparam name="T">Theme type.</typeparam>
        /// <returns><inheritdoc cref="ILocator{TTheme}.TryGetInstance"/></returns>
        public bool Contains<T>() => locator.Contains(typeof(T));

        /// <summary>
        /// <inheritdoc cref="ILocator{TTheme}.Add"/>
        /// </summary>
        /// <param name="instance"><inheritdoc cref="ILocator{TTheme}.Add"/></param>
        /// <typeparam name="T">Theme type.</typeparam>
        /// <exception cref="ArgumentNullException">Throw when the instance is null.</exception>
        public void Register<T>(T instance)
        {
            locator.Add(instance?.GetType() ?? typeof(T), instance!);
        }

        /// <summary>
        /// <inheritdoc cref="ILocator{TTheme}.Add"/>
        /// </summary>
        /// <typeparam name="T">Theme type.</typeparam>
        /// <exception cref="ArgumentNullException">Throw when the instance is null.</exception>
        public void Remove<T>() => locator.Remove(typeof(T));
    }
}