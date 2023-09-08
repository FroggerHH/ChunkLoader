using System;
using System.Linq;
using Extensions.Valheim;
using HarmonyLib;
using static MessageHud.MessageType;
using static ChunkLoader.Plugin;
using static Player;
using static Player.PlacementStatus;

namespace ChunkLoader;

[HarmonyPatch]
public class LimitByPlayerPatch
{
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))] [HarmonyPrefix] [HarmonyWrapSafe]
    private static bool PatchAdd(Player __instance, Piece piece, ref bool __result)
    {
        if (Player.m_debugMode ||
            __instance != m_localPlayer ||
            !piece.name.StartsWith("ChunkLoader_stone")) return true;

        if (ForceActive.Contains(ZoneSystem.instance.GetZone(m_localPlayer.m_placementGhost.transform.position)))
        {
            __result = false;
            Message("$chunkLoaderAlreadyPlacedInArea");
            m_localPlayer.m_placementStatus = Invalid;
            return false;
        }

        if (currentLoaders >= chunkLoadersLimitByPlayer.Value)
        {
            __result = false;
            Message("$youHaveTooManyChunkLoadersPlaced");
            m_localPlayer.m_placementStatus = Invalid;
            return false;
        }


        return true;
    }

    private static void Message(string msg) => m_localPlayer.Message(Center, msg);

    [HarmonyPatch(typeof(Piece), nameof(Piece.OnDestroy))] [HarmonyPostfix]
    private static void PatchRemove(Piece __instance)
    {
        if (!__instance.name.Contains("ChunkLoader_stone")) return;
        currentLoaders--;
        currentLoaders = Math.Max(0, currentLoaders);
        ForceActive.Remove(ZoneSystem.instance.GetZone(__instance.transform.position));
        ForceActiveBuffer.Remove(ZoneSystem.instance.GetZone(__instance.transform.position));
    }
}