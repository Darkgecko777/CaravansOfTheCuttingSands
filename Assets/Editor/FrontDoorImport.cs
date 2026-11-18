using UnityEditor;
using UnityEngine;

public class FrontDoorImport : AssetPostprocessor
{
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
        EditorApplication.delayCall += () => FixWind("Assets/Resources/FrontDoor/wind_sound.wav");
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
