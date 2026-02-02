using ChainedPuzzles;
using HarmonyLib;

namespace GTFO.LobbyExpansion.Patches.Harmony;

[HarmonyPatch(typeof(CP_Bioscan_Hud))]
public static class CP_Bioscan_HudPatch
{
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
}
