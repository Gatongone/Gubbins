using System;
using Gubbins.Game;

namespace Gubbins.Events
{
    public sealed class UnityLoopPhaseRegistrar : ILoopPhaseRegistrar
    {
        public void Register(LoopPhase phase, Action<float> handler)
        {
            var kind = Map(phase);
            if (kind != UnityLoop.Kind.Unknown)
                UnityLoop.RegisterUpdate(kind, handler);
        }

        public void Unregister(LoopPhase phase, Action<float> handler)
        {
            var kind = Map(phase);
            if (kind != UnityLoop.Kind.Unknown)
                UnityLoop.UnregisterUpdate(kind, handler);
        }

        private static UnityLoop.Kind Map(LoopPhase phase) => phase switch
        {
            LoopPhase.Early   => UnityLoop.Kind.EarlyUpdate,
            LoopPhase.Frame   => UnityLoop.Kind.Update,
            LoopPhase.Lately  => UnityLoop.Kind.PostLateUpdate,
            LoopPhase.Physics => UnityLoop.Kind.FixedUpdate,
            _                 => UnityLoop.Kind.Unknown
        };
    }
}
