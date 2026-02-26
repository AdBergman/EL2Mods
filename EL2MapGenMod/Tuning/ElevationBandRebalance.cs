// namespace: EL2MapGenMod.Tuning
// class: ElevationBandRebalance
using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Mercury.WorldGenerator.Algorithm;
using Amplitude.Mercury.WorldGenerator.Algorithm.Hex;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;

namespace EL2MapGenMod.Tuning
{
    internal static class ElevationBandRebalance
    {
        // ---------------------------------------------------------------------
        // LAKES
        // ---------------------------------------------------------------------
        public static void DigLakesBanded(WorldGeneratorContext context, int lakePresencePercent)
        {
            if (context == null)
                return;

            // Debug flags (kept from your version; harmless)
            foreach (District district in context.AllDistrict)
            {
                District d = district;
                int num = 0;
                if (d.ForbidLake) num |= 1;
                if (d.Content != District.Contents.Land) num |= 2;
                if (d.Elevation <= (sbyte)1) num |= 4;
                if (d.IsGridEdge(context.Grid)) num |= 8;
                if (d.DirectNeighboursAccess.Any(n => (int)n.Elevation < (int)d.Elevation)) num |= 16;
                if (d.DirectNeighboursAccess.Any(n => (int)n.Elevation >= (int)d.Elevation + (int)context.Input.Options.MaxCliffDeltaElevation)) num |= 32;
                d.LakeValue = num;
            }

            // 1) Candidate minima (local minima, inland, land, cliff-sane)
            List<District> nodes = new List<District>(
                context.AllDistrict
                    .Where(d => !d.ForbidLake)
                    .Where(d => d.Content == District.Contents.Land)
                    .Where(d => !d.IsGridEdge(context.Grid))
                    .Where(d => d.DirectNeighboursAccess.All(n => (int)n.Elevation >= (int)d.Elevation))
                    .Where(d => d.DirectNeighboursAccess.All(n => (int)n.Elevation < (int)d.Elevation + (int)context.Input.Options.MaxCliffDeltaElevation))
            );

            // 2) Band-aware bias on minima candidates
            nodes.RemoveAll(d =>
            {
                if (!WorldGenTuningProfile.AllowLowElevationLakeSeeds)
                {
                    if (d.Elevation <= WorldGenTuningProfile.LakeTooLowMaxElevation)
                        return true;
                }

                if (d.Elevation >= WorldGenTuningProfile.LakeTooHighMinElevation)
                {
                    if (context.Randomizer.Next(100) < WorldGenTuningProfile.LakeTooHighRejectPercent)
                        return true;
                }

                // Outside preferred band => soft reject
                if (d.Elevation < WorldGenTuningProfile.LakePreferredBandMinElevation ||
                    d.Elevation > WorldGenTuningProfile.LakePreferredBandMaxElevation)
                {
                    if (context.Randomizer.Next(100) < 25)
                        return true;
                }

                return false;
            });

            if (nodes.Count == 0)
                return;

            // 3) Connectivity of minima graph
            var connectivityChecker = new ConnectivityChecker<District>(new DistrictAdHocGraph(nodes));
            connectivityChecker.Execute();

            // 4) Build inland seas FIRST (big bodies, dug/clamped to <= 0)
            BuildInlandSeasFromCandidateComponents(context, connectivityChecker);

            // If we only want seas, stop here (prevents medium-lake spam)
            if (!WorldGenTuningProfile.AllowRegularLakesAfterSeas)
                return;

            // 5) Optional: regular lakes (NO pruning/splitting of big components)
            int setCount = connectivityChecker.ConnexNodeSets.Count;
            for (int i = 0; i < setCount; ++i)
            {
                HashSet<District> set = connectivityChecker.ConnexNodeSets[i];

                // If any node was converted during inland sea creation, skip this set
                if (set.Any(d => d.Content != District.Contents.Land))
                    continue;

                int sumCount = set.Sum(d => d.Count);

                if (sumCount >= (int)context.Input.Options.MinLakeArea &&
                    sumCount <= (int)context.Input.Options.MaxLakeArea &&
                    context.Randomizer.Next(100) < lakePresencePercent)
                {
                    // NOTE: clampBottomToZeroOrLower passed as false,
                    // but ApplyLakeToSet will still clamp if ClampAllLakeBottomToZeroOrLower is true.
                    ApplyLakeToSet(context, set, clampBottomToZeroOrLower: false);
                }
            }
        }

