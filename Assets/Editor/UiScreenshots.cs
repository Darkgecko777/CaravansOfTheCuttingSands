using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Caravans.FrontDoor;
using Caravans.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One menu walks every screen and writes 1920x1080 pictures for the art review.
// It changes no colours, layout, or copy.
public static class UiScreenshots
{
    const string RunKey = "Caravans.UiShots";
    const string PrevKey = "Caravans.UiShots.PrevSize";
    const string PrevOk = "Caravans.UiShots.PrevOk";
    const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";
    const int Width = 1920;
    const int Height = 1080;
    const double StepSeconds = 40d;

    enum Kind { Title, Play, PlayMap }
    enum Phase { Wait, Apply, Settle, Refresh, AfterRefresh, Capture, WaitFile }

    struct Shot
    {
        public string Apply;
        public string File;
        public string Scene;
        public string State;
        public Kind Need;
        public Kind Until;
    }

    static readonly Shot[] Shots =
    {
        ShotOf("title", "01_title.png", "TitleScreen", "Compact wood panel under the tagline, desert around it. Text-only NEW GAME, OPTIONS, and EXIT. No row bars. A bone tick marks the selected row. No Continue. Options closed. The sand is blowing.", Kind.Title, Kind.Title),
        ShotOf("title-options", "02_title_options.png", "TitleScreen", "Options open on the wood panel over the main menu. Master, music, and SFX sliders, fullscreen, resolution, and Show intro again as text rows, and Close. No row bars. A bone tick marks a checked row. No bone frame.", Kind.Title, Kind.Title),
        ShotOf("show-play", null, null, null, Kind.Title, Kind.Play),
        ShotOf("intro", "03_intro_card.png", "SampleScene", "First-run intro card centred over the Kharûn market. Header, body, tagline, and a text-only BEGIN. Play input is locked.", Kind.Play, Kind.Play),
        ShotOf("seed", null, null, null, Kind.Play, Kind.Play),
        ShotOf("market-empty", "10_market_empty.png", "SampleScene", "Market at Kharûn. No cell is selected. The wagon holds the seeded cargo.", Kind.Play, Kind.Play),
        ShotOf("market-buy", "11_market_selected_buy.png", "SampleScene", "Market at Kharûn. A shop cell with stock is selected.", Kind.Play, Kind.Play),
        ShotOf("market-sort", "12_market_sort_menu.png", "SampleScene", "Market at Kharûn. The sort menu is open.", Kind.Play, Kind.Play),
        ShotOf("market-wagon", "13_market_selected_wagon.png", "SampleScene", "Market at Kharûn. A wagon cell is selected.", Kind.Play, Kind.Play),
        ShotOf("ridgewatch", "15_market_ridgewatch.png", "SampleScene", "Market parked at Ridgewatch, so the top bar shows that longer name. The wagon still holds the seeded cargo.", Kind.Play, Kind.Play),
        ShotOf("inn", "20_plaza_inn.png", "SampleScene", "Plaza, Inn, at Kharûn.", Kind.Play, Kind.Play),
        ShotOf("gates", "21_plaza_gates.png", "SampleScene", "Plaza, Gates, at Kharûn. No road is lit. Neighbour road buttons sit inside the gates panel.", Kind.PlayMap, Kind.PlayMap),
        ShotOf("logbook", "40_logbook.png", "SampleScene", "Logbook open over the market at Kharûn, between the bars. Seeded buy and sale figures are on the rows. An unseen price is a dash.", Kind.Play, Kind.Play),
        ShotOf("options", "41_pause_menu.png", "SampleScene", "Pause menu open over the Kharûn market. Compact wood panel, text-only RESUME, OPTIONS, MAIN MENU, and QUIT. No row bars. A bone tick marks the selected row. Gear is visible on the top bar.", Kind.Play, Kind.Play),
        ShotOf("travel", "30_travel_map.png", "SampleScene", "Gates at Kharûn with the Draven road lit. The painted trail and the destination circle are up. Neighbour road buttons stay on the gates panel.", Kind.PlayMap, Kind.PlayMap),
        ShotOf("road", "31_road_choice.png", "SampleScene", "Gates at Kharûn with the Draven road lit. Careful, Steady, and Rush sit on the panel. Embark is live. No card covers the map.", Kind.PlayMap, Kind.PlayMap),
        ShotOf("depart", "32_departure.png", "SampleScene", "En route to Draven, paused at the Kharûn dock. The route line is drawn and the wagon has not moved.", Kind.PlayMap, Kind.PlayMap),
        ShotOf("encounter", "33_encounter.png", "SampleScene", "Encounter card open on the Draven road. The title is On the Road.", Kind.PlayMap, Kind.PlayMap),
        ShotOf("camp", "34_camp.png", "SampleScene", "Make camp card open on the Draven road.", Kind.PlayMap, Kind.PlayMap),
        ShotOf("resume", "35_resume_pace.png", "SampleScene", "Pace card after camp on the Draven road. Dawn and dusk are offered. The camp has advanced the clock.", Kind.PlayMap, Kind.PlayMap),
    };

