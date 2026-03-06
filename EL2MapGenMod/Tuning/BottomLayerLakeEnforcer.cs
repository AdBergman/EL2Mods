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
            Terrain lakeTerrain = ctx.Input.Settings.GetTerrain(ctx.Input.Settings.LakeTerrain);

            for (int i = 0; i < all.Length; i++)
            {
                District d = all[i];
                if (d == null)
                    continue;

                // --- NEW: Bump up deep pits to flatten the ocean floor ---
                if (d.Elevation < maxBottomWaterElevation)
                {
                    d.Elevation = maxBottomWaterElevation;
                }
                // ---------------------------------------------------------

                if (d.Elevation > maxBottomWaterElevation)
                    continue;

                if (noConvert != null && noConvert.Contains(d))
                    continue;

                d.Content = District.Contents.Lake;
                d.ForbidRiver = true;
                d.ForbidRidge = true;
                d.Terrain = lakeTerrain;
                bottomLayerLakes.Add(d);

                if (d.Positions == null)
                    continue;

                for (int p = 0; p < d.Positions.Length; p++)
                {
                    var hex = d.Positions[p];

                    if (ctx.HasLake != null)
                        ctx.HasLake[hex.Row, hex.Column] = true;

                    if (ctx.HasRiver != null)
                        ctx.HasRiver[hex.Row, hex.Column] = false;

                    if (ctx.TerrainData != null)
                        ctx.TerrainData[hex.Row, hex.Column] = lakeTerrain.Id;
                }
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