// namespace: EL2MapGenMod.Tuning
// class: BottomLayerLakeEnforcer
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;

namespace EL2MapGenMod.Tuning
{
    internal static class BottomLayerLakeEnforcer
    {
        private sealed class State
        {
            public HashSet<District> LastInjectedBottomLakeSet;
        }

        private static readonly ConditionalWeakTable<WorldGeneratorContext, State> StateByContext
            = new ConditionalWeakTable<WorldGeneratorContext, State>();

        public static void Apply(WorldGeneratorContext ctx)
        {
            if (ctx == null) return;
            if (!WorldGenTuningProfile.EnforceBottomLayerLakes) return;

            District[] all = ctx.AllDistrict;
            if (all == null || all.Length == 0) return;

            HashSet<District> forced = PersistentSeaTracker.GetForcedSeas(ctx);

            HashSet<District> noConvert = null;
            if (WorldGenTuningProfile.ProtectStartingIslandsFromBottomLakeConversion)
                noConvert = BuildNoConvertSetFromStartingIslands(ctx);

            HashSet<District> bottomLayerLakes = new HashSet<District>();

            for (int i = 0; i < all.Length; i++)
            {
                District d = all[i];
                if (d == null) continue;

                bool isForced = forced != null && forced.Contains(d);
                if (!isForced && d.Elevation > 0) continue;

                if (noConvert != null && noConvert.Contains(d)) continue;

                if (isForced)
                {
                    // restore no matter what vanilla did
                    d.Elevation = -1; // strongest guarantee against recesses
                    d.Content = District.Contents.Lake;
                }
                else
                {
                    if (d.Content == District.Contents.Land)
                        d.Content = District.Contents.Lake;
                }

                if (d.Content == District.Contents.Lake)
                    bottomLayerLakes.Add(d);
            }

            if (bottomLayerLakes.Count == 0) return;

            // optional de-dupe (safer if vanilla already created lake sets)
            for (int i = ctx.Lakes.Count - 1; i >= 0; i--)
            {
                var existing = ctx.Lakes[i];
                existing.ExceptWith(bottomLayerLakes);
                if (existing.Count == 0) ctx.Lakes.RemoveAt(i);
            }

            ctx.Lakes.Add(bottomLayerLakes);
        }

        private static HashSet<District> BuildNoConvertSetFromStartingIslands(WorldGeneratorContext ctx)
        {
            if (ctx.StartingIslandRegions == null || ctx.StartingIslandRegions.Count == 0)
                return null;

            HashSet<District> noConvert = new HashSet<District>();

            foreach (var kv in ctx.StartingIslandRegions)
            {
                HashSet<Region> regions = kv.Value;
                if (regions == null)
                    continue;

                foreach (Region r in regions)
                {
                    if (r?.Districts == null)
                        continue;

                    foreach (District d in r.Districts)
                    {
                        if (d != null)
                            noConvert.Add(d);
                    }
                }
            }

            return noConvert.Count > 0 ? noConvert : null;
        }
    }
}