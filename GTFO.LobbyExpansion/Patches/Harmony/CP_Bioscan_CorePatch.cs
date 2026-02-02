using ChainedPuzzles;
using HarmonyLib;
using Player;

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

        __instance.m_hud.SetPlayerData(
            __instance.m_puzzleIndex,
            actualCount,
            playersMax,
            localPlayerInScan,
            __instance.m_playerScanner.ScanPlayersRequired,
            __instance.m_playerScanner.ReduceWhenNoPlayer);
    }
}
