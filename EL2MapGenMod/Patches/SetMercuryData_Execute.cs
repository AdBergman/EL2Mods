using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(SetMercuryData), nameof(SetMercuryData.Execute))]
    internal static class SetMercuryData_Execute
    {
        private static void Postfix(object context)
        {
            WorldGeneratorContext ctx = context as WorldGeneratorContext;
            if (ctx == null)
                return;

            RecessSeaLevelTuner.Apply(ctx);
        }
    }
}