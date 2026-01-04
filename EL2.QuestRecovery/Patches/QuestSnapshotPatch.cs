using System;
using System.Collections;
using HarmonyLib;

namespace EL2.QuestRecovery.Patches
{
    [HarmonyPatch(typeof(Amplitude.Mercury.Interop.QuestSnapshot), "Synchronize")]
    internal static class QuestSnapshotPatch
    {
        // DEV: arm once, auto-complete the first MajorFaction quest we see, then disarm
        private static bool _devAutoCompleteOnce = true;
        private static string _devLastCompletedSignature = null;

        private static void Prefix()
        {
            try
            {
                object questController = InternalAccess.GetQuestController();
                if (questController == null)
                    return;

                IList quests = PatchHelper.ReadAsIList(questController, "Quests");

                // If no quests yet, log "none" once (PatchLogger will gate it)
                if (quests == null || quests.Count <= 0)
                {
                    PatchLogger.LogFactionQuestIfChanged(
                        null, -1, "", "", -1, -1, "");
                    return;
                }

                int questIndex;
                string questDef;
                bool found = QuestFinder.TryFindFirstPlayerMajorFactionQuestInProgress(
                    quests, out questIndex, out questDef);

                if (!found)
                {
                    PatchLogger.LogFactionQuestIfChanged(
                        null, -1, "", "", -1, -1, "");
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

                // DEV one-shot action:
                if (_devAutoCompleteOnce &&
                    string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(pendingChoicesInfo, "null", StringComparison.OrdinalIgnoreCase) &&
                    signature != _devLastCompletedSignature)
                {
                    QuestRecoveryPlugin.Log.LogInfo("[DEV] Auto-completing first faction quest once (then disarming). index=" + questIndex);

                    bool ok = InternalAccess.CompleteQuestStep(questIndex);

                    _devLastCompletedSignature = signature;
                    _devAutoCompleteOnce = false;

                    QuestRecoveryPlugin.Log.LogInfo("[DEV] Auto-complete result: " + (ok ? "OK" : "FAILED"));
                }
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
            }
        }
    }
}
