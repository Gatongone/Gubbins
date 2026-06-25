using System;
using System.Linq;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Context
{
    /// <summary>
    /// Represents serialized information for installing a type into a context.
    /// </summary>
    [Serializable]
    public struct SerializedInstallInfo
    {
        /// <summary>
        /// Gets or sets the scope of the type to be installed.
        /// </summary>
        public Scope Scope;

        /// <summary>
        /// Gets or sets the key associated with the type to be installed.
        /// </summary>
        public string Key;

        /// <summary>
        /// Gets or sets the serialized type to be installed.
        /// </summary>
        [TypeFrom(TypeKind.Implementation | TypeKind.NotGeneric)]
        public SerializedType Type;

        /// <summary>
        /// Gets or sets the serialized types that are bound to the type being installed.
        /// </summary>
        public SerializedType[] Bindings;

        /// <summary>
        /// Gets or sets the serialized spawner responsible for creating instances of the type being installed.
        /// </summary>
        [GenericArgumentFrom(typeof(ISpawner<>), nameof(Type))]
        public SerializedReference<ISpawner> Spawner;

        /// <summary>
        /// Gets or sets the serialized controller responsible for managing the scope of the type being installed.
        /// </summary>
        public SerializedReference<IScopeController> Controller;

        /// <summary>
        /// Gets or sets the number of instances to prewarm for the type being installed.
        /// </summary>
        public uint Prewarm;

        /// <summary>
        /// Implicitly converts a <see cref="SerializedInstallInfo"/> to an <see cref="InstallInfo"/>.
        /// </summary>
        /// <param name="info">The serialized install info to convert.</param>
        /// <returns>An <see cref="InstallInfo"/> instance representing the serialized install info.</returns>
        public static implicit operator InstallInfo(SerializedInstallInfo info) => info.ToInstallInfo();

        /// <summary>
        /// Converts the <see cref="SerializedInstallInfo"/> to an <see cref="InstallInfo"/> instance.
        /// </summary>
        /// <returns>An <see cref="InstallInfo"/> instance representing the serialized install info.</returns>
        public InstallInfo ToInstallInfo()
        {
            var result = new InstallInfo
            {
                Scope      = Scope,
                Key        = Key,
                Type       = Type,
                Bindings   = Bindings.Select(static item => item.Type).ToHashSet(),
                Spawner    = Spawner.Value,
                Controller = Controller.Value,
                BakeCount  = Prewarm
            };
            if (Prewarm > 0 && Spawner.Value != null)
            {
                if (Scope != Scope.Multiton)
                {
                    result.Instance = Spawner.Value.Spawn();
                }
                else
                {
                    var array = new object[Prewarm];
                    for (var i = 0; i < Prewarm; i++)
                    {
                        array[i] = Spawner.Value.Spawn();
                    }

                    result.Instances = array;
                }
            }

            return result;
        }
    }
}