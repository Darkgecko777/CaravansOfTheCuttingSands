using System.Collections.Generic;
using Caravans.FrontDoor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Caravans.Play
{
    // Review bench for asset sets. Market plays buy sounds. It does not buy or sell.
    public sealed class CargoSoundScreen : MonoBehaviour
    {
        const int Columns = 8;
        const float Header = 52f;
        const float NameBand = 0.30f;

        static readonly Color Ink = new Color(0.95f, 0.89f, 0.78f, 1f);
        static readonly Color Quiet = new Color(0.78f, 0.7f, 0.58f, 1f);
        static readonly Color Panel = new Color(0.2f, 0.15f, 0.11f, 0.94f);
        static readonly Color ButtonFace = new Color(0.45f, 0.32f, 0.22f, 1f);
        static readonly Color ButtonHot = new Color(0.66f, 0.46f, 0.28f, 1f);
        static readonly Color ButtonDown = new Color(0.3f, 0.2f, 0.14f, 1f);
        static readonly Color Field = new Color(0.28f, 0.21f, 0.16f, 1f);
        static readonly Color Rim = new Color(0.16f, 0.12f, 0.10f, 1f);

        enum Page { Market, FrontDoor, Map, Mounts }

        TMP_FontAsset font;
        Sprite quad;
        RectTransform cells;
        GridLayoutGroup grid;
        TextMeshProUGUI heard;
        Image[] rings;
        GoodId[] goods;
        GameObject marketPage;
        GameObject doorPage;
        GameObject mapPage;
        GameObject mountsPage;
        Button marketTab;
        Button doorTab;
        Button mapTab;
        Button mountsTab;
        Button saleButton;
        Button windButton;
        Page page = Page.Market;
        int selected = -1;
        bool fitted;

        void Awake()
        {
            EnsureEventSystem();
            quad = WhiteSprite();
            font = MakeFont();
            MarketSounds.Bind(gameObject);
            Build();
        }

        void Start()
        {
            fitted = FitCells();
        }

        void LateUpdate()
        {
            if (fitted)
                return;
            fitted = FitCells();
        }

        void Build()
        {
            var canvasGo = new GameObject("CargoSoundCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.GetComponent<RectTransform>();

            var frame = Child(root, "ShopFrame");
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = new Vector2(12f, 8f);
            frame.offsetMax = new Vector2(-12f, -12f);
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = quad;
            frameImage.color = Panel;
            frameImage.raycastTarget = false;

            var head = Child(frame, "Head");
            head.anchorMin = new Vector2(0f, 1f);
            head.anchorMax = Vector2.one;
            head.pivot = new Vector2(0.5f, 1f);
            head.sizeDelta = new Vector2(0f, Header);
            head.anchoredPosition = Vector2.zero;
            var row = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.padding = new RectOffset(8, 8, 6, 6);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var back = Tab(head, "Back", Gate.ShowTitle);
            back.GetComponent<LayoutElement>().preferredWidth = 120f;
            marketTab = Tab(head, "Market", () => Show(Page.Market));
            marketTab.GetComponent<LayoutElement>().preferredWidth = 150f;
            doorTab = Tab(head, "Front door", () => Show(Page.FrontDoor));
            doorTab.GetComponent<LayoutElement>().preferredWidth = 190f;
            mapTab = Tab(head, "Map", () => Show(Page.Map));
            mapTab.GetComponent<LayoutElement>().preferredWidth = 120f;
            mountsTab = Tab(head, "Mounts", () => Show(Page.Mounts));
            mountsTab.GetComponent<LayoutElement>().preferredWidth = 150f;

            var spacer = Child(head, "Spacer");
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            saleButton = Tab(head, "Sale", PressSale);
            saleButton.GetComponent<LayoutElement>().preferredWidth = 120f;
            windButton = Tab(head, "Wind", PlayWind);
            windButton.GetComponent<LayoutElement>().preferredWidth = 120f;

            heard = Body(head, 20f, TextAlignmentOptions.MidlineRight);
            heard.color = Quiet;
            heard.textWrappingMode = TextWrappingModes.NoWrap;
            heard.overflowMode = TextOverflowModes.Overflow;
            var heardElement = heard.gameObject.AddComponent<LayoutElement>();
            heardElement.preferredWidth = 420f;

            marketPage = BuildMarket(frame);
            doorPage = BuildFrontDoor(frame);
            mapPage = BuildMap(frame);
            mountsPage = BuildMounts(frame);
            Show(Page.Market);
        }

        GameObject BuildMarket(RectTransform frame)
        {
            cells = PageBody(frame, "Cells");
            grid = cells.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.cellSize = new Vector2(158f, 120f);

            var list = new List<GoodId>();
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                var good = (GoodId)i;
                if (good != GoodId.Rations)
                    list.Add(good);
            }

            goods = list.ToArray();
            rings = new Image[goods.Length];
            for (int i = 0; i < goods.Length; i++)
                rings[i] = MakeCell(goods[i], i);
            return cells.gameObject;
        }

        GameObject BuildFrontDoor(RectTransform frame)
        {
            var body = PageBody(frame, "FrontDoor");
            ShowPiece(body, "Title", 0f, 0.42f, FrontArt.TitleTexture(), null);
            ShowPiece(body, "Menu frame", 0.42f, 0.7f, null, FrontArt.MenuFrame());
            ShowPiece(body, "Button panel", 0.7f, 1f, null, FrontArt.ButtonPanel());
            return body.gameObject;
        }

        GameObject BuildMap(RectTransform frame)
        {
            var body = PageBody(frame, "Map");
            var texture = Resources.Load<Texture2D>("Map/upscale_map");
            var picture = Child(body, "Picture");
            picture.anchorMin = Vector2.zero;
            picture.anchorMax = Vector2.one;
            picture.offsetMin = new Vector2(8f, 8f);
            picture.offsetMax = new Vector2(-8f, -8f);
            var raw = picture.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            raw.color = texture != null ? Color.white : Color.clear;
            if (texture != null)
            {
                var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = texture.width / (float)Mathf.Max(texture.height, 1);
            }
            return body.gameObject;
        }

        GameObject BuildMounts(RectTransform frame)
        {
            var body = PageBody(frame, "Mounts");
            AddMount(body, "Duzhu", "Mounts/Duzhu", "Audio/mount-duzhu", "Duzhu", 0f, 0.5f);
            AddMount(body, "PincerBeast", "Mounts/PincerBeast", "Audio/mount-pincer-beast", "Pincer beast", 0.5f, 1f);
            return body.gameObject;
        }

        void AddMount(RectTransform body, string name, string texturePath, string clipPath, string caption, float x0, float x1)
        {
            var column = Child(body, name);
            column.anchorMin = new Vector2(x0, 0f);
            column.anchorMax = new Vector2(x1, 1f);
            column.offsetMin = new Vector2(8f, 8f);
            column.offsetMax = new Vector2(-8f, -8f);

            var texture = Resources.Load<Texture2D>(texturePath);
            var picture = Child(column, "Picture");
            picture.anchorMin = new Vector2(0f, 0f);
            picture.anchorMax = Vector2.one;
            picture.offsetMin = new Vector2(0f, 40f);
            picture.offsetMax = Vector2.zero;
            var raw = picture.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = true;
            raw.color = texture != null ? Color.white : Color.clear;
            var button = picture.gameObject.AddComponent<Button>();
            button.targetGraphic = raw;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            string captured = clipPath;
            button.onClick.AddListener(() => PlayMount(captured));
            if (texture != null)
            {
                var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = texture.width / (float)Mathf.Max(texture.height, 1);
            }

            var label = Body(column, 22f, TextAlignmentOptions.Bottom);
            label.text = caption;
            label.color = Ink;
            var cap = label.rectTransform;
            cap.anchorMin = Vector2.zero;
            cap.anchorMax = new Vector2(1f, 0f);
            cap.pivot = new Vector2(0.5f, 0f);
            cap.sizeDelta = new Vector2(0f, 36f);
            cap.anchoredPosition = new Vector2(0f, 4f);
        }

        void ShowPiece(RectTransform page, string name, float x0, float x1, Texture2D texture, Sprite sprite)
        {
            var column = Child(page, name);
            column.anchorMin = new Vector2(x0, 0f);
            column.anchorMax = new Vector2(x1, 1f);
            column.offsetMin = new Vector2(8f, 8f);
            column.offsetMax = new Vector2(-8f, -8f);

            var picture = Child(column, "Picture");
            picture.anchorMin = new Vector2(0f, 0f);
            picture.anchorMax = Vector2.one;
            picture.offsetMin = new Vector2(0f, 32f);
            picture.offsetMax = Vector2.zero;
            if (texture != null)
            {
                var raw = picture.gameObject.AddComponent<RawImage>();
                raw.texture = texture;
                raw.raycastTarget = false;
                var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = texture.width / (float)Mathf.Max(texture.height, 1);
            }
            else if (sprite != null)
            {
                var image = picture.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            var caption = Body(column, 18f, TextAlignmentOptions.Bottom);
            caption.text = name;
            var cap = caption.rectTransform;
            cap.anchorMin = Vector2.zero;
            cap.anchorMax = new Vector2(1f, 0f);
            cap.pivot = new Vector2(0.5f, 0f);
            cap.sizeDelta = new Vector2(0f, 28f);
            cap.anchoredPosition = Vector2.zero;
        }

        Image MakeCell(GoodId good, int index)
        {
            var rect = Child(cells, good.ToString());
            var ring = rect.gameObject.AddComponent<Image>();
            ring.sprite = quad;
            ring.color = Rim;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            int captured = index;
            button.onClick.AddListener(() => Press(captured));

            var rim = Child(rect, "Rim");
            var rimImage = rim.gameObject.AddComponent<Image>();
            rimImage.sprite = quad;
            rimImage.color = Rim;
            rimImage.raycastTarget = false;
            Stretch(rim, 4f);

            var face = Child(rect, "Face");
            var faceImage = face.gameObject.AddComponent<Image>();
            faceImage.sprite = quad;
            faceImage.color = Field;
            faceImage.raycastTarget = false;
            Stretch(face, 8f);

            var picture = Resources.Load<Texture2D>("Goods/" + good);
            var text = Body(rect, 18f, TextAlignmentOptions.Center);
            text.text = EconomyCatalog.GoodName(good);
            text.color = Ink;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 16f;
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = new Vector2(1f, NameBand);
            textRt.offsetMin = new Vector2(8f, 3f);
            textRt.offsetMax = new Vector2(-8f, -1f);
            if (picture != null)
            {
                var shot = Child(rect, "Picture");
                shot.anchorMin = new Vector2(0.12f, NameBand);
                shot.anchorMax = new Vector2(0.88f, 1f);
                shot.offsetMin = new Vector2(0f, 4f);
                shot.offsetMax = new Vector2(0f, -8f);
                var raw = shot.gameObject.AddComponent<RawImage>();
                raw.texture = picture;
                raw.raycastTarget = false;
                var fit = shot.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = picture.width / (float)Mathf.Max(picture.height, 1);
            }
            return ring;
        }

        void Press(int index)
        {
            if (selected >= 0 && selected < rings.Length)
                rings[selected].color = Rim;
            selected = index;
            rings[index].color = Ink;
            GoodId good = goods[index];
            MarketSounds.PlayBuy(good);
            heard.text = ClipName(MarketSounds.BuyResource(good));
        }

        void PressSale()
        {
            if (selected >= 0 && selected < rings.Length)
                rings[selected].color = Rim;
            selected = -1;
            MarketSounds.PlaySale();
            heard.text = ClipName(MarketSounds.SaleResource);
        }

        void PlayWind()
        {
            AudioClip clip = FrontArt.Wind();
            var source = GetComponent<AudioSource>();
            if (clip != null && source != null)
                source.PlayOneShot(clip);
            heard.text = "wind_sound";
        }

        void PlayMount(string resource)
        {
            AudioClip clip = Resources.Load<AudioClip>(resource);
            var source = GetComponent<AudioSource>();
            if (clip != null && source != null)
                source.PlayOneShot(clip);
            heard.text = ClipName(resource);
        }

        void Show(Page next)
        {
            page = next;
            marketPage.SetActive(next == Page.Market);
            doorPage.SetActive(next == Page.FrontDoor);
            mapPage.SetActive(next == Page.Map);
            mountsPage.SetActive(next == Page.Mounts);
            saleButton.gameObject.SetActive(next == Page.Market);
            windButton.gameObject.SetActive(next == Page.FrontDoor);
            Tint(marketTab, next == Page.Market);
            Tint(doorTab, next == Page.FrontDoor);
            Tint(mapTab, next == Page.Map);
            Tint(mountsTab, next == Page.Mounts);
            heard.text = "";
            if (next == Page.Market)
                fitted = FitCells();
        }

        static void Tint(Button button, bool selected)
        {
            var colors = button.colors;
            colors.normalColor = selected ? ButtonHot : ButtonFace;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        static string ClipName(string resource)
        {
            if (string.IsNullOrEmpty(resource))
                return "";
            int slash = resource.LastIndexOf('/');
            return slash < 0 ? resource : resource.Substring(slash + 1);
        }

        bool FitCells()
        {
            if (page != Page.Market || cells == null)
                return false;
            Canvas.ForceUpdateCanvases();
            float width = cells.rect.width;
            float height = cells.rect.height;
            int rows = goods.Length == 0 ? 1 : (goods.Length + Columns - 1) / Columns;
            if (width < 10f || height < 10f)
                return false;
            float cellW = (width - grid.padding.horizontal - (Columns - 1) * grid.spacing.x) / Columns;
            float cellH = (height - grid.padding.vertical - (rows - 1) * grid.spacing.y) / rows;
            if (cellW < 8f || cellH < 8f)
                return false;
            grid.cellSize = new Vector2(cellW, cellH);
            return true;
        }

        static RectTransform PageBody(RectTransform frame, string name)
        {
            var body = Child(frame, name);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(8f, 8f);
            body.offsetMax = new Vector2(-8f, -Header);
            return body;
        }

        Button Tab(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = quad;
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = ButtonFace;
            colors.highlightedColor = ButtonHot;
            colors.pressedColor = ButtonDown;
            colors.selectedColor = ButtonFace;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(action);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 40f;
            var text = Body(go.GetComponent<RectTransform>(), 20f, TextAlignmentOptions.Center);
            text.text = label;
            Stretch(text.rectTransform, 4f);
            return button;
        }

        TextMeshProUGUI Body(RectTransform parent, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        static RectTransform Child(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        TMP_FontAsset MakeFont()
        {
            Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (source == null)
                source = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return TMP_FontAsset.CreateFontAsset(source);
        }

        static Sprite WhiteSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
