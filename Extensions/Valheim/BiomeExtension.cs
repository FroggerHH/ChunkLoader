using System.Linq;

namespace Extensions.Valheim;

public static class BiomeExtension
{
    public static string GetLocalizationKey(this Heightmap.Biome biome) => "$biome_" + biome.ToString().ToLower();
}