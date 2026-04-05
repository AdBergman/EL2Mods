using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EL2.SyntheticHarmony
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SyntheticHarmonyPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.bergman.el2.syntheticharmony";
        public const string PluginName = "EL2 Synthetic Harmony";
        public const string PluginVersion = "2.2.0";

        internal static ManualLogSource Log;

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            Log.LogInfo("==================================================");
            Log.LogInfo("[SyntheticHarmony] BOOTSTRAP START");
            Log.LogInfo("[SyntheticHarmony] Plugin=" + PluginName);
            Log.LogInfo("[SyntheticHarmony] Version=" + PluginVersion);
            Log.LogInfo("[SyntheticHarmony] Guid=" + PluginGuid);

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Log.LogInfo("[SyntheticHarmony] Harmony.PatchAll complete");
            Log.LogInfo("[SyntheticHarmony] ACTIVE writer patch = MajorEmpire.ApplyGameDifficultyEffects postfix");
            Log.LogInfo("[SyntheticHarmony] ACTIVE observer patch = DepartmentOfTheInterior.UpdateEmpireApproval postfix");
            Log.LogInfo("==================================================");
        }

        private void OnDestroy()
        {
            if (harmony != null)
            {
                Log?.LogInfo("[SyntheticHarmony] Unpatching self");
                harmony.UnpatchSelf();
                harmony = null;
            }
        }
    }
}