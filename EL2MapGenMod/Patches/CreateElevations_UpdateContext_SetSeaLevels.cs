using System.Collections.Generic;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "UpdateContext")]
    internal static class CreateElevations_UpdateContext_SetSeaLevels
    {
        private static void Postfix(CreateElevations __instance)
        {
            var ctx = __instance?.Context;
            if (ctx == null) return;

            // Override
            ctx.RecessSeaLevels = new List<int> { 9, 7, 5, 3 };

            // Override
            ctx.WorldSeaLevel = 9; 
        }
    }
}