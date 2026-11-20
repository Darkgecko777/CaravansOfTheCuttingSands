using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    // Sizes are points at the 1920×1080 reference. Gold is for a header or a selected line.
    public static class UiType
    {
        public const float Small = 22f;
        public const float LogRow = 24f;
        public const float Price = 24f;
        public const float Tab = 26f;
        public const float Trade = 26f;
        public const float Body = 27f;
        public const float DetailName = 28f;
        public const float Hud = 30f;
        public const float Header = 38f;
        public const float Bar = 76f;
        public const float Space = 16f;
        public const float Gap = 24f;
        public const float SpaceLarge = 32f;
        public const float BoneEdge = 38f;
        public const float WoodEdge = 40f;
        public const float CardMinW = 520f;
        public const float CardMinH = 260f;
        public static readonly Color Cream = new Color(0.918f, 0.863f, 0.753f, 1f);
        public static readonly Color Umber = new Color(0.45f, 0.28f, 0.16f, 1f);
        public static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color Rod = new Color(0.86f, 0.72f, 0.42f, 1f);
        public static readonly Color Shade = new Color(0.08f, 0.05f, 0.02f, 0.9f);

        // Sliced border in reference pixels is sprite.border / (pixelsPerUnit / 100) / multiplier.
        public static float Tuck(Sprite sprite, float edgePx)
        {
            if (sprite == null || edgePx <= 0f)
                return 1f;
            float unit = Mathf.Max(sprite.pixelsPerUnit / 100f, 0.0001f);
            float raw = Mathf.Max(sprite.border.x, sprite.border.y) / unit;
            if (raw <= edgePx)
                return 1f;
            return raw / edgePx;
        }
    }

    public static class FrontUi
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
            RectTransform parent, string text, float size, bool title, Color color, Vector2 preferred, bool wrap = false, bool display = false)
        {
            var host = Box(parent, "Label", preferred);
            var element = host.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferred.x;
            element.preferredHeight = preferred.y;

            var font = display ? FrontArt.Display() : title ? FrontArt.Bold() : FrontArt.Regular();
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

        public static Image WoodSheet(RectTransform rect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            Sprite wood = FrontArt.WoodPanel();
            image.sprite = wood;
            image.type = wood != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = wood != null ? Color.white : new Color(0.28f, 0.18f, 0.1f, 0.94f);
            image.raycastTarget = true;
            if (wood != null)
                image.pixelsPerUnitMultiplier = UiType.Tuck(wood, UiType.WoodEdge);
            return image;
        }

        public static MenuRow MenuItem(
            RectTransform parent, string text, bool enabled, UnityAction click, float width, float height)
        {
            var rect = Box(parent, text, new Vector2(width, height));
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            hit.raycastTarget = true;

            var hoverGo = Box(rect, "Hover", Vector2.zero);
            Stretch(hoverGo, Vector2.zero);
            var hover = hoverGo.gameObject.AddComponent<Image>();
            Sprite strip = FrontArt.HoverStrip();
            hover.sprite = strip;
            hover.type = strip != null ? Image.Type.Sliced : Image.Type.Simple;
            hover.raycastTarget = false;
            hover.enabled = false;
            hoverGo.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 12, 2, 2);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var tickGo = Box(rect, "Tick", new Vector2(36f, 36f));
            var tickFit = tickGo.gameObject.AddComponent<LayoutElement>();
            tickFit.preferredWidth = 36f;
            tickFit.preferredHeight = 36f;
            var tick = tickGo.gameObject.AddComponent<Image>();
            tick.sprite = FrontArt.BoneTick();
            tick.type = Image.Type.Simple;
            tick.preserveAspect = true;
            tick.raycastTarget = false;
            tick.color = new Color(1f, 1f, 1f, 0f);

            var host = Box(rect, "Label", new Vector2(width - 56f, height));
            var hostFit = host.gameObject.AddComponent<LayoutElement>();
            hostFit.flexibleWidth = 1f;
            hostFit.preferredHeight = height;
            var font = FrontArt.Display();
            var shadow = MakeText(host, "Shadow", text, UiType.Body, font, Shadow, false);
            Stretch(shadow.rectTransform, new Vector2(2f, -2f));
            var label = MakeText(host, "Text", text, UiType.Body, font, enabled ? Gold : Disabled, false);
            Stretch(label.rectTransform, Vector2.zero);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.interactable = enabled;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            var menu = rect.gameObject.AddComponent<MenuRow>();
            menu.Bind(button, label, shadow, hover, tick, enabled);
            if (click != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null)
                        EventSystem.current.SetSelectedGameObject(rect.gameObject);
                    click();
                });
            }

            return menu;
        }

        // One bone edge over a dark hide sheet. The hide rim is tucked under the bone.
        public static Image FrameSheet(RectTransform rect)
        {
            var sheet = rect.gameObject.AddComponent<Image>();
            Sprite hide = FrontArt.HidePanel();
            sheet.sprite = hide;
            sheet.type = hide != null ? Image.Type.Sliced : Image.Type.Simple;
            sheet.color = Color.white;
            sheet.raycastTarget = true;
            if (hide != null)
                sheet.pixelsPerUnitMultiplier = UiType.Tuck(hide, 28f);

            var boneGo = new GameObject("Bone", typeof(RectTransform));
            boneGo.transform.SetParent(rect, false);
            var boneRt = boneGo.GetComponent<RectTransform>();
            boneRt.anchorMin = Vector2.zero;
            boneRt.anchorMax = Vector2.one;
            boneRt.offsetMin = Vector2.zero;
            boneRt.offsetMax = Vector2.zero;
            var bone = boneGo.AddComponent<Image>();
            Sprite frame = FrontArt.BoneFrame();
            bone.sprite = frame;
            bone.type = frame != null ? Image.Type.Sliced : Image.Type.Simple;
            bone.fillCenter = false;
            bone.raycastTarget = false;
            bone.color = Color.white;
            bone.pixelsPerUnitMultiplier = UiType.Tuck(frame, UiType.BoneEdge);
            boneGo.AddComponent<LayoutElement>().ignoreLayout = true;
            return sheet;
        }
    }

    public sealed class MenuRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        TextMeshProUGUI label;
        TextMeshProUGUI shadow;
        Image hover;
        Image tick;
        Button button;
        bool live;
        bool over;
        bool pressed;
        bool selected;
        bool marked;

        public Button Button => button;

        public void Bind(Button host, TextMeshProUGUI text, TextMeshProUGUI shade, Image strip, Image bone, bool enabled)
        {
            button = host;
            label = text;
            shadow = shade;
            hover = strip;
            tick = bone;
            live = enabled;
            Paint();
        }

        public void SetText(string text)
        {
            if (label != null)
                label.text = text;
            if (shadow != null)
                shadow.text = text;
        }

        public void SetChecked(bool on)
        {
            marked = on;
            Paint();
        }

        void Update()
        {
            bool enabled = button == null || button.interactable;
            if (enabled == live)
                return;
            live = enabled;
            if (!live)
            {
                over = false;
                pressed = false;
            }

            Paint();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!live)
                return;
            over = true;
            Paint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            over = false;
            pressed = false;
            Paint();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!live)
                return;
            pressed = true;
            Paint();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            Paint();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Paint();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Paint();
        }

        void Paint()
        {
            if (hover != null)
                hover.enabled = live && over;
            if (tick != null)
                tick.color = live && (selected || marked) ? Color.white : new Color(1f, 1f, 1f, 0f);
            if (label == null)
                return;
            if (!live)
                label.color = FrontUi.Disabled;
            else if (pressed)
                label.color = FrontUi.GoldDim;
            else if (over)
                label.color = FrontUi.GoldBright;
            else
                label.color = FrontUi.Gold;
        }
    }

    public sealed class OptionsSheet
    {
        static readonly int[] ResW = { 1280, 1600, 1920 };
        static readonly int[] ResH = { 720, 900, 1080 };

        MenuRow fullscreen;
        MenuRow resolution;
        MenuRow introAgain;

        public void Build(RectTransform frame, UnityAction close)
        {
            var layout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 48);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            FrontUi.Label(frame, "Options", UiType.Header, false, FrontUi.Gold, new Vector2(520f, 56f), false, true);
            FrontUi.Label(frame, "Master", UiType.Body, false, UiType.Cream, new Vector2(520f, 32f), false, true);
            VolumeSlider(frame, SessionSettings.Master, SessionSettings.SetMaster);
            FrontUi.Label(frame, "Music", UiType.Body, false, UiType.Cream, new Vector2(520f, 32f), false, true);
            VolumeSlider(frame, SessionSettings.Music, SessionSettings.SetMusic);
            FrontUi.Label(frame, "SFX", UiType.Body, false, UiType.Cream, new Vector2(520f, 32f), false, true);
            VolumeSlider(frame, SessionSettings.Sfx, SessionSettings.SetSfx);
            fullscreen = FrontUi.MenuItem(frame, FullscreenLine(), true, ToggleFullscreen, 520f, 52f);
            resolution = FrontUi.MenuItem(frame, ResolutionLine(), true, CycleResolution, 520f, 52f);
            introAgain = FrontUi.MenuItem(frame, IntroLine(), true, ToggleIntroAgain, 520f, 52f);
            FrontUi.MenuItem(frame, "CLOSE", true, close, 520f, 52f);
            Refresh();
        }

        public void Refresh()
        {
            if (fullscreen != null)
            {
                fullscreen.SetText(FullscreenLine());
                fullscreen.SetChecked(Screen.fullScreen);
            }

            if (resolution != null)
                resolution.SetText(ResolutionLine());
            if (introAgain != null)
            {
                introAgain.SetText(IntroLine());
                introAgain.SetChecked(SessionSettings.ShowIntroAgain);
            }
        }

        static string FullscreenLine()
        {
            return Screen.fullScreen ? "Fullscreen: On" : "Fullscreen: Off";
        }

        static string ResolutionLine()
        {
            return Screen.width + " \u00d7 " + Screen.height;
        }

        static string IntroLine()
        {
            return SessionSettings.ShowIntroAgain ? "Show intro again: On" : "Show intro again: Off";
        }

        void ToggleIntroAgain()
        {
            SessionSettings.SetShowIntroAgain(!SessionSettings.ShowIntroAgain);
            Refresh();
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            Refresh();
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
            if (resolution != null)
                resolution.SetText(ResW[next] + " \u00d7 " + ResH[next]);
        }

        static Slider VolumeSlider(RectTransform parent, float value, UnityAction<float> changed)
        {
            var row = FrontUi.Box(parent, "Volume", new Vector2(520f, 28f));
            var fit = row.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 520f;
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
    }

    // The four intro lines live here so a later tone pass edits one place.
    public static class IntroCardCopy
    {
        public const string Header = "Kharûn, at the edge of the Cutting Sands.";
        public const string Body = "Your House has given you one wagon, a few agents and a purse of scrubstone. Buy where goods are cheap, cross the sands, and sell where they're dear. Watch your water and rations, because the desert takes payment from anyone who forgets them. Prices change as towns run short or grow full, and your logbook remembers what you've seen.";
        public const string Tagline = "Every road has a price.";
        public const string Begin = "BEGIN";
    }

    // Master rides on the listener. Music scales the title theme. SFX scales goods clicks.
    public static class SessionSettings
    {
        const string MasterKey = "caravans.vol.master";
        const string MusicKey = "caravans.vol.music";
        const string SfxKey = "caravans.vol.sfx";
        public const string IntroSeenKey = "caravans.intro.seen";
        public const string IntroShowAgainKey = "caravans.intro.showAgain";

        public static float Master { get; private set; } = 1f;
        public static float Music { get; private set; } = 1f;
        public static float Sfx { get; private set; } = 1f;
        public static bool IntroSeen { get; private set; }
        public static bool ShowIntroAgain { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
            Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
            Sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            AudioListener.volume = Master;
            LoadIntro();
        }

        public static void LoadIntro()
        {
            IntroSeen = PlayerPrefs.GetInt(IntroSeenKey, 0) != 0;
            ShowIntroAgain = PlayerPrefs.GetInt(IntroShowAgainKey, 0) != 0;
        }

        public static bool ShouldShowIntro()
        {
            return !IntroSeen || ShowIntroAgain;
        }

        public static void MarkIntroSeen()
        {
            IntroSeen = true;
            ShowIntroAgain = false;
            PlayerPrefs.SetInt(IntroSeenKey, 1);
            PlayerPrefs.SetInt(IntroShowAgainKey, 0);
        }

        public static void SetShowIntroAgain(bool show)
        {
            ShowIntroAgain = show;
            PlayerPrefs.SetInt(IntroShowAgainKey, show ? 1 : 0);
        }

        public static void SetMaster(float value)
        {
            Master = Mathf.Clamp01(value);
            AudioListener.volume = Master;
            PlayerPrefs.SetFloat(MasterKey, Master);
        }

        public static void SetMusic(float value)
        {
            Music = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, Music);
        }

        public static void SetSfx(float value)
        {
            Sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, Sfx);
        }
    }
}
