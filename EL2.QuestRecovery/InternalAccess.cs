using System;
using System.Reflection;

namespace EL2.QuestRecovery
{
    internal static class InternalAccess
    {
        private static readonly BindingFlags AnyStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type _sandboxType;

        internal static object GetQuestController()
        {
            try
            {
                if (_sandboxType == null)
                {
                    _sandboxType = Type.GetType(
                        "Amplitude.Mercury.Sandbox.Sandbox, Amplitude.Mercury.Firstpass",
                        throwOnError: false);
                }

                if (_sandboxType == null)
                {
                    QuestRecoveryPlugin.Log.LogWarning("Could not find Sandbox type via reflection.");
                    return null;
                }

                // In decompile: "Amplitude.Mercury.Sandbox.Sandbox.QuestController = this;"
                // So QuestController is a static field or property on Sandbox.
                var field = _sandboxType.GetField("QuestController", AnyStatic);
                if (field != null)
                    return field.GetValue(null);

                var prop = _sandboxType.GetProperty("QuestController", AnyStatic);
                if (prop != null)
                    return prop.GetValue(null);

                QuestRecoveryPlugin.Log.LogWarning("Sandbox.QuestController not found (field/property).");
                return null;
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
                return null;
            }
        }
        
        internal static bool CompleteQuestStep(int questIndex)
        {
            try
            {
                object questController = GetQuestController();
                if (questController == null)
                {
                    QuestRecoveryPlugin.Log.LogWarning("CompleteQuestStep: QuestController is null.");
                    return false;
                }

                Type qcType = questController.GetType();

                // Look for a method named "CompleteQuestStep" that takes an int
                MethodInfo m = qcType.GetMethod(
                    "CompleteQuestStep",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(int) },
                    null);

                if (m == null)
                {
                    QuestRecoveryPlugin.Log.LogWarning("CompleteQuestStep: method not found on QuestController type " + qcType.FullName);
                    return false;
                }

                m.Invoke(questController, new object[] { questIndex });

                QuestRecoveryPlugin.Log.LogInfo("CompleteQuestStep invoked for questIndex=" + questIndex);
                return true;
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
                return false;
            }
        }
    }
}