        private static void BuildInlandSeasFromCandidateComponents(
            WorldGeneratorContext context,
            ConnectivityChecker<District> connectivityChecker)
        {
            int seasRequested = WorldGenTuningProfile.InlandSeaCount;
            if (seasRequested <= 0)
                return;

            int totalTiles = context.Grid.Rows * context.Grid.Columns;

            // Canonical target:
            // distribute TargetBottomWaterPercent across the number of inland seas requested.
            int totalTarget = Math.Max(1, (totalTiles * WorldGenTuningProfile.TargetBottomWaterPercent) / 100);
            int targetPerSea = Math.Max(1, totalTarget / Math.Max(1, seasRequested));

            // Rank candidate components: prefer bigger minima basins, then lower minima
            var components = connectivityChecker.ConnexNodeSets
                .Select(set => new
                {
                    Set = set,
                    Area = set.Sum(d => d.Count),
                    MinElev = set.Min(d => (int)d.Elevation)
                })
                .OrderByDescending(x => x.Area)
                .ThenBy(x => x.MinElev)
                .ToList();

            int seasToBuild = Math.Min(seasRequested, components.Count);
            if (seasToBuild <= 0)
                return;

            for (int s = 0; s < seasToBuild; s++)
            {
                HashSet<District> seedSet = components[s].Set;

                District seed = seedSet.OrderBy(d => (int)d.Elevation).FirstOrDefault();
                if (seed == null)
                    continue;

                // Grow a contiguous sea by expanding from low elevation outward
                var sea = GrowSea(context, seed, targetPerSea);

                if (sea.Count == 0)
                    continue;

                // Apply lake content + dig/clamp bottom
                ApplyLakeToSet(context, sea, clampBottomToZeroOrLower: WorldGenTuningProfile.ClampInlandSeaBottomToZeroOrLower);

                // track it so we can restore it after vanilla “bulldozers”
                PersistentSeaTracker.MarkForcedSea(context, sea);
            }
        }

        private static HashSet<District> GrowSea(WorldGeneratorContext context, District seed, int targetArea)
        {
            var sea = new HashSet<District>();
            var frontier = new List<District>();
            int area = 0;
            int safetyIterator = 0; // Emergency break

            sea.Add(seed);
            frontier.Add(seed);
            area += seed.Count;

            while (area < targetArea && frontier.Count > 0 && safetyIterator < 5000)
            {
                safetyIterator++;
                frontier.Sort((a, b) => ((int)a.Elevation).CompareTo((int)b.Elevation));
                District current = frontier[0];
                frontier.RemoveAt(0);

                foreach (District n in current.DirectNeighboursAccess)
                {
                    if (sea.Contains(n) || n.ForbidLake || n.Content != District.Contents.Land) continue;
                    if (WorldGenTuningProfile.InlandSeasAvoidGridEdge && n.IsGridEdge(context.Grid)) continue;

                    sea.Add(n);
                    frontier.Add(n);
                    area += n.Count;
                    if (area >= targetArea) break;
                }
            }
            return sea;
        }

        private static void ApplyLakeToSet(WorldGeneratorContext context, HashSet<District> lakeSet, bool clampBottomToZeroOrLower)
        {
            // Convert districts to lake
            foreach (District d in lakeSet)
                d.Content = District.Contents.Lake;

            // Find ring districts
            List<District> ring = lakeSet
                .SelectMany(d => d.DirectNeighboursAccess)
                .Where(n => !lakeSet.Contains(n))
                .ToList();

            if (ring.Count == 0)
            {
                // Still register so downstream sees "a lake exists"
                context.Lakes.Add(lakeSet);
                return;
            }

            int ringMin = ring.Min(d => (int)d.Elevation);
            int lakeBottomInt = ringMin - WorldGenTuningProfile.LakeDigDepth;

            // NEW: clamp bottoms for all lakes if configured
            if (clampBottomToZeroOrLower || WorldGenTuningProfile.ClampAllLakeBottomToZeroOrLower)
                lakeBottomInt = Math.Min(lakeBottomInt, 0);

            sbyte lakeBottom = (sbyte)lakeBottomInt;

            foreach (District d in lakeSet)
                d.Elevation = lakeBottom;

            foreach (District r in ring)
                r.MinElevationCauseLake = Math.Max(r.MinElevationCauseLake, (sbyte)(lakeBottomInt + 1));

            context.Lakes.Add(lakeSet);
        }

