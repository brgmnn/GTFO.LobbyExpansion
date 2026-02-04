using System.Collections.Generic;
using ChainedPuzzles;
using HarmonyLib;

namespace GTFO.LobbyExpansion.Patches.Harmony;

[HarmonyPatch(typeof(CP_Bioscan_Hud))]
public static class CP_Bioscan_HudPatch
{
    // Map HUD instance ID to Core (stable int key instead of object reference)
    internal static readonly Dictionary<int, CP_Bioscan_Core> HudToCoreMap = new();

    [HarmonyPatch(nameof(CP_Bioscan_Hud.SetPlayerData))]
    [HarmonyPostfix]
    public static void SetPlayerData__Postfix(CP_Bioscan_Hud __instance)
    {
        if (__instance.m_progressBarPlayerChar.Length >= PluginConfig.MaxPlayers)
            return;

        L.Verbose($"Expanding {nameof(__instance.m_progressBarPlayerChar)} from {__instance.m_progressBarPlayerChar.Length} to {PluginConfig.MaxPlayers}.");

        var expanded = new string[PluginConfig.MaxPlayers];
        for (var i = 0; i < PluginConfig.MaxPlayers; i++)
            expanded[i] = PluginConfig.GetBioscanLetter(i);

        __instance.m_progressBarPlayerChar = expanded;
    }

    [HarmonyPatch(nameof(CP_Bioscan_Hud.OnDestroy))]
    [HarmonyPostfix]
    public static void OnDestroy__Postfix(CP_Bioscan_Hud __instance)
    {
        HudToCoreMap.Remove(__instance.GetInstanceID());
    }

    [HarmonyPatch("Update")]  // Use string for private method
    [HarmonyPrefix]
    public static void Update__Prefix(CP_Bioscan_Hud __instance)
    {
        if (!HudToCoreMap.TryGetValue(__instance.GetInstanceID(), out var core))
            return;

        // Check core is still valid (Il2Cpp object could be destroyed)
        if (core == null || core.WasCollected)
            return;

        var state = core.m_sync.GetCurrentState();

        // Only update during active scan states (not Disabled/Finished/TimedOut)
        if (state.status == eBioscanStatus.Disabled ||
            state.status == eBioscanStatus.Finished ||
            state.status == eBioscanStatus.TimedOut)
            return;

        // Override with authoritative count from sync state
        __instance.m_playersInScan = state.playersInScan;
        __instance.m_playersMax = state.playersMax;
    }
}
