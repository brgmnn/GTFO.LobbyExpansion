using ChainedPuzzles;
using HarmonyLib;

namespace GTFO.LobbyExpansion.Patches.Harmony;

[HarmonyPatch(typeof(CP_Cluster_Hud))]
public static class CP_Cluster_HudPatch
{
    [HarmonyPatch(nameof(CP_Cluster_Hud.SetPlayerData))]
    [HarmonyPostfix]
    public static void SetPlayerData__Postfix(
        CP_Cluster_Hud __instance,
        int puzzleIndex,
        int playersInScan,
        int playersMax)
    {
        L.Verbose($"CP_Cluster_Hud.SetPlayerData: puzzleIndex={puzzleIndex}, playersInScan={playersInScan}, playersMax={playersMax}");
        L.Verbose($"  Stored values: m_playersInScan[{puzzleIndex}]={__instance.m_playersInScan[puzzleIndex]}");

        // Ensure wrapped HUD has expanded character array
        var wrappedHud = __instance.m_bioscanHUDComp?.TryCast<CP_Bioscan_Hud>();
        if (wrappedHud != null && wrappedHud.m_progressBarPlayerChar.Length < PluginConfig.MaxPlayers)
        {
            var expanded = new string[PluginConfig.MaxPlayers];
            for (var i = 0; i < PluginConfig.MaxPlayers; i++)
                expanded[i] = PluginConfig.GetBioscanLetter(i);
            wrappedHud.m_progressBarPlayerChar = expanded;
            L.Verbose($"  Expanded wrapped HUD char array to {PluginConfig.MaxPlayers}");
        }
    }
}
