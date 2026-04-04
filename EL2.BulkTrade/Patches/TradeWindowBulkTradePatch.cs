using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Sandbox;
using Amplitude.Mercury.UI;
using HarmonyLib;

namespace EL2.BulkTrade.Patches
{
    [HarmonyPatch(typeof(TradeWindow))]
    internal static class TradeWindowBulkTradePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SendOrderBuyResource")]
        private static bool SendOrderBuyResource_Prefix(TradeWindow __instance)
        {
            if (__instance.ResourceDefinition == null)
                return false;

            int quantity = BulkTradeInput.GetTradeQuantity();

            SandboxManager.PostOrder((Order)new OrderBuyResource
            {
                ResourceType = __instance.ResourceDefinition.ResourceType,
                Quantity = quantity
            });

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SendOrderSellResource")]
        private static bool SendOrderSellResource_Prefix(TradeWindow __instance)
        {
            if (__instance.ResourceDefinition == null)
                return false;

            int quantity = BulkTradeInput.GetTradeQuantity();

            SandboxManager.PostOrder((Order)new OrderSellResource
            {
                ResourceType = __instance.ResourceDefinition.ResourceType,
                Quantity = quantity
            });

            return false;
        }
    }
}