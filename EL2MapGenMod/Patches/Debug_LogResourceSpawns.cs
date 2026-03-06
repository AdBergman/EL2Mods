using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;
using BepInEx.Logging;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreatePointOfInterests), "HasPossiblePoi")]
    internal static class Debug_LogResourceSpawns
    {
        private static ManualLogSource _log = Logger.CreateLogSource("ResourceTracker");

        private static void Postfix(CreatePointOfInterests.Poi poi, District district, bool __result)
        {
            // Only log if the game successfully decided to place a Strategic or Luxury resource
            if (__result && poi != null && district != null)
            {
                var type = poi.Setting.Type;
                if (type == Amplitude.Mercury.WorldGenerator.Generator.WorldGeneratorSettings.PoiType.Strategic || 
                    type == Amplitude.Mercury.WorldGenerator.Generator.WorldGeneratorSettings.PoiType.Luxury)
                {
                    _log.LogInfo($"[SUCCESS] Spawning {type} at Elevation: {district.Elevation}");
                }
            }
        }
    }
}