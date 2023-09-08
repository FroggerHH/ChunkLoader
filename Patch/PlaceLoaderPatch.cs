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
        if (!__instance.name.Contains("ChunkLoader_stone")) return;
        __instance.StartCoroutine(WaiteForPlace(__instance));
    }

    private static IEnumerator WaiteForPlace(Piece piece)
    {
        yield return new WaitWhile(() => !piece.IsPlacedByPlayer() && !piece.m_nview.m_ghost);

        Plugin.ForceActive.Add(ZoneSystem.instance.GetZone(piece.transform.position));
        Plugin.ForceActiveBuffer.Add(ZoneSystem.instance.GetZone(piece.transform.position));
        SetForceActive();
        if (piece.IsCreator()) Plugin.currentLoaders++;
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