using UnityEditor;
using UnityEngine;

public class FrontDoorImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool door = assetPath.StartsWith("Assets/Resources/FrontDoor/") && assetPath.EndsWith(".png");
        bool ui = assetPath.StartsWith("Assets/Resources/Ui/") && assetPath.EndsWith(".png");
        if (!door && !ui)
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
        bool wide = assetPath.EndsWith("title_screen.png") || assetPath.EndsWith("settlement-desert.png");
        importer.maxTextureSize = wide || ui ? 2048 : 1024;
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
            FixTexture("Assets/Resources/Ui/parchment.png", 2048);
            FixTexture("Assets/Resources/Ui/parchment-plain.png", 2048);
            FixTexture("Assets/Resources/Ui/duzhu-hide-panel.png", 2048);
            FixTexture("Assets/Resources/FrontDoor/settlement-desert.png", 2048);
            FixTexture("Assets/Resources/Ui/banner.png", 2048);
            FixTexture("Assets/Resources/Ui/shimmersteel-rod.png", 2048);
            FixTexture("Assets/Resources/Ui/sinew-wrap.png", 2048);
            FixTexture("Assets/Resources/Ui/bone-frame.png", 2048);
            FixTexture("Assets/Resources/Ui/hide-header.png", 2048);
            FixTexture("Assets/Resources/Ui/resin-seal.png", 2048);
            FixTexture("Assets/Resources/Ui/resin-seal-hover.png", 2048);
            FixTexture("Assets/Resources/Ui/resin-seal-selected.png", 2048);
            FixTexture("Assets/Resources/Ui/resin-seal-gate.png", 2048);
            FixTexture("Assets/Resources/Ui/resin-seal-gate-hover.png", 2048);
            FixTexture("Assets/Resources/Ui/resin-seal-gate-selected.png", 2048);
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
