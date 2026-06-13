using System;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Context
{
    [Serializable]
    public struct SerializedInstallInfo
    {
        public Scope                                 Scope;
        public string                                Key;
        [TypeFrom(TypeKind.NotAbstract | TypeKind.NotInterface | TypeKind.NotGeneric)]
        public SerializedType                        Type;
        public SerializedType[]                      Bindings;
        [GenericArgumentFrom(typeof(ISpawner<>), nameof(Type))]
        public SerializedReference<ISpawner>         Spawner;
        public SerializedReference<IScopeController> Controller;
        public uint                                  Prewarm;
    }
}