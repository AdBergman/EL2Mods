using System;
using System.Reflection;
using HarmonyLib;

namespace EL2.SyntheticHarmony.Patches
{
    [HarmonyPatch]
    internal static class AIBaseSettlementApprovalPatch
    {
        private static bool firstSuccessfulWriteLogDone;

        private static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Amplitude.Mercury.Simulation.MajorEmpire");
            return AccessTools.Method(type, "ApplyGameDifficultyEffects");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            if (__instance == null)
                return;

            try
            {
                if (SyntheticApprovalLogic.IsHuman(__instance))
                    return;

                if (!SyntheticApprovalLogic.IsAI(__instance))
                    return;

                int empireIndex = SyntheticApprovalLogic.GetEmpireIndex(__instance);

                string beforeBase = SyntheticApprovalLogic.GetBaseSettlementApprovalString(__instance);

                bool applied = SyntheticApprovalLogic.ForceAIBaseSettlementApproval(
                    __instance,
                    SyntheticApprovalLogic.TargetAIBaseSettlementApproval);

                string afterBase = SyntheticApprovalLogic.GetBaseSettlementApprovalString(__instance);
                string approval = SyntheticApprovalLogic.GetEmpireApprovalString(__instance);
                string sumOfApproval = SyntheticApprovalLogic.GetSumOfApprovalString(__instance);
                string bonusApproval = SyntheticApprovalLogic.GetBonusApprovalOnSettlementString(__instance);

                if (applied || !firstSuccessfulWriteLogDone)
                {
                    SyntheticHarmonyPlugin.Log.LogInfo(
                        "[SyntheticHarmony][DifficultyHook] Empire=" + empireIndex +
                        " Human=False" +
                        " BeforeBase=" + beforeBase +
                        " Applied=" + applied +
                        " AfterBase=" + afterBase +
                        " Approval=" + approval +
                        " BonusApprovalOnSettlement=" + bonusApproval +
                        " SumOfApproval=" + sumOfApproval +
                        " TargetBaseSettlementApproval=" + SyntheticApprovalLogic.TargetAIBaseSettlementApproval
                    );

                    if (applied)
                        firstSuccessfulWriteLogDone = true;
                }
            }
            catch (Exception ex)
            {
                SyntheticHarmonyPlugin.Log.LogError(
                    "[SyntheticHarmony] AIBaseSettlementApprovalPatch failed: " + ex);
            }
        }
    }
}