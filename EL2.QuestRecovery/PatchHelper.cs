using System;
using System.Collections;
using System.Reflection;

namespace EL2.QuestRecovery
{
    internal static class PatchHelper
    {
        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static object ReadObj(object obj, string memberName)
        {
            if (obj == null || string.IsNullOrEmpty(memberName))
                return null;

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

        internal static int ReadInt(object obj, string memberName, int defaultValue)
        {
            object v = ReadObj(obj, memberName);
            return v is int i ? i : defaultValue;
        }

        internal static string ReadString(object obj, string memberName, string defaultValue)
        {
            object v = ReadObj(obj, memberName);
            if (v == null) return defaultValue;

            if (v is string s) return s;

            try { return v.ToString(); }
            catch { return defaultValue; }
        }

        internal static IList ReadAsIList(object obj, string memberName)
        {
            return ReadObj(obj, memberName) as IList;
        }

        internal static Array ReadAsArray(object obj, string memberName)
        {
            object v = ReadObj(obj, memberName);
            return v as Array;
        }

        internal static object FindQuestByIndex(IList quests, int index)
        {
            if (quests == null) return null;

            for (int i = 0; i < quests.Count; i++)
            {
                object q = quests[i];
                if (q == null) continue;

                int idx = ReadInt(q, "Index", -1);
                if (idx == index)
                    return q;
            }

            return null;
        }

        internal static string GetArrayInfo(object obj, string memberName)
        {
            object v = ReadObj(obj, memberName);
            if (v == null) return "null";

            var asIList = v as IList;
            if (asIList != null)
                return v.GetType().Name + "(Count=" + asIList.Count + ")";

            var asArray = v as Array;
            if (asArray != null)
                return v.GetType().Name + "(Length=" + asArray.Length + ")";

            return v.GetType().Name;
        }

        internal static string SafeToString(object v)
        {
            if (v == null) return "";
            try { return v.ToString(); }
            catch { return ""; }
        }
    }
}