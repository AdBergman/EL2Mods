using System;

namespace EL2.QuestRecovery.UI
{
    internal static class QuestRecoveryTargetState
    {
        // Core target state
        internal static volatile bool HasTarget;
        internal static volatile int QuestIndex = -1;
        internal static volatile string TargetLabel;

        // Debug text shown in overlay (details panel)
        internal static volatile string GoalDebugText;

        // Used by overlay to detect target changes + lock after clicking Complete
        internal static volatile string CurrentSignature;
        internal static volatile string LastAppliedSignature;

        // Used by plugin gating + logging
        internal static volatile string Status;
        internal static volatile string PendingChoicesInfo;

        // Optional timestamp (handy for future failsafe/unlock logic)
        internal static DateTime LastAppliedAtUtc;

        internal static void Clear()
        {
            HasTarget = false;
            QuestIndex = -1;
            TargetLabel = null;

            GoalDebugText = null;

            CurrentSignature = null;

            Status = null;
            PendingChoicesInfo = null;

            // NOTE:
            // - LastAppliedSignature intentionally preserved (so lock can survive transient refreshes)
            // - LastAppliedAtUtc intentionally preserved
        }

        /// <summary>
        /// The ONLY setter we keep: everything the UI + plugin needs in one call.
        /// </summary>
        internal static void SetFull(
            int questIndex,
            string label,
            string signature,
            string goalDebugText,
            string status,
            string pendingChoicesInfo)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;

            CurrentSignature = signature;
            GoalDebugText = goalDebugText;

            Status = status;
            PendingChoicesInfo = pendingChoicesInfo;
        }

        /// <summary>
        /// Overlay considers the action "locked" if the current target signature matches the last applied.
        /// This prevents spamming Complete on the exact same quest step.
        /// </summary>
        internal static bool IsLocked()
        {
            return HasTarget
                   && !string.IsNullOrWhiteSpace(CurrentSignature)
                   && CurrentSignature == LastAppliedSignature;
        }

        internal static bool IsPendingChoicesEmpty()
        {
            // PatchHelper.GetArrayInfo often returns strings like:
            // "null" or "len=0" or "SomeType(len=0)" or "len=3"
            if (string.IsNullOrWhiteSpace(PendingChoicesInfo))
                return true;

            string s = PendingChoicesInfo.Trim();

            if (s.IndexOf("len=0", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (s.Equals("null", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Called by overlay after user clicks Complete. Records the current signature so IsLocked() becomes true.
        /// </summary>
        internal static void MarkApplied()
        {
            LastAppliedSignature = CurrentSignature;
            LastAppliedAtUtc = DateTime.UtcNow;
        }
    }
}