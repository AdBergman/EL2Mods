using System;
using System.Collections;
using EL2.QuestRecovery.Safety;
using HarmonyLib;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery.Patches
{
    [HarmonyPatch(typeof(Amplitude.Mercury.Interop.QuestSnapshot), "Synchronize")]
    internal static class QuestSnapshotPatch
    {
        private static string _lastTargetSignature = null;

        private static void Prefix()
        {
            try
            {
                object questController = InternalAccess.GetQuestController();
                if (questController == null)
                    return;

                IList quests = PatchHelper.ReadAsIList(questController, "Quests");

                if (quests == null || quests.Count <= 0)
                {
                    _lastTargetSignature = null;
                    QuestRecoveryTargetState.Clear();
                    PatchLogger.LogFactionQuestIfChanged(null, -1, "", "", -1, -1, "");
                    return;
                }

                int questIndex;
                string questDef;
                bool found = QuestFinder.TryFindFirstPlayerMajorFactionQuestInProgress(
                    quests, out questIndex, out questDef);

                if (!found)
                {
                    _lastTargetSignature = null;
                    QuestRecoveryTargetState.Clear();
                    PatchLogger.LogFactionQuestIfChanged(null, -1, "", "", -1, -1, "");
                    return;
                }

                object questObj = PatchHelper.FindQuestByIndex(quests, questIndex);

                string status = PatchHelper.SafeToString(PatchHelper.ReadObj(questObj, "Status"));
                int stepIndex = PatchHelper.ReadInt(questObj, "StepIndex", -1);
                int turnOfStepStart = PatchHelper.ReadInt(questObj, "TurnOfStepStart", -1);
                string pendingChoicesInfo = PatchHelper.GetArrayInfo(questObj, "PendingChoices");

                // Signature for "same quest state" gating
                string signature =
                    questIndex + "|" +
                    (questDef ?? "") + "|" +
                    status + "|" +
                    stepIndex + "|" +
                    turnOfStepStart + "|" +
                    pendingChoicesInfo;

                PatchLogger.LogFactionQuestIfChanged(
                    signature, questIndex, questDef, status, stepIndex, turnOfStepStart, pendingChoicesInfo);

                // ✅ UI target: only update when it actually changes
                if (signature != _lastTargetSignature)
                {
                    _lastTargetSignature = signature;

                    string label =
                        $"QuestIndex {questIndex}\n" +
                        $"{questDef}\n" +
                        $"Status: {status} | Step: {stepIndex} | Started: T{turnOfStepStart}\n";

                    // ✅ Real goal/debug info (underlying prereq + ObjectiveLoreKey, etc)
                    string goalDebugText = "";
                    try
                    {
                        goalDebugText = QuestGoalDebugBuilder.BuildGoalDebug(questObj);
                    }
                    catch (Exception ex)
                    {
                        goalDebugText = "GoalDebugText build failed: " + ex.GetType().Name;
                    }

                    // Store all in shared state
                    QuestRecoveryTargetState.SetFull(questIndex, label, signature, goalDebugText);
                }
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
            }
        }
    }
}
