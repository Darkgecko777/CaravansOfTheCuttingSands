using UnityEditor;
using UnityEngine;

public class FrontDoorImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/FrontDoor/") || !assetPath.EndsWith(".png"))
            return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = assetPath.EndsWith("title_screen.png") ? 2048 : 1024;
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.EndsWith("FrontDoor/wind_sound.wav"))
            return;
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.4f;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = true;
    }

    [InitializeOnLoadMethod]
    static void FixIfImportedEarly()
    {
        EditorApplication.delayCall += () =>
        {
            FixTexture("Assets/Resources/FrontDoor/title_screen.png", 2048);
            FixTexture("Assets/Resources/FrontDoor/menu_frame.png", 1024);
            FixTexture("Assets/Resources/FrontDoor/button_panel.png", 1024);
            FixTexture("Assets/Resources/FrontDoor/plate_quiet.png", 1024);
            FixTexture("Assets/Resources/FrontDoor/plate_hot.png", 1024);
            FixTexture("Assets/Resources/FrontDoor/plate_pressed.png", 1024);
            FixWind("Assets/Resources/FrontDoor/wind_sound.wav");
        };
    }

    static void FixTexture(string path, int size)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        if (importer.textureType == TextureImporterType.Default
            && !importer.mipmapEnabled
            && importer.isReadable
            && importer.maxTextureSize >= size
            && importer.textureCompression == TextureImporterCompression.Uncompressed)
            return;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = size;
        importer.SaveAndReimport();
    }

    static void FixWind(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
            return;
        var settings = importer.defaultSampleSettings;
        if (settings.loadType == AudioClipLoadType.Streaming
            && settings.compressionFormat == AudioCompressionFormat.Vorbis
            && importer.forceToMono)
            return;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.4f;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = true;
        importer.SaveAndReimport();
    }
}
