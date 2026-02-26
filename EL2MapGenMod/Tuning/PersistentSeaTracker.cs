using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;

namespace EL2MapGenMod.Tuning
{
    internal static class PersistentSeaTracker
    {
        private sealed class State
        {
            public readonly HashSet<District> ForcedSeas = new HashSet<District>();
        }

        private static readonly ConditionalWeakTable<WorldGeneratorContext, State> StateByContext
            = new ConditionalWeakTable<WorldGeneratorContext, State>();

        public static void MarkForcedSea(WorldGeneratorContext ctx, HashSet<District> sea)
        {
            if (ctx == null || sea == null || sea.Count == 0) return;
            StateByContext.GetOrCreateValue(ctx).ForcedSeas.UnionWith(sea);
        }

        public static HashSet<District> GetForcedSeas(WorldGeneratorContext ctx)
        {
            if (ctx == null) return null;
            return StateByContext.GetOrCreateValue(ctx).ForcedSeas;
        }
    }
}