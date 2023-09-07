using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using static UnityEngine.Object;

namespace Extensions;

public static class RendererExtension
{
    private static readonly CoroutineHandler coroutineHandler;
    private static List<Renderer> flashingRenderers = new();

    static RendererExtension()
    {
        coroutineHandler = new GameObject("CoroutineHandler").AddComponent<CoroutineHandler>();
        Object.DontDestroyOnLoad(coroutineHandler);
    }

    public static async void Flash(this Renderer renderer, Color color, Color returnColor, float time = 0.3f,
        Action callback = null)
    {
        if (flashingRenderers.Contains(renderer)) return;
        await HighlightObject(renderer, color, returnColor, time);
        callback?.Invoke();
    }

    private static async Task HighlightObject(Renderer obj, Color color, Color returnColor, float time)
    {
        flashingRenderers.Add(obj);
        var renderersInChildren = obj.GetComponentsInChildren<Renderer>();
        Material heightmapMaterial = null;
        if (obj.name == "Terrain")
        {
            var heightmap = obj.GetComponent<Heightmap>();
            heightmapMaterial = Instantiate(new Material(heightmap.m_meshRenderer.material));
            heightmap.m_meshRenderer.material.shader = Shader.Find("Standard");
        }

        foreach (var renderer in renderersInChildren)
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", color * 0.7f);
                material.color = color;
            }
        }

        await Task.Delay((int)TimeSpan.FromSeconds(time).TotalMilliseconds);
        if (obj.name == "Terrain" && heightmapMaterial) obj.material = heightmapMaterial;
        //else
        foreach (var renderer in renderersInChildren)
        foreach (var material in renderer.materials)
        {
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", returnColor * 0f);
            material.color = returnColor;
        }

        flashingRenderers.Remove(obj);
    }

    private class CoroutineHandler : MonoBehaviour
    {
    }
}