using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    static class FrontUi
    {
        public static readonly Color Gold = new Color(0.91f, 0.78f, 0.42f, 1f);
        public static readonly Color GoldBright = new Color(0.98f, 0.88f, 0.55f, 1f);
        public static readonly Color GoldDim = new Color(0.75f, 0.62f, 0.32f, 1f);
        public static readonly Color TitleGold = new Color(0.95f, 0.85f, 0.55f, 1f);
        public static readonly Color Subtitle = new Color(0.85f, 0.72f, 0.5f, 1f);
        public static readonly Color HouseTitle = new Color(0.92f, 0.78f, 0.45f, 1f);
        public static readonly Color HouseSubtitle = new Color(0.7f, 0.55f, 0.35f, 1f);
        public static readonly Color Shadow = new Color(0.08f, 0.05f, 0.02f, 0.85f);
        public static readonly Color Disabled = new Color(0.55f, 0.5f, 0.42f, 0.85f);
        public static readonly Color Available = new Color(0.55f, 0.85f, 0.5f, 1f);
        public static readonly Color Locked = new Color(0.55f, 0.4f, 0.3f, 1f);
        public static readonly Color LockedMark = new Color(0.4f, 0.35f, 0.3f, 1f);
        public static readonly Color LockedName = new Color(0.45f, 0.4f, 0.35f, 1f);
        public static readonly Color LockedFlavour = new Color(0.35f, 0.32f, 0.28f, 1f);
        public static readonly Color Bone = new Color(0.9f, 0.85f, 0.75f, 1f);
        public static readonly Color Flavour = new Color(0.7f, 0.6f, 0.45f, 1f);
        public static readonly Color Back = new Color(0.65f, 0.55f, 0.4f, 1f);

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var host = new GameObject("EventSystem");
            host.AddComponent<EventSystem>();
            var module = host.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static RectTransform Canvas(string name, int sort)
        {
            var host = new GameObject(name, typeof(RectTransform));
            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sort;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            host.AddComponent<GraphicRaycaster>();
            return host.GetComponent<RectTransform>();
        }

        public static RectTransform Box(RectTransform parent, string name, Vector2 size)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        public static TextMeshProUGUI Label(
            RectTransform parent, string text, float size, bool title, Color color, Vector2 preferred, bool wrap = false)
        {
            var host = Box(parent, "Label", preferred);
            var element = host.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferred.x;
            element.preferredHeight = preferred.y;

            var font = title ? FrontArt.Bold() : FrontArt.Regular();
            var shadow = MakeText(host, "Shadow", text, size, font, Shadow, wrap);
            Stretch(shadow.rectTransform, new Vector2(2f, -2f));
            var label = MakeText(host, "Text", text, size, font, color, wrap);
            Stretch(label.rectTransform, Vector2.zero);
            return label;
        }

        static TextMeshProUGUI MakeText(
            RectTransform parent, string name, string text, float size, TMP_FontAsset font, Color color, bool wrap)
        {
            var rect = Box(parent, name, Vector2.zero);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        static void Stretch(RectTransform rect, Vector2 shift)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = shift;
            rect.offsetMax = shift;
        }

        public static Button MenuButton(RectTransform parent, string text, bool enabled, UnityEngine.Events.UnityAction click)
        {
            var rect = Box(parent, text, new Vector2(280f, 84f));
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 280f;
            element.preferredHeight = 84f;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = FrontArt.ButtonPanel();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.28f, 0.18f, 0.1f, 0.94f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = enabled;
            if (click != null)
                button.onClick.AddListener(click);
            var label = Label(rect, text, 28f, true, enabled ? Gold : Disabled, new Vector2(280f, 84f));
            var host = (RectTransform)label.transform.parent;
            host.anchorMin = Vector2.zero;
            host.anchorMax = Vector2.one;
            host.offsetMin = Vector2.zero;
            host.offsetMax = Vector2.zero;
            var hostElement = host.GetComponent<LayoutElement>();
            if (hostElement != null)
                hostElement.ignoreLayout = true;
            rect.gameObject.AddComponent<PlatePress>().Bind(label);
            return button;
        }
    }

    sealed class PlatePress : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        TextMeshProUGUI label;
        Button button;
        bool live;
        Vector3 target = Vector3.one;

        public void Bind(TextMeshProUGUI text)
        {
            label = text;
            button = GetComponent<Button>();
            live = button == null || button.interactable;
        }

        void Update()
        {
            bool enabled = button == null || button.interactable;
            if (enabled != live)
            {
                live = enabled;
                target = Vector3.one;
                if (label != null)
                    label.color = live ? FrontUi.Gold : FrontUi.Disabled;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!live)
                return;
            target = Vector3.one * 1.05f;
            if (label != null)
                label.color = FrontUi.GoldBright;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            target = Vector3.one;
            if (label != null && live)
                label.color = FrontUi.Gold;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!live)
                return;
            target = Vector3.one * 0.96f;
            if (label != null)
                label.color = FrontUi.GoldDim;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!live)
                return;
            bool over = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, eventData.position, eventData.pressEventCamera);
            target = over ? Vector3.one * 1.05f : Vector3.one;
            if (label != null)
                label.color = over ? FrontUi.GoldBright : FrontUi.Gold;
        }
    }
}