        // ---------------------------------------------------------------------
        // RIDGES
        // ---------------------------------------------------------------------
        public static void SproutRidgesBanded(WorldGeneratorContext context, int ridgePresencePercent)
        {
            if (context == null)
                return;

            List<District> nodes = new List<District>();
            int length = context.AllDistrict.Length;

            for (int i = 0; i < length; ++i)
            {
                District district = context.AllDistrict[i];

                if (district.Content == District.Contents.Land &&
                    district.MotherRegion.LandMassType == Region.LandMassTypes.Continent &&
                    (int)district.Elevation >= (int)context.Input.Options.RidgeMinElevation &&
                    !district.ForbidRidge &&
                    !district.IsGridEdge(context.Grid))
                {
                    bool isLocalMax = true;
                    for (int n = 0; n < district.DirectNeighboursAccess.Count; ++n)
                    {
                        if ((int)district.DirectNeighboursAccess[n].Elevation > (int)district.Elevation)
                        {
                            isLocalMax = false;
                            break;
                        }
                    }

                    if (!isLocalMax)
                        continue;

                    // Deep band penalty (avoid shore-cliffs / ridge soup at waterline)
                    if (district.Elevation <= WorldGenTuningProfile.RidgeDeepBandMaxElevation)
                    {
                        if (context.Randomizer.Next(100) < WorldGenTuningProfile.RidgeDeepBandCandidateRejectPercent)
                            continue;
                    }

                    // Starting band reduction (avoid over-ridging the very top plateau)
                    if (district.Elevation >= WorldGenTuningProfile.RidgeStartingBandMinElevation &&
                        district.Elevation <= WorldGenTuningProfile.RidgeStartingBandMaxElevation)
                    {
                        if (context.Randomizer.Next(100) < WorldGenTuningProfile.RidgeStartingBandCandidateRejectPercent)
                            continue;
                    }

                    nodes.Add(district);
                }
            }

            bool changed;
            ConnectivityChecker<District> connectivityChecker;

            do
            {
                changed = false;
                connectivityChecker = new ConnectivityChecker<District>(new DistrictAdHocGraph(nodes));
                connectivityChecker.Execute();

                int count = connectivityChecker.ConnexNodeSets.Count;
                for (int index = 0; index < count; ++index)
                {
                    int sumCount = 0;
                    HashSet<District> set = connectivityChecker.ConnexNodeSets[index];
                    foreach (District d in set)
                        sumCount += d.Count;

                    if (sumCount > (int)context.Input.Options.MaxRidgeSize * 2)
                    {
                        // Split oversized ridges (keep some big chains, but not continent-spanning walls)
                        District start = set.ElementAt(context.Randomizer.Next(set.Count));
                        var working = new HashSet<District> { start };
                        var tmp = new List<District>();

                        while (working.Count < set.Count / 2)
                        {
                            tmp.Clear();
                            foreach (District d1 in working)
                                foreach (District d2 in d1.DirectNeighboursAccess)
                                    if (set.Contains(d2))
                                        tmp.Add(d2);

                            foreach (District d3 in tmp)
                                working.Add(d3);
                        }

                        tmp.Clear();
                        foreach (District d4 in working)
                            foreach (District d5 in d4.DirectNeighboursAccess)
                                if (set.Contains(d5) && !working.Contains(d5))
                                    tmp.Add(d5);

                        foreach (District d6 in tmp)
                            nodes.Remove(d6);

                        changed = true;
                    }
                    else if (sumCount > (int)context.Input.Options.MaxRidgeSize)
                    {
                        nodes.Remove(set.ElementAt(context.Randomizer.Next(set.Count)));
                        changed = true;
                    }
                    else if (sumCount < (int)context.Input.Options.MinRidgeSize)
                    {
                        foreach (District d in set)
                            nodes.Remove(d);
                    }
                }
            }
            while (changed);

            // Place ridges with band-aware presence weighting:
            // We want "more ridges than just the very top", but avoid shoreline walls.
            int setCountFinal = connectivityChecker.ConnexNodeSets.Count;
            for (int index = 0; index < setCountFinal; ++index)
            {
                HashSet<District> set = connectivityChecker.ConnexNodeSets[index];

                int sumCount = 0;
                sbyte maxElevation = sbyte.MinValue;
                sbyte minElevation = sbyte.MaxValue;

                foreach (District d in set)
                {
                    sumCount += d.Count;
                    if (d.Elevation > maxElevation) maxElevation = d.Elevation;
                    if (d.Elevation < minElevation) minElevation = d.Elevation;
                }

                if (sumCount < (int)context.Input.Options.MinRidgeSize)
                    continue;

                int effectivePresence = ridgePresencePercent;

                // Avoid boosting ridges that touch the shoreline band (1..2) to prevent "sea walls"
                bool touchesShoreBand = minElevation <= 2;

                // Mid/high elevation ridges: boost to ensure ridges aren't only at the top
                // (Use your intermediate band range, but only if the cluster isn't hugging shores)
                if (!touchesShoreBand &&
                    maxElevation >= WorldGenTuningProfile.RidgeIntermediateBandMinElevation &&
                    maxElevation <= WorldGenTuningProfile.RidgeIntermediateBandMaxElevation)
                {
                    effectivePresence += WorldGenTuningProfile.RidgeIntermediateBandPresenceBonusPercent;
                }

                // Starting band penalty (top plateau)
                if (maxElevation >= WorldGenTuningProfile.RidgeStartingBandMinElevation &&
                    maxElevation <= WorldGenTuningProfile.RidgeStartingBandMaxElevation)
                {
                    effectivePresence -= WorldGenTuningProfile.RidgeStartingBandCandidateRejectPercent;
                }

                // Deep band penalty
                if (maxElevation <= WorldGenTuningProfile.RidgeDeepBandMaxElevation)
                {
                    effectivePresence -= 25;
                }

                if (effectivePresence < 0) effectivePresence = 0;
                if (effectivePresence > 100) effectivePresence = 100;

                if (context.Randomizer.Next(100) <= effectivePresence)
                {
                    Ridge ridge = new Ridge();
                    context.Ridges.Add(ridge);

                    foreach (District d in set)
                    {
                        ridge.Hexes.AddRange((IEnumerable<HexPos>)d);
                        d.Content = District.Contents.Ridge;
                    }
                }
            }
        }
    }
}