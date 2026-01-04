using System;

namespace EL2.QuestRecovery
{
    internal static class PatchLogger
    {
        private static string _lastFactionQuestSignature = null;

        internal static void LogFactionQuestIfChanged(
            string signature,
            int questIndex,
            string questDef,
            string status,
            int stepIndex,
            int turnOfStepStart,
            string pendingChoicesInfo)
        {
            if (signature == _lastFactionQuestSignature)
                return;

            _lastFactionQuestSignature = signature;

            if (signature == null)
            {
                QuestRecoveryPlugin.Log.LogInfo("[FactionQuest] none in progress for player.");
                return;
            }

            QuestRecoveryPlugin.Log.LogInfo(
                "[FactionQuest] index=" + questIndex +
                " status=" + (string.IsNullOrEmpty(status) ? "?" : status) +
                " stepIndex=" + stepIndex +
                " def=" + (string.IsNullOrEmpty(questDef) ? "?" : questDef) +
                " turnStart=" + turnOfStepStart +
                " pendingChoices=" + pendingChoicesInfo
            );
        }
    }
}