using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using Extensions.Valheim;
using PieceManager;
using ServerSync;
using UnityEngine;

namespace ChunkLoader;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("com.Frogger.NoUselessWarnings", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    #region values

    internal const string ModName = "ChunkLoader", ModVersion = "1.0.2", ModGUID = "com.Frogger." + ModName;
    internal static Harmony harmony = new(ModGUID);

    internal static Plugin _self;
    public static HashSet<Vector2i> ForceActive = new();
    public static int loadersOnLocalPlayer = 0;
    public static HashSet<Vector2i> ForceActiveBuffer = new();

    #endregion

    #region tools

    public static void Debug(object msg) { _self.Logger.LogInfo(msg); }

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

    static string ConfigFileName = $"com.Frogger.{ModName}.cfg";
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

    internal void UpdateConfiguration()
    {
        try
        {
            ChunkLoaderMono.m_fuelItem =
                ObjectDB.instance.GetItem(fuelItemConfig.Value) ?? ObjectDB.instance.GetItem("Thunderstone");
            ChunkLoaderMono.m_infiniteFuel = infiniteFuelConfig.Value;
            ChunkLoaderMono.m_maxFuel = maxFuelConfig.Value;
            ChunkLoaderMono.m_startFuel = startFuelConfig.Value;
            ChunkLoaderMono.minutesForOneFuelItem = minutesForOneFuelItemConfig.Value;

            Debug("Configuration Received");
        }
        catch (Exception e)
        {
            DebugError($"Configuration error: {e.Message}", false);
        }
    }

    #endregion

    internal static ConfigEntry<int> chunkLoadersLimitByPlayer;
    internal static ConfigEntry<int> maxFuelConfig;
    internal static ConfigEntry<int> startFuelConfig;
    internal static ConfigEntry<string> fuelItemConfig;
    internal static ConfigEntry<int> minutesForOneFuelItemConfig;
    internal static ConfigEntry<bool> infiniteFuelConfig;

    private void Awake()
    {
        _self = this;
        Config.SaveOnConfigSet = false;
        SetupWatcher();
        Config.ConfigReloaded += (_, _) => UpdateConfiguration();
        Config.SaveOnConfigSet = true;
        Config.Save();

        chunkLoadersLimitByPlayer = config("Main", "ChunkLoaders limit by player", 2, "");
        maxFuelConfig = config("Fuelling", "Max fuel", 100, "");
        startFuelConfig = config("Fuelling", "Start fuel", 1, "");
        fuelItemConfig = config("Fuelling", "Fuel item", "Thunderstone", "");
        minutesForOneFuelItemConfig = config("Fuelling", "Minutes for one fuel item", 5, "");
        infiniteFuelConfig = config("Fuelling", "Infinite fuel", false, "");

        harmony.PatchAll();

        #region Piece

        var piece = new BuildPiece("chunkloader", "ChunkLoader_stone");
        piece.Prefab.AddComponent<ChunkLoaderMono>();
        piece.Category.Add(BuildPieceCategory.Misc);
        piece.Crafting.Set(CraftingTable.Forge);
        piece.RequiredItems.Add("Stone", 25, true);
        piece.RequiredItems.Add("Thunderstone", 5, true);
        piece.RequiredItems.Add("SurtlingCore", 1, true);
        piece.SpecialProperties.NoConfig = false;
        piece.Name
            .English("observation column")
            .Swedish("observationskolumn")
            .French("colonne d'observation")
            .Italian("colonna di osservazione")
            .German("Beobachtungssäule")
            .Spanish("columna de observación")
            .Russian("Колонна наблюдения")
            .Romanian("coloana de observare")
            .Bulgarian("колона за наблюдение")
            .Macedonian("колона за набљудување")
            .Finnish("havaintokolonni")
            .Danish("observationskolonne")
            .Norwegian("observasjonskolonne")
            .Icelandic("athugunardálkur")
            .Turkish("gözlem sütunu")
            .Lithuanian("stebėjimo kolona")
            .Czech("pozorovací sloup")
            .Hungarian("megfigyelő oszlop")
            .Slovak("pozorovací stĺp")
            .Polish("kolumna obserwacyjna")
            .Dutch("observatie kolom")
            .Chinese("观察柱")
            .Japanese("観察コラム")
            .Korean("관측 칼럼")
            .Hindi("अवलोकन स्तंभ")
            .Thai("คอลัมน์สังเกตการณ์")
            .Croatian("promatrački stup")
            .Georgian("დაკვირვების სვეტი")
            .Greek("στήλη παρατήρησης")
            .Serbian("колона за посматрање")
            .Ukrainian("Колона спостереження");
        piece.Description
            .English("The area around the column is always active")
            .Swedish("Området runt kolumnen är alltid aktivt")
            .French("La zone autour de la colonne est toujours active")
            .Italian("L'area attorno alla colonna è sempre attiva")
            .German("Der Bereich um die Säule herum ist immer aktiv")
            .Spanish("El área alrededor de la columna siempre está activa.")
            .Russian("Территория вокруг колонны всегда активна")
            .Romanian("Zona din jurul coloanei este întotdeauna activă")
            .Bulgarian("Зоната около колоната винаги е активна")
            .Macedonian("Областа околу колоната е секогаш активна")
            .Finnish("Sarakkeen ympärillä oleva alue on aina aktiivinen")
            .Danish("Området omkring søjlen er altid aktivt")
            .Norwegian("Området rundt kolonnen er alltid aktivt")
            .Icelandic("Svæðið í kringum súluna er alltaf virkt")
            .Turkish("Sütunun etrafındaki alan her zaman aktiftir")
            .Lithuanian("Teritorija aplink koloną visada aktyvi")
            .Czech("Oblast kolem sloupce je vždy aktivní")
            .Hungarian("Az oszlop körüli terület mindig aktív")
            .Slovak("Oblasť okolo stĺpca je vždy aktívna")
            .Polish("Obszar wokół kolumny jest zawsze aktywny")
            .Dutch("Het gebied rond de kolom is altijd actief")
            .Chinese("柱周围的区域始终处于活动状态")
            .Japanese("列の周囲の領域は常にアクティブです")
            .Korean("기둥 주변 영역은 항상 활성화되어 있습니다.")
            .Hindi("स्तम्भ के आसपास का क्षेत्र सदैव सक्रिय रहता है")
            .Thai("พื้นที่รอบคอลัมน์ใช้งานอยู่เสมอ")
            .Croatian("Područje oko stupca uvijek je aktivno")
            .Georgian("სვეტის მიმდებარე ტერიტორია ყოველთვის აქტიურია")
            .Greek("Η περιοχή γύρω από τη στήλη είναι πάντα ενεργή")
            .Serbian("Подручје око колоне је увек активно")
            .Ukrainian("Територія навколо колони завжди активна");

        #endregion

        //LocalizationManager.Localizer.Load();
    }
}