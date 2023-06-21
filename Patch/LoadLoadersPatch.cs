using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ChunkLoader;

[HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
public class LoadLoadersPatch
{
    [HarmonyPostfix]
    static void Postfix(Game __instance)
    {
        if (!__instance.m_firstSpawn) return;

        LoadForceActive();
    }

    private static void LoadForceActive()
    {
        ZoneSystem.instance.GetGlobalKey("ForceActive", out string key);
        PlaceLoaderPatch.initingLoaders = true;
        ParseForceActive(key);
        Game.instance.StartCoroutine(WaiteForLoadAllLoaders());
    }

    private static IEnumerator WaiteForLoadAllLoaders()
    {
        ZoneSystem.instance.GetGlobalKey("ChunkLoadersCount", out string count);
        int count_ = int.Parse(count);
        yield return new WaitUntil(() =>  IsAllLoaderInited(count_));
        PlaceLoaderPatch.initingLoaders = false;
    }

    private static bool IsAllLoaderInited(int count)
    {
        return PlaceLoaderPatch.initingLoadersCount == count;
    }

    private static void ParseForceActive(string value)
    {
        if(string.IsNullOrEmpty(value)) return;
        Plugin.ForceActive = value.Split('|').Select(s => s.Trim()).Select(s => s.Split(',')).Where(s => s.Length == 2)
            .Select(
                s =>
                {
                    try
                    {
                        return new Vector2i(int.Parse(s[0]), int.Parse(s[1]));
                    }
                    catch
                    {
                        return new Vector2i();
                    }
                }).ToHashSet();
    }
}