using System.Collections.Generic;
using Caravans.FrontDoor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Caravans.Play
{
    public sealed class PlayPresenter : MonoBehaviour
    {
        const float TopBarHeight = 72f;
        const float ViewBarHeight = 64f;
        const float BannerHeight = 84f;
        const float TabHeight = 76f;

        static readonly Color Ink = new Color(0.95f, 0.89f, 0.78f, 1f);
        static readonly Color Quiet = new Color(0.78f, 0.7f, 0.58f, 1f);
        static readonly Color Panel = new Color(0.2f, 0.15f, 0.11f, 0.94f);
        static readonly Color Bar = new Color(0.12f, 0.09f, 0.07f, 0.96f);
        static readonly Color ButtonFace = new Color(0.45f, 0.32f, 0.22f, 1f);
        static readonly Color ButtonHot = new Color(0.66f, 0.46f, 0.28f, 1f);
        static readonly Color ButtonDown = new Color(0.3f, 0.2f, 0.14f, 1f);
        static readonly Color ButtonOff = new Color(0.24f, 0.19f, 0.16f, 0.85f);
        static readonly Color Field = new Color(0.28f, 0.21f, 0.16f, 1f);

        readonly List<Button> buttons = new List<Button>();
        readonly List<GameObject> raised = new List<GameObject>();
        readonly Dictionary<GoodId, Button> tradeButtons = new Dictionary<GoodId, Button>();
        readonly List<Button> neighborRoads = new List<Button>();
        readonly List<Image> npcTokens = new List<Image>();

        CaravanSession session;
        bool lesson;
        TMP_FontAsset font;
        Sprite quad;
        Sprite disc;
        int drawn = -1;

        enum Page { None, Market, Plaza, House }

        Page page = Page.None;
        bool selling;
        bool optionsOpen;
        bool lookingAtMap;
        bool logbookOpen;
        string listenLine = string.Empty;

        RectTransform dock;
        RectTransform travel;
        RectTransform mapRoot;
        RawImage mapImage;
        RawImage fogImage;
        Material fogMaterial;
        RectTransform token;
        RectTransform[] weatherDiscs;
        TextMeshProUGUI[] weatherLabels;
        Image dimmer;
        RectTransform callout;
        TextMeshProUGUI calloutText;
        RectTransform cardRoot;
        TextMeshProUGUI cardBody;
        TextMeshProUGUI cardResult;
        Button steelButton;
        Button kenButton;
        Button continueButton;
        RectTransform houseRoot;
        TextMeshProUGUI houseNote;
        RectTransform optionsRoot;
        Button skipButton;
        TextMeshProUGUI placeText;
        TextMeshProUGUI clockText;
        TextMeshProUGUI coinText;
        TextMeshProUGUI cargoText;
        TextMeshProUGUI bannerName;
        TextMeshProUGUI bannerTag;
        TextMeshProUGUI outlookText;
        TextMeshProUGUI listenText;
        TextMeshProUGUI legText;
        TextMeshProUGUI housePageText;
        RectTransform pinnedHost;
        RectTransform rowHost;
        RectTransform scrollRoot;
        RectTransform plazaColumn;
        TextMeshProUGUI emptySell;
        PlaceId roadsDock;
        bool roadsReady;
        Button marketTab;
        Button plazaTab;
        Button houseTab;
        Button buyTab;
        Button sellTab;
        Button roadButton;
        Button waitButton;
        Button waterButton;
        Button foodButton;
        Button listenButton;
        Button lookButton;
        Button backButton;
        Button gearButton;
        Button logbookButton;
        RectTransform logbookRoot;
        RectTransform logbookRows;
        Button pauseButton;
        Button pace1;
        Button pace2;
        Button pace4;
        GameObject marketPage;
        GameObject plazaPage;
        GameObject housePage;
        GameObject paceBar;

        void Awake()
        {
            HouseId house = HouseFromGate();
            // A new game does not enter the jars lesson.
            lesson = false;
            session = CaravanSession.AtHouse(house);
            EnsureEventSystem();
            quad = MakeSprite(false);
            disc = MakeSprite(true);
            font = MakeFont();
            Build();
            Redraw();
        }

        void Update()
        {
            EnsureMap();
            if (session.Traveling && !session.HoldTravel && !optionsOpen && !lookingAtMap)
                session.Tick(Time.unscaledDeltaTime);
            if (session.Revision != drawn)
                Redraw();
            UpdateLive();
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void Build()
        {
            var canvasGo = new GameObject("PlayCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvas.additionalShaderChannels = ExtraChannels();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.GetComponent<RectTransform>();
            Stretch(root);

            dock = Child(root, "Dock");
            FillBelowBar(dock);
            travel = Child(root, "Travel");
            FillBelowBar(travel);

            BuildDock();
            BuildTravel();
            BuildTopBar(root);
            BuildViewBar(root);
            BuildLogbook(root);
            dimmer = ImageOn(Child(root, "Dimmer"), new Color(0.05f, 0.04f, 0.03f, 0.72f), true);
            Stretch(dimmer.rectTransform);
            RaiseCanvas(dimmer.gameObject, 12);
            callout = Overlay(root, "Callout", new Vector2(460f, 180f), 40);
            calloutText = Body(callout, 22, TextAlignmentOptions.Center);
            Stretch(calloutText.rectTransform, 16f);
            BuildCard(root);
            BuildHouseChoice(root);
            BuildOptions(root);
        }

        void BuildDock()
        {
            var banner = ImageOn(Child(dock, "Banner"), new Color(0.32f, 0.23f, 0.16f, 1f), false);
            TopStrip(banner.rectTransform, BannerHeight);
            bannerName = Body(banner.rectTransform, 32, TextAlignmentOptions.Center);
            var nameRt = bannerName.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0.35f);
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            bannerTag = Body(banner.rectTransform, 16, TextAlignmentOptions.Center);
            bannerTag.color = Quiet;
            var tagRt = bannerTag.rectTransform;
            tagRt.anchorMin = Vector2.zero;
            tagRt.anchorMax = new Vector2(1f, 0.4f);
            tagRt.offsetMin = Vector2.zero;
            tagRt.offsetMax = Vector2.zero;
            bannerTag.text = "Banner to be added";

            var body = Child(dock, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(0f, TabHeight);
            body.offsetMax = new Vector2(0f, -BannerHeight);

            marketPage = BuildMarket(body);
            plazaPage = BuildPlaza(body);
            housePage = BuildHousePage(body);

            var tabs = ImageOn(Child(dock, "Tabs"), Bar, false);
            BottomStrip(tabs.rectTransform, TabHeight);
            var row = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f;
            row.padding = new RectOffset(24, 24, 12, 12);
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            marketTab = Tab(tabs.rectTransform, "Market", () => OpenMarket());
            plazaTab = Tab(tabs.rectTransform, "Plaza", () => OpenPlaza());
            houseTab = Tab(tabs.rectTransform, "House", () => OpenHouse());
        }

        GameObject BuildMarket(RectTransform body)
        {
            var page = Child(body, "Market");
            Stretch(page);
            var sub = Child(page, "SubTabs");
            TopStrip(sub, 64f);
            var layout = sub.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(28, 28, 8, 8);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            buyTab = Tab(sub, "Buy", () => { selling = false; Redraw(); });
            sellTab = Tab(sub, "Sell", () => OpenSell());
            buyTab.GetComponent<LayoutElement>().preferredWidth = 160f;
            sellTab.GetComponent<LayoutElement>().preferredWidth = 160f;

            pinnedHost = Child(page, "Pinned");
            pinnedHost.anchorMin = new Vector2(0f, 1f);
            pinnedHost.anchorMax = new Vector2(0.66f, 1f);
            pinnedHost.pivot = new Vector2(0.5f, 1f);
            pinnedHost.anchoredPosition = new Vector2(0f, -64f);
            pinnedHost.sizeDelta = new Vector2(0f, 0f);
            var pinnedRows = pinnedHost.gameObject.AddComponent<VerticalLayoutGroup>();
            pinnedRows.spacing = 8f;
            pinnedRows.padding = new RectOffset(12, 12, 8, 8);
            pinnedRows.childControlWidth = true;
            pinnedRows.childControlHeight = true;
            pinnedRows.childForceExpandWidth = true;
            pinnedRows.childForceExpandHeight = false;

            scrollRoot = Child(page, "Scroll");
            scrollRoot.anchorMin = Vector2.zero;
            scrollRoot.anchorMax = new Vector2(0.66f, 1f);
            scrollRoot.offsetMin = new Vector2(12f, 8f);
            scrollRoot.offsetMax = new Vector2(0f, -64f);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = Child(scrollRoot, "Viewport");
            Stretch(viewport);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewport;

            rowHost = Child(viewport, "Rows");
            rowHost.anchorMin = new Vector2(0f, 1f);
            rowHost.anchorMax = new Vector2(1f, 1f);
            rowHost.pivot = new Vector2(0.5f, 1f);
            rowHost.anchoredPosition = Vector2.zero;
            rowHost.sizeDelta = new Vector2(0f, 0f);
            var rows = rowHost.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 8f;
            rows.padding = new RectOffset(12, 12, 8, 8);
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = false;
            var fitter = rowHost.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rowHost;
            emptySell = Body(page, 22, TextAlignmentOptions.Center);
            var emptyRt = emptySell.rectTransform;
            emptyRt.anchorMin = Vector2.zero;
            emptyRt.anchorMax = new Vector2(0.66f, 1f);
            emptyRt.offsetMin = new Vector2(24f, 24f);
            emptyRt.offsetMax = new Vector2(-12f, -72f);
            emptySell.text = "The wagon is empty.";
            return page.gameObject;
        }

        GameObject BuildPlaza(RectTransform body)
        {
            var page = Child(body, "Plaza");
            Stretch(page);
            var column = Child(page, "Column");
            plazaColumn = column;
            column.anchorMin = new Vector2(0.5f, 0f);
            column.anchorMax = new Vector2(0.5f, 1f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(520f, 0f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 28, 28);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            roadButton = StackButton(column, "Take the road", () => session.Depart());
            waitButton = StackButton(column, "Wait an hour", () => session.WaitOneHour());
            waterButton = StackButton(column, "Fill water", () => session.TryBuy(GoodId.Water));
            foodButton = StackButton(column, "Fill food", () => session.TryBuy(GoodId.Rations));
            listenButton = StackButton(column, "Listen", () =>
            {
                listenLine = "Socialize is not in this slice.";
                Redraw();
            });
            lookButton = StackButton(column, "Look at the map", () =>
            {
                lookingAtMap = true;
                Redraw();
            });
            outlookText = Body(column, 20, TextAlignmentOptions.Center);
            outlookText.color = Quiet;
            var fit = outlookText.gameObject.AddComponent<LayoutElement>();
            fit.preferredHeight = 90f;
            listenText = Body(column, 20, TextAlignmentOptions.Center);
            listenText.color = Ink;
            var listenFit = listenText.gameObject.AddComponent<LayoutElement>();
            listenFit.preferredHeight = 48f;
            return page.gameObject;
        }

        GameObject BuildHousePage(RectTransform body)
        {
            var page = Child(body, "House");
            Stretch(page);
            housePageText = Body(page, 24, TextAlignmentOptions.Center);
            Stretch(housePageText.rectTransform, 32f);
            housePageText.text = "Jobs and standing are not in this slice.";
            return page.gameObject;
        }

        void BuildTravel()
        {
            var frame = Child(travel, "MapFrame");
            Stretch(frame, 12f);
            mapRoot = Child(frame, "Map");
            Stretch(mapRoot);
            var fit = mapRoot.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = RoadPath.Aspect;

            mapImage = mapRoot.gameObject.AddComponent<RawImage>();
            mapImage.color = new Color(0.72f, 0.61f, 0.42f, 1f);
            mapImage.raycastTarget = false;

            weatherDiscs = new RectTransform[3];
            weatherLabels = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var discImage = ImageOn(Child(mapRoot, "Weather" + i), Color.white, false);
                discImage.sprite = disc;
                weatherDiscs[i] = discImage.rectTransform;
                weatherLabels[i] = Body(weatherDiscs[i], 16, TextAlignmentOptions.Center);
                Stretch(weatherLabels[i].rectTransform);
                weatherLabels[i].raycastTarget = false;
            }

            var fogGo = Child(mapRoot, "Fog");
            Stretch(fogGo);
            fogImage = fogGo.gameObject.AddComponent<RawImage>();
            fogImage.texture = quad.texture;
            fogImage.raycastTarget = false;
            fogImage.color = Color.white;
            var shader = Shader.Find("Caravans/VisionFog");
            if (shader != null)
            {
                fogMaterial = new Material(shader);
                fogImage.material = fogMaterial;
            }
            else
            {
                fogImage.color = new Color(1f, 1f, 1f, 0f);
                Debug.LogWarning("Vision fog shader was not found. The map stays unshaded.");
            }

            var tokenImage = ImageOn(Child(mapRoot, "Caravan"), new Color(0.93f, 0.82f, 0.55f, 1f), false);
            tokenImage.sprite = disc;
            token = tokenImage.rectTransform;
            token.sizeDelta = new Vector2(22f, 22f);

            for (int i = 0; i < 9; i++)
            {
                var npcImage = ImageOn(Child(mapRoot, "Npc" + i), Color.white, false);
                npcImage.sprite = disc;
                npcImage.rectTransform.sizeDelta = new Vector2(14f, 14f);
                npcTokens.Add(npcImage);
            }

            var legBar = ImageOn(Child(travel, "Leg"), new Color(0.1f, 0.07f, 0.05f, 0.82f), false);
            TopStrip(legBar.rectTransform, 40f);
            legText = Body(legBar.rectTransform, 20, TextAlignmentOptions.Center);
            Stretch(legText.rectTransform, 4f);

            paceBar = new GameObject("Pace", typeof(RectTransform));
            paceBar.transform.SetParent(travel, false);
            var paceRt = paceBar.GetComponent<RectTransform>();
            BottomStrip(paceRt, 72f);
            var paceLayout = paceBar.AddComponent<HorizontalLayoutGroup>();
            paceLayout.spacing = 12f;
            paceLayout.childAlignment = TextAnchor.MiddleCenter;
            paceLayout.childControlWidth = false;
            paceLayout.childControlHeight = true;
            paceLayout.childForceExpandWidth = false;
            paceLayout.childForceExpandHeight = true;
            paceLayout.padding = new RectOffset(0, 0, 10, 10);
            pauseButton = Tab(paceRt, "Pause", () => session.SetPaused(true));
            pace1 = Tab(paceRt, "1×", () => session.ChoosePace(1));
            pace2 = Tab(paceRt, "2×", () => session.ChoosePace(2));
            pace4 = Tab(paceRt, "4×", () => session.ChoosePace(4));
            pauseButton.GetComponent<LayoutElement>().preferredWidth = 140f;
            pace1.GetComponent<LayoutElement>().preferredWidth = 100f;
            pace2.GetComponent<LayoutElement>().preferredWidth = 100f;
            pace4.GetComponent<LayoutElement>().preferredWidth = 100f;

            backButton = Tab(travel, "Back to the plaza", () =>
            {
                lookingAtMap = false;
                Redraw();
            });
            var backRt = backButton.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 24f);
            backRt.sizeDelta = new Vector2(280f, 52f);
        }

        void BuildTopBar(RectTransform root)
        {
            var bar = ImageOn(Child(root, "TopBar"), Bar, false);
            TopStrip(bar.rectTransform, TopBarHeight);
            RaiseCanvas(bar.gameObject, 20);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(24, 16, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            placeText = HudText(bar.rectTransform, 280f, TextAlignmentOptions.MidlineLeft);
            clockText = HudText(bar.rectTransform, 220f, TextAlignmentOptions.MidlineLeft);
            coinText = HudText(bar.rectTransform, 160f, TextAlignmentOptions.MidlineLeft);
            cargoText = HudText(bar.rectTransform, 0f, TextAlignmentOptions.MidlineLeft);
            cargoText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            gearButton = Tab(bar.rectTransform, "Gear", ToggleOptions);
            gearButton.GetComponent<LayoutElement>().preferredWidth = 120f;
        }

        void BuildViewBar(RectTransform root)
        {
            var bar = ImageOn(Child(root, "ViewBar"), Bar, false);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = Vector2.one;
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.sizeDelta = new Vector2(0f, ViewBarHeight);
            bar.rectTransform.anchoredPosition = new Vector2(0f, -TopBarHeight);
            RaiseCanvas(bar.gameObject, 24);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(24, 16, 6, 6);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            logbookButton = Tab(bar.rectTransform, "Logbook", ToggleLogbook);
            logbookButton.GetComponent<LayoutElement>().preferredWidth = 220f;
        }

        void BuildLogbook(RectTransform root)
        {
            logbookRoot = Child(root, "Logbook");
            logbookRoot.anchorMin = Vector2.zero;
            logbookRoot.anchorMax = Vector2.one;
            logbookRoot.offsetMin = new Vector2(36f, 28f);
            logbookRoot.offsetMax = new Vector2(-36f, -(TopBarHeight + ViewBarHeight + 16f));
            ImageOn(logbookRoot, Panel, true);
            RaiseCanvas(logbookRoot.gameObject, 18);

            var header = Child(logbookRoot, "Header");
            TopStrip(header, 48f);
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.padding = new RectOffset(16, 16, 8, 8);
            headerRow.spacing = 12f;
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = true;
            ChartLabel(header, "Good", 1f, 0f);
            ChartLabel(header, "Lowest buy", 0f, 420f);
            ChartLabel(header, "Highest sale", 0f, 420f);

            var scrollRt = Child(logbookRoot, "Scroll");
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, -48f);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = Child(scrollRt, "Viewport");
            Stretch(viewport);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.sprite = quad;
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewport;

            logbookRows = Child(viewport, "Rows");
            logbookRows.anchorMin = new Vector2(0f, 1f);
            logbookRows.anchorMax = new Vector2(1f, 1f);
            logbookRows.pivot = new Vector2(0.5f, 1f);
            logbookRows.anchoredPosition = Vector2.zero;
            logbookRows.sizeDelta = new Vector2(0f, 0f);
            var rows = logbookRows.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 4f;
            rows.padding = new RectOffset(8, 8, 4, 8);
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = false;
            var fitter = logbookRows.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = logbookRows;
            logbookRoot.gameObject.SetActive(false);
        }

        void BuildCard(RectTransform root)
        {
            cardRoot = Child(root, "Card");
            Stretch(cardRoot);
            RaiseCanvas(cardRoot.gameObject, 36);
            ImageOn(cardRoot, new Color(0.04f, 0.03f, 0.02f, 0.55f), true);
            var panel = Overlay(cardRoot, "Panel", new Vector2(760f, 440f), 0);
            Center(panel, new Vector2(760f, 440f));
            var title = Body(panel, 28, TextAlignmentOptions.Center);
            title.text = "The road";
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = Vector2.one;
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 64f);
            titleRt.anchoredPosition = Vector2.zero;
            cardBody = Body(panel, 22, TextAlignmentOptions.Center);
            var bodyRt = cardBody.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0.55f);
            bodyRt.anchorMax = new Vector2(1f, 0.82f);
            bodyRt.offsetMin = new Vector2(28f, 0f);
            bodyRt.offsetMax = new Vector2(-28f, 0f);
            cardBody.text = "A giant horned goat-lizard blocks the road.";
            cardResult = Body(panel, 22, TextAlignmentOptions.Center);
            var resultRt = cardResult.rectTransform;
            resultRt.anchorMin = new Vector2(0f, 0.42f);
            resultRt.anchorMax = new Vector2(1f, 0.78f);
            resultRt.offsetMin = new Vector2(28f, 0f);
            resultRt.offsetMax = new Vector2(-28f, 0f);
            steelButton = CardButton(panel, "Steel\nFight it\nCertain · 8 coin", new Vector2(-170f, -70f), () => session.ResolveCard(true));
            kenButton = CardButton(panel, "Ken\nFeed it\nCertain · 8 coin", new Vector2(170f, -70f), () => session.ResolveCard(false));
            continueButton = CardButton(panel, "The road is open", new Vector2(0f, -70f), () => session.ContinueFromCard());
            continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 64f);
        }

        void BuildHouseChoice(RectTransform root)
        {
            houseRoot = Child(root, "HouseChoice");
            Stretch(houseRoot);
            RaiseCanvas(houseRoot.gameObject, 42);
            ImageOn(houseRoot, new Color(0.04f, 0.03f, 0.02f, 0.72f), true);
            var panel = Overlay(houseRoot, "Panel", new Vector2(640f, 620f), 0);
            Center(panel, new Vector2(640f, 620f));
            var column = Child(panel, "Houses");
            Stretch(column, 28f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var title = Body(column, 24, TextAlignmentOptions.Center);
            title.text = "Choose the house. The wagon keeps this kit.";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            houseNote = Body(column, 18, TextAlignmentOptions.Center);
            houseNote.color = Quiet;
            houseNote.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            AddHouseButton(column, HouseId.Kharun);
            AddHouseButton(column, HouseId.Zamath);
            AddHouseButton(column, HouseId.Thalor);
            AddHouseButton(column, HouseId.Veythar);
            AddHouseButton(column, HouseId.Ghorath);
        }

        static HouseId HouseFromGate()
        {
            switch (Gate.ChosenHouse)
            {
                case "house_zamath":
                    return HouseId.Zamath;
                case "house_thalor":
                    return HouseId.Thalor;
                case "house_veythar":
                    return HouseId.Veythar;
                case "house_ghorath":
                    return HouseId.Ghorath;
                default:
                    return HouseId.Kharun;
            }
        }

        void AddHouseButton(RectTransform column, HouseId house)
        {
            var button = Tab(column, CaravanSession.HouseName(house), () =>
            {
                if (session.ChooseHouse(house))
                {
                    if (lesson && !session.Skipped)
                        TutorialOption.Set(false);
                    page = Page.Plaza;
                    lookingAtMap = false;
                }
            });
            button.GetComponent<LayoutElement>().preferredHeight = 52f;
        }

        void BuildOptions(RectTransform root)
        {
            optionsRoot = Child(root, "Options");
            Stretch(optionsRoot);
            RaiseCanvas(optionsRoot.gameObject, 50);
            var backdrop = ImageOn(optionsRoot, new Color(0.04f, 0.03f, 0.02f, 0.55f), true);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(ToggleOptions);
            var panel = Overlay(optionsRoot, "Panel", new Vector2(480f, 280f), 0);
            Center(panel, new Vector2(480f, 280f));
            var title = Body(panel, 24, TextAlignmentOptions.Center);
            title.text = "Save is not in this slice.";
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.55f);
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(20f, 0f);
            titleRt.offsetMax = new Vector2(-20f, -16f);
            skipButton = CardButton(panel, "Skip the lesson", new Vector2(0f, 10f), () =>
            {
                optionsOpen = false;
                lookingAtMap = false;
                page = Page.None;
                session.SkipTutorial();
            });
            var close = CardButton(panel, "Close", new Vector2(0f, -70f), ToggleOptions);
            close.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 52f);
            skipButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 56f);
        }

        void Redraw()
        {
            drawn = session.Revision;
            ClearRaised();
            dock.gameObject.SetActive(!session.Traveling && !lookingAtMap);
            travel.gameObject.SetActive(session.Traveling || lookingAtMap);
            marketPage.SetActive(page == Page.Market);
            plazaPage.SetActive(page == Page.Plaza);
            housePage.SetActive(page == Page.House);
            houseTab.gameObject.SetActive(EconomyCatalog.IsCity(session.Dock));
            paceBar.SetActive(session.Traveling);
            backButton.gameObject.SetActive(lookingAtMap && !session.Traveling);
            cardRoot.gameObject.SetActive(session.CardPending || session.ResultPending);
            houseRoot.gameObject.SetActive(session.Step == TutorialStep.HouseChoice);
            optionsRoot.gameObject.SetActive(optionsOpen);
            logbookRoot.gameObject.SetActive(logbookOpen);
            skipButton.gameObject.SetActive(session.Step != TutorialStep.HouseChoice && session.Step != TutorialStep.Free);

            bannerName.text = CaravanSession.PlaceName(session.Dock);
            bannerTag.text = session.PatronLine();
            roadButton.GetComponentInChildren<TextMeshProUGUI>().text = session.Dock == PlaceId.Kharun
                ? "Take the road to Draven"
                : "Take the road to Kharûn";
            outlookText.text = session.OutlookLine();
            listenText.text = listenLine;
            waterButton.GetComponentInChildren<TextMeshProUGUI>().text = FillLabel("Fill water", GoodId.Water);
            foodButton.GetComponentInChildren<TextMeshProUGUI>().text = FillLabel("Fill food", GoodId.Rations);
            houseNote.text = session.HouseNote ?? string.Empty;

            bool showChoices = session.CardPending;
            steelButton.gameObject.SetActive(showChoices);
            kenButton.gameObject.SetActive(showChoices);
            cardBody.gameObject.SetActive(showChoices);
            continueButton.gameObject.SetActive(session.ResultPending);
            cardResult.gameObject.SetActive(session.ResultPending);
            if (session.ResultPending)
            {
                cardResult.text = session.CardWasSteel
                    ? "You drive it off the road. The coin is spent."
                    : "You feed it. It leaves the road. The coin is spent.";
            }

            RebuildRows();
            if (MarketInFront())
                session.NoteMarketShown(selling);
            RebuildLogbook();
            SyncRoads();
            ApplyLesson();
            UpdateLive();
        }

        void RebuildRows()
        {
            tradeButtons.Clear();
            ClearChildren(pinnedHost);
            ClearChildren(rowHost);
            buttons.RemoveAll(button => button == null);

            bool any = false;
            int pinned = 0;
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                int count = selling ? session.CargoOf(good) : session.StockOf(session.Dock, good);
                if (count <= 0)
                    continue;
                any = true;
                bool lessonGood = lesson && (good == GoodId.ShimmersteelJars
                    || good == GoodId.FingerFungus
                    || good == GoodId.Water
                    || good == GoodId.Rations);
                tradeButtons[good] = MakeRow(lessonGood ? pinnedHost : rowHost, good, count);
                if (lessonGood)
                    pinned += 1;
            }

            float height = pinned * 72f;
            if (pinnedHost != null)
            {
                Vector2 size = pinnedHost.sizeDelta;
                size.y = height;
                pinnedHost.sizeDelta = size;
            }

            if (scrollRoot != null)
                scrollRoot.offsetMax = new Vector2(0f, -(64f + height));

            emptySell.gameObject.SetActive(page == Page.Market && selling && !any);
            bool showRows = !(page == Page.Market && selling && !any);
            pinnedHost.gameObject.SetActive(showRows);
            scrollRoot.gameObject.SetActive(showRows);
        }

        static void ClearChildren(RectTransform host)
        {
            if (host == null)
                return;
            for (int i = host.childCount - 1; i >= 0; i--)
                DestroyImmediate(host.GetChild(i).gameObject);
        }

        void SyncRoads()
        {
            bool free = session.Step == TutorialStep.Free;
            if (!free)
            {
                roadButton.gameObject.SetActive(true);
                if (neighborRoads.Count > 0)
                    ClearNeighborRoads();
                return;
            }

            roadButton.gameObject.SetActive(false);
            if (roadsReady && roadsDock == session.Dock)
                return;

            ClearNeighborRoads();
            IReadOnlyList<PlaceId> roads = session.RoadsOut();
            for (int i = 0; i < roads.Count; i++)
            {
                PlaceId dest = roads[i];
                string label = "Take the road to " + CaravanSession.PlaceName(dest);
                Button button = StackButton(plazaColumn, label, () => session.DepartTo(dest));
                button.transform.SetSiblingIndex(i);
                neighborRoads.Add(button);
            }

            roadsDock = session.Dock;
            roadsReady = true;
        }

        void ClearNeighborRoads()
        {
            for (int i = 0; i < neighborRoads.Count; i++)
            {
                Button button = neighborRoads[i];
                if (button == null)
                    continue;
                buttons.Remove(button);
                DestroyImmediate(button.gameObject);
            }

            neighborRoads.Clear();
            roadsReady = false;
        }

        Button MakeRow(RectTransform host, GoodId good, int count)
        {
            var row = Child(host, good.ToString());
            var background = row.gameObject.AddComponent<Image>();
            background.sprite = quad;
            background.color = Field;
            background.raycastTarget = false;
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 64f;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var swatch = ImageOn(Child(row, "Swatch"), Swatch(good), false);
            swatch.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;
            var name = Body(row, 20, TextAlignmentOptions.MidlineLeft);
            string label = CaravanSession.GoodName(good);
            if (EconomyCatalog.IsContraband(session.Dock, good))
                label += " · contraband";
            name.text = label;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var countText = Body(row, 18, TextAlignmentOptions.MidlineRight);
            countText.text = selling ? "Wagon " + count : "Shelf " + count;
            countText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
            var price = Body(row, 18, TextAlignmentOptions.MidlineRight);
            int shown = selling ? session.SellValue(session.Dock, good) : session.BuyCost(session.Dock, good);
            price.text = shown + " coin";
            price.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

            GoodId captured = good;
            Button button = Tab(row, selling ? "Sell" : "Buy", () =>
            {
                if (selling)
                    session.TrySell(captured);
                else
                    session.TryBuy(captured);
            });
            button.GetComponent<LayoutElement>().preferredWidth = 110f;
            return button;
        }

        void ApplyLesson()
        {
            bool free = session.Step == TutorialStep.Free;
            bool cardUp = session.CardPending || session.ResultPending;
            bool houseUp = session.Step == TutorialStep.HouseChoice;
            bool dim = !free && !cardUp && !houseUp && !session.Traveling && !lookingAtMap
                && session.Step != TutorialStep.UsePace;
            dimmer.gameObject.SetActive(dim && !optionsOpen);
            callout.gameObject.SetActive(false);

            if (free)
            {
                ApplyFreeButtons();
                return;
            }

            LockButtons();
            gearButton.interactable = true;
            logbookButton.interactable = true;
            steelButton.interactable = session.CardPending;
            kenButton.interactable = session.CardPending;
            continueButton.interactable = session.ResultPending;
            if (optionsOpen)
            {
                EnableUnder(optionsRoot);
                return;
            }

            if (houseUp)
            {
                EnableUnder(houseRoot);
                return;
            }

            if (cardUp)
                return;

            switch (session.Step)
            {
                case TutorialStep.OpenMarket:
                    Aim(marketTab, "Vekra of Kharûn, the clerk. Draven takes Shimmersteel Jars. Open the market. The gear skips this lesson.");
                    break;
                case TutorialStep.BuyJars:
                    AimBuyJars();
                    break;
                case TutorialStep.LeaveForDraven:
                    AimRoad("Take the road to Draven.");
                    break;
                case TutorialStep.UsePace:
                    AimPace();
                    break;
                case TutorialStep.RidingOut:
                case TutorialStep.RidingHome:
                    AllowPace();
                    break;
                case TutorialStep.ArriveDraven:
                    AimSell("Draven buys the jars. Open the market.", "Sell the jars.");
                    break;
                case TutorialStep.SellJars:
                    AimSell("Open the market.", "Sell the jars. One press is one unit.");
                    break;
                case TutorialStep.BuyFungus:
                    AimFungus();
                    break;
                case TutorialStep.LeaveForKharun:
                    AimRoad("Take the road back to Kharûn. This leg stays quiet.");
                    break;
                case TutorialStep.SellFungus:
                    AimFungusSale();
                    break;
            }
        }

        void AimBuyJars()
        {
            if (page != Page.Market)
            {
                Aim(marketTab, "The jars are on the market.");
                return;
            }

            if (session.CargoOf(GoodId.ShimmersteelJars) < 1)
            {
                AimTrade(GoodId.ShimmersteelJars, false, "Buy Shimmersteel Jars. One press is one unit.");
                return;
            }

            Aim(plazaTab, "Open the plaza when the wagon has the jars you want.");
            AllowTrade(GoodId.ShimmersteelJars);
        }

        void AimFungus()
        {
            if (page != Page.Market)
            {
                Aim(marketTab, "Finger Fungus is the cave good. Open the market.");
                return;
            }

            if (selling)
            {
                Aim(buyTab, "Open Buy.");
                return;
            }

            if (session.CargoOf(GoodId.FingerFungus) < 1)
            {
                AimTrade(GoodId.FingerFungus, false, "Buy Finger Fungus. Bring it back to Kharûn.");
                return;
            }

            Aim(plazaTab, "Open the plaza when you have the fungus you want.");
            AllowTrade(GoodId.FingerFungus);
        }

        void AimFungusSale()
        {
            if (page != Page.Market)
            {
                Aim(marketTab, "Sell the Finger Fungus at Kharûn.");
                return;
            }

            if (!selling)
            {
                Aim(sellTab, "Open Sell.");
                return;
            }

            AimTrade(GoodId.FingerFungus, true, "Sell the Finger Fungus. The coin is the point of the trip.");
        }

        void AimSell(string closed, string open)
        {
            if (page != Page.Market)
            {
                Aim(marketTab, closed);
                return;
            }

            if (!selling)
            {
                Aim(sellTab, "Open Sell.");
                return;
            }

            AimTrade(GoodId.ShimmersteelJars, true, open);
        }

        void AimRoad(string line)
        {
            if (page != Page.Plaza)
            {
                Aim(plazaTab, "The plaza is where you leave.");
                return;
            }

            Aim(roadButton, line);
        }

        void AimPace()
        {
            PlaceCallout(true, "The caravan moves on its own. Choose a pace. The hour follows the road.");
            AllowPace();
            pauseButton.interactable = false;
        }

        void AllowPace()
        {
            Allow(pauseButton.gameObject);
            Allow(pace1.gameObject);
            Allow(pace2.gameObject);
            Allow(pace4.gameObject);
            pauseButton.interactable = true;
            pace1.interactable = true;
            pace2.interactable = true;
            pace4.interactable = true;
        }

        void AimTrade(GoodId good, bool sell, string line)
        {
            if (selling != sell)
            {
                Aim(sell ? sellTab : buyTab, sell ? "Open Sell." : "Open Buy.");
                return;
            }

            Button button;
            if (!tradeButtons.TryGetValue(good, out button) || button == null)
            {
                Aim(marketTab, line);
                return;
            }

            Aim(button, line);
        }

        void AllowTrade(GoodId good)
        {
            Button button;
            if (tradeButtons.TryGetValue(good, out button) && button != null)
            {
                button.interactable = Afford(good, false);
                Allow(button.gameObject);
            }
        }

        void ApplyFreeButtons()
        {
            gearButton.interactable = true;
            logbookButton.interactable = true;
            marketTab.interactable = true;
            plazaTab.interactable = true;
            houseTab.interactable = EconomyCatalog.IsCity(session.Dock);
            for (int i = 0; i < neighborRoads.Count; i++)
            {
                if (neighborRoads[i] != null)
                    neighborRoads[i].interactable = !session.Traveling;
            }
            buyTab.interactable = true;
            sellTab.interactable = true;
            roadButton.interactable = !session.Traveling;
            waitButton.interactable = true;
            waterButton.interactable = Afford(GoodId.Water, false);
            foodButton.interactable = Afford(GoodId.Rations, false);
            listenButton.interactable = true;
            lookButton.interactable = !session.Traveling;
            pauseButton.interactable = session.Traveling;
            pace1.interactable = true;
            pace2.interactable = true;
            pace4.interactable = true;
            backButton.interactable = true;
            steelButton.interactable = session.CardPending;
            kenButton.interactable = session.CardPending;
            continueButton.interactable = session.ResultPending;
            foreach (var pair in tradeButtons)
            {
                if (pair.Value == null)
                    continue;
                pair.Value.interactable = Afford(pair.Key, selling);
            }
        }

        void LockButtons()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                    buttons[i].interactable = false;
            }
        }

        static void EnableUnder(RectTransform root)
        {
            var found = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < found.Length; i++)
                found[i].interactable = true;
        }

        bool Afford(GoodId good, bool sell)
        {
            if (session.Traveling)
                return false;
            if (sell)
                return session.CargoOf(good) > 0;
            if (session.StockOf(session.Dock, good) <= 0)
                return false;
            return session.Coin - session.BuyCost(session.Dock, good) >= session.CoinReserve;
        }

        void Aim(Button button, string line)
        {
            if (button == null)
                return;
            button.interactable = true;
            Allow(button.gameObject);
            PlaceCallout(false, line);
        }

        void Allow(GameObject go)
        {
            if (go == null || raised.Contains(go))
                return;
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;
            canvas.additionalShaderChannels = ExtraChannels();
            go.AddComponent<GraphicRaycaster>();
            raised.Add(go);
        }

        void PlaceCallout(bool pace, string line)
        {
            callout.gameObject.SetActive(true);
            calloutText.text = line;
            if (pace)
            {
                callout.anchorMin = new Vector2(0.5f, 0f);
                callout.anchorMax = new Vector2(0.5f, 0f);
                callout.pivot = new Vector2(0.5f, 0f);
                callout.anchoredPosition = new Vector2(0f, 96f);
                callout.sizeDelta = new Vector2(720f, 120f);
            }
            else
            {
                callout.anchorMin = new Vector2(1f, 0.5f);
                callout.anchorMax = new Vector2(1f, 0.5f);
                callout.pivot = new Vector2(1f, 0.5f);
                callout.anchoredPosition = new Vector2(-36f, 20f);
                callout.sizeDelta = new Vector2(460f, 210f);
            }
        }

        void ClearRaised()
        {
            for (int i = 0; i < raised.Count; i++)
            {
                GameObject go = raised[i];
                if (go == null)
                    continue;
                var raycaster = go.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    DestroyImmediate(raycaster);
                var canvas = go.GetComponent<Canvas>();
                if (canvas != null)
                    DestroyImmediate(canvas);
            }

            raised.Clear();
        }

        void UpdateLive()
        {
            if (placeText == null)
                return;
            placeText.text = session.BoundLabel();
            clockText.text = "Day " + session.Day + ", hour " + Mathf.FloorToInt(session.Hour);
            coinText.text = "Coin " + session.Coin;
            cargoText.text = CargoLine();
            if (legText != null)
                legText.text = session.Traveling ? session.LegWeatherLine() : string.Empty;
            TintPace(pauseButton, session.Pace == 0 && session.Traveling);
            TintPace(pace1, session.Pace == 1);
            TintPace(pace2, session.Pace == 2);
            TintPace(pace4, session.Pace == 4);
            TintPace(logbookButton, logbookOpen);
            UpdateMap();
        }

        void UpdateMap()
        {
            if (mapRoot == null || !mapRoot.gameObject.activeInHierarchy)
                return;
            EnsureMap();
            float sun = TravelScale.Sunlight(session.Hour);
            if (mapImage.texture != null)
                mapImage.color = Color.Lerp(new Color(0.55f, 0.62f, 0.8f), Color.white, sun);

            Vector2 point = session.CaravanPoint;
            PlaceOnMap(token, point, 22f);
            int npcCount = session.NpcCount;
            for (int i = 0; i < npcTokens.Count; i++)
            {
                bool show = i < npcCount;
                npcTokens[i].gameObject.SetActive(show);
                if (!show)
                    continue;
                NpcMark mark = session.NpcMarkAt(i);
                float angle = i * 0.9f;
                Vector2 shifted = mark.Point;
                shifted.x += Mathf.Cos(angle) * 0.008f;
                shifted.y += Mathf.Sin(angle) * 0.008f;
                PlaceOnMap(npcTokens[i].rectTransform, shifted, 14f);
                npcTokens[i].color = HouseColor(mark.House);
            }
            float height = mapRoot.rect.height;
            var masses = session.Masses;
            for (int i = 0; i < weatherDiscs.Length && i < masses.Count; i++)
            {
                WeatherMass mass = masses[i];
                float diameter = Mathf.Max(48f, mass.Radius * 2f * height);
                PlaceOnMap(weatherDiscs[i], mass.Center, diameter);
                weatherDiscs[i].GetComponent<Image>().color = WeatherColor(mass.Kind);
                weatherLabels[i].text = CaravanSession.WeatherName(mass.Kind);
            }

            if (fogMaterial != null)
            {
                fogMaterial.SetVector("_Center", new Vector4(point.x, 1f - point.y, 0f, 0f));
                fogMaterial.SetFloat("_Radius", session.VisionRadius());
                fogMaterial.SetFloat("_Soft", 0.035f);
                fogMaterial.SetFloat("_Aspect", RoadPath.Aspect);
            }
        }

        static void PlaceOnMap(RectTransform rect, Vector2 topLeft, float diameter)
        {
            var anchor = new Vector2(topLeft.x, 1f - topLeft.y);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(diameter, diameter);
        }

        void EnsureMap()
        {
            if (mapImage == null || mapImage.texture != null)
                return;
            var texture = Resources.Load<Texture2D>("Map/upscale_map");
            if (texture == null)
                return;
            mapImage.texture = texture;
            mapImage.color = Color.white;
        }

        void OpenMarket()
        {
            page = Page.Market;
            session.NoteMarketOpened();
            Redraw();
        }

        void OpenPlaza()
        {
            page = Page.Plaza;
            session.NotePlazaOpened();
            Redraw();
        }

        void OpenHouse()
        {
            page = Page.House;
            Redraw();
        }

        void OpenSell()
        {
            selling = true;
            session.NoteSellOpened();
            Redraw();
        }

        void ToggleOptions()
        {
            optionsOpen = !optionsOpen;
            Redraw();
        }

        void ToggleLogbook()
        {
            logbookOpen = !logbookOpen;
            Redraw();
        }

        bool MarketInFront()
        {
            return page == Page.Market
                && !logbookOpen
                && !optionsOpen
                && !lookingAtMap
                && !session.Traveling;
        }

        void RebuildLogbook()
        {
            if (!logbookOpen || logbookRows == null)
                return;

            ClearChildren(logbookRows);
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                var row = Child(logbookRows, good.ToString());
                var background = row.gameObject.AddComponent<Image>();
                background.sprite = quad;
                background.color = Field;
                background.raycastTarget = false;
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 12f;
                layout.padding = new RectOffset(8, 8, 4, 4);
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                ChartLabel(row, CaravanSession.GoodName(good), 1f, 0f);
                ChartLabel(row, LoggedBuyText(good), 0f, 420f);
                ChartLabel(row, LoggedSaleText(good), 0f, 420f);
            }
        }

        string LoggedBuyText(GoodId good)
        {
            if (!session.HasLoggedBuy(good))
                return string.Empty;
            return session.LoggedBuy(good) + " coin, " + CaravanSession.PlaceName(session.LoggedBuyPlace(good));
        }

        string LoggedSaleText(GoodId good)
        {
            if (!session.HasLoggedSale(good))
                return string.Empty;
            return session.LoggedSale(good) + " coin, " + CaravanSession.PlaceName(session.LoggedSalePlace(good));
        }

        TextMeshProUGUI ChartLabel(RectTransform parent, string label, float flex, float width)
        {
            var text = Body(parent, 20, TextAlignmentOptions.MidlineLeft);
            text.text = label;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            var element = text.gameObject.AddComponent<LayoutElement>();
            if (flex > 0f)
                element.flexibleWidth = flex;
            else
                element.preferredWidth = width;
            return text;
        }

        string FillLabel(string verb, GoodId good)
        {
            if (session.StockOf(session.Dock, good) <= 0)
                return verb + " · none on the shelf";
            return verb + " · " + session.BuyCost(session.Dock, good) + " coin";
        }

        string CargoLine()
        {
            var parts = new List<string>();
            parts.Add(session.CargoSpace() + "/" + EconomyRates.WagonSpace);
            parts.Add(session.CargoWeight() + "/" + EconomyRates.WagonWeight);
            int named = 0;
            int extra = 0;
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                int count = session.CargoOf(good);
                if (count <= 0)
                    continue;
                if (named < 3)
                {
                    parts.Add(CaravanSession.GoodName(good) + " " + count);
                    named += 1;
                }
                else
                {
                    extra += 1;
                }
            }

            if (named == 0)
                return parts[0] + "  " + parts[1];
            if (extra > 0)
                parts.Add("+" + extra);
            return string.Join("  ", parts);
        }

        void TintPace(Button button, bool selected)
        {
            if (button == null)
                return;
            var colors = button.colors;
            colors.normalColor = selected ? ButtonHot : ButtonFace;
            button.colors = colors;
        }

        static Color HouseColor(HouseId house)
        {
            switch (house)
            {
                case HouseId.Kharun:
                    return new Color(0.55f, 0.72f, 0.84f, 1f);
                case HouseId.Zamath:
                    return new Color(0.45f, 0.62f, 0.38f, 1f);
                case HouseId.Thalor:
                    return new Color(0.72f, 0.28f, 0.22f, 1f);
                case HouseId.Veythar:
                    return new Color(0.55f, 0.38f, 0.68f, 1f);
                default:
                    return new Color(0.78f, 0.52f, 0.22f, 1f);
            }
        }

        static Color WeatherColor(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Heat:
                    return new Color(0.77f, 0.32f, 0.12f, 0.38f);
                case WeatherKind.Wind:
                    return new Color(0.85f, 0.78f, 0.58f, 0.32f);
                case WeatherKind.Sandstorm:
                    return new Color(0.62f, 0.42f, 0.22f, 0.5f);
                default:
                    return new Color(1f, 1f, 1f, 0f);
            }
        }

        static Color Swatch(GoodId good)
        {
            switch (good)
            {
                case GoodId.ShimmersteelJars:
                    return new Color(0.76f, 0.79f, 0.82f, 1f);
                case GoodId.FingerFungus:
                    return new Color(0.58f, 0.42f, 0.62f, 1f);
                case GoodId.Water:
                    return new Color(0.42f, 0.6f, 0.72f, 1f);
                default:
                    return new Color(0.71f, 0.54f, 0.32f, 1f);
            }
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
            colors.disabledColor = ButtonOff;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(action);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 52f;
            var text = Body(go.GetComponent<RectTransform>(), 20, TextAlignmentOptions.Center);
            text.text = label;
            text.raycastTarget = false;
            Stretch(text.rectTransform, 4f);
            buttons.Add(button);
            return button;
        }

        Button StackButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var button = Tab(parent, label, action);
            button.GetComponent<LayoutElement>().preferredHeight = 56f;
            return button;
        }

        Button CardButton(RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var button = Tab(parent, label, action);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 110f);
            var element = button.GetComponent<LayoutElement>();
            if (element != null)
                element.ignoreLayout = true;
            return button;
        }

        TextMeshProUGUI HudText(RectTransform parent, float width, TextAlignmentOptions alignment)
        {
            var text = Body(parent, 22, alignment);
            var element = text.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            return text;
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

        RectTransform Overlay(RectTransform parent, string name, Vector2 size, int sort)
        {
            var image = ImageOn(Child(parent, name), Panel, true);
            image.rectTransform.sizeDelta = size;
            if (sort > 0)
                RaiseCanvas(image.gameObject, sort);
            return image.rectTransform;
        }

        Image ImageOn(RectTransform rect, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = quad;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        static RectTransform Child(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        static void FillBelowBar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, -(TopBarHeight + ViewBarHeight));
        }

        static void TopStrip(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        static void BottomStrip(RectTransform rect, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        static void RaiseCanvas(GameObject go, int sort)
        {
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sort;
            canvas.additionalShaderChannels = ExtraChannels();
            go.AddComponent<GraphicRaycaster>();
        }

        static AdditionalCanvasShaderChannels ExtraChannels()
        {
            return AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
        }

        TMP_FontAsset MakeFont()
        {
            Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (source == null)
                source = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return TMP_FontAsset.CreateFontAsset(source);
        }

        static Sprite MakeSprite(bool circle)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = circle ? Mathf.Clamp01(radius - dist) : 1f;
                    if (circle && dist > radius - 3f && dist <= radius)
                        texture.SetPixel(x, y, new Color(0.25f, 0.14f, 0.08f, alpha));
                    else
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
