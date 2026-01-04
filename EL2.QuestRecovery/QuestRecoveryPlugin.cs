using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
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
            // TODO: return true only when you have a valid target quest in memory
            // For now, allow it whenever the quest window is open.
            return UiState.IsQuestWindowOpen;
        }

        private string GetTargetLabel()
        {
            // TODO: return a nice label from the quest you detected:
            // e.g. "Main Quest: Chapter 2 – Step 3"
            return "Target: (wire from QuestSnapshotPatch)";
        }

        private void SkipCurrentQuestStep()
        {
            // TODO: call your already-working completion logic here.
            // Example (you will replace with your real call):
            Log.LogWarning("[QuestRecovery] SkipCurrentQuestStep invoked (TODO: wire real target)");

            // InternalAccess.CompleteQuestStep(targetQuestSimulationIndex);
        }
    }
}
