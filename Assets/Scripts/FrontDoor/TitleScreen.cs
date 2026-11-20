using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    public sealed class TitleScreen : MonoBehaviour
    {
        GameObject optionsRoot;
        Button newGame;
        OptionsSheet options;

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
                FrontUi.Label(column, "Caravans of the Cutting Sands", 42f, false, FrontUi.TitleGold, new Vector2(1400f, 64f), false, true);
            }

            FrontUi.Label(column, "Every road has a price.", UiType.Small, false, FrontUi.Subtitle, new Vector2(1400f, 36f));

            var frame = FrontUi.Box(column, "MenuFrame", new Vector2(560f, 440f));
            var frameElement = frame.gameObject.AddComponent<LayoutElement>();
            frameElement.preferredWidth = 560f;
            frameElement.preferredHeight = 440f;
            FrontUi.WoodSheet(frame);
            var frameLayout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            frameLayout.padding = new RectOffset(48, 48, 48, 48);
            frameLayout.spacing = 12f;
            frameLayout.childAlignment = TextAnchor.MiddleCenter;
            frameLayout.childControlWidth = true;
            frameLayout.childControlHeight = true;
            frameLayout.childForceExpandWidth = false;
            frameLayout.childForceExpandHeight = false;

            newGame = FrontUi.MenuItem(frame, "NEW GAME", true, Gate.ConfirmRun, 440f, 64f).Button;
            FrontUi.MenuItem(frame, "OPTIONS", true, OpenOptions, 440f, 64f);
            FrontUi.MenuItem(frame, "EXIT", true, Quit, 440f, 64f);
            BuildOptions(canvas);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(newGame.gameObject);

            StormDirector.Ensure().Resume();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;
            if (optionsRoot != null && optionsRoot.activeSelf)
                CloseOptions();
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

            var frame = FrontUi.Box(root, "MenuFrame", new Vector2(680f, 680f));
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            FrontUi.WoodSheet(frame);
            options = new OptionsSheet();
            options.Build(frame, CloseOptions);

            optionsRoot = root.gameObject;
            optionsRoot.SetActive(false);
        }

        void OpenOptions()
        {
            if (options != null)
                options.Refresh();
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

        public static void Quit()
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
            if (optionsRoot == null)
                return;
            if (open && options != null)
                options.Refresh();
            optionsRoot.SetActive(open);
        }
#endif
    }
}
