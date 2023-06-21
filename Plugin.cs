using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using PieceManager;
using ServerSync;
using UnityEngine;

namespace ChunkLoader;

[BepInPlugin(ModGUID, ModName, ModVersion)]
internal class Plugin : BaseUnityPlugin
{
    #region values

    internal const string ModName = "ChunkLoader", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
    internal static Harmony harmony = new(ModGUID);

    internal static Plugin _self;
    public static HashSet<Vector2i> ForceActive = new();

    #endregion

    #region tools

    public static void Debug(object msg)
    {
        _self.Logger.LogInfo(msg);
    }

    public void DebugError(object msg, bool showWriteToDev)
    {
        if (showWriteToDev)
        {
            msg += "Write to the developer and moderator if this happens often.";
        }

        Logger.LogError(msg);
    }

    public void DebugWarning(object msg, bool showWriteToDev)
    {
        if (showWriteToDev)
        {
            msg += "Write to the developer and moderator if this happens often.";
        }

        Logger.LogWarning(msg);
    }

    #endregion

    #region ConfigSettings

    static string ConfigFileName = "com.Frogger.DiscordWard.cfg";
    DateTime LastConfigChange;

    public static readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    private static ConfigEntry<Toggle> serverConfigLocked = null!;

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
    {
        setter(config.Value);
        config.SettingChanged += (_, _) => setter(config.Value);
    }

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    #endregion

    #region Config

    private void SetupWatcher()
    {
        FileSystemWatcher fileSystemWatcher = new(Paths.ConfigPath, ConfigFileName);
        fileSystemWatcher.Changed += ConfigChanged;
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
        {
            return;
        }

        LastConfigChange = DateTime.Now;
        try
        {
            Config.Reload();
        }
        catch
        {
            DebugError("Can't reload Config", true);
        }
    }

    #endregion


    private void Awake()
    {
        _self = this;

        harmony.PatchAll();

        BuildPiece piece = new("ChunkLoader".ToLower(), "ChunkLoader_stone");
        piece.Category.Add(BuildPieceCategory.Misc);
        piece.SnapshotPiece(piece.Prefab);
        piece.Crafting.Set(CraftingTable.Forge);
        piece.RequiredItems.Add("Stone", 25, true);
        piece.RequiredItems.Add("Thunderstone", 5, true);
        piece.RequiredItems.Add("SurtlingCore", 1, true);
        piece.Name
            .Russian("Колонна наблюдения")
            .English("Observation column");
        piece.Description
            .Russian("Территория вокруг колонны всегда активна")
            .English("The area around the column is always active");
    }
}