using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

namespace Extensions.Valheim;

public sealed class ModBase
{
    private ModBase(string modName, string modAuthor, string modVersion)
    {
        ModName = modName;
        ModAuthor = modAuthor;
        ModVersion = modVersion;
        ModGUID = "com." + ModAuthor + '.' + ModName;
        harmony = new Harmony(ModGUID);
        ConfigFileName = $"com.{ModAuthor}.{ModName}.cfg";
    }

    public static ModBase CreateMod(BaseUnityPlugin plugin, string modName, string modAuthor, string modVersion)
    {
        if (mod)
            throw new Exception($"Trying to create new mod {modName}, but {ModName} already exists");

        mod = new ModBase(modName, modAuthor, modVersion);
        ModBase.plugin = plugin;
        mod.Init();
        return mod;
    }

    private void Init()
    {
        plugin.Config.SaveOnConfigSet = false;
        SetupWatcher();
        plugin.Config.ConfigReloaded += (_, _) => UpdateConfiguration();
        plugin.Config.SaveOnConfigSet = true;
        plugin.Config.Save();

        serverConfigLocked = config("General", "ServerConfigLock", Toggle.Off, "");
        configSync.AddLockingConfigEntry(serverConfigLocked);

        harmony.PatchAll();
    }

    public static implicit operator bool(ModBase modBase) { return modBase != null; }

    #region values

    public static string ModName, ModAuthor, ModVersion, ModGUID;
    private readonly Harmony harmony;
    public static ModBase mod;
    public static BaseUnityPlugin plugin;
    public Action OnConfigurationChanged;

    #endregion

    #region Debug

    public static void Debug(object msg)
    {
        plugin.Logger.LogInfo(msg);
        if (Console.IsVisible()) Console.instance.AddString($"[{ModName}] {msg}");
    }

    public static void DebugError(object msg, bool showWriteToDev = true)
    {
        if (Console.IsVisible()) Console.instance.AddString($"<color=red>[{ModName}] {msg}</color>");
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";
        plugin.Logger.LogError(msg);
    }

    public static void DebugWarning(object msg, bool showWriteToDev = true)
    {
        if (Console.IsVisible()) Console.instance.AddString($"<color=yellow>[{ModName}] {msg}</color>");
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";
        plugin.Logger.LogWarning(msg);
    }

    #endregion


    #region ConfigSettings

    #region Core

    private static string ConfigFileName = "-1";
    private DateTime LastConfigChange;

    public readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    private static ConfigEntry<Toggle> serverConfigLocked = null!;

    public ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        var configEntry = plugin.Config.Bind(group, name, value, description);

        var syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    public ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    #endregion

    #region Updating

    private void SetupWatcher()
    {
        FileSystemWatcher fileSystemWatcher = new(Paths.ConfigPath, ConfigFileName);
        fileSystemWatcher.Changed += ConfigChanged;
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 2) return;
        LastConfigChange = DateTime.Now;

        try
        {
            plugin.Config.Reload();
        }
        catch
        {
            DebugError("Unable reload config");
        }
    }

    private void UpdateConfiguration()
    {
        try
        {
            OnConfigurationChanged?.Invoke();

            var sucsessMsg = "Configuration Received";
            Debug(sucsessMsg);
        }
        catch (Exception e)
        {
            object errorMsg = $"Configuration error: {e.Message}";
            DebugError(errorMsg, false);
        }
    }

    #endregion

    #endregion
}