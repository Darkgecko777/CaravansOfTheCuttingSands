using System;
using System.IO;
using System.Reflection;
using Caravans.FrontDoor;
using Caravans.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One still of the travel sandbox. It does not walk the full screenshot set.
public static class TravelDemoShot
{
    const string Key = "Caravans.TravelDemoShot";
    const string PhaseKey = "Caravans.TravelDemoShot.Phase";
    const string PrevKey = "Caravans.TravelDemoShot.PrevSize";
    const string PrevOk = "Caravans.TravelDemoShot.PrevOk";
    const string SteamOffVariable = "CARAVANS_EDITOR_NO_STEAM";
    const string BackgroundKey = "Caravans.TravelDemoShot.Background";
    const string BackgroundOk = "Caravans.TravelDemoShot.BackgroundOk";
    const string FileName = "60_travel_demo.png";
    const int Width = 1920;
    const int Height = 1080;

    static bool hooked;
    static double phaseStart;
    static string pendingFile;
    static long pendingLength;
    static int pendingStable;

    [InitializeOnLoadMethod]
    static void Boot()
    {
        if (SessionState.GetBool(Key, false))
            Hook();
    }

    [MenuItem("Caravans/Capture Travel Demo")]
    public static void Capture()
    {
        Hook();
        if (EditorApplication.isPlaying)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.isPlaying = false;
            return;
        }

