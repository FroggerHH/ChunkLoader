namespace ChunkLoader;

[HarmonyPatch]
public class LimitByPlayerPatch
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    private static void Patch(Player __instance)
    {
        if (!Player.m_localPlayer || Player.m_localPlayer != __instance) return;
        if (__instance.m_placementGhost?.GetPrefabName() != "ChunkLoader_stone") return;
        var piece = __instance.m_placementGhost?.GetComponent<Piece>();
        if (!piece) return;

        var zdos = ZDOMan.instance.GetImportantZDOs(prefabHash);
        if (zdos.Count >= chunkLoadersLimitByPlayer.Value)
        {
            __instance.m_placementStatus = Player.PlacementStatus.Invalid;
            __instance.SetPlacementGhostValid(false);
            Message("$youHaveTooManyChunkLoadersPlaced");
        } else if (zdos.Any(x => x.GetPosition().GetZone() == m_localPlayer.transform.position.GetZone()))
        {
            __instance.m_placementStatus = Player.PlacementStatus.Invalid;
            __instance.SetPlacementGhostValid(false);
            Message("$chunkLoaderAlreadyPlacedInArea");
        }

        void Message(string msg) { m_localPlayer.Message(Center, msg); }
    }
}