#if GUBBINS_ENABLED
using System;
using Gubbins.Game;

namespace Gubbins.Events
{
    public sealed class GodotLoopPhaseRegistrar : ILoopPhaseRegistrar
    {
        public void Register(LoopPhase phase, Action<float> handler)
        {
            var kind = Map(phase);
            if (kind.HasValue)
                GodotLoop.RegisterUpdate(kind.Value, handler);
        }

        public void Unregister(LoopPhase phase, Action<float> handler)
        {
            var kind = Map(phase);
            if (kind.HasValue)
                GodotLoop.UnregisterUpdate(kind.Value, handler);
        }

        private static GodotLoop.Kind? Map(LoopPhase phase) => phase switch
        {
            LoopPhase.Early   => GodotLoop.Kind.Preprocess,
            LoopPhase.Frame   => GodotLoop.Kind.Process,
            LoopPhase.Lately  => GodotLoop.Kind.Postprocess,
            LoopPhase.Physics => GodotLoop.Kind.Physics,
            _                 => null
        };
    }
}
#endif
