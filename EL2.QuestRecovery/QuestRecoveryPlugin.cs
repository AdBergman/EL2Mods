using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery
{
    [BepInPlugin("com.calmbreakfast.el2.questrecovery", "EL2 Quest Recovery", "1.1.0")]
    public sealed class QuestRecoveryPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ConfigEntry<float> OverlayX;
        internal static ConfigEntry<float> OverlayY;

        private Harmony _harmony;
        private QuestRecoveryOverlay _overlay;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo("EL2 Quest Recovery loaded.");

            _harmony = new Harmony("com.calmbreakfast.el2.questrecovery");
            _harmony.PatchAll();
            Logger.LogInfo("EL2 Quest Recovery Harmony patched.");

            OverlayX = Config.Bind("UI", "OverlayX", -1f, "Overlay X position in pixels. -1 = auto.");
            OverlayY = Config.Bind("UI", "OverlayY", -1f, "Overlay Y position in pixels. -1 = auto.");

            _overlay = gameObject.AddComponent<QuestRecoveryOverlay>();
            _overlay.InitLogger(Logger);

            _overlay.CanSkip = CanSkipNow;
            _overlay.GetTargetLabel = GetTargetLabel;
            _overlay.GetGoalDebugText = GetGoalDebugText;
            _overlay.SkipAction = SkipCurrentQuestStep;

            Logger.LogInfo("QuestRecoveryOverlay initialized.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();

            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }
        }

        private bool CanSkipNow()
        {
            if (!UiState.IsQuestWindowOpen) return false;
            if (!QuestRecoveryTargetState.HasTarget) return false;

            if (!string.Equals(QuestRecoveryTargetState.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!QuestRecoveryTargetState.IsPendingChoicesEmpty())
                return false;

            return true;
        }

        private string GetTargetLabel()
        {
            if (!QuestRecoveryTargetState.HasTarget)
                return "No target yet.\nOpen the Quest window and wait a moment.";

            return QuestRecoveryTargetState.TargetLabel ?? $"QuestIndex={QuestRecoveryTargetState.QuestIndex}";
        }

        private string GetGoalDebugText()
        {
            return QuestRecoveryTargetState.GoalDebugText ?? "";
        }

        private void SkipCurrentQuestStep()
        {
            if (!QuestRecoveryTargetState.HasTarget)
            {
                Log.LogWarning("[QuestRecovery] Skip requested but no target is available.");
                return;
            }

            int questIndex = QuestRecoveryTargetState.QuestIndex;

            // Minimal log: only the NextQuest name.
            TryLogNextQuestName(questIndex);

            // Keep or remove depending on how quiet you want the mod to be.
            Log.LogWarning(
                $"[QuestRecovery] User confirmed: CompleteQuestStep questIndex={questIndex} status={QuestRecoveryTargetState.Status} pendingChoices={QuestRecoveryTargetState.PendingChoicesInfo}");

            bool ok = InternalAccess.CompleteQuestStepAndFinalize(questIndex);
            if (!ok)
                Log.LogWarning("[QuestRecovery] CompleteQuestStepAndFinalize failed.");
        }

        private void TryLogNextQuestName(int questIndex)
        {
            try
            {
                object questController = InternalAccess.GetQuestController();
                if (questController == null)
                    return;

                IList quests = PatchHelper.ReadAsIList(questController, "Quests");
                if (quests == null)
                    return;

                object questObj = PatchHelper.FindQuestByIndex(quests, questIndex);
                if (questObj == null)
                    return;

                int stepIndex = PatchHelper.ReadInt(questObj, "StepIndex", -1);

                object choiceDef = PatchHelper.ReadObj(questObj, "QuestChoiceDefinition");
                object stepsObj = PatchHelper.ReadObj(choiceDef, "QuestSteps");

                if (!(stepsObj is IList steps) || stepIndex < 0 || stepIndex >= steps.Count)
                    return;

                object stepObj = steps[stepIndex];
                object nextQuestObj = PatchHelper.ReadObj(stepObj, "NextQuest");

                string nextQuestName = PatchHelper.SafeToString(
                    PatchHelper.ReadObj(nextQuestObj, "ElementName"));

                if (string.IsNullOrWhiteSpace(nextQuestName))
                    nextQuestName = "(none)";

                Log.LogWarning(
                    $"[QuestRecovery] NextQuest after questIndex={questIndex} stepIndex={stepIndex} => '{nextQuestName}'");
            }
            catch (Exception ex)
            {
                // Keep errors visible; they matter.
                Log.LogError(ex);
            }
        }
    }
}
