using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using EL2.QuestRecovery.UI;

namespace EL2.QuestRecovery
{
    [BepInPlugin("com.calmbreakfast.el2.questrecovery", "EL2 Quest Recovery", "0.0.1")]
    public class QuestRecoveryPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private Harmony _harmony;

        private QuestRecoveryOverlay _overlay;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo("EL2 Quest Recovery loaded.");

            _harmony = new Harmony("com.calmbreakfast.el2.questrecovery");
            _harmony.PatchAll();
            Logger.LogInfo("EL2 Quest Recovery Harmony patched.");

            // ✅ Create the overlay as a Unity component on the same GameObject as the plugin
            _overlay = gameObject.AddComponent<QuestRecoveryOverlay>();
            _overlay.InitLogger(Logger);

            // ✅ Wire up overlay -> your mod state/actions
            _overlay.CanSkip = CanSkipNow;
            _overlay.GetTargetLabel = GetTargetLabel;
            _overlay.SkipAction = SkipCurrentQuestStep;

            Logger.LogInfo("QuestRecoveryOverlay initialized.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();

            // Not strictly required, but tidy
            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }
        }

        // -----------------------------
        // Wiring stubs (replace later)
        // -----------------------------

        private bool CanSkipNow()
        {
            // Only allow if the quest window is open AND we have a target AND no pending choices.
            if (!UiState.IsQuestWindowOpen) return false;
            if (!QuestRecoveryTargetState.HasTarget) return false;

            // Optional: if you include PendingChoices in the label only, skip this.
            // Better: store pendingChoicesInfo as a field later.
            // For now, keep it simple and allow when target exists.
            return true;
        }

        private string GetTargetLabel()
        {
            if (!QuestRecoveryTargetState.HasTarget)
                return "No target yet.\nOpen the Quest window and wait a moment.";

            return QuestRecoveryTargetState.TargetLabel ?? $"QuestIndex={QuestRecoveryTargetState.QuestIndex}";
        }

        private void SkipCurrentQuestStep()
        {
            if (!QuestRecoveryTargetState.HasTarget)
            {
                Log.LogWarning("[QuestRecovery] Skip requested but no target is available.");
                return;
            }

            int questIndex = QuestRecoveryTargetState.QuestIndex;

            Log.LogWarning($"[QuestRecovery] User confirmed: CompleteQuestStep questIndex={questIndex}");
            bool ok = InternalAccess.CompleteQuestStep(questIndex);

            if (!ok)
                Log.LogWarning("[QuestRecovery] CompleteQuestStep failed.");
        }

    }
}
