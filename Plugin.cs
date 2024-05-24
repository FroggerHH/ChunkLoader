using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using LocalizationManager;
using ChunkLoader.PieceManager;
using static ChunkLoader.ChunkLoaderMono;

namespace ChunkLoader;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("com.Frogger.NoUselessWarnings", DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    internal const string ModName = "ChunkLoader",
        ModVersion = "1.6.0",
        ModGUID = $"com.{ModAuthor}.{ModName}",
        ModAuthor = "Frogger";

    public static HashSet<Vector2i> ForceActive = new();
    public static List<ZDO> loadersZDOs = new();
    public static readonly string prefab = "ChunkLoader_stone";

    internal static ConfigEntry<int> chunkLoadersLimitByPlayer;
    internal static ConfigEntry<int> maxFuelConfig;
    internal static ConfigEntry<int> startFuelConfig;
    internal static ConfigEntry<string> fuelItemConfig;
    internal static ConfigEntry<int> minutesForOneFuelItemConfig;
    internal static ConfigEntry<bool> infiniteFuelConfig;
    internal static ConfigEntry<Color> terrainFlashColorConfig;

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGUID, true);
        OnConfigurationChanged += UpdateConfiguration;

        chunkLoadersLimitByPlayer = config("Main", "ChunkLoaders limit by player", 2, "");
        terrainFlashColorConfig = config("Main", "Terrain flash color", Color.yellow, "");
        maxFuelConfig = config("Fuelling", "Max fuel", 100, "");
        startFuelConfig = config("Fuelling", "Start fuel", 1, "");
        fuelItemConfig = config("Fuelling", "Fuel item", "Thunderstone", "");
        minutesForOneFuelItemConfig = config("Fuelling", "Minutes for one fuel item", 5, "");
        infiniteFuelConfig = config("Fuelling", "Infinite fuel", false, "");

        Localizer.Load();
        InvokeRepeating(nameof(UpdateForceActive), 5, 3);
        StartCoroutine(WaiteForLoad());
    }

    private IEnumerator WaiteForLoad()
    {
        yield return new WaitUntil(() => Chainloader._loaded);
        AddPiece();
    }

    internal static void AddPiece()
    {
        Debug($"Adding piece {prefab}");
        var piece = new BuildPiece("chunkloader", "ChunkLoader_stone");
        piece.Prefab.AddComponent<ChunkLoaderMono>();
        piece.Category.Set(BuildPieceCategory.Misc);
        piece.Crafting.Set(CraftingTable.Forge);
        piece.RequiredItems.Add("Stone", 25, true);
        piece.RequiredItems.Add("Thunderstone", 5, true);
        piece.RequiredItems.Add("SurtlingCore", 1, true);
        piece.Name
            .English("Observation column")
            .Swedish("Observationskolumn")
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
    }

    private async Task UpdateForceActive()
    {
        try
        {
            if (ZDOMan.instance == null) return;
            loadersZDOs = await ZoneSystem.instance.GetWorldObjectsAsync(prefab);
            ForceActive.Clear();
            foreach (var zdo in loadersZDOs)
                if (zdo.GetBool(burningZDOKey))
                    ForceActive.Add(zdo.GetPosition().GetZone());

            // Debug($"ForceActive: {ForceActive.GetString()}");
        }
        catch (Exception e)
        {
            DebugError($"Failed to update force active: {e.Message}");
        }
    }


    private static void UpdateConfiguration()
    {
        var objectDB = ObjectDB.instance;
        if (objectDB)
        {
            var item = objectDB.GetItem(fuelItemConfig.Value);
            if (item) m_fuelItem = item;
            else
            {
                m_fuelItem = objectDB.GetItem("Thunderstone");
                DebugWarning($"Item '{fuelItemConfig.Value}' not found. Using default 'Thunderstone'.");
            }
        }

        m_infiniteFuel = infiniteFuelConfig.Value;
        m_maxFuel = maxFuelConfig.Value;
        m_startFuel = startFuelConfig.Value;
        minutesForOneFuelItem = minutesForOneFuelItemConfig.Value;
        flashColor = terrainFlashColorConfig.Value;
    }
}