using UnityEditor;

// The jars-run map is a painted atlas. Keep it sharp enough that the road
// the caravan follows is the road on the picture.
public class MapTextureSetup : AssetPostprocessor
{
    const string Path = "Assets/Resources/Map/upscale_map.png";

    void OnPreprocessTexture()
    {
        if (assetPath != Path)
            return;
        Apply((TextureImporter)assetImporter);
    }

    [InitializeOnLoadMethod]
    static void FixIfImportedEarly()
    {
        EditorApplication.delayCall += () =>
        {
            var importer = AssetImporter.GetAtPath(Path) as TextureImporter;
            if (importer == null)
                return;
            if (importer.textureType == TextureImporterType.Default
                && !importer.mipmapEnabled
                && importer.maxTextureSize >= 4096
                && importer.textureCompression == TextureImporterCompression.Uncompressed)
                return;
            Apply(importer);
            importer.SaveAndReimport();
        };
    }

    static void Apply(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
    }
}
