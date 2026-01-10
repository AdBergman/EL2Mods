using System;

namespace EL2.QuestRecovery
{
    internal static class PatchLogger
    {
        private static string _lastFactionQuestSignature;

        internal static void LogFactionQuestIfChanged(
            string signature,
            int questIndex,
            string questDef)
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
                "[FactionQuest] current=" +
                (string.IsNullOrEmpty(questDef) ? "?" : questDef) +
                " (index=" + questIndex + ")"
            );
        }
    }
}