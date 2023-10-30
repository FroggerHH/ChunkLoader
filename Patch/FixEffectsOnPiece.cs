namespace ChunkLoader;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
public class FixEffectsOnPiece
{
    [HarmonyPostfix]
    private static void Postfix(ZNetScene __instance)
    {
        var chunkLoader = __instance.GetPrefab("ChunkLoader_stone").GetComponent<WearNTear>();
        var wood_floor = __instance.GetPrefab("wood_floor").GetComponent<WearNTear>();

        chunkLoader.GetComponent<Piece>().m_placeEffect = wood_floor.GetComponent<Piece>().m_placeEffect;
        chunkLoader.m_destroyedEffect = wood_floor.m_destroyedEffect;
        chunkLoader.m_hitEffect = wood_floor.m_hitEffect;
    }
}