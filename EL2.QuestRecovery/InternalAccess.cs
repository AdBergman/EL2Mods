using System;
using System.Collections;
using System.Reflection;

namespace EL2.QuestRecovery
{
    internal static class InternalAccess
    {
        private const BindingFlags AnyStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type _sandboxType;
        private static MemberInfo _sandboxQuestControllerMember; // FieldInfo or PropertyInfo
        private static MethodInfo _completeQuestStepMethod;

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
                    return null;

                if (_sandboxQuestControllerMember == null)
                {
                    // Prefer field if present (matches decompile: Sandbox.QuestController = this;)
                    FieldInfo f = _sandboxType.GetField("QuestController", AnyStatic);
                    if (f != null)
                    {
                        _sandboxQuestControllerMember = f;
                    }
                    else
                    {
                        PropertyInfo p = _sandboxType.GetProperty("QuestController", AnyStatic);
                        if (p != null)
                            _sandboxQuestControllerMember = p;
                    }
                }

                FieldInfo field = _sandboxQuestControllerMember as FieldInfo;
                if (field != null)
                    return field.GetValue(null);

                PropertyInfo prop = _sandboxQuestControllerMember as PropertyInfo;
                if (prop != null)
                    return prop.GetValue(null, null);

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
                    return false;

                // Cache MethodInfo once per runtime (QuestController type is stable)
                if (_completeQuestStepMethod == null)
                {
                    Type qcType = questController.GetType();
                    _completeQuestStepMethod = qcType.GetMethod(
                        "CompleteQuestStep",
                        AnyInstance,
                        binder: null,
                        types: new[] { typeof(int) },
                        modifiers: null);

                    if (_completeQuestStepMethod == null)
                        return false;
                }

                _completeQuestStepMethod.Invoke(questController, new object[] { questIndex });
                return true;
            }
            catch (TargetInvocationException tie)
            {
                QuestRecoveryPlugin.Log.LogError(
                    "[QuestRecovery] CompleteQuestStep threw: " +
                    (tie.InnerException != null ? tie.InnerException.ToString() : tie.ToString()));
                return false;
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Calls QuestController.CompleteQuestStep(questIndex) and then best-effort finalizes
        /// reward effects that have IsTwoStepSpawnOngoing=true by calling EndSpawnEntities().
        /// This mirrors the important part of QuestController.OnQuestDialogEnd for rewards.
        /// </summary>
        internal static bool CompleteQuestStepAndFinalize(int questIndex)
        {
            if (!CompleteQuestStep(questIndex))
                return false;

            try
            {
                FinalizeTwoStepRewardSpawns(questIndex);
            }
            catch (Exception ex)
            {
                // Completion still counts as success; finalization is best-effort.
                QuestRecoveryPlugin.Log.LogError("[QuestRecovery] Post-complete finalization failed: " + ex);
            }

            return true;
        }

        private static int FinalizeTwoStepRewardSpawns(int questIndex)
        {
            object questController = GetQuestController();
            if (questController == null)
                return 0;

            IList quests = PatchHelper.ReadAsIList(questController, "Quests");
            if (quests == null || quests.Count == 0)
                return 0;

            // Fast path: questIndex is usually the slot index.
            object questObj = (questIndex >= 0 && questIndex < quests.Count) ? quests[questIndex] : null;

            // Fallback: scan by Quest.Index
            if (questObj == null || PatchHelper.ReadInt(questObj, "Index", -999) != questIndex)
                questObj = PatchHelper.FindQuestByIndex(quests, questIndex);

            if (questObj == null)
                return 0;

            // If EndingDialogPlayed is already true, engine already did this (or should have).
            if (ReadBool(questObj, "EndingDialogPlayed", false))
                return 0;

            // Mark it so repeated clicks don't repeatedly finalize (best-effort).
            WriteBoolIfPossible(questObj, "EndingDialogPlayed", true);

            object rewardObj = PatchHelper.ReadObj(questObj, "Reward");
            if (rewardObj == null)
                return 0;

            int numberOfRewards = PatchHelper.ReadInt(rewardObj, "NumberOfRewards", -1);
            if (numberOfRewards <= 0)
                return 0;

            Array effectsArray = PatchHelper.ReadAsArray(rewardObj, "Effects");
            if (effectsArray == null || effectsArray.Length == 0)
                return 0;

            int finalizedCount = 0;
            int len = Math.Min(numberOfRewards, effectsArray.Length);

            for (int i = 0; i < len; i++)
            {
                object effect = effectsArray.GetValue(i);
                if (effect == null)
                    continue;

                // Can't reference ISimulationEventEffectNarrativeSpawner directly; detect via members.
                if (!ReadBool(effect, "IsTwoStepSpawnOngoing", false))
                    continue;

                MethodInfo endSpawn = effect.GetType().GetMethod(
                    "EndSpawnEntities",
                    AnyInstance,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);

                if (endSpawn == null)
                    continue;

                try
                {
                    endSpawn.Invoke(effect, null);
                    finalizedCount++;
                }
                catch (TargetInvocationException tie)
                {
                    QuestRecoveryPlugin.Log.LogError(
                        "[QuestRecovery] EndSpawnEntities threw: " +
                        (tie.InnerException != null ? tie.InnerException.ToString() : tie.ToString()));
                }
                catch (Exception ex)
                {
                    QuestRecoveryPlugin.Log.LogError("[QuestRecovery] EndSpawnEntities failed: " + ex);
                }
            }

            return finalizedCount;
        }

        private static bool ReadBool(object obj, string memberName, bool defaultValue)
        {
            object v = PatchHelper.ReadObj(obj, memberName);
            if (v == null) return defaultValue;

            try { return Convert.ToBoolean(v); }
            catch { return defaultValue; }
        }

        private static void WriteBoolIfPossible(object obj, string memberName, bool value)
        {
            if (obj == null) return;

            try
            {
                Type t = obj.GetType();

                FieldInfo f = t.GetField(memberName, AnyInstance);
                if (f != null && f.FieldType == typeof(bool))
                {
                    f.SetValue(obj, value);
                    return;
                }

                PropertyInfo p = t.GetProperty(memberName, AnyInstance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(bool))
                {
                    p.SetValue(obj, value, null);
                }
            }
            catch
            {
                // best-effort only
            }
        }
    }
}
