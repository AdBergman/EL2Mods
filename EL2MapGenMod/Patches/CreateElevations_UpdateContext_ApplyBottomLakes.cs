using System.Reflection;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "UpdateContext")]
    internal static class CreateElevations_UpdateContext_ApplyBottomLakes
    {
        private static readonly FieldInfo FinalVerifyPassField =
            AccessTools.Field(typeof(CreateElevations), "finalVerifyPass");

        private static void Postfix(CreateElevations __instance)
        {
            if (__instance == null)
                return;

            if (FinalVerifyPassField != null)
            {
                object val = FinalVerifyPassField.GetValue(__instance);
                if (val is bool b && !b)
                    return;
            }

            WorldGeneratorContext ctx = __instance.Context;
            if (ctx == null)
                return;

            BottomLayerLakeEnforcer.Apply(ctx);
        }
    }
}