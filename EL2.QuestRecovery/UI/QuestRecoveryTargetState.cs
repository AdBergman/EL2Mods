using System;

namespace EL2.QuestRecovery.UI
{
    internal static class QuestRecoveryTargetState
    {
        internal static volatile bool HasTarget;
        internal static volatile int QuestIndex = -1;
        internal static volatile string TargetLabel;
        
        internal static volatile string CurrentSignature;
        
        internal static volatile string LastAppliedSignature;

        // Optional: timestamp to support a failsafe unlock later (we can ignore for now)
        internal static DateTime LastAppliedAtUtc;

        internal static void Clear()
        {
            HasTarget = false;
            QuestIndex = -1;
            TargetLabel = null;
            CurrentSignature = null;
            // NOTE: we intentionally do NOT clear LastAppliedSignature here.
        }
        
        internal static void Set(int questIndex, string label)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;
            // CurrentSignature remains whatever the patch sets later.
        }
        
        internal static void Set(int questIndex, string label, string signature)
        {
            HasTarget = true;
            QuestIndex = questIndex;
            TargetLabel = label;
            CurrentSignature = signature;
        }
        
        internal static bool IsLocked()
        {
            return HasTarget
                   && !string.IsNullOrWhiteSpace(CurrentSignature)
                   && CurrentSignature == LastAppliedSignature;
        }

        // NEW: overlay will call this after user clicks Recover
        internal static void MarkApplied()
        {
            LastAppliedSignature = CurrentSignature;
            LastAppliedAtUtc = DateTime.UtcNow;
        }
    }
}
