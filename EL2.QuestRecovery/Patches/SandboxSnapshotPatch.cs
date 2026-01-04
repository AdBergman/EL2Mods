using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using EL2.QuestRecovery.Safety;

namespace EL2.QuestRecovery.Patches
{
    [HarmonyPatch(typeof(Amplitude.Mercury.Interop.SandboxSnapshot), "Synchronize")]
    internal static class SandboxSnapshotPatch
    {
        private static void Postfix(object simulationData)
        {
            try
            {
                if (simulationData == null)
                {
                    SafetyState.SetUnknown();
                    return;
                }

                int remoteLocalCount = PatchHelper.ReadInt(simulationData, "RemoteSandboxLocalDataCount", 0);
                int remoteReplicatedCount = PatchHelper.ReadInt(simulationData, "RemoteSandboxReplicatedDataCount", 0);

                object gameServerId = PatchHelper.ReadObj(simulationData, "GameServerIdentifier");
                object netSyncStatus = PatchHelper.ReadObj(simulationData, "NetworkSynchronizationStatus");

                // Fast path: no remotes => SP
                if (remoteLocalCount == 0 && remoteReplicatedCount == 0)
                {
                    MultiplayerDetector.UpdateFromSandboxSnapshot(
                        remoteLocalCount, remoteReplicatedCount, gameServerId, netSyncStatus,
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    );
                    return;
                }

                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Prefer replicated ids (network peers)
                AddIdsFromList(
                    ids,
                    PatchHelper.ReadObj(simulationData, "RemoteSandboxReplicatedData") as IList,
                    remoteReplicatedCount,
                    "NetworkIdentifier"
                );

                // Local ids can be kept as a backup signal (optional)
                AddIdsFromList(
                    ids,
                    PatchHelper.ReadObj(simulationData, "RemoteSandboxLocalData") as IList,
                    remoteLocalCount,
                    "Identifier"
                );

                MultiplayerDetector.UpdateFromSandboxSnapshot(
                    remoteLocalCount,
                    remoteReplicatedCount,
                    gameServerId,
                    netSyncStatus,
                    ids
                );
            }
            catch (Exception ex)
            {
                QuestRecoveryPlugin.Log.LogError(ex);
                SafetyState.SetUnknown();
            }
        }

        private static void AddIdsFromList(HashSet<string> ids, IList list, int count, string fieldName)
        {
            if (list == null || count <= 0) return;

            int max = Math.Min(count, list.Count);
            for (int i = 0; i < max; i++)
            {
                object entry = list[i];
                object idObj = PatchHelper.ReadObj(entry, fieldName);
                string s = SafeToString(idObj);
                if (!string.IsNullOrWhiteSpace(s)) ids.Add(s);
            }
        }

        private static string SafeToString(object v)
        {
            try { return v?.ToString() ?? ""; }
            catch { return ""; }
        }
    }
}
