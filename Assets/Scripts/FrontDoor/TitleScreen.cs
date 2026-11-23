using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    public sealed class TitleScreen : MonoBehaviour
    {
        RectTransform menuColumn;
        RectTransform menuFrame;
        GameObject optionsRoot;
        GameObject confirmRoot;
        Button newGame;
        Button continueRow;
        Button confirmBack;
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

            menuColumn = FrontUi.Box(canvas, "Column", new Vector2(1400f, 760f));
            var column = menuColumn;
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

            BuildMenu(SaveSlot.HasValidSave());
            BuildOptions(canvas);
            BuildConfirm(canvas);
            SelectDefault();
            StormDirector.Ensure().Resume();
        }

        void BuildMenu(bool hasSave)
        {
            if (menuFrame != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(menuFrame.gameObject);
#else
                Destroy(menuFrame.gameObject);
#endif
            }

            continueRow = null;
            const float itemWidth = 440f;
            const float itemHeight = 64f;
            const float pad = 48f;
            const float gap = 12f;
            int rows = hasSave ? 4 : 3;
            float frameWidth = itemWidth + pad * 2f;
            float frameHeight = itemHeight * rows + gap * (rows - 1) + pad * 2f;
            menuFrame = FrontUi.Box(menuColumn, "MenuFrame", new Vector2(frameWidth, frameHeight));
            var frameElement = menuFrame.gameObject.AddComponent<LayoutElement>();
            frameElement.minWidth = frameWidth;
            frameElement.preferredWidth = frameWidth;
            frameElement.flexibleWidth = 0f;
            frameElement.minHeight = frameHeight;
            frameElement.preferredHeight = frameHeight;
            frameElement.flexibleHeight = 0f;
            FrontUi.WoodSheet(menuFrame);
            var frameLayout = menuFrame.gameObject.AddComponent<VerticalLayoutGroup>();
            frameLayout.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            frameLayout.spacing = gap;
            frameLayout.childAlignment = TextAnchor.MiddleCenter;
            frameLayout.childControlWidth = true;
            frameLayout.childControlHeight = true;
            frameLayout.childForceExpandWidth = false;
            frameLayout.childForceExpandHeight = false;

            if (hasSave)
                continueRow = FrontUi.MenuItem(menuFrame, "CONTINUE", true, Gate.ContinueRun, itemWidth, itemHeight).Button;
            newGame = FrontUi.MenuItem(menuFrame, "NEW GAME", true, StartNewGame, itemWidth, itemHeight).Button;
            FrontUi.MenuItem(menuFrame, "OPTIONS", true, OpenOptions, itemWidth, itemHeight);
            FrontUi.MenuItem(menuFrame, "EXIT", true, Quit, itemWidth, itemHeight);
        }

        void SelectDefault()
        {
            Button row = continueRow != null ? continueRow : newGame;
            if (EventSystem.current != null && row != null)
                EventSystem.current.SetSelectedGameObject(row.gameObject);
        }

        void StartNewGame()
        {
            if (continueRow != null)
                OpenConfirm();
            else
                Gate.ConfirmRun();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;
            if (confirmRoot != null && confirmRoot.activeSelf)
            {
                CloseConfirm();
                return;
            }

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

            var frame = FrontUi.Box(root, "MenuFrame", new Vector2(680f, 760f));
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            FrontUi.WoodSheet(frame);
            options = new OptionsSheet();
            options.Build(frame, CloseOptions);

            optionsRoot = root.gameObject;
            optionsRoot.SetActive(false);
        }

        void BuildConfirm(RectTransform canvas)
        {
            var root = FrontUi.Box(canvas, "Confirm", new Vector2(1920f, 1080f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = UiType.Backdrop;
            backdrop.raycastTarget = true;

            const float contentWidth = 520f;
            const float pad = 48f;
            const float gap = 12f;
            const float itemHeight = 64f;
            float frameWidth = contentWidth + pad * 2f;
            float frameHeight = 52f + 44f + itemHeight * 2f + gap * 3f + pad * 2f;
            var frame = FrontUi.Box(root, "MenuFrame", new Vector2(frameWidth, frameHeight));
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            FrontUi.WoodSheet(frame);
            var layout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            layout.spacing = gap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            FrontUi.Label(frame, "Start a new run?", UiType.Header, false, UiType.Cream, new Vector2(contentWidth, 52f), false, true);
            FrontUi.Label(frame, "This overwrites your saved run.", UiType.Body, false, UiType.Cream, new Vector2(contentWidth, 44f));
            FrontUi.MenuItem(frame, "NEW GAME", true, ConfirmNewGame, contentWidth, itemHeight);
            confirmBack = FrontUi.MenuItem(frame, "BACK", true, CloseConfirm, contentWidth, itemHeight).Button;
            confirmRoot = root.gameObject;
            confirmRoot.SetActive(false);
        }

        void OpenConfirm()
        {
            if (confirmRoot == null)
                return;
            confirmRoot.SetActive(true);
            if (EventSystem.current != null && confirmBack != null)
                EventSystem.current.SetSelectedGameObject(confirmBack.gameObject);
        }

        void CloseConfirm()
        {
            if (confirmRoot != null)
                confirmRoot.SetActive(false);
            if (EventSystem.current != null && newGame != null)
                EventSystem.current.SetSelectedGameObject(newGame.gameObject);
        }

        void ConfirmNewGame()
        {
            SaveSlot.Clear();
            Gate.ConfirmRun();
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
            SelectDefault();
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
        public bool ShotReady => optionsRoot != null && confirmRoot != null && menuFrame != null;

        public void ShotOptions(bool open)
        {
            if (optionsRoot == null)
                return;
            if (open && options != null)
                options.Refresh();
            optionsRoot.SetActive(open);
        }

        public void ShotSaveState(bool hasSave)
        {
            if (menuColumn == null)
                return;
            BuildMenu(hasSave);
            if (confirmRoot != null)
                confirmRoot.SetActive(false);
            SelectDefault();
            Canvas.ForceUpdateCanvases();
            if (menuFrame != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(menuFrame);
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuColumn);
        }

        public void ShotConfirm(bool open)
        {
            if (confirmRoot == null)
                return;
            if (open)
                OpenConfirm();
            else
                confirmRoot.SetActive(false);
        }
#endif
    }
}
