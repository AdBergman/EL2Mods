using System;
using System.Collections;
using System.Reflection;

namespace EL2.QuestRecovery
{
    internal static class QuestFinder
    {
        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static bool TryFindFirstPlayerMajorFactionQuestInProgress(
            IList quests,
            out int questIndex,
            out string questDefName)
        {
            questIndex = -1;
            questDefName = "";

            if (quests == null || quests.Count <= 0)
                return false;

            for (int i = 0; i < quests.Count; i++)
            {
                object quest = quests[i];
                if (quest == null)
                    continue;

                // SP assumption (your current rule): player empire is 0
                int majorEmpireIndex = ReadInt(quest, "MajorEmpireIndex", -1);
                if (majorEmpireIndex != 0)
                    continue;

                // Only in-progress quests
                string status = SafeToString(ReadObj(quest, "Status"));
                if (!string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Major faction quest classification
                string categoryType = SafeNestedToString(quest, "QuestCategoryDefinition", "QuestCategoryType");
                if (!string.Equals(categoryType, "MajorFaction", StringComparison.OrdinalIgnoreCase))
                    continue;

                string categoryDefName = SafeNestedToString(quest, "QuestCategoryDefinition", "Name");
                if (!string.Equals(categoryDefName, "QuestCategory_FactionQuest", StringComparison.OrdinalIgnoreCase))
                    continue;

                // If we get here, this is a "player major faction quest in progress"
                questIndex = ReadInt(quest, "Index", -1);
                questDefName = SafeNestedToString(quest, "QuestDefinition", "Name");

                return questIndex >= 0;
            }

            return false;
        }

        // ---------------- Reflection helpers ----------------

        private static object ReadObj(object obj, string memberName)
        {
            if (obj == null) return null;
            if (string.IsNullOrEmpty(memberName)) return null;

            Type t = obj.GetType();

            FieldInfo f = t.GetField(memberName, AnyInstance);
            if (f != null)
            {
                try { return f.GetValue(obj); } catch { return null; }
            }

            PropertyInfo p = t.GetProperty(memberName, AnyInstance);
            if (p != null)
            {
                try { return p.GetValue(obj, null); } catch { return null; }
            }

            return null;
        }

        private static int ReadInt(object obj, string memberName, int defaultValue)
        {
            object v = ReadObj(obj, memberName);
            if (v is int) return (int)v;
            return defaultValue;
        }

        private static string SafeNestedToString(object obj, string parentMember, string childMember)
        {
            try
            {
                object parent = ReadObj(obj, parentMember);
                if (parent == null) return "";

                object child = ReadObj(parent, childMember);
                return SafeToString(child);
            }
            catch
            {
                return "";
            }
        }

        private static string SafeToString(object v)
        {
            if (v == null) return "";
            try { return v.ToString(); }
            catch { return ""; }
        }
    }
}
