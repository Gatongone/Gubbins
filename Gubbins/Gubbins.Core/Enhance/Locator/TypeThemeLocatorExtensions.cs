namespace Gubbins.Enhance;

public static class TypeThemeLocatorExtensions
{
    /// <summary>
    /// <inheritdoc cref="ILocator{TTheme}.Locate{T}(TTheme)"/>
    /// </summary>
    /// <param name="locator">Type theme locator.</param>
    /// <typeparam name="T">Theme type.</typeparam>
    /// <returns><inheritdoc cref="ILocator{TTheme}.Locate{T}(TTheme)"/></returns>
    public static T? Locate<T>(this ILocator<Type> locator) where T : class
    {
        return locator.Locate<T>(typeof(T));
    }

    /// <summary>
    /// <inheritdoc cref="ILocator{TTheme}.TryGetInstance"/>
    /// </summary>
    /// <param name="locator">Type theme locator.</param>
    /// <param name="instance"><inheritdoc cref="ILocator{TTheme}.TryGetInstance"/></param>
    /// <typeparam name="T">Theme type.</typeparam>
    /// <returns><inheritdoc cref="ILocator{TTheme}.TryGetInstance"/></returns>
    public static bool TryGetInstance<T>(this Locator<Type> locator, out T? instance) where T : class
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
    /// <param name="locator">Type theme locator.</param>
    /// <typeparam name="T">Theme type.</typeparam>
    /// <returns><inheritdoc cref="ILocator{TTheme}.TryGetInstance"/></returns>
    public static bool Contains<T>(this Locator<Type> locator) => locator.Contains(typeof(T));

    /// <summary>
    /// <inheritdoc cref="ILocator{TTheme}.Add"/>
    /// </summary>
    /// <param name="locator">Type theme locator.</param>
    /// <param name="instance"><inheritdoc cref="ILocator{TTheme}.Add"/></param>
    /// <typeparam name="T">Theme type.</typeparam>
    /// <exception cref="ArgumentNullException">Throw when the instance is null.</exception>
    public static void Register<T>(this Locator<Type> locator, T instance)
    {
        locator.Add(instance?.GetType() ?? typeof(T), instance!);
    }

    /// <summary>
    /// <inheritdoc cref="ILocator{TTheme}.Add"/>
    /// </summary>
    /// <param name="locator">Type theme locator.</param>
    /// <typeparam name="T">Theme type.</typeparam>
    /// <exception cref="ArgumentNullException">Throw when the instance is null.</exception>
    public static void Remove<T>(this Locator<Type> locator) => locator.Remove(typeof(T));
}