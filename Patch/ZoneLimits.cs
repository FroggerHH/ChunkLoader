namespace ChunkLoader;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.InActiveArea), typeof(Vector2i), typeof(Vector2i))]
public class InActiveArea
{
    private static bool Prefix(Vector2i zone, Vector2i refCenterZone, ref bool __result)
    {
        if (ForceActive.Contains(zone))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OutsideActiveArea), typeof(Vector3), typeof(Vector3))]
public class OutsideActiveArea
{
    private static bool Prefix(Vector3 point, Vector3 refPoint, ref bool __result)
    {
        var zone2 = instance.GetZone(point);
        if (ForceActive.Contains(zone2))
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
    private static void Postfix(ZDOMan __instance, Vector2i sector, int area, List<ZDO> sectorObjects)
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
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.FindDistantObjects))]
public class FindDistantObjects
{
    private static bool Prefix(Vector2i sector) { return !ForceActive.Contains(sector); }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones))]
public class CreateLocalZones
{
    private static void Postfix(ZoneSystem __instance, ref bool __result)
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
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.IsActiveAreaLoaded))]
public class IsActiveAreaLoaded
{
    private static void Postfix(ZoneSystem __instance, ref bool __result)
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