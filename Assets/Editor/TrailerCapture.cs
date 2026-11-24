using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Caravans.FrontDoor;
using Caravans.Play;
using Caravans.Steam;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Records the trailer clips from the connected Editor. Play Mode only.
public static class TrailerCapture
{
    const string RunKey = "Caravans.Trailer";
    const string SandboxKey = "Caravans.Trailer.SaveDir";
    const string PrevKey = "Caravans.Trailer.PrevSize";
    const string PrevOk = "Caravans.Trailer.PrevOk";
    const string OverlayOk = "Caravans.Trailer.OverlayOk";
    const string GizmosKey = "Caravans.Trailer.Gizmos";
    const string StatsKey = "Caravans.Trailer.Stats";
    const string ShowGizmosKey = "Caravans.Trailer.ShowGizmos";
    const string MuteKey = "Caravans.Trailer.Mute";
    const string MuteOk = "Caravans.Trailer.MuteOk";
    const string BackgroundKey = "Caravans.Trailer.Background";
    const string BackgroundOk = "Caravans.Trailer.BackgroundOk";
    const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";
    const string SteamOffVariable = "CARAVANS_EDITOR_NO_STEAM";
    const int Width = 1920;
    const int Height = 1080;
    const int StormSeed = 4765690;
    const double WaitSeconds = 40d;
    const double FileSeconds = 10d;
    const double StallSeconds = 20d;

    enum Phase { Wait, Apply, Settle, Record, WaitFile }

    struct Shot
    {
        public string Id;
        public string Nn;
        public string Name;
        public bool Title;
        public bool Spare;
        public float Seconds;
        public int Frames;
        public string Scene;
        public string Setup;
        public float SlotStart;
        public float SlotEnd;
        public string[] ActionAt;
        public string[] ActionWhat;
    }

    [Serializable]
    class ManifestFile
    {
        public string unityVersion;
        public string recorderVersion;
        public string gitHead;
        public int runSeed;
        public bool steamSuppressed;
        public string capturedAtUtc;
        public int fps;
        public int width;
        public int height;
        public ManifestShot[] shots;
    }

    [Serializable]
    class ManifestShot
    {
        public string nn;
        public string name;
        public string file;
        public bool spare;
        public float seconds;
        public int frames;
        public string scene;
        public string setup;
        public ManifestAction[] actions;
        public ManifestEvent[] events;
        public string fingerprintStart;
        public string fingerprintEnd;
        public ManifestSlot editSlot;
    }

    [Serializable]
    class ManifestAction
    {
        public float t;
        public string what;
    }

    [Serializable]
    class ManifestEvent
    {
        public string name;
        public float t;
    }

    [Serializable]
    class ManifestSlot
    {
        public float start;
        public float end;
    }

