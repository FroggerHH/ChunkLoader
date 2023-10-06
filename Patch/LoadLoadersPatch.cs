namespace ChunkLoader;

[HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
public class LoadLoadersPatch
{
    [HarmonyPostfix]
    private static void Postfix(Game __instance)
    {
        if (!__instance.m_firstSpawn) return;

        LoadForceActive();
    }

    private static void LoadForceActive()
    {
        instance.GetGlobalKey("ForceActive", out var key);
        ParseForceActive(key);
        Game.instance.StartCoroutine(FilterForceActive());
    }

    private static IEnumerator FilterForceActive()
    {
        yield return new WaitForSeconds(25);

        if (ForceActiveBuffer.Count > 0)
        {
            Plugin.ForceActive.Clear();
            Plugin.ForceActive = ForceActiveBuffer;
        }

        HashSet<Vector2i> ForceActive = new();

        foreach (var i in Plugin.ForceActive)
            if (!ForceActive.Contains(i))
                ForceActive.Add(i);

        Plugin.ForceActive = ForceActive;

        Game.instance.StartCoroutine(FilterForceActive());
    }

    private static void ParseForceActive(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        ForceActive = value.Split('|').Select(s => s.Trim()).Select(s => s.Split(',')).Where(s => s.Length == 2)
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