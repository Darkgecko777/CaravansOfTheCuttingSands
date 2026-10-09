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
        Button tutorialButton;
        public static void Spawn()
        {
            var house = FindAnyObjectByType<HouseSelectScreen>();
            if (house != null)
            {
                house.gameObject.SetActive(false);
                Destroy(house.gameObject);
            }

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

            FrontUi.Label(column, "Caravans of the Cutting Sands", 42f, true, FrontUi.TitleGold, new Vector2(1400f, 64f));
            FrontUi.Label(column, "Buy on knowledge. Risk the journey.", 16f, false, FrontUi.Subtitle, new Vector2(1400f, 28f));

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

            newGame = FrontUi.MenuButton(buttons, "NEW GAME", true, Gate.ShowHouse);
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
            backdrop.color = new Color(0.04f, 0.03f, 0.02f, 0.55f);
            var backdropButton = root.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(CloseOptions);

            var frame = FrontUi.Box(root, "MenuFrame", new Vector2(400f, 520f));
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = FrontArt.MenuFrame();
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;
            frameImage.color = frameImage.sprite != null ? Color.white : new Color(0.42f, 0.32f, 0.2f, 0.92f);

            var column = FrontUi.Box(frame, "Buttons", new Vector2(320f, 420f));
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.anchoredPosition = Vector2.zero;
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            FrontUi.Label(column, "Options", 28f, true, FrontUi.TitleGold, new Vector2(320f, 48f));
            tutorialButton = FrontUi.MenuButton(column, TutorialCaption(), true, ToggleTutorial);
            FrontUi.MenuButton(column, "CLOSE", true, CloseOptions);

            optionsRoot = root.gameObject;
            optionsRoot.SetActive(false);
        }

        void OpenOptions()
        {
            optionsRoot.SetActive(true);
            SetCaption(tutorialButton, TutorialCaption());
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(tutorialButton.gameObject);
        }

        void CloseOptions()
        {
            optionsRoot.SetActive(false);
            if (EventSystem.current != null && newGame != null)
                EventSystem.current.SetSelectedGameObject(newGame.gameObject);
        }

        void ToggleTutorial()
        {
            TutorialOption.Set(!TutorialOption.Enabled);
            SetCaption(tutorialButton, TutorialCaption());
        }

        static string TutorialCaption()
        {
            return TutorialOption.Enabled ? "TUTORIAL ON" : "TUTORIAL OFF";
        }

        static void SetCaption(Button button, string text)
        {
            if (button == null)
                return;
            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
                labels[i].text = text;
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
    }
}
