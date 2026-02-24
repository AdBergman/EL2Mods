using System;
using System.Reflection;
using HarmonyLib;

namespace EL2MapGenMod.Util
{
    internal static class WorldGenReflection
    {
        private static FieldInfo _contextField;

        public static object GetTaskContext(object taskInstance)
        {
            if (taskInstance == null)
                return null;

            // Cache once
            if (_contextField == null)
            {
                _contextField = AccessTools.Field(taskInstance.GetType(), "Context");

                if (_contextField == null)
                {
                    _contextField = FindContextFieldUpHierarchy(taskInstance.GetType());
                }
            }

            if (_contextField == null)
                return null;

            return _contextField.GetValue(taskInstance);
        }

        private static FieldInfo FindContextFieldUpHierarchy(Type type)
        {
            Type current = type;

            while (current != null)
            {
                FieldInfo field = current.GetField(
                    "Context",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field != null)
                    return field;

                current = current.BaseType;
            }

            return null;
        }
    }
}