        Environment.SetEnvironmentVariable(SteamOffVariable, "1");
        SessionState.SetBool(BackgroundKey, Application.runInBackground);
        SessionState.SetBool(BackgroundOk, true);
        Application.runInBackground = true;
        ForceGameView();
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/TitleScreen.unity");
        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
        SessionState.SetBool(Key, true);
        SessionState.SetString(PhaseKey, "title");
        phaseStart = EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying = true;
    }

    static void Hook()
    {
        if (hooked)
            return;
        EditorApplication.update += Tick;
        hooked = true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false))
            return;
        if (!EditorApplication.isPlaying)
            return;
        // An unfocused Editor skips player frames, so the queued shot never lands.
        Application.runInBackground = true;
        string phase = SessionState.GetString(PhaseKey, "title");
        if (EditorApplication.timeSinceStartup - phaseStart > 25d)
        {
            Fail("Travel demo capture timed out in " + phase + ".");
            return;
        }

        if (phase == "title")
        {
            if (SceneManager.GetActiveScene().name != Gate.TitleScene)
                return;
            Button button = FindButton("TRAVEL DEMO");
            if (button == null)
                return;
            phaseStart = EditorApplication.timeSinceStartup;
            SessionState.SetString(PhaseKey, "demo");
            button.onClick.Invoke();
            return;
        }

        if (phase == "demo")
        {
            if (SceneManager.GetActiveScene().name != Gate.TravelDemoScene)
                return;
            TravelDemo demo = UnityEngine.Object.FindAnyObjectByType<TravelDemo>();
            if (demo == null || !demo.ShotReady)
                return;
            if (Gate.RunConfirmed)
            {
                Fail("Travel demo started a run.");
                return;
            }

            demo.PrepareStill();
            phaseStart = EditorApplication.timeSinceStartup;
            SessionState.SetString(PhaseKey, "settle");
            return;
        }

        if (phase == "settle")
        {
            if (EditorApplication.timeSinceStartup - phaseStart < 0.6d)
                return;
            string path = Path.Combine(ProjectRoot(), "Screenshots", FileName);
            if (File.Exists(path))
                File.Delete(path);
            pendingFile = path;
            pendingLength = 0;
            pendingStable = 0;
            ScreenCapture.CaptureScreenshot(path, 1);
            phaseStart = EditorApplication.timeSinceStartup;
            SessionState.SetString(PhaseKey, "file");
            return;
        }

        if (phase == "file")
        {
            if (!FileSettled(pendingFile))
                return;
            int width;
            int height;
            if (!ReadPngSize(pendingFile, out width, out height) || width != Width || height != Height)
            {
                Fail("Travel demo shot is " + width + "x" + height + ".");
                return;
            }

            Debug.Log("Captured travel demo to Screenshots/" + FileName + ".");
            SessionState.SetBool(Key, false);
            RestoreSize();
            RestoreBackground();
            EditorApplication.isPlaying = false;
        }
    }

    static Button FindButton(string name)
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == name)
                return buttons[i];
        }

        return null;
    }

    static void Fail(string message)
    {
        Debug.LogError(message);
        SessionState.SetBool(Key, false);
        RestoreSize();
        RestoreBackground();
        EditorApplication.isPlaying = false;
    }

    static bool FileSettled(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;
        long length;
        try
        {
            length = new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return false;
        }

        if (length < 24)
            return false;
        if (length == pendingLength)
            pendingStable++;
        else
        {
            pendingLength = length;
            pendingStable = 0;
        }

        return pendingStable >= 2;
    }

    static bool ReadPngSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        byte[] header = new byte[24];
        using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            if (stream.Read(header, 0, header.Length) < header.Length)
                return false;
        }

        if (header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71)
            return false;
        width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        return true;
    }

    static string ProjectRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    static void ForceGameView()
    {
        EditorWindow view = GameView();
        int current = SelectedSize(view);
        int target = FindOrAddSize(view);
        if (!SessionState.GetBool(PrevOk, false))
        {
            SessionState.SetInt(PrevKey, current);
            SessionState.SetBool(PrevOk, true);
        }

        SelectSize(view, target);
        view.Repaint();
    }

    static void RestoreSize()
    {
        if (!SessionState.GetBool(PrevOk, false))
            return;
        try
        {
            SelectSize(GameView(), SessionState.GetInt(PrevKey, 0));
        }
        catch (Exception)
        {
        }

        SessionState.SetBool(PrevOk, false);
    }

    static void RestoreBackground()
    {
        if (!SessionState.GetBool(BackgroundOk, false))
            return;
        Application.runInBackground = SessionState.GetBool(BackgroundKey, false);
        SessionState.SetBool(BackgroundOk, false);
    }

    static EditorWindow GameView()
    {
        Type type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (type == null)
            throw new InvalidOperationException("Game view type is missing.");
        return EditorWindow.GetWindow(type);
    }

    static int SelectedSize(EditorWindow view)
    {
        PropertyInfo property = FindProperty(view.GetType(), "selectedSizeIndex");
        if (property == null)
            throw new InvalidOperationException("Game view selectedSizeIndex is missing.");
        return (int)property.GetValue(view, null);
    }

    static void SelectSize(EditorWindow view, int index)
    {
        PropertyInfo property = FindProperty(view.GetType(), "selectedSizeIndex");
        if (property != null && property.CanWrite)
            property.SetValue(view, index, null);
        MethodInfo callback = view.GetType().GetMethod(
            "SizeSelectionCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (callback != null)
            callback.Invoke(view, new object[] { index, null });
    }

    static int FindOrAddSize(EditorWindow view)
    {
        object sizes = SizesInstance();
        object group = CurrentGroup(sizes, view);
        MethodInfo getSize = group.GetType().GetMethod("GetGameViewSize");
        MethodInfo totalMethod = group.GetType().GetMethod("GetTotalCount");
        if (getSize == null || totalMethod == null)
            throw new InvalidOperationException("Game view size group is missing its lookup.");
        int total = (int)totalMethod.Invoke(group, null);
        for (int i = 0; i < total; i++)
        {
            object size = getSize.Invoke(group, new object[] { i });
            int w;
            int h;
            string kind;
            ReadSize(size, out w, out h, out kind);
            if (kind == "FixedResolution" && w == Width && h == Height)
                return i;
        }

        Type sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        Type enumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        if (sizeType == null || enumType == null)
            throw new InvalidOperationException("Game view size type is missing.");
        object fixedResolution = Enum.Parse(enumType, "FixedResolution");
        ConstructorInfo ctor = sizeType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) });
        if (ctor == null)
            throw new InvalidOperationException("Game view size constructor is missing.");
        object custom = ctor.Invoke(new object[] { fixedResolution, Width, Height, "1920x1080" });
        MethodInfo add = group.GetType().GetMethod("AddCustomSize");
        if (add == null)
            throw new InvalidOperationException("Game view AddCustomSize is missing.");
        add.Invoke(group, new object[] { custom });
        return (int)totalMethod.Invoke(group, null) - 1;
    }

    static void ReadSize(object size, out int width, out int height, out string kind)
    {
        Type type = size.GetType();
        width = (int)type.GetProperty("width").GetValue(size, null);
        height = (int)type.GetProperty("height").GetValue(size, null);
        kind = type.GetProperty("sizeType").GetValue(size, null).ToString();
    }

    static object SizesInstance()
    {
        Type sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        if (sizesType == null)
            throw new InvalidOperationException("GameViewSizes is missing.");
        Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        PropertyInfo instance = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (instance == null)
            throw new InvalidOperationException("GameViewSizes instance is missing.");
        return instance.GetValue(null, null);
    }

    static object CurrentGroup(object sizes, EditorWindow view)
    {
        object groupType = null;
        PropertyInfo current = FindProperty(view.GetType(), "currentSizeGroupType");
        if (current != null)
        {
            MethodInfo getter = current.GetGetMethod(true);
            groupType = getter.IsStatic ? current.GetValue(null, null) : current.GetValue(view, null);
        }

        if (groupType == null)
        {
            PropertyInfo onSizes = sizes.GetType().GetProperty(
                "currentGroupType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onSizes != null)
                groupType = onSizes.GetValue(sizes, null);
        }

        if (groupType == null)
            throw new InvalidOperationException("Game view size group is missing.");
        MethodInfo getGroup = sizes.GetType().GetMethod("GetGroup");
        if (getGroup == null)
            throw new InvalidOperationException("GameViewSizes.GetGroup is missing.");
        return getGroup.Invoke(sizes, new object[] { groupType });
    }

    static PropertyInfo FindProperty(Type type, string name)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        while (type != null)
        {
            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null)
                return property;
            type = type.BaseType;
        }

        return null;
    }
}
