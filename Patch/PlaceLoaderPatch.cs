using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ChunkLoader;

[HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
public class PlaceLoaderPatch
{
    [HarmonyPostfix]
    static void Postfix(Piece __instance)
    {
        if (!__instance.name.StartsWith("ChunkLoader_stone")) return;
        __instance.StartCoroutine(WaiteForPlace(__instance));
    }

    private static IEnumerator WaiteForPlace(Piece piece)
    {
        yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

        Plugin.ForceActive.Add(ZoneSystem.instance.GetZone(piece.transform.position));
        Plugin.ForceActiveBuffer.Add(ZoneSystem.instance.GetZone(piece.transform.position));
        SetForceActive();
        if (piece.IsCreator()) Plugin.loadersOnLocalPlayer++;
    }

    private static string GetCountOfLoaders()
    {
        ZoneSystem.instance.GetGlobalKey("ChunkLoadersCount", out string countKey);
        return countKey;
    }

    private static void SetForceActive()
    {
        ZoneSystem.instance.SetGlobalKey(string.Format("{0} {1}", "ForceActive",
            SaveForceActive().ToString((IFormatProvider)CultureInfo.InvariantCulture)));
    }

    public static string SaveForceActive()
    {
        return string.Join("|", Plugin.ForceActive.Select(s => $"{s.x},{s.y}"));
    }
}