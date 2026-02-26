using System.Reflection;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using EL2MapGenMod.Tuning;
using EL2MapGenMod.Util;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "UpdateContext")]
    internal static class CreateElevations_UpdateContext_LayerDistribution
    {
        private static readonly FieldInfo FinalVerifyPassField =
            AccessTools.Field(typeof(CreateElevations), "finalVerifyPass");

        private static void Prefix(CreateElevations __instance)
        {
            if (FinalVerifyPassField != null)
            {
                object val = FinalVerifyPassField.GetValue(__instance);
                if (val is bool b && !b)
                    return;
            }

            WorldGeneratorContext ctx = WorldGenReflection.GetTaskContext(__instance) as WorldGeneratorContext;
            if (ctx == null)
                return;

            BottomLayerLakeEnforcer.Apply(ctx);
        }
    }
}