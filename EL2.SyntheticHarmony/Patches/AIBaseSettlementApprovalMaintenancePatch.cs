using System;
using System.Reflection;
using HarmonyLib;

namespace EL2.SyntheticHarmony.Patches
{
    [HarmonyPatch]
    internal static class AIBaseSettlementApprovalMaintenancePatch
    {
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

                int empireIndex = SyntheticApprovalLogic.GetEmpireIndex(empire);
                int targetBaseSettlementApproval = SyntheticApprovalLogic.GetTargetAIBaseSettlementApproval();

                bool needsReapply = !SyntheticApprovalLogic.IsBaseSettlementApprovalEqualTo(
                    empire,
                    targetBaseSettlementApproval);

                if (!needsReapply)
                    return;

                string beforeBase = SyntheticApprovalLogic.GetBaseSettlementApprovalString(empire);

                bool applied = SyntheticApprovalLogic.ForceAIBaseSettlementApproval(
                    empire,
                    targetBaseSettlementApproval);

                string afterBase = SyntheticApprovalLogic.GetBaseSettlementApprovalString(empire);
                string approval = SyntheticApprovalLogic.GetEmpireApprovalString(empire);
                string sumOfApproval = SyntheticApprovalLogic.GetSumOfApprovalString(empire);
                string bonusApproval = SyntheticApprovalLogic.GetBonusApprovalOnSettlementString(empire);

                SyntheticHarmonyPlugin.Log.LogInfo(
                    "[SyntheticHarmony][Maintenance][Turn " + turn + "]" +
                    " Empire=" + empireIndex +
                    " AI=True" +
                    " BeforeBase=" + beforeBase +
                    " Applied=" + applied +
                    " AfterBase=" + afterBase +
                    " Approval=" + approval +
                    " BonusApprovalOnSettlement=" + bonusApproval +
                    " SumOfApproval=" + sumOfApproval +
                    " TargetBaseSettlementApproval=" + targetBaseSettlementApproval
                );
            }
            catch (Exception ex)
            {
                SyntheticHarmonyPlugin.Log.LogError(
                    "[SyntheticHarmony] AIBaseSettlementApprovalMaintenancePatch failed: " + ex);
            }
        }
    }
}