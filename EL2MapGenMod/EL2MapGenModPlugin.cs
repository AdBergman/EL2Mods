using BepInEx;
using HarmonyLib;

namespace EL2MapGenMod
{
    [BepInPlugin("com.bergman.el2.persistentwater", "EL2 Persistent Water Mod", "1.1.0")]
    public sealed class EL2MapGenModPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony("com.bergman.el2.persistentwater");
            harmony.PatchAll();
            Logger.LogInfo("Persistent Water Mod Loaded: Forcing ~30% bottom-layer water coverage.");
        }
    }
}