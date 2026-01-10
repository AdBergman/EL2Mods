using System;
using System.Collections;
using HarmonyLib;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery.Patches
{
    [HarmonyPatch(typeof(Amplitude.Mercury.Interop.QuestSnapshot), "Synchronize")]
    internal static class QuestSnapshotPatch
    {
        // Used for overlay updates and to avoid rebuilding label/debug text every tick.
        private static string _lastTargetSignature;

        // Used to log ONLY when the "current quest acquired/changed" event happens.
        private static string _lastLoggedQuestKey;

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
                    ClearTarget();
                    return;
                }

                if (!QuestFinder.TryFindFirstPlayerMajorFactionQuestInProgress(
                        quests, out int questIndex, out string questDef))
                {
                    ClearTarget();
                    return;
                }

                object questObj = PatchHelper.FindQuestByIndex(quests, questIndex);
                if (questObj == null)
                    return;

                string status = PatchHelper.SafeToString(PatchHelper.ReadObj(questObj, "Status"));
                int stepIndex = PatchHelper.ReadInt(questObj, "StepIndex", -1);
                int turnOfStepStart = PatchHelper.ReadInt(questObj, "TurnOfStepStart", -1);
                string pendingChoicesInfo = PatchHelper.GetArrayInfo(questObj, "PendingChoices");

                // Signature for "same quest state" gating (overlay + internal)
                string signature =
                    questIndex + "|" +
                    (questDef ?? "") + "|" +
                    status + "|" +
                    stepIndex + "|" +
                    turnOfStepStart + "|" +
                    pendingChoicesInfo;

                // Key for "new/current quest acquired" logging:
                // we intentionally do NOT include PendingChoices to avoid noisy toggles.
                string questKey =
                    questIndex + "|" +
                    (questDef ?? "") + "|" +
                    status + "|" +
                    stepIndex + "|" +
                    turnOfStepStart;

                // ✅ LOG LINE #1: only when quest changes / advances
                if (questKey != _lastLoggedQuestKey)
                {
                    _lastLoggedQuestKey = questKey;

                    // This should be your single "New/Current quest acquired" line.
                    // (Keep it compact; the overlay still shows full details.)
                    QuestRecoveryPlugin.Log.LogInfo(
                        $"[FactionQuest] index={questIndex} status={status} stepIndex={stepIndex} def={questDef} turnStart={turnOfStepStart}");
                }

                // ✅ Overlay state: update only when something relevant changed
                if (signature == _lastTargetSignature)
                    return;

                _lastTargetSignature = signature;

                string label =
                    $"QuestIndex {questIndex}\n" +
                    $"{questDef}\n" +
                    $"Status: {status} | Step: {stepIndex} | Started: T{turnOfStepStart}\n" +
                    $"PendingChoices: {pendingChoicesInfo}\n";

                // Optional: keep goal text (even if you later decide to hide it via UI toggle)
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

        private static void ClearTarget()
        {
            _lastTargetSignature = null;
            _lastLoggedQuestKey = null;

            QuestRecoveryTargetState.Clear();

            // Don’t log anything here (no “none in progress” spam).
            // If you still want ONE line when it disappears, do it in PatchLogger instead.
        }
    }
}
