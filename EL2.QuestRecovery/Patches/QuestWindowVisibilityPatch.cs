using HarmonyLib;
using Amplitude.Mercury.UI;
using Amplitude.Mercury.UI.Windows;
using Amplitude.UI;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery.Patches
{
    [HarmonyPatch(typeof(GameWindow), "OnVisibilityChanged")]
    internal static class QuestWindowVisibilityPatch
    {
        private static bool IsOpen(UIAbstractShowable.VisibilityState s)
            => s == UIAbstractShowable.VisibilityState.PreShowing
               || s == UIAbstractShowable.VisibilityState.Showing
               || s == UIAbstractShowable.VisibilityState.Visible;

        static void Postfix(
            GameWindow __instance,
            UIAbstractShowable.VisibilityState oldState,
            UIAbstractShowable.VisibilityState newState)
        {
            if (!(__instance is QuestWindow))
                return;

            UiState.IsQuestWindowOpen = IsOpen(newState);
        }
    }
}