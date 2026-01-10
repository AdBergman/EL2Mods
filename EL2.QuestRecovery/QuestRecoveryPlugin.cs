using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EL2.QuestRecovery.UI;
using HarmonyLib;

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

        // Optional: keep startup quiet if you want (set to false to keep current behavior).
        private const bool LogStartupLines = true;

        private void Awake()
        {
            Log = Logger;

            if (LogStartupLines)
                Log.LogInfo("EL2 Quest Recovery loaded.");

            _harmony = new Harmony("com.calmbreakfast.el2.questrecovery");
            _harmony.PatchAll();

            if (LogStartupLines)
                Log.LogInfo("EL2 Quest Recovery Harmony patched.");

            OverlayX = Config.Bind("UI", "OverlayX", -1f, "Overlay X position in pixels. -1 = auto.");
            OverlayY = Config.Bind("UI", "OverlayY", -1f, "Overlay Y position in pixels. -1 = auto.");

            _overlay = gameObject.AddComponent<QuestRecoveryOverlay>();
            _overlay.InitLogger(Logger);

            _overlay.CanComplete = CanCompleteNow;
            _overlay.GetTargetLabel = GetTargetLabel;
            _overlay.GetGoalDebugText = GetGoalDebugText;
            _overlay.CompleteAction = CompleteCurrentQuestStep;

            if (LogStartupLines)
                Log.LogInfo("QuestRecoveryOverlay initialized.");
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

        private static bool CanCompleteNow()
        {
            if (!UiState.IsQuestWindowOpen) return false;
            if (!QuestRecoveryTargetState.HasTarget) return false;

            if (!string.Equals(QuestRecoveryTargetState.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
                return false;

            // Don’t allow completing when there are choices (branching / selection pending).
            if (!QuestRecoveryTargetState.IsPendingChoicesEmpty())
                return false;

            return true;
        }

        private static string GetTargetLabel()
        {
            if (!QuestRecoveryTargetState.HasTarget)
                return "No target yet.\nOpen the Quest window and wait a moment.";

            return QuestRecoveryTargetState.TargetLabel ?? $"QuestIndex={QuestRecoveryTargetState.QuestIndex}";
        }

        private static string GetGoalDebugText()
        {
            return QuestRecoveryTargetState.GoalDebugText ?? "";
        }

        private static void CompleteCurrentQuestStep()
        {
            if (!QuestRecoveryTargetState.HasTarget)
            {
                Log.LogWarning("[QuestRecovery] Complete requested but no target is available.");
                return;
            }

            int questIndex = QuestRecoveryTargetState.QuestIndex;

            // Minimal line #1 (on click): “Complete -> NextQuest …”
            TryLogNextQuestName(questIndex);

            bool ok = InternalAccess.CompleteQuestStepAndFinalize(questIndex);
            if (!ok)
                Log.LogWarning("[QuestRecovery] CompleteQuestStepAndFinalize failed.");
        }

        private static void TryLogNextQuestName(int questIndex)
        {
            try
            {
                object questController = InternalAccess.GetQuestController();
                if (questController == null) return;

                IList quests = PatchHelper.ReadAsIList(questController, "Quests");
                if (quests == null) return;

                object questObj = PatchHelper.FindQuestByIndex(quests, questIndex);
                if (questObj == null) return;

                int stepIndex = PatchHelper.ReadInt(questObj, "StepIndex", -1);
                if (stepIndex < 0) return;

                object choiceDef = PatchHelper.ReadObj(questObj, "QuestChoiceDefinition");
                object stepsObj = PatchHelper.ReadObj(choiceDef, "QuestSteps");

                IList steps = stepsObj as IList;
                if (steps == null) return;

                if (stepIndex >= steps.Count) return;

                object stepObj = steps[stepIndex];
                object nextQuestObj = PatchHelper.ReadObj(stepObj, "NextQuest");

                string nextQuestName = PatchHelper.SafeToString(
                    PatchHelper.ReadObj(nextQuestObj, "ElementName"));

                if (string.IsNullOrWhiteSpace(nextQuestName))
                    nextQuestName = "(none)";

                Log.LogWarning(
                    $"[QuestRecovery] Complete -> NextQuest '{nextQuestName}' (questIndex={questIndex}, stepIndex={stepIndex})");
            }
            catch (Exception ex)
            {
                // Keep errors visible; they matter.
                Log.LogError(ex);
            }
        }
    }
}