using System;
using System.Collections.Generic;

namespace EL2.QuestRecovery.Safety
{
    internal static class MultiplayerDetector
    {
        private static bool _initialized;
        private static SafetyMode _lastMode;

        internal static void UpdateFromSandboxSnapshot(
            int remoteLocalCount,
            int remoteReplicatedCount,
            object gameServerIdentifier,
            object networkSyncStatus,
            HashSet<string> remoteIds)
        {
            string serverId = SafeToString(gameServerIdentifier);

            if (remoteIds == null)
            {
                remoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            remoteIds.RemoveWhere(IsDefaultIdentifierString);

            // Remove the local server id (SP often reports it as the only "remote")
            if (!IsDefaultIdentifierString(serverId))
                remoteIds.Remove(serverId);

            SafetyMode mode = remoteIds.Count > 0 ? SafetyMode.Multiplayer : SafetyMode.SinglePlayer;

            if (!_initialized || mode != _lastMode)
            {
                _initialized = true;
                _lastMode = mode;

                QuestRecoveryPlugin.Log.LogInfo(
                    $"[Safety] {mode} (sandbox snapshot: remoteLocal={remoteLocalCount}, remoteReplicated={remoteReplicatedCount}, " +
                    $"serverId={serverId}, netSync={SafeToString(networkSyncStatus)}, remoteIds={FormatIds(remoteIds)})"
                );
            }

            if (mode == SafetyMode.Multiplayer) SafetyState.SetMultiplayer();
            else SafetyState.SetSinglePlayer();
        }

        private static bool IsDefaultIdentifierString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return true;

            string t = s.Trim();

            if (t.Equals("0", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.Equals("0x0", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.Equals("0x0000000000000000", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.Equals("00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase)) return true;

            // all-zero hex: 0x000...
            if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 2; i < t.Length; i++)
                    if (t[i] != '0') return false;
                return true;
            }

            // "undefined"/"none"
            if (t.IndexOf("undefined", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (t.IndexOf("none", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private static string FormatIds(HashSet<string> ids)
            => (ids == null || ids.Count == 0) ? "[]" : "[" + string.Join(", ", ids) + "]";

        private static string SafeToString(object v)
        {
            try { return v?.ToString() ?? ""; }
            catch { return ""; }
        }
    }
}
