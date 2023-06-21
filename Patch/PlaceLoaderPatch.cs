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
    public static bool initingLoaders = false;
    public static int initingLoadersCount = 0;

    [HarmonyPostfix]
    static void Postfix(Piece __instance)
    {
        if (initingLoaders) return;
        __instance.StartCoroutine(WaiteForPlace(__instance));
    }

    private static IEnumerator WaiteForPlace(Piece piece)
    {
        yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

        Plugin.ForceActive.Add(ZoneSystem.instance.GetZone(piece.transform.position));
        var countKey = GetCountOfLoaders();
        if (!int.TryParse(countKey, out var count)) count = 0;
        ZoneSystem.instance.SetGlobalKey(string.Format("{0} {1}", "ChunkLoadersCount",
            (count + 1).ToString((IFormatProvider)CultureInfo.InvariantCulture)));
        initingLoadersCount++;
        SetForceActive();
    }

    private static string GetCountOfLoaders()
    {
        ZoneSystem.instance.GetGlobalKey("ChunkLoadersCount", out string countKey);
        return countKey;
    }

    private static void SetForceActive()
    {
        if (initingLoaders) return;
        ZoneSystem.instance.SetGlobalKey(string.Format("{0} {1}", "ForceActive",
            SaveForceActive().ToString((IFormatProvider)CultureInfo.InvariantCulture)));
    }

    public static string SaveForceActive()
    {
        return string.Join("|", Plugin.ForceActive.Select(s => $"{s.x},{s.y}"));
    }
}