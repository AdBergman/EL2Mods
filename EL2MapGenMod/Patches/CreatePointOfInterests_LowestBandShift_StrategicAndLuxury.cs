
using System.Reflection;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;
using EL2MapGenMod.Tuning;
using EL2MapGenMod.Util;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch]
    internal static class CreatePointOfInterests_LowestBandShift_StrategicAndLuxury
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(CreatePointOfInterests),
                "HasPossiblePoi",
                new[] { typeof(int), typeof(CreatePointOfInterests.Poi), typeof(District), typeof(bool) }
            );
        }

        private static bool Prefix(
            CreatePointOfInterests __instance,
            int recessIndex,
            CreatePointOfInterests.Poi poi,
            District district,
            bool noWater,
            ref bool __result)
        {
            var ctx = __instance?.Context;

            var runState = (ctx != null && WorldgenTelemetry.Enabled) ? WorldgenTelemetry.GetOrCreate(ctx) : null;
            if (runState != null) runState.Poi.TotalChecks++;

            if (__instance == null || poi == null || district == null || ctx == null)
            {
                if (runState != null) runState.Poi.Reject_NullOrInvalid++;
                return true;
            }

            var type = poi.Setting.Type;
            bool isStrategicOrLuxury =
                type == WorldGeneratorSettings.PoiType.Strategic ||
                type == WorldGeneratorSettings.PoiType.Luxury;

            if (!isStrategicOrLuxury)
            {
                if (runState != null) runState.Poi.Reject_TypeNotTracked++;
                return true;
            }

            if (runState != null) runState.Poi.StrategicLuxuryChecks++;

            var seaLevels = ctx.RecessSeaLevels;
            if (seaLevels == null || seaLevels.Count == 0)
                return true;

            // Clamp incoming recessIndex to valid list range first
            int clamped = recessIndex;
            if (clamped < 0) clamped = 0;
            if (clamped >= seaLevels.Count) clamped = seaLevels.Count - 1;

            // Then clamp to "available tiers" (your requirement)
            if (WorldGenTuningProfile.ClampStrategicLuxuryToAvailableTiers)
            {
                int min = WorldGenTuningProfile.StrategicLuxuryMinRecessIndex;
                int max = WorldGenTuningProfile.StrategicLuxuryMaxRecessIndex;

                // Keep within [0..seaLevels.Count-1] as well
                if (min < 0) min = 0;
                if (max >= seaLevels.Count) max = seaLevels.Count - 1;
                if (max < min) max = min;

                if (clamped < min) clamped = min;
                if (clamped > max) clamped = max;
            }

            // Bottom band fix ONLY if this band is the "lake band" (<=0 or last band)
            int lowerForBand = seaLevels[clamped];
            bool isBottomBand = (clamped == seaLevels.Count - 1) || (lowerForBand <= 0);

            int effectiveRecessIndex = clamped;
            if (isBottomBand)
            {
                effectiveRecessIndex = clamped + 1;
                if (effectiveRecessIndex >= seaLevels.Count)
                    effectiveRecessIndex = seaLevels.Count - 1;

                if (runState != null) runState.Poi.StrategicLuxuryShifted++;
            }

            // Content gating (allow ridge as land-like)
            if (noWater)
            {
                if (district.Content != District.Contents.Land &&
                    district.Content != District.Contents.Ridge)
                {
                    if (runState != null) runState.Poi.Reject_Content++;
                    __result = false;
                    return false;
                }
            }
            else
            {
                if (district.Content != District.Contents.Land &&
                    district.Content != District.Contents.Coastal &&
                    district.Content != District.Contents.Ocean &&
                    district.Content != District.Contents.Ridge)
                {
                    if (runState != null) runState.Poi.Reject_Content++;
                    __result = false;
                    return false;
                }
            }

            // Elevation gate using effective index
            int upper = effectiveRecessIndex >= 1
                ? seaLevels[effectiveRecessIndex - 1]
                : (int)ctx.Input.Options.StartLandMaxElevation;

            int lower = seaLevels[effectiveRecessIndex];

            if ((int)district.Elevation <= lower || (int)district.Elevation > upper)
            {
                if (runState != null) runState.Poi.Reject_BandElevation++;
                __result = false;
                return false;
            }

            bool hasValidRiverState = false;
            bool hasPoiAlready = false;
            bool wonderNear = false;

            for (int i = 0; i < district.Positions.Length; ++i)
            {
                ref var hex = ref district.Positions[i];

                hasPoiAlready |= !string.IsNullOrEmpty(ctx.PointOfInterestMap[hex.Row, hex.Column]);
                wonderNear |= ctx.ExistWonderNear(hex, 1);

                bool hasRiver = ctx.HasRiver[hex.Row, hex.Column];
                if (hasRiver)
                {
                    if (poi.Setting.River != WorldGeneratorSettings.RiverPossibility.NoRiver)
                        hasValidRiverState = true;
                }
                else
                {
                    if (poi.Setting.River != WorldGeneratorSettings.RiverPossibility.OnlyRiver)
                        hasValidRiverState = true;
                }
            }

            if (hasPoiAlready)
            {
                if (runState != null) runState.Poi.Reject_AlreadyHasPoi++;
                __result = false;
                return false;
            }

            if (wonderNear)
            {
                if (runState != null) runState.Poi.Reject_WonderNear++;
                __result = false;
                return false;
            }

            if (!hasValidRiverState)
            {
                if (runState != null) runState.Poi.Reject_RiverConstraint++;
                __result = false;
                return false;
            }

            if (runState != null) runState.Poi.AllowedChecks++;

            __result = true;
            return false;
        }
    }
}