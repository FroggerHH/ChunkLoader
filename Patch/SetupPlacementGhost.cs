namespace ChunkLoader;

[HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
public class SetupPlacementGhost
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (!__instance || !__instance.m_placementGhost) return;
        foreach (var child in __instance.m_placementGhost.GetComponentsInChildren<ChunkLoaderMono>())
            Destroy(child);
    }
}