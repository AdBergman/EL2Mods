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

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Log.LogInfo("[" + PluginName + "] v" + PluginVersion + " loaded and patched.");
        }

        private void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
        }
    }
}