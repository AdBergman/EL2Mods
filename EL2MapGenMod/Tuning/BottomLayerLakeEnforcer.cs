using System.Collections.Generic;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;

namespace EL2MapGenMod.Tuning
{
    internal static class BottomLayerLakeEnforcer
    {
        public static void Apply(WorldGeneratorContext ctx)
        {
            if (ctx == null)
                return;

            District[] all = ctx.AllDistrict;
            if (all == null || all.Length == 0)
                return;

            HashSet<District> noConvert = null;
            if (WorldGenTuningProfile.ProtectStartingIslandsFromBottomLakeConversion)
                noConvert = BuildNoConvertSetFromStartingIslands(ctx);

            HashSet<District> bottomLayerLakes = new HashSet<District>();
            sbyte maxBottomWaterElevation = WorldGenTuningProfile.BottomWaterMaxElevation;

            for (int i = 0; i < all.Length; i++)
            {
                District d = all[i];
                if (d == null)
                    continue;

                if (d.Elevation > maxBottomWaterElevation)
                    continue;

                if (noConvert != null && noConvert.Contains(d))
                    continue;

                if (d.Content == District.Contents.Land)
                    d.Content = District.Contents.Lake;

                if (d.Content == District.Contents.Lake)
                    bottomLayerLakes.Add(d);
            }

            if (bottomLayerLakes.Count == 0)
                return;

            if (ctx.Lakes == null)
                ctx.Lakes = new List<HashSet<District>>();

            for (int i = ctx.Lakes.Count - 1; i >= 0; i--)
            {
                HashSet<District> existing = ctx.Lakes[i];
                if (existing == null)
                {
                    ctx.Lakes.RemoveAt(i);
                    continue;
                }

                existing.ExceptWith(bottomLayerLakes);

                if (existing.Count == 0)
                    ctx.Lakes.RemoveAt(i);
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
                    if (r == null || r.Districts == null)
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