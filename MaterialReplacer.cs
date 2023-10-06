using UnityEngine.Rendering;

namespace PieceManager;

[PublicAPI]
public static class MaterialReplacer
{
    public enum ShaderType
    {
        PieceShader,
        VegetationShader,
        RockShader,
        RugShader,
        GrassShader,
        CustomCreature,
        UseUnityShader
    }

    private static readonly Dictionary<GameObject, bool> ObjectToSwap;
    internal static readonly Dictionary<string, Material> OriginalMaterials;
    private static readonly Dictionary<GameObject, ShaderType> ObjectsForShaderReplace;

    private static bool hasRun;

    static MaterialReplacer()
    {
        OriginalMaterials = new Dictionary<string, Material>();
        ObjectToSwap = new Dictionary<GameObject, bool>();
        ObjectsForShaderReplace = new Dictionary<GameObject, ShaderType>();
        var harmony = new Harmony("org.bepinex.helpers.PieceManager");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZoneSystem), nameof(ZoneSystem.Start)),
            postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MaterialReplacer),
                nameof(ReplaceAllMaterialsWithOriginal))));
    }

    public static void RegisterGameObjectForShaderSwap(GameObject go, ShaderType type)
    {
        if (ObjectsForShaderReplace.ContainsKey(go)) return;
        ObjectsForShaderReplace.Add(go, type);
    }

    public static void RegisterGameObjectForMatSwap(GameObject go, bool isJotunnMock = false)
    {
        if (ObjectToSwap.ContainsKey(go)) return;
        ObjectToSwap.Add(go, isJotunnMock);
    }

    private static void GetAllMaterials()
    {
        var allMats = Resources.FindObjectsOfTypeAll<Material>();
        foreach (var item in allMats) OriginalMaterials[item.name] = item;
    }

    [HarmonyPriority(Priority.VeryHigh)]
    private static void ReplaceAllMaterialsWithOriginal()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        if (hasRun) return;
        if (OriginalMaterials.Count <= 0) GetAllMaterials();
        foreach (var renderer in ObjectToSwap.Keys.SelectMany(gameObject =>
                     gameObject.GetComponentsInChildren<Renderer>(true)))
        {
            ObjectToSwap.TryGetValue(renderer.gameObject, out var jotunnPrefabFlag);
            var newMats = new Material[renderer.sharedMaterials.Length];
            var i = 0;
            foreach (var t in renderer.sharedMaterials)
            {
                var replacementString = jotunnPrefabFlag ? "JVLmock_" : "_REPLACE_";
                if (!t.name.StartsWith(replacementString, StringComparison.Ordinal)) continue;
                var matName = t.name.Replace(" (Instance)", string.Empty)
                    .Replace(replacementString, "");

                if (OriginalMaterials.ContainsKey(matName))
                {
                    newMats[i] = OriginalMaterials[matName];
                } else
                {
                    DebugWarning("No suitable material found to replace: " + matName);
                    OriginalMaterials[matName] = newMats[i];
                }

                ++i;
            }

            renderer.materials = newMats;
            renderer.sharedMaterials = newMats;
        }

        foreach (var renderer in ObjectsForShaderReplace.Keys.SelectMany(gameObject =>
                     gameObject.GetComponentsInChildren<Renderer>(true)))
        {
            ObjectsForShaderReplace.TryGetValue(renderer.gameObject.transform.root.gameObject,
                out var shaderType);
            if (renderer == null) continue;
            foreach (var t in renderer.sharedMaterials)
            {
                if (t == null) continue;
                var name = t.shader.name;
                switch (shaderType)
                {
                    case ShaderType.PieceShader:
                        t.shader = Shader.Find("Custom/Piece");
                        break;
                    case ShaderType.VegetationShader:
                        t.shader = Shader.Find("Custom/Vegetation");
                        break;
                    case ShaderType.RockShader:
                        t.shader = Shader.Find("Custom/StaticRock");
                        break;
                    case ShaderType.RugShader:
                        t.shader = Shader.Find("Custom/Rug");
                        break;
                    case ShaderType.GrassShader:
                        t.shader = Shader.Find("Custom/Grass");
                        break;
                    case ShaderType.CustomCreature:
                        t.shader = Shader.Find("Custom/Creature");
                        break;
                    case ShaderType.UseUnityShader:
                        t.shader = Shader.Find(Shader.Find(name) != null ? name : "ToonDeferredShading2017");
                        break;
                    default:
                        t.shader = Shader.Find("ToonDeferredShading2017");
                        break;
                }
            }
        }

        hasRun = true;
    }
}