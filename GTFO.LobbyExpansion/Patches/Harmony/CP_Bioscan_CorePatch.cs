using System.Collections.Generic;
using ChainedPuzzles;
using HarmonyLib;
using Player;
using UnityEngine;

namespace GTFO.LobbyExpansion.Patches.Harmony;

[HarmonyPatch(typeof(CP_Bioscan_Core))]
public static class CP_Bioscan_CorePatch
{
    [HarmonyPatch("OnSyncStateChange")]
    [HarmonyPostfix]
    public static void OnSyncStateChange__Postfix(
        CP_Bioscan_Core __instance,
        eBioscanStatus status,
        List<PlayerAgent> playersInScan,
        int playersMax)
    {
        if (status != eBioscanStatus.Scanning)
            return;

        int actualCount = __instance.m_sync.GetCurrentState().playersInScan;
        int listCount = playersInScan?.Count ?? 0;

        if (actualCount <= listCount)
            return;

        L.LogExecutingMethod();

        // Preserve what the original method already computed (with proper Unity null checks)
        bool localPlayerInScan = __instance.m_hud.m_localPlayerInScan;

        // Fallback: if local player wasn't in the truncated 4-slot list, check by distance
        if (!localPlayerInScan)
        {
            try
            {
                if (PlayerManager.TryGetLocalPlayerAgent(out var localPlayer) && localPlayer.Alive)
                {
                    var scanner = __instance.m_PlayerScannerComp.TryCast<CP_PlayerScanner>();
                    if (scanner != null)
                    {
                        float radiusSqr = scanner.m_scanRadiusSqr;
                        float distSqr = (localPlayer.Position - scanner.transform.position).sqrMagnitude;
                        localPlayerInScan = distSqr < radiusSqr;
                    }
                }
            }
            catch { /* Preserve original value if fallback fails */ }
        }

        __instance.m_hud.SetPlayerData(
            __instance.m_puzzleIndex,
            actualCount,
            playersMax,
            localPlayerInScan,
            __instance.m_playerScanner.ScanPlayersRequired,
            __instance.m_playerScanner.ReduceWhenNoPlayer);
    }
}