    static readonly Shot[] Shots =
    {
        Make("01", "title_sand", true, false, 8f, 0f, 7.43f, "TitleScreen", "Title scene, no save (ShotSaveState(false)), Options closed, the storm seed fixed, the sand blowing.", null, null),
        Make("02", "kharun_market", false, false, 6f, 7.43f, 12.03f, "SampleScene", "Kharûn market, Day 1 at 07:00, nothing selected, starting packs.", new[] { "2.5" }, new[] { "SelectShop" }),
        Make("03", "depart_draven", false, false, 6f, 12.03f, 16.74f, "SampleScene", "Kharûn Gates, two pack units bought at Kharûn, full kit, Day 1 at 08:00. The thrask token sits on the Kharûn dock.", new[] { "0.6", "1.6", "1.7" }, new[] { "AskRoad(Draven)", "Embark()", "ChoosePace(2)" }),
        Make("04", "road_day", false, false, 6.5f, 16.74f, 21.66f, "SampleScene", "Kharûn → Draven, Steady, 2×, Day 2 at 10:00, clear sky on the road, leg advanced to 0.30, quiet road, three duzhu rivals on the same road.", null, null),
        Make("05", "market_buy", false, false, 4f, 21.66f, 24.22f, "SampleScene", "Kharûn market, coin 600, packs empty.", new[] { "0.5", "1.2", "1.8" }, new[] { "SelectShop", "PressBuy()", "PressBuy()" }),
        Make("06", "market_sell", false, false, 4f, 24.22f, 26.63f, "SampleScene", "Draven market, three pack units of goods Draven buys above their basis.", new[] { "0.5", "1.2" }, new[] { "SelectWagon(0)", "PressSell()" }),
        Make("07", "inn_fill", false, false, 4f, 26.63f, 29.02f, "SampleScene", "Draven, Plaza, Inn, kit at 40% water and 40% rations, coin 300.", new[] { "0.8", "1.6" }, new[] { "TryFillWater()", "TryFillFood()" }),
        Make("08", "road_dusk", false, false, 6.5f, 29.02f, 33.83f, "SampleScene", "Kharûn → Brineford, Steady, 2×, Day 3 at 16:30, leg advanced to 0.25, quiet road, clear sky.", null, null),
        Make("09", "tavern_hire", false, false, 6f, 33.83f, 38.62f, "SampleScene", "Kharûn, Plaza, Tavern, coin 400, no agents.", new[] { "1.0", "2.6" }, new[] { "HireShownAgent(Guard)", "HireShownAgent(Scout)" }),
        Make("10", "gates_scout_rush", false, false, 4f, 38.62f, 41.03f, "SampleScene", "Kharûn Gates, scout hired, two pack units, full kit.", new[] { "0.5", "1.3" }, new[] { "AskRoad(Draven)", "PickPace(Rush)" }),
        Make("11", "encounter_good", false, false, 6.5f, 41.03f, 45.84f, "SampleScene", "On the Draven road, paused, card CA-11 (The Hermit) open.", new[] { "2.0" }, new[] { "ResolveOutcome good" }),
        Make("12", "skins_low", false, false, 4f, 45.84f, 48.23f, "SampleScene", "On the Draven road, packs without drink, low kit, the Skins Run Low card open.", null, null),
        Make("13", "guard_save", false, false, 4f, 48.23f, 50.62f, "SampleScene", "Guard hired, Rush, three pack units, on the Draven road paused, card CA-03 (Soft Ground) open.", new[] { "1.0" }, new[] { "ResolveOutcome bad" }),
        Make("14", "sandstorm_in", false, false, 7f, 50.62f, 55.43f, "SampleScene", "Kharûn → Draven, Steady, 2×, Day 2 at 11:00, leg advanced to 0.20, quiet road. The sandstorm mass closes on the caravan.", null, null),
        Make("15", "rush_cargo_loss", false, false, 7f, 55.43f, 60.23f, "SampleScene", "Rush, no guard, four pack units, on the Draven road paused, card CA-04 (Glass-thorn Patch) open.", new[] { "2.0" }, new[] { "ResolveOutcome bad" }),
        Make("16", "logbook_road_note", false, false, 4f, 60.23f, 62.62f, "SampleScene", "Shot 15's outcome already in the log, docked at Draven, market open.", new[] { "0.4" }, new[] { "ToggleLogbook()" }),
        Make("17", "village_flip", false, false, 6.5f, 62.62f, 67.43f, "SampleScene", "Draven market, one sale from flipping the patron, one pack unit with a basis.", new[] { "1.0", "1.8" }, new[] { "SelectWagon(0)", "PressSell()" }),
        Make("18", "rank_up", false, false, 5f, 67.43f, 69.82f, "SampleScene", "Kharûn market, points just under rank 5, one pack unit whose sale crosses it.", new[] { "0.6", "1.2" }, new[] { "SelectWagon(0)", "PressSell()" }),
        Make("19", "tavern_map_piece", false, false, 6.5f, 69.82f, 74.63f, "SampleScene", "Kharûn, Plaza, Tavern, rank 1, coin 2000, one lead.", new[] { "1.5" }, new[] { "BuyShownPiece(1)" }),
        Make("20", "westmark_gate", false, false, 4f, 74.63f, 77.02f, "SampleScene", "Westmark Gates, six pieces owned, points 1000, full kit.", new[] { "0.8" }, new[] { "AskSarnRoad()" }),
        Make("21", "sarn_road", false, false, 5f, 77.02f, 79.41f, "SampleScene", "Westmark Gates with the Sarn road lit, six pieces owned, full kit.", new[] { "0.3", "0.4" }, new[] { "EmbarkSarnRoad()", "ChoosePace(2)" }),
        Make("22", "embarked", false, false, 4f, 79.41f, 81.83f, "SampleScene", "The Embarked ledger.", null, null),
        Make("23", "camp_card", false, true, 4f, 0f, 0f, "SampleScene", "Draven road, the Make camp card.", null, null),
        Make("24", "departure_short", false, true, 4f, 0f, 0f, "SampleScene", "Kharûn Gates, low kit, the real short-kit departure card for Brineford.", null, null),
        Make("25", "rescue_dry", false, true, 4f, 0f, 0f, "SampleScene", "Draven road, the Dry card.", null, null),
        Make("26", "sarns_rest", false, true, 4f, 0f, 0f, "SampleScene", "The Sarn's Rest arrival card.", null, null),
        Make("27", "ridgewatch_market", false, true, 5f, 0f, 0f, "SampleScene", "Ridgewatch market, starting packs.", new[] { "1.5" }, new[] { "SelectShop" }),
        Make("28", "road_night_4x", false, true, 6f, 0f, 0f, "SampleScene", "Kharûn → Draven, Steady, 4×, Day 2 at 22:00, leg advanced to 0.40, quiet road, clear sky.", null, null)
    };

