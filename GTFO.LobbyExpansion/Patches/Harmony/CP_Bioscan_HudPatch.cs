using System.Collections.Generic;
using ChainedPuzzles;
using HarmonyLib;

namespace GTFO.LobbyExpansion.Patches.Harmony;

[HarmonyPatch(typeof(CP_Bioscan_Hud))]
public static class CP_Bioscan_HudPatch
{
    // Cache Core reference for each HUD (set during Core.Setup)
    internal static readonly Dictionary<CP_Bioscan_Hud, CP_Bioscan_Core> HudToCoreMap = new();

    [HarmonyPatch(nameof(CP_Bioscan_Hud.SetPlayerData))]
    [HarmonyPostfix]
    public static void SetPlayerData__Postfix(CP_Bioscan_Hud __instance)
    {
        L.LogExecutingMethod();

        if (__instance.m_progressBarPlayerChar.Length >= PluginConfig.MaxPlayers)
            return;

        L.Verbose($"Expanding {nameof(__instance.m_progressBarPlayerChar)} from {__instance.m_progressBarPlayerChar.Length} to {PluginConfig.MaxPlayers}.");

        var expanded = new string[PluginConfig.MaxPlayers];
        for (var i = 0; i < PluginConfig.MaxPlayers; i++)
            expanded[i] = ((char)('A' + i)).ToString();

        __instance.m_progressBarPlayerChar = expanded;
    }

    [HarmonyPatch(nameof(CP_Bioscan_Hud.Update))]
    [HarmonyPrefix]
    public static void Update__Prefix(CP_Bioscan_Hud __instance)
    {
        if (!HudToCoreMap.TryGetValue(__instance, out var core))
            return;

        var state = core.m_sync.GetCurrentState();
        if (state.status != eBioscanStatus.Scanning)
            return;

        // Override with authoritative count from sync state
        __instance.m_playersInScan = state.playersInScan;
        __instance.m_playersMax = state.playersMax;
    }
}
