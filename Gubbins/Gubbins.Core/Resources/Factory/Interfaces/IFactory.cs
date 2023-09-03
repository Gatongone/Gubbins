/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/09/03-04:32:56
 * Github: https://github.com/Gatongone
 * Description: Resource factory interface.
 */

namespace Gubbins.Resources;

/// <summary>
/// Represents a factory that creates instances of a specified type.
/// </summary>
/// <typeparam name="TProduct">The type of the product.</typeparam>
public interface IFactory<out TProduct>
{
    /// <summary>
    /// Get a instance of the specified type.
    /// </summary>
    /// <returns>An instance of the specified type.</returns>
    TProduct Spawn();
}