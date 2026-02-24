using HarmonyLib;
using Amplitude.Mercury.Options;
using Amplitude.Mercury.WorldGenerator;
using Amplitude.Mercury.WorldGenerator.Generator;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(GameOptionsManager), nameof(GameOptionsManager.CreateWorldGeneratorFromGameOptions))]
    internal static class GameOptionsManager_CreateWorldGeneratorFromGameOptions
    {
        private static void Postfix(ref WorldGeneratorInput __result)
        {
            if (__result?.Options == null)
                return;

            WorldGenTuner.Apply(__result.Options);
        }
    }
}