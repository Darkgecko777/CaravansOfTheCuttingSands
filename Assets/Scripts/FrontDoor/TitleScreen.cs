using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    public sealed class TitleScreen : MonoBehaviour
    {
        GameObject optionsRoot;
        Button newGame;
        TextMeshProUGUI fullscreenLabel;
        TextMeshProUGUI resolutionLabel;

        static readonly int[] ResW = { 1280, 1600, 1920 };
        static readonly int[] ResH = { 720, 900, 1080 };
        public static void Spawn()
        {
            var existing = FindAnyObjectByType<TitleScreen>();
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            var host = new GameObject("TitleScreen");
            host.AddComponent<TitleScreen>();
        }

        void Start()
        {
            FrontUi.EnsureEventSystem();
            BuildDesert();
            var canvas = FrontUi.Canvas("TitleCanvas", 20);
            canvas.SetParent(transform, false);

            var column = FrontUi.Box(canvas, "Column", new Vector2(1400f, 760f));
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.anchoredPosition = Vector2.zero;
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ApprovedArt art = ApprovedArt.Load();
            if (art != null && art.logoTitle != null)
            {
                var logo = FrontUi.Box(column, "Logo", new Vector2(980f, 240f));
                var logoFit = logo.gameObject.AddComponent<LayoutElement>();
                logoFit.preferredWidth = 980f;
                logoFit.preferredHeight = 240f;
                var logoImage = logo.gameObject.AddComponent<Image>();
                logoImage.sprite = art.logoTitle;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;
            }
            else
            {
                FrontUi.Label(column, "Caravans of the Cutting Sands", 42f, true, FrontUi.TitleGold, new Vector2(1400f, 64f));
            }

            FrontUi.Label(column, "Every road has a price.", UiType.Small, false, FrontUi.Subtitle, new Vector2(1400f, 36f));

            var frame = FrontUi.Box(column, "MenuFrame", new Vector2(400f, 520f));
            var frameElement = frame.gameObject.AddComponent<LayoutElement>();
            frameElement.preferredWidth = 400f;
            frameElement.preferredHeight = 520f;
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = FrontArt.MenuFrame();
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;
            frameImage.color = frameImage.sprite != null ? Color.white : new Color(0.42f, 0.32f, 0.2f, 0.92f);

            var buttons = FrontUi.Box(frame, "Buttons", new Vector2(320f, 420f));
            buttons.anchorMin = buttons.anchorMax = new Vector2(0.5f, 0.5f);
            buttons.pivot = new Vector2(0.5f, 0.5f);
            buttons.anchoredPosition = Vector2.zero;
            var buttonLayout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.spacing = 16f;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;

            newGame = FrontUi.MenuButton(buttons, "NEW GAME", true, Gate.ConfirmRun);
            FrontUi.MenuButton(buttons, "CONTINUE", false, null);
            FrontUi.MenuButton(buttons, "OPTIONS", true, OpenOptions);
            FrontUi.MenuButton(buttons, "EXIT", true, Quit);
            BuildOptions(canvas);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(newGame.gameObject);

            StormDirector.Ensure().Resume();
        }

        void BuildOptions(RectTransform canvas)
        {
            var root = FrontUi.Box(canvas, "Options", new Vector2(1920f, 1080f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = UiType.Backdrop;
            var backdropButton = root.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(CloseOptions);

            var frame = FrontUi.Box(root, "MenuFrame", new Vector2(560f, 680f));
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            FrontUi.FrameSheet(frame);
            var layout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            FrontUi.Label(frame, "Options", UiType.Header, true, FrontUi.Gold, new Vector2(440f, 56f));
            FrontUi.Label(frame, "Master", UiType.Body, false, UiType.Cream, new Vector2(440f, 32f));
            VolumeSlider(frame, SessionSettings.Master, SessionSettings.SetMaster);
            FrontUi.Label(frame, "Music", UiType.Body, false, UiType.Cream, new Vector2(440f, 32f));
            VolumeSlider(frame, SessionSettings.Music, SessionSettings.SetMusic);
            FrontUi.Label(frame, "SFX", UiType.Body, false, UiType.Cream, new Vector2(440f, 32f));
            VolumeSlider(frame, SessionSettings.Sfx, SessionSettings.SetSfx);
            TextControl(frame, "Fullscreen", FullscreenLine(), ToggleFullscreen, out fullscreenLabel);
            TextControl(frame, "Resolution", ResolutionLine(), CycleResolution, out resolutionLabel);
            TextControl(frame, "CLOSE", "CLOSE", CloseOptions, out _);

            optionsRoot = root.gameObject;
            optionsRoot.SetActive(false);
        }

        static Slider VolumeSlider(RectTransform parent, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            var row = FrontUi.Box(parent, "Volume", new Vector2(420f, 28f));
            var fit = row.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 420f;
            fit.preferredHeight = 28f;
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = UiType.Umber;

            var fillGo = FrontUi.Box(row, "Fill", Vector2.zero);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(4f, 8f);
            fillRt.offsetMax = new Vector2(-4f, -8f);
            var fill = fillGo.gameObject.AddComponent<Image>();
            fill.color = UiType.Cream;

            var handleGo = FrontUi.Box(row, "Handle", new Vector2(16f, 28f));
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16f, 28f);
            var handle = handleGo.gameObject.AddComponent<Image>();
            handle.color = FrontUi.Gold;

            var slider = row.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            var nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;
            if (changed != null)
                slider.onValueChanged.AddListener(changed);
            return slider;
        }

        static Button TextControl(
            RectTransform parent, string name, string label, UnityEngine.Events.UnityAction click, out TextMeshProUGUI text)
        {
            var row = FrontUi.Box(parent, name, new Vector2(420f, 48f));
            var fit = row.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 420f;
            fit.preferredHeight = 48f;
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            if (click != null)
                button.onClick.AddListener(click);
            text = FrontUi.Label(row, label, UiType.Body, true, FrontUi.Gold, new Vector2(420f, 48f));
            var host = (RectTransform)text.transform.parent;
            host.anchorMin = Vector2.zero;
            host.anchorMax = Vector2.one;
            host.offsetMin = Vector2.zero;
            host.offsetMax = Vector2.zero;
            var hostElement = host.GetComponent<LayoutElement>();
            if (hostElement != null)
                hostElement.ignoreLayout = true;
            return button;
        }

        static string FullscreenLine()
        {
            return Screen.fullScreen ? "Fullscreen: On" : "Fullscreen: Off";
        }

        static string ResolutionLine()
        {
            return Screen.width + " \u00d7 " + Screen.height;
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (fullscreenLabel != null)
                fullscreenLabel.text = FullscreenLine();
        }

        void CycleResolution()
        {
            int next = 0;
            for (int i = 0; i < ResW.Length; i++)
            {
                if (Screen.width == ResW[i] && Screen.height == ResH[i])
                {
                    next = (i + 1) % ResW.Length;
                    break;
                }
            }

            Screen.SetResolution(ResW[next], ResH[next], Screen.fullScreen);
            if (resolutionLabel != null)
                resolutionLabel.text = ResW[next] + " \u00d7 " + ResH[next];
        }

        void OpenOptions()
        {
            optionsRoot.SetActive(true);
        }

        void CloseOptions()
        {
            optionsRoot.SetActive(false);
            if (EventSystem.current != null && newGame != null)
                EventSystem.current.SetSelectedGameObject(newGame.gameObject);
        }

        void BuildDesert()
        {
            var texture = FrontArt.TitleTexture();
            var camera = Camera.main;
            if (texture == null || camera == null)
                return;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Desert";
            quad.transform.SetParent(transform, false);
            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * Mathf.Max(camera.aspect, 0.05f);
            quad.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);
            quad.transform.localScale = new Vector3(width, height, 1f);
            var renderer = quad.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.mainTexture = texture;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            // Runtime assemblies do not reference UnityEditor. Exit play mode by reflection.
            var editor = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            editor?.GetProperty("isPlaying")?.SetValue(null, false);
#endif
        }

#if UNITY_EDITOR
        public bool ShotReady => optionsRoot != null;

        public void ShotOptions(bool open)
        {
            if (optionsRoot != null)
                optionsRoot.SetActive(open);
        }
#endif
    }
}
