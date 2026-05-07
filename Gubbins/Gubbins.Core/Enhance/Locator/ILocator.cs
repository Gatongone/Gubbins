using System.Diagnostics.CodeAnalysis;

namespace Gubbins.Enhance;

[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface ILocator<TTheme>
{
    /// <summary>
    /// Remove instance and its theme.
    /// </summary>
    /// <param name="theme">Instance theme.</param>
    /// <param name="instance">Registered target.</param>
    void Add(TTheme theme, object instance);

    /// <summary>
    /// Remove instance and its theme.
    /// </summary>
    /// <param name="theme">Located theme.</param>
    /// <returns>Remove successfully.</returns>
    bool Remove(TTheme theme);

    /// <summary>
    /// Remove instance and its theme.
    /// </summary>
    /// <param name="instance">Instance in locator cache.</param>
    /// <returns>Remove successfully.</returns>
    bool Remove(object instance);

    /// <summary>
    /// Verify if the theme can be located.
    /// </summary>
    /// <param name="theme">Located theme.</param>
    /// <returns>If the theme can be located</returns>
    bool Contains(TTheme theme);

    /// <summary>
    /// Verify if the instance can be located.
    /// </summary>
    /// <param name="instance">Located instance.</param>
    /// <returns>If the instance can be located</returns>
    bool Contains(object instance);

    /// <summary>
    /// Try get instance by theme.
    /// </summary>
    /// <param name="theme">Located theme</param>
    /// <param name="instance">Located instance.</param>
    /// <returns>If the theme has been located.</returns>
    bool TryGetInstance(TTheme theme, out object? instance);

    /// <summary>
    /// Try get instance by theme.
    /// </summary>
    /// <param name="instance">Located instance.</param>
    /// <param name="theme">Located theme</param>
    /// <returns>If the instance has been located.</returns>
    bool TryGetTheme(object instance, out TTheme? theme);

    /// <summary>
    /// Clear all theme.
    /// </summary>
    void Clear();

    /// <summary>
    /// Locate instance with theme. It would create a new one if the instance can't located in cache..
    /// </summary>
    /// <param name="theme">Instance theme.</param>
    /// <typeparam name="T">Located instance type.</typeparam>
    /// <returns>Located instance. It would be null when locate failed.</returns>
    T? Locate<T>(TTheme theme) where T : class;
}