namespace ChunkLoader;

[HarmonyPatch]
public class ForceActivePatches
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.InActiveArea), typeof(Vector2i), typeof(Vector2i))]
    [HarmonyPostfix]
    private static void InActiveArea(Vector2i zone, ref bool __result)
    {
        if (__result == true) return;
        if (ForceActive.Contains(zone)) __result = true;
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OutsideActiveArea), typeof(Vector3), typeof(Vector3))]
    [HarmonyPostfix]
    private static void OutsideActiveArea(Vector3 point, ref bool __result)
    {
        if (__result == false) return;
        if (ForceActive.Contains(ZoneSystem.instance.GetZone(point))) __result = false;
    }

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindSectorObjects))]
    [HarmonyPostfix]
    private static void FindSectorObjects(ZDOMan __instance, Vector2i sector, int area, List<ZDO> sectorObjects)
    {
        if (ForceActive.Count == 0) return;
        HashSet<Vector2i> added = new() { sector };
        for (var i = 1; i <= area; i++)
        {
            for (var j = sector.x - i; j <= sector.x + i; j++)
            {
                added.Add(new Vector2i(j, sector.y - i));
                added.Add(new Vector2i(j, sector.y + i));
            }

            for (var k = sector.y - i + 1; k <= sector.y + i - 1; k++)
            {
                added.Add(new Vector2i(sector.x - i, k));
                added.Add(new Vector2i(sector.x + i, k));
            }
        }

        foreach (var zone in ForceActive)
        {
            if (added.Contains(zone)) continue;
            __instance.FindObjects(zone, sectorObjects);
        }
    }


    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindDistantObjects))]
    [HarmonyPrefix]
    private static bool FindDistantObjects(Vector2i sector) => !ForceActive.Contains(sector);


    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones))]
    [HarmonyPostfix]
    private static void CreateLocalZones(ZoneSystem __instance, ref bool __result)
    {
        if (ForceActive.Count == 0) return;
        if (__result) return;
        foreach (var zone in ForceActive)
            if (__instance.PokeLocalZone(zone))
            {
                __result = true;
                break;
            }
    }


    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.IsActiveAreaLoaded))]
    [HarmonyPostfix]
    private static void IsActiveAreaLoaded(ZoneSystem __instance, ref bool __result)
    {
        if (ForceActive.Count == 0) return;
        if (!__result) return;
        foreach (var zone in ForceActive)
            if (!__instance.m_zones.ContainsKey(zone))
            {
                __result = false;
                break;
            }
    }
}