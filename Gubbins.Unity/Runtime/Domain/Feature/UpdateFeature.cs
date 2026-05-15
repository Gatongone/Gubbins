// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/22-20:33:33
// Github: https://github.com/Gatongone

using Gubbins.Engine;
using JetBrains.Annotations;

namespace Gubbins.Entities
{
    [PublicAPI]
    internal class UpdateFeature : IFeature
    {
        public bool Enable { get; set; } = true;
        public int Priority => int.MinValue + 50;

        public void Evaluate(ICategory category, ISystem system)
        {
            if (RegisterUpdate(system))
            {
                category.OnReset += () => UnregisterUpdate(system);
            }
        }


         private static bool RegisterUpdate(object obj)
         {
             var registered = false;
             switch (obj)
             {
                 case IInitUpdatable initable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.Initialization, initable.OnInitUpdate);
                     registered = true;
                     break;
                 case IEarlyUpdatable earlyUpdatable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.PreUpdate, earlyUpdatable.OnEarlyUpdate);
                     registered = true;
                     break;
                 case IPreUpdatable preUpdatable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.PreUpdate, preUpdatable.OnPreUpdate);
                     registered = true;
                     break;
                 case IFixedUpdatable fixedUpdatable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.FixedUpdate, fixedUpdatable.OnFixedUpdate);
                     registered = true;
                     break;
                 case IUpdatable updatable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.Update, updatable.OnUpdate);
                     registered = true;
                     break;
                 case IPreLateUpdatable preLateUpdatable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.PreLateUpdate, preLateUpdatable.OnPreLateUpdate);
                     registered = true;
                     break;
                 case IPostLateUpdatable postLateUpdatable:
                     UnityLoop.RegisterUpdate(UnityLoop.UpdateKind.PostLateUpdate, postLateUpdatable.OnPostLateUpdate);
                     registered = true;
                     break;
             }

             return registered;
         }

         private static bool UnregisterUpdate(object obj)
         {
             var unregistered = false;
             switch (obj)
             {
                 case IInitUpdatable initable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.Initialization, initable.OnInitUpdate);
                     unregistered = true;
                     break;
                 case IEarlyUpdatable earlyUpdatable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.PreUpdate, earlyUpdatable.OnEarlyUpdate);
                     unregistered = true;
                     break;
                 case IPreUpdatable preUpdatable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.PreUpdate, preUpdatable.OnPreUpdate);
                     unregistered = true;
                     break;
                 case IFixedUpdatable fixedUpdatable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.FixedUpdate, fixedUpdatable.OnFixedUpdate);
                     unregistered = true;
                     break;
                 case IUpdatable updatable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.Update, updatable.OnUpdate);
                     unregistered = true;
                     break;
                 case IPreLateUpdatable preLateUpdatable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.PreLateUpdate, preLateUpdatable.OnPreLateUpdate);
                     unregistered = true;
                     break;
                 case IPostLateUpdatable postLateUpdatable:
                     UnityLoop.UnregisterUpdate(UnityLoop.UpdateKind.PostLateUpdate, postLateUpdatable.OnPostLateUpdate);
                     unregistered = true;
                     break;
             }

             return unregistered;
         }
     }
}