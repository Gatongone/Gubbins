/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/09/03-04:32:02
 * Github: https://github.com/Gatongone
 * Description: Pool interface.
 */

namespace Gubbins.Resources;

/// <summary>
/// Represents a pool of objects that can be reused.
/// </summary>
/// <typeparam name="TPoolObject">The type of object stored in the pool.</typeparam>
public interface IPool<TPoolObject> : IFactory<TPoolObject>
{
    /// <summary>
    /// Recycles an object by returning it back to the pool.
    /// </summary>
    /// <param name="instance">The object to be recycled.</param>
    void Recycle(TPoolObject instance);
}