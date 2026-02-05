using System.Collections.Generic;
using ChainedPuzzles;
using HarmonyLib;
using Player;
using SNetwork;
using UnityEngine;

namespace GTFO.LobbyExpansion.Patches.Harmony;

[HarmonyPatch(typeof(CP_Bioscan_Core))]
public static class CP_Bioscan_CorePatch
{
    [HarmonyPatch(nameof(CP_Bioscan_Core.Setup))]
    [HarmonyPostfix]
    public static void Setup__Postfix(CP_Bioscan_Core __instance)
    {
        // Use m_HUDComp (public Component) instead of m_hud (private interface)
        var hud = __instance.m_HUDComp?.TryCast<CP_Bioscan_Hud>();
        if (hud != null)
        {
            CP_Bioscan_HudPatch.HudToCoreMap[hud.GetInstanceID()] = __instance;
            L.Verbose($"Registered HUD mapping: {hud.GetInstanceID()} -> Core");
        }
    }

    [HarmonyPatch("OnSyncStateChange")]
    [HarmonyPostfix]
    public static void OnSyncStateChange__Postfix(
        CP_Bioscan_Core __instance,
        eBioscanStatus status,
        List<PlayerAgent> playersInScan,
        int playersMax)
    {
        // Get counts for logging/HUD fix
        int syncCount = __instance.m_sync.GetCurrentState().playersInScan;
        int listCount = playersInScan?.Count ?? 0;

        L.Verbose($"OnSyncStateChange: status={status}, puzzleIndex={__instance.m_puzzleIndex}");
        L.Verbose($"  syncCount={syncCount}, listCount={listCount}, playersMax={playersMax}");

        if (status != eBioscanStatus.Scanning)
            return;

        // Fix travel scan movement - only on master (clients receive synced state)
        if (SNet.IsMaster && __instance.IsMovable)
        {
            var movingComp = __instance.m_movingComp;
            if (movingComp != null && movingComp.OnlyMoveWhenScannig)
            {
                // Calculate true player count by checking positions ourselves
                // Don't trust sync state - it may be stale due to movement feedback loop
                int actualCount = CountPlayersInScan(__instance);

                bool requireAll = __instance.m_playerScanner.ScanPlayersRequired.RequireAllPlayers();
                bool requireSolo = __instance.m_playerScanner.ScanPlayersRequired.RequireSoloPlayer();
                bool noRequirement = __instance.m_playerScanner.ScanPlayersRequired == PlayerRequirement.None;

                bool shouldMove = noRequirement
                    || (requireAll && actualCount == playersMax)
                    || (requireSolo && actualCount == 1);

                if (shouldMove)
                {
                    L.Verbose($"Resuming movement: actualCount={actualCount}, playersMax={playersMax}");
                    movingComp.ResumeMovement();
                }
                else
                {
                    L.Verbose($"Pausing movement: actualCount={actualCount}, playersMax={playersMax}");
                    movingComp.PauseMovement();
                }
            }
        }

        // HUD fix: only needed when sync count exceeds list count
        if (syncCount <= listCount)
            return;

        L.LogExecutingMethod();

        bool localPlayerInScan = false;
        if (__instance.enabled && playersInScan != null)
        {
            for (int i = 0; i < listCount; i++)
            {
                if (playersInScan[i] != null && playersInScan[i].IsLocallyOwned)
                {
                    localPlayerInScan = true;
                    break;
                }
            }
        }

        if (!localPlayerInScan)
        {
            if (PlayerManager.TryGetLocalPlayerAgent(out var localPlayer) && localPlayer.Alive)
            {
                var scanner = __instance.m_PlayerScannerComp?.TryCast<CP_PlayerScanner>();
                if (scanner != null)
                {
                    float radius = scanner.Radius;
                    float distSqr = (localPlayer.Position - __instance.transform.position).sqrMagnitude;
                    localPlayerInScan = distSqr < radius * radius;
                }
            }
        }

        __instance.m_hud.SetPlayerData(
            __instance.m_puzzleIndex,
            syncCount,
            playersMax,
            localPlayerInScan,
            __instance.m_playerScanner.ScanPlayersRequired,
            __instance.m_playerScanner.ReduceWhenNoPlayer);
    }

    private static int CountPlayersInScan(CP_Bioscan_Core core)
    {
        var scanner = core.m_PlayerScannerComp?.TryCast<CP_PlayerScanner>();
        if (scanner == null)
            return 0;

        float radiusSqr = scanner.Radius * scanner.Radius;
        Vector3 scanPos = core.transform.position;
        int count = 0;

        var players = PlayerManager.PlayerAgentsInLevel;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player != null && player.Alive)
            {
                float distSqr = (player.Position - scanPos).sqrMagnitude;
                if (distSqr < radiusSqr)
                    count++;
            }
        }

        return count;
    }
}
