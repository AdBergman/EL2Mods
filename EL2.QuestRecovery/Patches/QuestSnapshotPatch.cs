using System;
using System.Collections;
using HarmonyLib;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery.Patches
{
    [HarmonyPatch(typeof(Amplitude.Mercury.Interop.QuestSnapshot), "Synchronize")]
    internal static class QuestSnapshotPatch
    {
        private static string _lastTargetSignature;

        private static void Prefix()
        {
            try
            {
                object questController = InternalAccess.GetQuestController();
                if (questController == null)
                    return;

                IList quests = PatchHelper.ReadAsIList(questController, "Quests");
                if (quests == null || quests.Count == 0)
                {
                    _lastTargetSignature = null;
                    QuestRecoveryTargetState.Clear();
                    PatchLogger.LogFactionQuestIfChanged(null, -1, "", "", -1, -1, "");
                    return;
                }

                if (!QuestFinder.TryFindFirstPlayerMajorFactionQuestInProgress(quests, out int questIndex, out string questDef))
                {
                    _lastTargetSignature = null;
                    QuestRecoveryTargetState.Clear();
                    PatchLogger.LogFactionQuestIfChanged(null, -1, "", "", -1, -1, "");
                    return;
                }

                object questObj = PatchHelper.FindQuestByIndex(quests, questIndex);
                if (questObj == null)
                    return;

                string status = PatchHelper.SafeToString(PatchHelper.ReadObj(questObj, "Status"));
                int stepIndex = PatchHelper.ReadInt(questObj, "StepIndex", -1);
                int turnOfStepStart = PatchHelper.ReadInt(questObj, "TurnOfStepStart", -1);
                string pendingChoicesInfo = PatchHelper.GetArrayInfo(questObj, "PendingChoices");

                string signature =
                    questIndex + "|" +
                    (questDef ?? "") + "|" +
                    status + "|" +
                    stepIndex + "|" +
                    turnOfStepStart + "|" +
                    pendingChoicesInfo;

                // Single, deduped status line
                PatchLogger.LogFactionQuestIfChanged(
                    signature, questIndex, questDef, status, stepIndex, turnOfStepStart, pendingChoicesInfo);

                // UI target: update only when it actually changes
                if (signature == _lastTargetSignature)
                    return;

                _lastTargetSignature = signature;

                string label =
                    $"QuestIndex {questIndex}\n" +
                    $"{questDef}\n" +
                    $"Status: {status} | Step: {stepIndex} | Started: T{turnOfStepStart}\n" +
                    $"PendingChoices: {pendingChoicesInfo}\n";

                string goalDebugText = "";
                try
                {
                    goalDebugText = QuestGoalDebugBuilder.BuildGoalDebug(questObj);
                }
                catch (Exception ex)
                {
                    goalDebugText = "GoalDebugText build failed: " + ex.GetType().Name;
                }

                QuestRecoveryTargetState.SetFull(
                    questIndex,
                    label,
                    signature,
                    goalDebugText,
                    status,
                    pendingChoicesInfo);
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
            }
        }
    }
}
