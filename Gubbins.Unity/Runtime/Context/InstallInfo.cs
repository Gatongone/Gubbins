using System;
using System.Linq;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Context
{
    [Serializable]
    public struct SerializedInstallInfo
    {
        public Scope Scope;

        public string Key;

        [TypeFrom(TypeKind.NotAbstract | TypeKind.NotInterface | TypeKind.NotGeneric)]
        public SerializedType Type;

        public SerializedType[] Bindings;

        [GenericArgumentFrom(typeof(ISpawner<>), nameof(Type))]
        public SerializedReference<ISpawner> Spawner;

        public SerializedReference<IScopeController> Controller;

        public uint Prewarm;

        public static implicit operator InstallInfo(SerializedInstallInfo info) => info.ToInstallInfo();

        public InstallInfo ToInstallInfo()
        {
            var spawner = Spawner;
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
                        array[i] = spawner.Value.Spawn();
                    }

                    result.Instances = array;
                }
            }

            return result;
        }
    }
}