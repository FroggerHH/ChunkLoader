using Extensions.Valheim;
using HarmonyLib;

namespace TotemsOfUndying.Patch;

[HarmonyPatch]
public class UpdateConfigOnGameStart
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void UpdateConfig() => ModBase.plugin?.Config.Reload();
}