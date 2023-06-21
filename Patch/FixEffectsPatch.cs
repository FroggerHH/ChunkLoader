using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ChunkLoader;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
public class FixEffectsPatch
{
    [HarmonyPostfix]
    static void Postfix(ZNetScene __instance)
    {
        var chunkLoader = __instance.GetPrefab("ChunkLoader_stone");
        var guard_stone = __instance.GetPrefab("guard_stone");

        chunkLoader.GetComponent<Piece>().m_placeEffect = guard_stone.GetComponent<Piece>().m_placeEffect;
        chunkLoader.GetComponent<WearNTear>().m_destroyedEffect =
            guard_stone.GetComponent<WearNTear>().m_destroyedEffect;
        chunkLoader.GetComponent<WearNTear>().m_hitEffect = guard_stone.GetComponent<WearNTear>().m_hitEffect;
    }
}