    const string Notes =
        "The encounter card, the camp card, and the after-camp pace card are part of play. They are 33_encounter.png, 34_camp.png, and 35_resume_pace.png. Play has no tooltip type and no separate dialog type.\n\n"
        + "21_plaza_gates.png is the gates with no road lit. 30_travel_map.png is those same gates with the Kharûn to Draven road lit. That gates view is the travel map the player opens, and the neighbour road buttons stay on its panel.\n\n"
        + "31_road_choice.png is the gates panel with the Draven road lit. Careful, Steady, and Rush are on that panel, and Embark leaves. No card covers the map. The pace card opens after camp, which is 35_resume_pace.png, in the order Careful, Steady, Rush.\n\n"
        + "House, Tavern, and Stables stay off the layout while ShowUnfinishedFeatures is off, so this capture does not include those rooms. Gear stays on the top bar and opens the pause menu, which is part of play. The title menu is a compact wood panel under the tagline. The title, title Options, and the pause menu use that wood panel and text-only items, with a bone tick and no row bars. Title Options includes Show intro again.\n\n"
        + "03_intro_card.png is the first-run intro card, one wood panel over the Kharûn market. It shows the header, the body, the tagline, and a text-only BEGIN. Play input is locked. Later New Games skip the card unless Show intro again is on.\n\n"
        + "15_market_ridgewatch.png is the long dock name. Sarn's Rest is a landmark. House, Tavern, and Stables are off the bar in this capture.\n\n"
        + "The caravan is House Kharûn. This run buys the first goods Kharûn will sell until four units are aboard, and it records the buy and sale figures the market would show. The clock stays at the new-game hour until the camp card.\n";

    static bool updateHooked;
    static bool modeHooked;
    static int step;
    static Phase phase;
    static double stepStart;
    static int anchorFrame;
    static string pendingFile;
    static long pendingLength;
    static int pendingStable;

    [InitializeOnLoadMethod]
    static void Boot()
    {
        string key = SessionState.GetString(RunKey, "");
        if (key == "enter" || key == "walk" || key == "exit" || key == "done")
            Hook();
    }

    [MenuItem("Caravans/Capture UI Screenshots")]
    static void Capture()
    {
        Hook();
        string key = SessionState.GetString(RunKey, "");
        if (key == "enter" || key == "walk" || key == "exit")
        {
            UnityEngine.Debug.LogError("UI screenshot capture is already running.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            SessionState.SetString(RunKey, "exit");
            EditorApplication.isPlaying = false;
            return;
        }

        Begin();
    }

    static void Begin()
    {
        try
        {
            PrepareFolder();
            step = 0;
            phase = Phase.Wait;
            SessionState.SetString(RunKey, "enter");
            SessionState.SetBool(PrevOk, false);
            ForceGameView();
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
            if (scene == null)
            {
                Abort("TitleScreen scene is missing.");
                return;
            }

            EditorSceneManager.playModeStartScene = scene;
            EditorApplication.isPlaying = true;
        }
        catch (Exception error)
        {
            Abort(error.Message);
        }
    }

    static void Hook()
    {
        if (!updateHooked)
        {
            EditorApplication.update += Tick;
            updateHooked = true;
        }

        if (!modeHooked)
        {
            EditorApplication.playModeStateChanged += OnMode;
            modeHooked = true;
        }
    }

    static void Unhook()
    {
        if (updateHooked)
        {
            EditorApplication.update -= Tick;
            updateHooked = false;
        }

        if (modeHooked)
        {
            EditorApplication.playModeStateChanged -= OnMode;
            modeHooked = false;
        }
    }

    static void OnMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            if (SessionState.GetString(RunKey, "") != "enter")
                return;
            SessionState.SetString(RunKey, "walk");
            step = 0;
            phase = Phase.Wait;
            stepStart = EditorApplication.timeSinceStartup;
            Application.runInBackground = true;
            try
            {
                ForceGameView();
            }
            catch (Exception error)
            {
                Abort(error.Message);
            }

            return;
        }

