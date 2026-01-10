using System;
using System.Collections;

namespace EL2.QuestRecovery
{
    internal static class QuestFinder
    {
        internal static bool TryFindFirstPlayerMajorFactionQuestInProgress(
            IList quests,
            out int questIndex,
            out string questDefName)
        {
            questIndex = -1;
            questDefName = "";

            if (quests == null || quests.Count == 0)
                return false;

            for (int i = 0; i < quests.Count; i++)
            {
                object quest = quests[i];
                if (quest == null) continue;

                // Player empire is 0
                if (PatchHelper.ReadInt(quest, "MajorEmpireIndex", -1) != 0)
                    continue;

                // Only InProgress
                string status = PatchHelper.ReadString(quest, "Status", "");
                if (!string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Must be MajorFaction + faction quest category
                string catType = ReadNestedString(quest, "QuestCategoryDefinition", "QuestCategoryType");
                if (!string.Equals(catType, "MajorFaction", StringComparison.OrdinalIgnoreCase))
                    continue;

                string catName = ReadNestedString(quest, "QuestCategoryDefinition", "Name");
                if (!string.Equals(catName, "QuestCategory_FactionQuest", StringComparison.OrdinalIgnoreCase))
                    continue;

                questIndex = PatchHelper.ReadInt(quest, "Index", -1);
                questDefName = ReadNestedString(quest, "QuestDefinition", "Name");

                return questIndex >= 0;
            }

            return false;
        }

        private static string ReadNestedString(object obj, string parentMember, string childMember)
        {
            object parent = PatchHelper.ReadObj(obj, parentMember);
            if (parent == null) return "";
            return PatchHelper.ReadString(parent, childMember, "");
        }
    }
}
