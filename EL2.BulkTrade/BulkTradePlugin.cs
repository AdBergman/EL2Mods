using BepInEx;
using HarmonyLib;

namespace EL2.BulkTrade
{
    [BepInPlugin("com.bergman.el2.bulktrade", "EL2 Bulk Trade", "1.0.0")]
    public sealed class BulkTradePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony("com.bergman.el2.bulktrade");
            harmony.PatchAll();
            Logger.LogInfo("EL2 Bulk Trading loaded.");
        }
    }
}