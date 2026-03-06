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

                    // Deep band penalty (avoid shore-cliffs)
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
                        // Split too long ridges
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
            
            // Adds more ridges in different elevation levels.
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
                bool touchesShoreBand = minElevation <= 4; // Changed from 2 to 4

                // Mid/high elevation ridges: boost to ensure ridges aren't only at the top
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