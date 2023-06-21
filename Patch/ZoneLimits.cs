using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ChunkLoader;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.InActiveArea), new[] { typeof(Vector2i), typeof(Vector2i) })]
public class InActiveArea
{
    static bool Prefix(Vector2i zone, Vector2i refCenterZone, ref bool __result)
    {
        if (Plugin.ForceActive.Contains(zone))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OutsideActiveArea), new[] { typeof(Vector3), typeof(Vector3) })]
public class OutsideActiveArea
{
    static bool Prefix(Vector3 point, Vector3 refPoint, ref bool __result)
    {
        var zone2 = ZoneSystem.instance.GetZone(point);
        if (Plugin.ForceActive.Contains(zone2))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindSectorObjects))]
public class FindSectorObjects
{
    static void Postfix(ZDOMan __instance, Vector2i sector, int area, List<ZDO> sectorObjects)
    {
        if (Plugin.ForceActive.Count == 0) return;
        HashSet<Vector2i> added = new() { sector };
        for (int i = 1; i <= area; i++)
        {
            for (int j = sector.x - i; j <= sector.x + i; j++)
            {
                added.Add(new(j, sector.y - i));
                added.Add(new(j, sector.y + i));
            }

            for (int k = sector.y - i + 1; k <= sector.y + i - 1; k++)
            {
                added.Add(new(sector.x - i, k));
                added.Add(new(sector.x + i, k));
            }
        }

        foreach (var zone in Plugin.ForceActive)
        {
            if (added.Contains(zone)) continue;
            __instance.FindObjects(zone, sectorObjects);
        }
    }
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindDistantObjects))]
public class FindDistantObjects
{
    static bool Prefix(Vector2i sector)
    {
        return !Plugin.ForceActive.Contains(sector);
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones))]
public class CreateLocalZones
{
    static void Postfix(ZoneSystem __instance, ref bool __result)
    {
        if (Plugin.ForceActive.Count == 0) return;
        if (__result) return;
        foreach (var zone in Plugin.ForceActive)
        {
            if (__instance.PokeLocalZone(zone))
            {
                __result = true;
                break;
            }
        }
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.IsActiveAreaLoaded))]
public class IsActiveAreaLoaded
{
    static void Postfix(ZoneSystem __instance, ref bool __result)
    {
        if (Plugin.ForceActive.Count == 0) return;
        if (!__result) return;
        foreach (var zone in Plugin.ForceActive)
        {
            if (!__instance.m_zones.ContainsKey(zone))
            {
                __result = false;
                break;
            }
        }
    }
}