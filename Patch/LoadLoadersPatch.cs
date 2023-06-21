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
        ParseForceActive(key);
        Game.instance.StartCoroutine(FilterForceActive());
    }

    private static IEnumerator FilterForceActive()
    {
        yield return new WaitForSeconds(25);

        if (Plugin.ForceActiveBuffer.Count > 0)
        {
            Plugin.ForceActive.Clear();
            Plugin.ForceActive = Plugin.ForceActiveBuffer;
        }

        HashSet<Vector2i> ForceActive = new();

        foreach (var i in Plugin.ForceActive)
        {
            if (!ForceActive.Contains(i)) ForceActive.Add(i);
        }

        Plugin.ForceActive = ForceActive;

        Game.instance.StartCoroutine(FilterForceActive());
    }

    private static void ParseForceActive(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
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