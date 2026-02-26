using HarmonyLib;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch]
    internal static class RecessController_LowerSeaLevel_Floor
    {
        // Patch internal type by string.
        private static System.Reflection.MethodBase TargetMethod()
        {
            // Likely assembly: "Amplitude.Mercury.Simulation"
            // Type name must match the real namespace in the game.
            // From your OriginalGame.txt it is "RecessController" in Amplitude.Mercury.Simulation.
            return AccessTools.Method("Amplitude.Mercury.Simulation.RecessController:LowerSeaLevel");
        }

        // Signature in vanilla: private void LowerSeaLevel(int recessDepth)
        private static void Prefix(object __instance, ref int recessDepth)
        {
            int floor = EL2MapGenMod.Tuning.WorldGenTuningProfile.PersistentSeaLevelFloor;
            if (floor <= 0)
                return;

            // Read CurrentSeaLevel via Harmony Traverse (works even if internal/private)
            int currentSeaLevel = Traverse.Create(__instance).Property("CurrentSeaLevel").GetValue<int>();

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