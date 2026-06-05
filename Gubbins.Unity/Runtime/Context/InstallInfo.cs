using System;
using System.Collections.Generic;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Context
{
    [Serializable]
    public class InstallInfo
    {
        public  Scope                                 Scope;
        public  string                                Key;
        public  SerializedType                        Type;
        public  HashSet<Type>                         Bindings;
        public  UnityEngine.Object                    Prototype;
        public  SerializedReference<ISpawner>         Factory;
        public  SerializedReference<IScopeController> Controller;
        public  uint                                  Prewarm;
    }
}