        if (change != PlayModeStateChange.EnteredEditMode)
            return;

        string key = SessionState.GetString(RunKey, "");
        EditorSceneManager.playModeStartScene = null;
        if (key == "exit")
        {
            Begin();
            return;
        }

        if (SessionState.GetBool(PrevOk, false))
            RestoreSize();
        SessionState.SetString(RunKey, "");
        Unhook();
    }

    static void Tick()
    {
        string key = SessionState.GetString(RunKey, "");
        if (key != "walk")
            return;
        if (EditorApplication.isCompiling || !EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                Abort("Play mode ended before the capture finished.");
            return;
        }

        EditorApplication.isPaused = false;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            Advance();
        }
        catch (Exception error)
        {
            Abort(error.Message);
        }
    }

    static void Advance()
    {
        if (step >= Shots.Length)
        {
            Finish();
            return;
        }

        Shot shot = Shots[step];
        if (phase == Phase.Wait)
        {
            if (TimedOut(shot))
                return;
            if (!Ready(shot.Need))
                return;
            phase = Phase.Apply;
        }

        if (phase == Phase.Apply)
        {
            if (!DoApply(shot))
            {
                Abort("Could not apply " + shot.Apply + " in " + SceneManager.GetActiveScene().name + ".");
                return;
            }

            phase = Phase.Settle;
            anchorFrame = -1;
            stepStart = EditorApplication.timeSinceStartup;
        }

        if (phase == Phase.Settle)
        {
            if (TimedOut(shot))
                return;
            if (!Ready(shot.Until))
            {
                anchorFrame = -1;
                return;
            }

            if (anchorFrame < 0)
                anchorFrame = Time.frameCount;
            if (Time.frameCount < anchorFrame + 3)
                return;
            if (string.IsNullOrEmpty(shot.File))
            {
                Next();
                return;
            }

            phase = Phase.Refresh;
        }

        if (phase == Phase.Refresh)
        {
            if (!DoApply(shot))
            {
                Abort("Could not refresh " + shot.Apply + ".");
                return;
            }

            phase = Phase.AfterRefresh;
            anchorFrame = Time.frameCount;
        }

        if (phase == Phase.AfterRefresh)
        {
            if (Time.frameCount < anchorFrame + 2)
                return;
            phase = Phase.Capture;
        }

        if (phase == Phase.Capture)
        {
            BeginCapture(shot.File);
            phase = Phase.WaitFile;
            anchorFrame = Time.frameCount;
            pendingLength = -1;
            pendingStable = 0;
        }

        if (phase == Phase.WaitFile)
        {
            if (Time.frameCount > anchorFrame + 240)
            {
                Abort("Screenshot was not written: " + pendingFile + ".");
                return;
            }

            if (!FileSettled(pendingFile))
                return;
            int w;
            int h;
            if (!ReadPngSize(pendingFile, out w, out h) || w != Width || h != Height)
            {
                Abort("Picture " + shot.File + " is " + w + "x" + h
                    + " with the game view at " + Screen.width + "x" + Screen.height
                    + ". Expected " + Width + "x" + Height + ".");
                return;
            }

            Next();
        }
    }

    static void Next()
    {
        step++;
        phase = Phase.Wait;
        stepStart = EditorApplication.timeSinceStartup;
        anchorFrame = -1;
        if (step >= Shots.Length)
            Finish();
    }

    static bool TimedOut(Shot shot)
    {
        if (EditorApplication.timeSinceStartup - stepStart < StepSeconds)
            return false;
        Abort("Timed out on " + shot.Apply + " in " + SceneManager.GetActiveScene().name + ".");
        return true;
    }

    static void Finish()
    {
        int count = 0;
        for (int i = 0; i < Shots.Length; i++)
        {
            if (!string.IsNullOrEmpty(Shots[i].File))
                count++;
        }

        WriteIndex();
        UnityEngine.Debug.Log("Captured " + count + " screenshots to Screenshots/.");
        SessionState.SetString(RunKey, "done");
        EditorSceneManager.playModeStartScene = null;
        RestoreSize();
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
    }

    static void Abort(string reason)
    {
        UnityEngine.Debug.LogError("UI screenshot capture failed. " + reason);
        SessionState.SetString(RunKey, "");
        EditorSceneManager.playModeStartScene = null;
        RestoreSize();
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
    }

    static bool DoApply(Shot shot)
    {
        switch (shot.Apply)
        {
            case "title":
                return TitleOptions(false);
            case "title-options":
                return TitleOptions(true);
            case "show-play":
                Gate.ConfirmRun();
                return true;
            default:
                PlayPresenter play = UnityEngine.Object.FindAnyObjectByType<PlayPresenter>();
                return play != null && play.Shot(shot.Apply);
        }
    }

    static bool TitleOptions(bool open)
    {
        TitleScreen title = UnityEngine.Object.FindAnyObjectByType<TitleScreen>();
        if (title == null || !title.ShotReady)
            return false;
        title.ShotOptions(open);
        return true;
    }

    static bool Ready(Kind kind)
    {
        if (!FadeIdle())
            return false;
        string scene = SceneManager.GetActiveScene().name;
        switch (kind)
        {
            case Kind.Title:
                TitleScreen title = UnityEngine.Object.FindAnyObjectByType<TitleScreen>();
                return title != null && title.ShotReady && scene == Gate.TitleScene;
            case Kind.Play:
                return scene == Gate.PlayScene && UnityEngine.Object.FindAnyObjectByType<PlayPresenter>() != null;
            case Kind.PlayMap:
                PlayPresenter play = UnityEngine.Object.FindAnyObjectByType<PlayPresenter>();
                return scene == Gate.PlayScene && play != null && play.ShotMapReady;
            default:
                return false;
        }
    }

    static bool FadeIdle()
    {
        StormDirector storm = UnityEngine.Object.FindAnyObjectByType<StormDirector>();
        return storm != null && storm.ShotFadeIdle;
    }

    static void BeginCapture(string file)
    {
        string path = PicturePath(file);
        if (File.Exists(path))
            File.Delete(path);
        pendingFile = path;
        ScreenCapture.CaptureScreenshot(path, 1);
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

    static void PrepareFolder()
    {
        string dir = Folder();
        Directory.CreateDirectory(dir);
        string[] pngs = Directory.GetFiles(dir, "*.png");
        for (int i = 0; i < pngs.Length; i++)
            File.Delete(pngs[i]);
        string index = Path.Combine(dir, "index.md");
        if (File.Exists(index))
            File.Delete(index);
    }

    static void WriteIndex()
    {
        var text = new StringBuilder();
        text.Append("# UI screenshots\n\nCaptured ");
        text.Append(DateTime.Now.ToString("yyyy-MM-dd"));
        text.Append(" at commit ");
        text.Append(CommitLine());
        text.Append(".\n\n| File | Scene | State |\n| --- | --- | --- |\n");
        for (int i = 0; i < Shots.Length; i++)
        {
            Shot shot = Shots[i];
            if (string.IsNullOrEmpty(shot.File))
                continue;
            text.Append("| ");
            text.Append(shot.File);
            text.Append(" | ");
            text.Append(shot.Scene);
            text.Append(" | ");
            text.Append(shot.State);
            text.Append(" |\n");
        }

        text.Append("\n");
        text.Append(Notes);
        File.WriteAllText(Path.Combine(Folder(), "index.md"), text.ToString());
    }

    static string CommitLine()
    {
        string root = ProjectRoot();
        string hash = Git(root, "rev-parse --short HEAD");
        if (string.IsNullOrEmpty(hash))
            return "unknown";
        string dirty = Git(root, "status --porcelain");
        if (!string.IsNullOrEmpty(dirty))
            return hash + " (working tree)";
        return hash;
    }

    static string Git(string root, string args)
    {
        try
        {
            var start = new ProcessStartInfo();
            start.FileName = "git";
            start.Arguments = "-C \"" + root + "\" " + args;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            using (Process process = Process.Start(start))
            {
                if (process == null)
                    return "";
                string output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(5000))
                    return "";
                if (process.ExitCode != 0)
                    return "";
                return output.Trim();
            }
        }
        catch (Exception)
        {
            return "";
        }
    }

    static string Folder()
    {
        return Path.Combine(ProjectRoot(), "Screenshots");
    }

    static string PicturePath(string file)
    {
        return Path.Combine(Folder(), file);
    }

    static string ProjectRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    static Shot ShotOf(string apply, string file, string scene, string state, Kind need, Kind until)
    {
        Shot shot;
        shot.Apply = apply;
        shot.File = file;
        shot.Scene = scene;
        shot.State = state;
        shot.Need = need;
        shot.Until = until;
        return shot;
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
