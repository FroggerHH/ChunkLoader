namespace ChunkLoader;

[HarmonyPatch]
public class LimitByPlayerPatch
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))] [HarmonyPostfix]
    private static void PatchUpdate(Player __instance) { TheCheck(__instance, false); }


    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))] [HarmonyPrefix]
    private static void PatchPlace(Player __instance) { TheCheck(__instance, true); }

    private static void TheCheck(Player __instance, bool showMessage)
    {
        if (!m_localPlayer || m_localPlayer != __instance) return;
        if (m_debugMode) return;
        if (__instance.m_placementGhost?.GetPrefabName() != "ChunkLoader_stone") return;
        var piece = __instance.m_placementGhost?.GetComponent<Piece>();
        if (!piece) return;

        if (ForceActive.Contains(m_localPlayer.transform.position.GetZone()))
        {
            __instance.m_placementStatus = Invalid;
            __instance.SetPlacementGhostValid(false);
            if (showMessage) Message("$chunkLoaderAlreadyPlacedInArea");
            return;
        }

        var zdos = loadersZDOs.Where(x => x.GetLong(ZDOVars.s_creator) == m_localPlayer.GetPlayerID()).ToList();
        if (zdos.Count >= chunkLoadersLimitByPlayer.Value)
        {
            __instance.m_placementStatus = Invalid;
            __instance.SetPlacementGhostValid(false);
            if (showMessage) Message("$youHaveTooManyChunkLoadersPlaced");
        }

        void Message(string msg) { m_localPlayer.Message(Center, msg); }
    }
}