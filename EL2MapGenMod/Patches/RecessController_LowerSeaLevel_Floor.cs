using System.Reflection;
using HarmonyLib;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch]
    internal static class RecessController_LowerSeaLevel_Floor
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("Amplitude.Mercury.Simulation.RecessController:LowerSeaLevel");
        }

        // private void LowerSeaLevel(int recessDepth)
        private static void Prefix(object __instance, ref int recessDepth)
        {
            int floor = EL2MapGenMod.Tuning.WorldGenTuningProfile.PersistentSeaLevelFloor;
            if (floor <= 0)
                return;

            // Use Harmony's Traverse to safely and cleanly pull the hidden variable.
            var traverse = Traverse.Create(__instance);
            
            // It tries to grab the Property first. If it doesn't exist, it grabs the Field.
            int currentSeaLevel = traverse.Property("CurrentSeaLevel").PropertyExists() 
                ? traverse.Property<int>("CurrentSeaLevel").Value 
                : traverse.Field<int>("CurrentSeaLevel").Value;

            // If we STILL couldn't read it (e.g., both failed and returned 0, or it's natively negative)
            if (currentSeaLevel < 0)
                return;

            int maxAllowedDepth = currentSeaLevel - floor;

            if (maxAllowedDepth <= 0)
            {
                recessDepth = 0;
                return;
            }

            if (recessDepth > maxAllowedDepth)
                recessDepth = maxAllowedDepth;
        }
    }
}