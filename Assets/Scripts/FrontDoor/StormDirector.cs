using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Caravans.FrontDoor
{
    // The title theme keeps tapering if the player leaves the title early.
    // The sand does not. It is taken down on the same frame the fade begins.
    public sealed class StormDirector : MonoBehaviour
    {
        public const float FadeSeconds = 2.2f;
        public const float MusicFadeSeconds = 1.5f;
        const float FadeStepCap = 0.1f;

        static StormDirector instance;

        SandBedSim sim;
        StormFade fade;
        StormFade musicFade;
        Camera stormCamera;
        AudioSource theme;
        Mesh mesh;
        MeshRenderer grains;
        Vector3[] vertices;
        Vector2[] uvs;
        Color32[] colors;
        bool visualsHidden;
        bool fadeSettled;

        public static StormDirector Ensure()
        {
            if (instance != null)
                return instance;
            var existing = FindAnyObjectByType<StormDirector>();
            if (existing != null)
                return existing;
            var host = new GameObject("Storm");
            return host.AddComponent<StormDirector>();
        }

#if UNITY_EDITOR
        public bool ShotFadeIdle => fade != null && !fade.Active;
#endif

        public void Resume()
        {
            fade.Resume();
            musicFade.Resume();
            sim.ResetMotion();
            visualsHidden = false;
            fadeSettled = false;
            if (theme.clip == null)
                theme.clip = FrontArt.TitleTheme();
            theme.loop = true;
            theme.volume = SessionSettings.Music;
            if (theme.clip != null && !theme.isPlaying)
                theme.Play();
        }

        public void BeginFade()
        {
            HideVisuals();
            if (!(fade.Active || fade.Finished))
                fade.Begin(1f, FadeSeconds);
            if (!(musicFade.Active || musicFade.Finished))
            {
                if (theme.clip == null)
                    theme.clip = FrontArt.TitleTheme();
                if (theme.clip != null && !theme.isPlaying)
                    theme.Play();
                musicFade.Begin(theme.volume, MusicFadeSeconds);
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                // Destroy waits until end of frame. LateUpdate must not run on the spare.
                enabled = false;
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            fade = new StormFade();
            musicFade = new StormFade();
            sim = new SandBedSim(System.Environment.TickCount);
            BuildRig();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Gate.TitleScene)
                return;

            // Sand is title-only. A second Begin does not restart the taper.
            BeginFade();
        }

        void LateUpdate()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                return;
            }

            // A domain reload clears these fields and may not run Awake again.
            if (fade == null || musicFade == null || sim == null)
                Recover();
            if (fade == null)
                return;

            float step = Mathf.Min(Time.unscaledDeltaTime, FadeStepCap);
            if (fade.Active)
                fade.Tick(step);
            TickTheme(step);

            if (fade.Finished)
            {
                if (fadeSettled)
                    return;
                HideVisuals();
                fadeSettled = true;
                return;
            }

            bool onTitle = SceneManager.GetActiveScene().name == Gate.TitleScene;
            if (fade.Active || !onTitle)
            {
                HideVisuals();
                return;
            }

            sim.Tick(Time.unscaledDeltaTime);
            Draw(1f);
            grains.enabled = true;
            stormCamera.enabled = true;
            Attach();
        }

        void TickTheme(float step)
        {
            if (theme == null)
                return;
            if (musicFade.Active)
                musicFade.Tick(step);
            if (musicFade.Finished)
            {
                if (theme.isPlaying)
                    theme.Stop();
                theme.volume = 0f;
                return;
            }

            if (musicFade.Active)
                theme.volume = musicFade.AudioAmplitude;
            else if (theme.isPlaying)
                theme.volume = SessionSettings.Music;
        }

        void HideVisuals()
        {
            grains.enabled = false;
            stormCamera.enabled = false;
            if (visualsHidden)
                return;
            visualsHidden = true;
            Detach();
        }

        void Recover()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            fade = new StormFade();
            musicFade = new StormFade();
            if (sim == null)
                sim = new SandBedSim(System.Environment.TickCount);
            ClearRig();
            BuildRig();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void ClearRig()
        {
            Camera[] cameras = GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].name == "StormCamera")
                    Destroy(cameras[i].gameObject);
            }

            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].name == "SandGrains")
                    Destroy(renderers[i].gameObject);
            }

            AudioSource[] sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    Destroy(sources[i]);
            }

            stormCamera = null;
            grains = null;
            mesh = null;
            theme = null;
            vertices = null;
        }

        void BuildRig()
        {
            var cameraObject = new GameObject("StormCamera");
            cameraObject.transform.SetParent(transform, false);
            stormCamera = cameraObject.AddComponent<Camera>();
            stormCamera.orthographic = true;
            stormCamera.orthographicSize = SandBedSim.ViewH * 0.5f;
            stormCamera.clearFlags = CameraClearFlags.Depth;
            stormCamera.nearClipPlane = 0.3f;
            stormCamera.farClipPlane = 40f;
            stormCamera.depth = 50f;
            stormCamera.allowMSAA = false;
            stormCamera.enabled = false;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Overlay;
            cameraData.renderShadows = false;

            theme = gameObject.AddComponent<AudioSource>();
            theme.playOnAwake = false;
            theme.loop = true;
            theme.spatialBlend = 0f;
            theme.dopplerLevel = 0f;
            theme.volume = SessionSettings.Music;

            int quads = sim.GrainCount;
            vertices = new Vector3[quads * 4];
            uvs = new Vector2[quads * 4];
            colors = new Color32[quads * 4];
            var triangles = new int[quads * 6];
            for (int i = 0; i < quads; i++)
            {
                int v = i * 4;
                int t = i * 6;
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(1f, 1f);
                uvs[v + 3] = new Vector2(0f, 1f);
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            mesh = new Mesh();
            mesh.name = "SandGrains";
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.triangles = triangles;

            var grainObject = new GameObject("SandGrains");
            grainObject.transform.SetParent(transform, false);
            var filter = grainObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            grains = grainObject.AddComponent<MeshRenderer>();
            grains.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            grains.receiveShadows = false;
            grains.sharedMaterial = FrontArt.SandMaterial();
            grains.enabled = false;
        }

        void Draw(float visual)
        {
            float aspect = stormCamera.aspect;
            if (aspect < 0.05f)
                aspect = 16f / 9f;
            float halfHeight = SandBedSim.ViewH * 0.5f;
            float halfWidth = halfHeight * aspect;
            var origin = new Vector3(50000f, 50000f, 0f);
            stormCamera.transform.position = new Vector3(origin.x + halfWidth, origin.y + halfHeight, -10f);
            sim.Write(vertices, colors, origin, halfWidth, halfHeight, visual);
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
        }

        void Attach()
        {
            var basis = Camera.main;
            if (basis == null || basis == stormCamera)
                return;
            var mine = stormCamera.GetComponent<UniversalAdditionalCameraData>();
            mine.renderType = CameraRenderType.Overlay;
            var baseData = basis.GetComponent<UniversalAdditionalCameraData>();
            if (baseData == null)
                baseData = basis.gameObject.AddComponent<UniversalAdditionalCameraData>();
            if (baseData.renderType == CameraRenderType.Overlay)
                return;
            if (!baseData.cameraStack.Contains(stormCamera))
                baseData.cameraStack.Add(stormCamera);
        }

        void Detach()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == stormCamera)
                    continue;
                var data = cameras[i].GetComponent<UniversalAdditionalCameraData>();
                if (data == null || data.renderType != CameraRenderType.Base)
                    continue;
                var stack = data.cameraStack;
                if (stack != null && stack.Contains(stormCamera))
                    stack.Remove(stormCamera);
            }
        }
    }
}