    static bool updateHooked;
    static bool modeHooked;
    static bool failed;
    static int step;
    static Phase phase;
    static double stepStart;
    static double fileStart;
    static double lastAdvance;
    static int anchorFrame;
    static EntityId previousPlayId;
    static bool havePreviousPlay;
    static bool confirmed;
    static int lastCaptureFrame;
    static string fingerprintStart;
    static string pendingFile;
    static long pendingLength;
    static int pendingStable;
    static RecorderController recorder;
    static RecorderControllerSettings recorderSettings;
    static MovieRecorderSettings movieSettings;
    static ManifestFile manifest;

    [InitializeOnLoadMethod]
    static void Boot()
    {
        string key = SessionState.GetString(RunKey, "");
        if (key == "enter" || key == "walk" || key == "exit" || key == "stop" || key == "abort")
            Hook();
    }

    [MenuItem("Caravans/Trailer/Capture All")]
    static void Capture()
    {
        Hook();
        string key = SessionState.GetString(RunKey, "");
        if (key == "enter" || key == "walk" || key == "exit")
        {
            UnityEngine.Debug.LogError("Trailer capture is already running.");
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

    [MenuItem("Caravans/Trailer/Stop Capture")]
    static void Stop()
    {
        Hook();
        failed = true;
        StopRecorder();
        SessionState.SetString(RunKey, "stop");
        EditorSceneManager.playModeStartScene = null;
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        else
            CleanupEdit();
    }

    static void Begin()
    {
        try
        {
            failed = false;
            PrepareFolder();
            BeginSaveSandbox();
            step = 0;
            phase = Phase.Wait;
            confirmed = false;
            havePreviousPlay = false;
            manifest = new ManifestFile();
            manifest.unityVersion = Application.unityVersion;
            manifest.recorderVersion = "5.1.8";
            manifest.gitHead = GitHead();
            manifest.runSeed = 4817;
            manifest.steamSuppressed = true;
            manifest.capturedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            manifest.fps = 60;
            manifest.width = Width;
            manifest.height = Height;
            manifest.shots = new ManifestShot[0];
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
            TrailerClock.Begin(StormSeed);
            Environment.SetEnvironmentVariable(SteamOffVariable, "1");
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
            string folder = SessionState.GetString(SandboxKey, "");
            if (!string.IsNullOrEmpty(folder))
                SaveSlot.DirectoryOverride = folder;
            if (SteamManager.Initialized || UnityEngine.Object.FindAnyObjectByType<SteamManager>() != null)
            {
                Abort("Steam started during the trailer capture.");
                return;
            }

            SessionState.SetBool(BackgroundOk, true);
            SessionState.SetBool(BackgroundKey, Application.runInBackground);
            Application.runInBackground = true;
            SessionState.SetString(RunKey, "walk");
            step = 0;
            phase = Phase.Wait;
            confirmed = false;
            havePreviousPlay = false;
            stepStart = EditorApplication.timeSinceStartup;
            try
            {
                ForceGameView();
                HideOverlays();
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
            EndSaveSandbox();
            Begin();
            return;
        }

        CleanupEdit();
    }

    static void CleanupEdit()
    {
        StopRecorder();
        EndSaveSandbox();
        TrailerClock.End();
        ReleaseSteamOff();
        RestoreOverlays();
        RestoreBackground();
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

        EditorUtility.audioMasterMute = true;
        try
        {
            if (phase == Phase.Record)
            {
                if (ClipFailed())
                    return;
                if (ClipDone())
                {
                    FinishClip();
                    return;
                }

                NoteAdvance();
                return;
            }

            if (phase == Phase.WaitFile)
            {
                PollFile();
                return;
            }

            Phase before = phase;
            Step();
            if (failed || phase == Phase.WaitFile || phase == Phase.Record)
                return;
            if (before != phase && phase == Phase.Record)
                return;
            Pump();
        }
        catch (Exception error)
        {
            Abort(error.Message);
        }
    }

    static void Step()
    {
        if (step >= Shots.Length)
        {
            Finish();
            return;
        }

        Shot shot = Shots[step];
        if (phase == Phase.Wait)
        {
            if (TimedOut(shot, WaitSeconds))
                return;
            if (shot.Title)
            {
                if (!TitleReady())
                    return;
                phase = Phase.Apply;
            }
            else
            {
                if (!confirmed)
                {
                    PlayPresenter current = FindPlay();
                    havePreviousPlay = current != null;
                    if (havePreviousPlay)
                        previousPlayId = current.GetEntityId();
                    Gate.ConfirmRun();
                    confirmed = true;
                    stepStart = EditorApplication.timeSinceStartup;
                    return;
                }

                PlayPresenter play = FindPlay();
                if (play == null || !play.ShotMapReady)
                    return;
                if (havePreviousPlay && play.GetEntityId() == previousPlayId)
                    return;
                phase = Phase.Apply;
            }
        }

        if (phase == Phase.Apply)
        {
            if (shot.Title)
            {
                if (!PrepareTitle())
                {
                    Abort("Could not prepare the title.");
                    return;
                }
            }
            else
            {
                PlayPresenter play = FindPlay();
                if (play == null || !play.TrailerBegin(shot.Id))
                {
                    string reason = play == null ? "Play is missing." : play.TrailerFault;
                    Abort(string.IsNullOrEmpty(reason) ? "Setup returned false." : reason);
                    return;
                }
            }

            phase = Phase.Settle;
            anchorFrame = -1;
            stepStart = EditorApplication.timeSinceStartup;
        }

        if (phase == Phase.Settle)
        {
            if (TimedOut(shot, WaitSeconds))
                return;
            if (shot.Title)
            {
                if (!TitleReady())
                {
                    anchorFrame = -1;
                    return;
                }
            }
            else
            {
                PlayPresenter play = FindPlay();
                if (play == null || !play.ShotMapReady)
                {
                    anchorFrame = -1;
                    return;
                }
            }

            if (anchorFrame < 0)
                anchorFrame = Time.frameCount;
            if (Time.frameCount < anchorFrame + 3)
                return;
            BeginRecord(shot);
        }
    }

    static void BeginRecord(Shot shot)
    {
        StopRecorder();
        string path = ClipPath(shot);
        if (File.Exists(path))
            File.Delete(path);
        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        var encoder = new CoreEncoderSettings();
        encoder.Codec = CoreEncoderSettings.OutputCodec.MP4;
        // Recorder 5.1.8 writes EncodingProfile only when quality is Custom. High quality stays on the old API and comes out Constrained Baseline.
        encoder.EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Custom;
        encoder.EncodingProfile = CoreEncoderSettings.H264EncodingProfile.High;
        encoder.TargetBitRate = 25f;
        if (!encoder.SupportsCurrentPlatform())
        {
            Abort("H.264 recording is not available.");
            return;
        }

        recorderSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderSettings.hideFlags = HideFlags.HideAndDontSave;
        movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.hideFlags = HideFlags.HideAndDontSave;
        movieSettings.Enabled = true;
        movieSettings.EncoderSettings = encoder;
        movieSettings.CaptureAudio = false;
        movieSettings.CaptureAlpha = false;
        movieSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = Width,
            OutputHeight = Height
        };
        movieSettings.OutputFile = path.Substring(0, path.Length - 4).Replace('\\', '/');
        recorderSettings.AddRecorderSettings(movieSettings);
        recorderSettings.SetRecordModeToManual();
        recorderSettings.FrameRate = 60f;
        recorderSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        recorderSettings.CapFrameRate = true;
        recorderSettings.ExitPlayMode = false;
        recorder = new RecorderController(recorderSettings);
        RecorderOptions.VerboseMode = false;
        recorder.PrepareRecording();
        if (!recorder.StartRecording())
        {
            Abort("The recorder did not start.");
            return;
        }

        if (shot.Title)
        {
            TrailerClock.ResetFrames();
            fingerprintStart = "title";
        }
        else
        {
            PlayPresenter play = FindPlay();
            play.TrailerStartFrames();
            fingerprintStart = play.TrailerFingerprint;
        }

        phase = Phase.Record;
        lastCaptureFrame = -1;
        lastAdvance = EditorApplication.timeSinceStartup;
        pendingFile = path;
    }

    static bool ClipDone()
    {
        Shot shot = Shots[step];
        if (shot.Title)
            return TrailerClock.Frames >= shot.Frames;
        PlayPresenter play = FindPlay();
        return play != null && play.TrailerShotDone;
    }

    static bool ClipFailed()
    {
        Shot shot = Shots[step];
        if (shot.Title)
            return false;
        PlayPresenter play = FindPlay();
        if (play == null)
        {
            Abort("Play closed during " + shot.Id + ".");
            return true;
        }

        if (!string.IsNullOrEmpty(play.TrailerFault))
        {
            Abort(play.TrailerFault);
            return true;
        }

        return false;
    }

    static void NoteAdvance()
    {
        int frame = Shots[step].Title ? TrailerClock.Frames : FindPlay() != null ? FindPlay().TrailerFrameCount : 0;
        if (frame != lastCaptureFrame)
        {
            lastCaptureFrame = frame;
            lastAdvance = EditorApplication.timeSinceStartup;
            return;
        }

        if (EditorApplication.timeSinceStartup - lastAdvance > StallSeconds)
            Abort("Recording stalled.");
    }

    static void FinishClip()
    {
        Shot shot = Shots[step];
        string endPrint = shot.Title ? "title" : FindPlay().TrailerFingerprint;
        int eventCount = 0;
        PlayPresenter play = shot.Title ? null : FindPlay();
        if (play != null)
            eventCount = play.TrailerEventCount;
        StopRecorder();
        var entry = new ManifestShot();
        entry.nn = shot.Nn;
        entry.name = shot.Name;
        entry.file = shot.Id + ".mp4";
        entry.spare = shot.Spare;
        entry.seconds = shot.Seconds;
        entry.frames = shot.Frames;
        entry.scene = shot.Scene;
        entry.setup = shot.Setup;
        entry.fingerprintStart = fingerprintStart;
        entry.fingerprintEnd = endPrint;
        entry.editSlot = new ManifestSlot();
        entry.editSlot.start = shot.SlotStart;
        entry.editSlot.end = shot.SlotEnd;
        entry.actions = new ManifestAction[shot.ActionAt == null ? 0 : shot.ActionAt.Length];
        for (int i = 0; i < entry.actions.Length; i++)
        {
            entry.actions[i] = new ManifestAction();
            entry.actions[i].t = float.Parse(shot.ActionAt[i], CultureInfo.InvariantCulture);
            entry.actions[i].what = shot.ActionWhat[i];
        }

        entry.events = new ManifestEvent[eventCount];
        for (int i = 0; i < eventCount; i++)
        {
            entry.events[i] = new ManifestEvent();
            entry.events[i].name = play.TrailerEventName(i);
            entry.events[i].t = play.TrailerEventFrame(i) / 60f;
        }

        var list = new ManifestShot[manifest.shots.Length + 1];
        for (int i = 0; i < manifest.shots.Length; i++)
            list[i] = manifest.shots[i];
        list[list.Length - 1] = entry;
        manifest.shots = list;
        WriteManifest();
        phase = Phase.WaitFile;
        fileStart = EditorApplication.timeSinceStartup;
        pendingLength = -1;
        pendingStable = 0;
    }

    static void PollFile()
    {
        if (EditorApplication.timeSinceStartup - fileStart > FileSeconds)
        {
            Abort("Clip was not written: " + pendingFile + ".");
            return;
        }

        if (!FileSettled(pendingFile))
            return;
        Next();
    }

    static void Next()
    {
        step++;
        phase = Phase.Wait;
        confirmed = false;
        havePreviousPlay = false;
        stepStart = EditorApplication.timeSinceStartup;
        anchorFrame = -1;
        if (step >= Shots.Length)
            Finish();
    }

    static void Finish()
    {
        StopRecorder();
        WriteManifest();
        UnityEngine.Debug.Log("Trailer capture wrote " + Shots.Length + " clips to Recordings/Trailer/.");
        SessionState.SetString(RunKey, "done");
        EditorSceneManager.playModeStartScene = null;
        RestoreSize();
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
    }

    static bool TimedOut(Shot shot, double seconds)
    {
        if (EditorApplication.timeSinceStartup - stepStart < seconds)
            return false;
        Abort("Timed out on " + shot.Id + ".");
        return true;
    }

    static void Abort(string reason)
    {
        if (failed)
            return;
        failed = true;
        string name = step >= 0 && step < Shots.Length ? Shots[step].Id : "the trailer";
        UnityEngine.Debug.LogError("Trailer capture failed on " + name + ". " + reason);
        StopRecorder();
        SessionState.SetString(RunKey, "abort");
        EditorSceneManager.playModeStartScene = null;
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        else
            CleanupEdit();
    }

    static void StopRecorder()
    {
        if (recorder != null)
        {
            try
            {
                if (recorder.IsRecording())
                    recorder.StopRecording();
            }
            catch (Exception)
            {
            }

            recorder = null;
        }

        if (movieSettings != null)
        {
            UnityEngine.Object.DestroyImmediate(movieSettings);
            movieSettings = null;
        }

        if (recorderSettings != null)
        {
            UnityEngine.Object.DestroyImmediate(recorderSettings);
            recorderSettings = null;
        }
    }

    static void Pump()
    {
        EditorApplication.isPaused = false;
        EditorApplication.QueuePlayerLoopUpdate();
    }

    static bool TitleReady()
    {
        if (!FadeIdle())
            return false;
        if (SceneManager.GetActiveScene().name != Gate.TitleScene)
            return false;
        TitleScreen title = UnityEngine.Object.FindAnyObjectByType<TitleScreen>();
        return title != null && title.ShotReady;
    }

    static bool PrepareTitle()
    {
        TitleScreen title = UnityEngine.Object.FindAnyObjectByType<TitleScreen>();
        if (title == null || !title.ShotReady)
            return false;
        title.ShotOptions(false);
        title.ShotSaveState(false);
        return true;
    }

    static bool FadeIdle()
    {
        StormDirector[] storms = UnityEngine.Object.FindObjectsByType<StormDirector>(FindObjectsInactive.Exclude);
        if (storms == null || storms.Length == 0)
            return false;
        for (int i = 0; i < storms.Length; i++)
        {
            if (storms[i] != null && storms[i].ShotFadeIdle)
                return true;
        }

        return false;
    }

    static PlayPresenter FindPlay()
    {
        return UnityEngine.Object.FindAnyObjectByType<PlayPresenter>();
    }

    static void ReleaseSteamOff()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        Environment.SetEnvironmentVariable(SteamOffVariable, null);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RestoreSaveSandbox()
    {
        string key = SessionState.GetString(RunKey, "");
        if (key != "enter" && key != "walk" && key != "exit")
            return;
        string folder = SessionState.GetString(SandboxKey, "");
        if (string.IsNullOrEmpty(folder))
            return;
        SaveSlot.DirectoryOverride = folder;
    }

    static void BeginSaveSandbox()
    {
        string folder = Path.Combine(Application.temporaryCachePath, "caravans-trailer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        SessionState.SetString(SandboxKey, folder);
        SaveSlot.DirectoryOverride = folder;
    }

    static void EndSaveSandbox()
    {
        string folder = SessionState.GetString(SandboxKey, "");
        SaveSlot.DirectoryOverride = null;
        SessionState.SetString(SandboxKey, "");
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return;
        try
        {
            Directory.Delete(folder, true);
        }
        catch (Exception error)
        {
            UnityEngine.Debug.LogWarning("Trailer save folder was not removed. " + error.Message);
        }
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

        if (length < 32)
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

    static void PrepareFolder()
    {
        string dir = Folder();
        Directory.CreateDirectory(dir);
        string[] clips = Directory.GetFiles(dir, "*.mp4");
        for (int i = 0; i < clips.Length; i++)
            File.Delete(clips[i]);
        string index = Path.Combine(dir, "shots.json");
        if (File.Exists(index))
            File.Delete(index);
    }

    static void WriteManifest()
    {
        if (manifest == null)
            return;
        File.WriteAllText(Path.Combine(Folder(), "shots.json"), JsonUtility.ToJson(manifest, true));
    }

    static string GitHead()
    {
        try
        {
            var start = new ProcessStartInfo();
            start.FileName = "git";
            start.Arguments = "-C \"" + ProjectRoot() + "\" rev-parse --short HEAD";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            using (Process process = Process.Start(start))
            {
                if (process == null)
                    return string.Empty;
                string output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(5000) || process.ExitCode != 0)
                    return string.Empty;
                return output.Trim();
            }
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    static string ClipPath(Shot shot)
    {
        return Path.Combine(Folder(), shot.Id + ".mp4");
    }

    static string Folder()
    {
        return Path.Combine(ProjectRoot(), "Recordings", "Trailer");
    }

    static string ProjectRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    static Shot Make(string nn, string name, bool title, bool spare, float seconds, float slotStart, float slotEnd, string scene, string setup, string[] at, string[] what)
    {
        Shot shot;
        shot.Id = nn + "_" + name;
        shot.Nn = nn;
        shot.Name = name;
        shot.Title = title;
        shot.Spare = spare;
        shot.Seconds = seconds;
        shot.Frames = (int)Math.Round(seconds * 60d, MidpointRounding.AwayFromZero);
        shot.Scene = scene;
        shot.Setup = setup;
        shot.SlotStart = slotStart;
        shot.SlotEnd = slotEnd;
        shot.ActionAt = at;
        shot.ActionWhat = what;
        return shot;
    }

    static void HideOverlays()
    {
        EditorWindow view = GameView();
        bool gizmos = ReadBool(view, "m_Gizmos");
        bool stats = ReadBool(view, "m_Stats");
        bool show = ReadBool(view, "m_ShowGizmos");
        SessionState.SetBool(GizmosKey, gizmos);
        SessionState.SetBool(StatsKey, stats);
        SessionState.SetBool(ShowGizmosKey, show);
        SessionState.SetBool(OverlayOk, true);
        WriteBool(view, "m_Gizmos", false);
        WriteBool(view, "m_Stats", false);
        WriteBool(view, "m_ShowGizmos", false);
        SessionState.SetBool(MuteKey, EditorUtility.audioMasterMute);
        SessionState.SetBool(MuteOk, true);
        EditorUtility.audioMasterMute = true;
        view.Repaint();
    }

    static void RestoreOverlays()
    {
        if (SessionState.GetBool(MuteOk, false))
        {
            EditorUtility.audioMasterMute = SessionState.GetBool(MuteKey, false);
            SessionState.SetBool(MuteOk, false);
        }

        if (!SessionState.GetBool(OverlayOk, false))
            return;
        try
        {
            EditorWindow view = GameView();
            WriteBool(view, "m_Gizmos", SessionState.GetBool(GizmosKey, false));
            WriteBool(view, "m_Stats", SessionState.GetBool(StatsKey, false));
            WriteBool(view, "m_ShowGizmos", SessionState.GetBool(ShowGizmosKey, false));
            view.Repaint();
        }
        catch (Exception)
        {
        }

        SessionState.SetBool(OverlayOk, false);
    }

    static void RestoreBackground()
    {
        if (!SessionState.GetBool(BackgroundOk, false))
            return;
        Application.runInBackground = SessionState.GetBool(BackgroundKey, false);
        SessionState.SetBool(BackgroundOk, false);
    }

    static bool ReadBool(EditorWindow view, string name)
    {
        FieldInfo field = FindField(view.GetType(), name);
        if (field == null || field.FieldType != typeof(bool))
            return false;
        return (bool)field.GetValue(view);
    }

    static void WriteBool(EditorWindow view, string name, bool value)
    {
        FieldInfo field = FindField(view.GetType(), name);
        if (field == null || field.FieldType != typeof(bool))
            return;
        field.SetValue(view, value);
    }

    static FieldInfo FindField(Type type, string name)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        while (type != null)
        {
            FieldInfo field = type.GetField(name, flags);
            if (field != null)
                return field;
            type = type.BaseType;
        }

        return null;
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
