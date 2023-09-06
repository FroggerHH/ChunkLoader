using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using Extensions.Valheim;
using ServerSync;
using UnityEngine;

namespace Extensions.Valheim;

public sealed class ModBase
{
    #region values

    public static string ModName, ModAuthor, ModVersion, ModGUID;
    private Harmony harmony;
    public static ModBase mod;
    private static BaseUnityPlugin plugin;

    #endregion

    public static ModBase Create(BaseUnityPlugin plugin, string modName, string modAuthor, string modVersion)
    {
        if (mod)
            throw new($"Trying to create new mod {modName}, but {ModName} already exists");

        return new(plugin, modName, modAuthor, modVersion);
    }

    private void Init()
    {
        Debug("BaseMod loaded");
    }

    #region Debug

    public static void Debug(object msg) { plugin.Logger.LogInfo(msg); }

    public static void DebugError(object msg, bool showWriteToDev = true)
    {
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";
        plugin.Logger.LogError(msg);
    }

    public static void DebugWarning(object msg, bool showWriteToDev = true)
    {
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";
        plugin.Logger.LogWarning(msg);
    }

    #endregion

    private ModBase(BaseUnityPlugin plugin,string modName, string modAuthor, string modVersion)
    {
        ModBase.plugin = plugin;
        ModBase.mod = this;
        ModName = modName;
        ModAuthor = modAuthor;
        ModVersion = modVersion;
        ModGUID = "com." + ModAuthor + '.' + ModName;
        harmony = new(ModGUID);
    }

    public static implicit operator bool(ModBase modBase) => modBase != null;
}