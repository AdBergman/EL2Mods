using System;

namespace EL2.QuestRecovery.UI
{
    internal static class QuestRecoveryTargetState
    {
        internal static volatile bool HasTarget;
        internal static volatile int QuestIndex = -1;
        internal static volatile string TargetLabel;

        // Debug text shown in overlay (real goal / prereq info)
        internal static volatile string GoalDebugText;

        internal static volatile string CurrentSignature;
        internal static volatile string LastAppliedSignature;

        // Optional: timestamp to support a failsafe unlock later
        internal static DateTime LastAppliedAtUtc;

        internal static void Clear()
        {
            HasTarget = false;
            QuestIndex = -1;
            TargetLabel = null;
            GoalDebugText = null;
            CurrentSignature = null;
            // NOTE: LastAppliedSignature intentionally preserved
        }

        // ─────────────────────────────────────────────────────────────
        // Explicit setters (no ambiguous "Set")
        // ─────────────────────────────────────────────────────────────

        internal static void SetWithLabel(int questIndex, string label)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;
        }

        internal static void SetWithDebugText(int questIndex, string label, string goalDebugText)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;
            GoalDebugText = goalDebugText;
        }

        internal static void SetWithSignature(int questIndex, string label, string signature)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;
            CurrentSignature = signature;
        }

        // Explicit “all fields” variant (useful in QuestSnapshotPatch)
        internal static void SetFull(
            int questIndex,
            string label,
            string signature,
            string goalDebugText)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;
            CurrentSignature = signature;
            GoalDebugText = goalDebugText;
        }

        // ─────────────────────────────────────────────────────────────

        internal static bool IsLocked()
        {
            return HasTarget
                   && !string.IsNullOrWhiteSpace(CurrentSignature)
                   && CurrentSignature == LastAppliedSignature;
        }

        // Overlay calls this after user clicks Recover
        internal static void MarkApplied()
        {
            LastAppliedSignature = CurrentSignature;
            LastAppliedAtUtc = DateTime.UtcNow;
        }
    }
}
