using System.Collections.Generic;
using ChainedPuzzles;
using HarmonyLib;
using Player;
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
        int actualCount = __instance.m_sync.GetCurrentState().playersInScan;
        int listCount = playersInScan?.Count ?? 0;

        L.Verbose($"OnSyncStateChange: status={status}, puzzleIndex={__instance.m_puzzleIndex}");
        L.Verbose($"  actualCount={actualCount}, listCount={listCount}, playersMax={playersMax}");

        if (status != eBioscanStatus.Scanning)
            return;

        // Fix travel scan movement when actual count exceeds list count
        // The original method used listCount for the flag3 check, but we have
        // the real count from sync state
        if (actualCount > listCount && __instance.IsMovable)
        {
            var movingComp = __instance.m_movingComp;
            if (movingComp != null && movingComp.OnlyMoveWhenScannig)
            {
                bool requireAll = __instance.m_playerScanner.ScanPlayersRequired.RequireAllPlayers();
                bool requireSolo = __instance.m_playerScanner.ScanPlayersRequired.RequireSoloPlayer();
                bool noRequirement = __instance.m_playerScanner.ScanPlayersRequired == PlayerRequirement.None;

                // Recalculate flag1 using actual count instead of list count
                bool shouldMove = noRequirement
                    || (requireAll && actualCount == playersMax)
                    || (requireSolo && actualCount == 1);

                if (shouldMove)
                {
                    L.Verbose($"Resuming movement: actualCount={actualCount}, playersMax={playersMax}");
                    movingComp.ResumeMovement();
                }
            }
        }

        // HUD fix: only needed when actual count exceeds list count
        if (actualCount <= listCount)
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
                var scanner = __instance.m_PlayerScannerComp.TryCast<CP_PlayerScanner>();
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
            actualCount,
            playersMax,
            localPlayerInScan,
            __instance.m_playerScanner.ScanPlayersRequired,
            __instance.m_playerScanner.ReduceWhenNoPlayer);
    }
}
