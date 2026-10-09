using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Caravans.FrontDoor
{
    // Wind outlives house select and keeps tapering if the player leaves early.
    // The sand does not. It is taken down on the same frame the fade begins.
    public sealed class StormDirector : MonoBehaviour
    {
        public const float FadeSeconds = 2.2f;
        const float WindBaseDb = -12f;
        const float WindGustDb = 14f;
        const float WindPitchMin = 0.96f;
        const float WindPitchRange = 0.10f;
        const float FadeStepCap = 0.1f;

        static StormDirector instance;

        SandBedSim sim;
        StormFade fade;
        Camera stormCamera;
        AudioSource wind;
        AudioListener listener;
        Mesh mesh;
        MeshRenderer grains;
        Vector3[] vertices;
        Vector2[] uvs;
        Color32[] colors;
        bool listenersSettled;
        bool windMissingNoted;
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

        public void Resume()
        {
            fade.Resume();
            sim.ResetMotion();
            visualsHidden = false;
            fadeSettled = false;
            listenersSettled = false;
            if (wind.clip == null)
                wind.clip = FrontArt.Wind();
            wind.loop = true;
            wind.volume = DbToLinear(WindBaseDb);
            wind.pitch = WindPitchMin;
            if (wind.clip != null && !wind.isPlaying)
                wind.Play();
            if (wind.clip == null && !windMissingNoted)
            {
                windMissingNoted = true;
                Debug.LogWarning("Front door wind clip is missing. Sand still plays.");
            }
            HarmonizeListeners();
        }

        public void BeginFade()
        {
            HideVisuals();
            listenersSettled = false;
            if (!(fade.Active || fade.Finished))
            {
                if (wind.clip == null)
                    wind.clip = FrontArt.Wind();
                if (wind.clip != null && !wind.isPlaying)
                    wind.Play();
                fade.Begin(wind.volume, FadeSeconds);
            }

            HarmonizeListeners();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            fade = new StormFade();
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
            listenersSettled = false;
            if (scene.name == Gate.TitleScene)
            {
                HarmonizeListeners();
                return;
            }

            // Sand is title-only. A second Begin does not restart the taper.
            BeginFade();
        }

        void LateUpdate()
        {
            if (fade.Active)
            {
                fade.Tick(Mathf.Min(Time.unscaledDeltaTime, FadeStepCap));
                wind.volume = fade.AudioAmplitude;
            }

            if (fade.Finished)
            {
                if (fadeSettled)
                    return;
                if (wind.isPlaying)
                    wind.Stop();
                wind.volume = 0f;
                HideVisuals();
                listenersSettled = false;
                HarmonizeListeners();
                fadeSettled = true;
                return;
            }

            bool onTitle = SceneManager.GetActiveScene().name == Gate.TitleScene;
            if (fade.Active || !onTitle)
            {
                HideVisuals();
                if (!listenersSettled)
                    HarmonizeListeners();
                return;
            }

            float gust = sim.Tick(Time.unscaledDeltaTime);
            ApplyGust(gust);
            Draw(1f);
            grains.enabled = true;
            stormCamera.enabled = true;
            Attach();
            if (!listenersSettled)
                HarmonizeListeners();
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

            listener = cameraObject.AddComponent<AudioListener>();
            listener.enabled = false;

            wind = gameObject.AddComponent<AudioSource>();
            wind.playOnAwake = false;
            wind.loop = true;
            wind.spatialBlend = 0f;
            wind.dopplerLevel = 0f;
            wind.volume = DbToLinear(WindBaseDb);

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

        void ApplyGust(float gust)
        {
            gust = Mathf.Clamp01(gust);
            wind.volume = DbToLinear(WindBaseDb + gust * WindGustDb);
            wind.pitch = WindPitchMin + gust * WindPitchRange;
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

        void HarmonizeListeners()
        {
            bool useStorm = wind.isPlaying && !fade.Finished;
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            if (useStorm)
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    if (listeners[i] != listener)
                        listeners[i].enabled = false;
                }
                listener.enabled = true;
            }
            else
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    if (listeners[i] != listener)
                        listeners[i].enabled = true;
                }
                listener.enabled = false;
            }
            listenersSettled = true;
        }

        static float DbToLinear(float db)
        {
            return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }
    }
}
