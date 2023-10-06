using System.Globalization;

namespace ChunkLoader;

[HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
public class PlaceLoaderPatch
{
    [HarmonyPostfix]
    private static void Postfix(Piece __instance)
    {
        if (!__instance.name.Contains("ChunkLoader_stone")) return;
        __instance.StartCoroutine(WaiteForPlace(__instance));
    }

    private static IEnumerator WaiteForPlace(Piece piece)
    {
        yield return new WaitWhile(() => !piece.IsPlacedByPlayer() && !piece.m_nview.m_ghost);

        ForceActive.Add(instance.GetZone(piece.transform.position));
        ForceActiveBuffer.Add(instance.GetZone(piece.transform.position));
        SetForceActive();
        if (piece.IsCreator()) currentLoaders++;
    }

    private static void SetForceActive()
    {
        instance.SetGlobalKey(string.Format("{0} {1}", "ForceActive",
            SaveForceActive().ToString(CultureInfo.InvariantCulture)));
    }

    public static string SaveForceActive() { return string.Join("|", ForceActive.Select(s => $"{s.x},{s.y}")); }
}