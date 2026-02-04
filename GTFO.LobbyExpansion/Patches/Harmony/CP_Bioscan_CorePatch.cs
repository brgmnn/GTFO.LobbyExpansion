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
    [HarmonyPrefix]
    public static void OnSyncStateChange__Prefix(
        CP_Bioscan_Core __instance,
        ref List<PlayerAgent> playersInScan)
    {
        // Get the actual player count from sync state
        var actualCount = __instance.m_sync.GetCurrentState().playersInScan;

        // If actual count exceeds list size, pad the list with nulls
        // This ensures count == actualCount for movement checks
        if (actualCount > playersInScan.Count)
        {
            L.Verbose($"Padding playersInScan list from {playersInScan.Count} to {actualCount}");
            while (playersInScan.Count < actualCount)
                playersInScan.Add(null);
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
