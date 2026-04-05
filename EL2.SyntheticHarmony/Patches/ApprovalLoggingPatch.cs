using System;
using System.Reflection;
using HarmonyLib;

namespace EL2.SyntheticHarmony.Patches
{
    [HarmonyPatch]
    internal static class ApprovalLoggingPatch
    {
        private static int lastLoggedTurn = -1;

        private static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Amplitude.Mercury.Simulation.DepartmentOfTheInterior");
            return AccessTools.Method(type, "UpdateEmpireApproval");
        }

        [HarmonyPostfix]
        private static void Postfix(object empire)
        {
            if (empire == null)
                return;

            try
            {
                if (SyntheticApprovalLogic.IsHuman(empire))
                    return;

                if (!SyntheticApprovalLogic.IsAI(empire))
                    return;

                if (!SyntheticApprovalLogic.HasAnySettlements(empire))
                    return;

                int turn = SyntheticApprovalLogic.GetTurn();
                if (turn <= 0)
                    return;

                if (turn == lastLoggedTurn)
                    return;

                // Let the maintenance patch own the threshold turns.
                // That keeps total logging to one line per turn max.
                if (turn % 10 == 0)
                    return;

                lastLoggedTurn = turn;

                int empireIndex = SyntheticApprovalLogic.GetEmpireIndex(empire);
                int targetBaseSettlementApproval = SyntheticApprovalLogic.GetTargetAIBaseSettlementApproval();

                string approval = SyntheticApprovalLogic.GetEmpireApprovalString(empire);
                string baseSettlementApproval = SyntheticApprovalLogic.GetBaseSettlementApprovalString(empire);
                string bonusApprovalOnSettlement = SyntheticApprovalLogic.GetBonusApprovalOnSettlementString(empire);
                string sumOfApproval = SyntheticApprovalLogic.GetSumOfApprovalString(empire);

                SyntheticHarmonyPlugin.Log.LogInfo(
                    "[SyntheticHarmony][Turn " + turn + "]" +
                    " Empire=" + empireIndex +
                    " AI=True" +
                    " TargetBaseSettlementApproval=" + targetBaseSettlementApproval +
                    " Approval=" + approval +
                    " BaseSettlementApproval=" + baseSettlementApproval +
                    " BonusApprovalOnSettlement=" + bonusApprovalOnSettlement +
                    " SumOfApproval=" + sumOfApproval
                );
            }
            catch (Exception ex)
            {
                SyntheticHarmonyPlugin.Log.LogError(
                    "[SyntheticHarmony] ApprovalLoggingPatch failed: " + ex);
            }
        }
    }
}