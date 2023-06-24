using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ChunkLoader;

[HarmonyPatch]
public class LimitByPlayerPatch
{
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode)),
     HarmonyPostfix]
    static void PatchAdd(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        if (Player.m_debugMode)
            return;

        if (__result == false) return;
        if (Plugin.loadersOnLocalPlayer < Plugin.chunkLoadersLimitByPlayer.Value) return;
        __result = false;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece)), HarmonyPostfix]
    static void PatchRemove(Player __instance, bool __result)
    {
        if (__result == false) return;
        RaycastHit hitInfo;
        if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out hitInfo,
                50f, __instance.m_removeRayMask) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) <
            __instance.m_maxPlaceDistance)
        {
            Piece piece = hitInfo.collider.GetComponentInParent<Piece>();
            if (!piece) return;
            if (!piece.name.StartsWith("ChunkLoader_stone")) return;
            Plugin.loadersOnLocalPlayer--;
        }
    }
}