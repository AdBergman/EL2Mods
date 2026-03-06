using System.Linq;
using HarmonyLib;
using BepInEx.Logging;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreatePointOfInterests), "Execute")]
    internal static class LogRecessSeaLevels
    {
        private static bool _logged;
        private static ManualLogSource _log = Logger.CreateLogSource("EL2MapGenMod");

        private static void Prefix(CreatePointOfInterests __instance)
        {
            if (_logged) return;

            var ctx = __instance?.Context;
            if (ctx?.RecessSeaLevels == null || ctx.RecessSeaLevels.Count == 0)
                return;

            string levels = string.Join(", ", ctx.RecessSeaLevels.Select(x => x.ToString()));

            _log.LogInfo("=======================================");
            _log.LogInfo($"RecessSeaLevels: [{levels}]");
            _log.LogInfo($"StartLandElevation: {ctx.Input.Options.StartLandElevation}");
            _log.LogInfo("=======================================");

            _logged = true;
        }
    }
}