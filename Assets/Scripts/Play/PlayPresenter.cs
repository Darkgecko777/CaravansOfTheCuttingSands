using System.Collections.Generic;
using System.Globalization;
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
        const float MarketStrip = 48f;
        const float WagonPanelWidth = 520f;
        const float FrameGap = 20f;
        const float FrameHeader = 52f;
        const float WeightLine = 36f;
        const float ShopCellW = 158f;
        const float ShopCellH = 120f;
        const float WagonCellW = 236f;
        const float WagonCellH = 140f;
        const float GatesPanelRight = 448f;
        const int ShopColumns = 8;
        const int WagonColumns = 2;

        static readonly Color Ink = new Color(0.95f, 0.89f, 0.78f, 1f);
        static readonly Color Quiet = new Color(0.78f, 0.7f, 0.58f, 1f);
        static readonly Color Panel = new Color(0.2f, 0.15f, 0.11f, 0.94f);
        static readonly Color Bar = new Color(0.12f, 0.09f, 0.07f, 0.96f);
        static readonly Color ButtonFace = new Color(0.45f, 0.32f, 0.22f, 1f);
        static readonly Color ButtonHot = new Color(0.66f, 0.46f, 0.28f, 1f);
        static readonly Color ButtonDown = new Color(0.3f, 0.2f, 0.14f, 1f);
        static readonly Color ButtonOff = new Color(0.24f, 0.19f, 0.16f, 0.85f);
        static readonly Color Field = new Color(0.28f, 0.21f, 0.16f, 1f);
        static readonly Color DemandHigh = new Color(0.70f, 0.24f, 0.18f, 1f);
        static readonly Color DemandMid = new Color(0.62f, 0.48f, 0.26f, 1f);
        static readonly Color DemandLow = new Color(0.34f, 0.46f, 0.30f, 1f);
        static readonly Color RimQuiet = new Color(0.16f, 0.12f, 0.10f, 1f);
        static readonly Color FaceDim = new Color(0.18f, 0.14f, 0.11f, 1f);
        static readonly Color WagonGround = new Color(0.32f, 0.23f, 0.16f, 1f);
        const string InkHex = "#F2E3C7";
        const string DayHex = "#E6C36A";
        const string NightHex = "#8EBFEE";

        readonly List<Button> buttons = new List<Button>();
        readonly List<GameObject> raised = new List<GameObject>();
        readonly List<Button> neighborRoads = new List<Button>();
        readonly List<PlaceId> neighborDests = new List<PlaceId>();
        readonly List<Button> sortChoices = new List<Button>();
        readonly List<ShopSort> sortChoiceKinds = new List<ShopSort>();
        readonly List<Image> npcTokens = new List<Image>();

        CaravanSession session;
        TMP_FontAsset font;
        Sprite quad;
        Sprite disc;
        int drawn = -1;

        enum Page { None, Market, Plaza, House }
        enum PlazaSpot { Inn, Gates, Tavern, Stables }
        enum ShopSort { Catalog, Name, PriceHigh, PriceLow, Demand }

        Page page = Page.None;
        PlazaSpot plazaSpot = PlazaSpot.Gates;
        ShopSort shopSort = ShopSort.Catalog;
        bool sortMenuOpen;
        bool shopSelected;
        GoodId pickedGood;
        int wagonPick = -1;
        bool optionsOpen;
        bool lookingAtMap;
        bool logbookOpen;
        bool choosingRoad;
        bool roadLit;
        PlaceId pendingDest;
        RoadPace draftPace = RoadPace.Steady;
        string listenLine = string.Empty;

        RectTransform dock;
        RectTransform travel;
        RectTransform mapFrame;
        RectTransform mapRoot;
        RectTransform mapSheet;
        RectTransform mapRoads;
        RectTransform routeRoot;
        Image routeMark;
        readonly List<Image> routeSegments = new List<Image>();
        Sprite ring;
        RawImage mapImage;
        RawImage fogImage;
        Material fogMaterial;
        RectTransform token;
        Image dimmer;
        RectTransform cardRoot;
        TextMeshProUGUI cardTitle;
        TextMeshProUGUI cardBody;
        Button rushButton;
        Button steadyButton;
        Button carefulButton;
        Button confirmPaceButton;
        Button dawnButton;
        Button duskButton;
        Button campButton;
        Button continueButton;
        Button backPaceButton;
        RectTransform optionsRoot;
        TextMeshProUGUI placeText;
        TextMeshProUGUI clockText;
        TextMeshProUGUI travelText;
        TextMeshProUGUI coinText;
        TextMeshProUGUI cargoText;
        TextMeshProUGUI bannerName;
        TextMeshProUGUI bannerTag;
        TextMeshProUGUI outlookText;
        TextMeshProUGUI listenText;
        TextMeshProUGUI legText;
        TextMeshProUGUI housePageText;
        RectTransform shopHost;
        RectTransform wagonHost;
        GridLayoutGroup shopGrid;
        GridLayoutGroup wagonGrid;
        RectTransform sortMenu;
        TextMeshProUGUI detailText;
        TextMeshProUGUI weightText;
        RectTransform gatesColumn;
        GameObject innSpot;
        GameObject gatesSpot;
        GameObject tavernSpot;
        GameObject stablesSpot;
        PlaceId roadsDock;
        bool roadsReady;
        Button marketTab;
        Button plazaTab;
        Button houseTab;
        Button buyButton;
        Button sellButton;
        Button sortButton;
        Button innTab;
        Button gatesTab;
        Button tavernTab;
        Button stablesTab;
        Button innWaitButton;
        Button innDawnButton;
        Button innDuskButton;
        Button restButton;
        Button waterButton;
        Button foodButton;
        Button listenButton;
        Button gatesCareful;
        Button gatesSteady;
        Button gatesRush;
        Button embarkButton;
        TextMeshProUGUI nextCampText;
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
            session = CaravanSession.AtHouse(house);
            EnsureEventSystem();
            quad = MakeSprite(false);
            disc = MakeSprite(true);
            ring = MakeRing();
            font = MakeFont();
            MarketSounds.Bind(gameObject);
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
            BuildRoadChoice(root);
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

            var strip = Child(page, "Detail");
            TopStrip(strip, MarketStrip);
            detailText = Body(strip, 22, TextAlignmentOptions.MidlineLeft);
            Stretch(detailText.rectTransform, 16f);

            var shop = ImageOn(Child(page, "ShopFrame"), Panel, false);
            shop.rectTransform.anchorMin = Vector2.zero;
            shop.rectTransform.anchorMax = Vector2.one;
            shop.rectTransform.offsetMin = new Vector2(12f, 8f);
            shop.rectTransform.offsetMax = new Vector2(-(WagonPanelWidth + FrameGap + 12f), -MarketStrip);

            var shopHead = Child(shop.rectTransform, "Head");
            TopStrip(shopHead, FrameHeader);
            var shopRow = shopHead.gameObject.AddComponent<HorizontalLayoutGroup>();
            shopRow.spacing = 8f;
            shopRow.padding = new RectOffset(8, 8, 6, 6);
            shopRow.childAlignment = TextAnchor.MiddleLeft;
            shopRow.childControlWidth = true;
            shopRow.childControlHeight = true;
            shopRow.childForceExpandWidth = false;
            shopRow.childForceExpandHeight = true;
            sortButton = Tab(shopHead, "Sort", ToggleSortMenu);
            var sortFit = sortButton.GetComponent<LayoutElement>();
            sortFit.preferredWidth = 150f;
            sortFit.preferredHeight = 40f;
            var shopSpacer = Child(shopHead, "Spacer");
            var shopSpace = shopSpacer.gameObject.AddComponent<LayoutElement>();
            shopSpace.flexibleWidth = 1f;
            buyButton = Tab(shopHead, "Buy", PressBuy);
            var buyFit = buyButton.GetComponent<LayoutElement>();
            buyFit.preferredWidth = 160f;
            buyFit.preferredHeight = 40f;

            shopHost = Child(shop.rectTransform, "Cells");
            shopHost.anchorMin = Vector2.zero;
            shopHost.anchorMax = Vector2.one;
            shopHost.offsetMin = new Vector2(8f, 8f);
            shopHost.offsetMax = new Vector2(-8f, -FrameHeader);
            shopGrid = shopHost.gameObject.AddComponent<GridLayoutGroup>();
            shopGrid.cellSize = new Vector2(ShopCellW, ShopCellH);
            shopGrid.spacing = new Vector2(6f, 6f);
            shopGrid.padding = new RectOffset(4, 4, 4, 4);
            shopGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            shopGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            shopGrid.childAlignment = TextAnchor.UpperCenter;
            shopGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            shopGrid.constraintCount = ShopColumns;

            sortMenu = ImageOn(Child(shop.rectTransform, "SortMenu"), Bar, true).rectTransform;
            sortMenu.anchorMin = new Vector2(0f, 1f);
            sortMenu.anchorMax = new Vector2(0f, 1f);
            sortMenu.pivot = new Vector2(0f, 1f);
            sortMenu.anchoredPosition = new Vector2(8f, -FrameHeader);
            sortMenu.sizeDelta = new Vector2(280f, 188f);
            var menuColumn = sortMenu.gameObject.AddComponent<VerticalLayoutGroup>();
            menuColumn.spacing = 4f;
            menuColumn.padding = new RectOffset(8, 8, 8, 8);
            menuColumn.childAlignment = TextAnchor.UpperCenter;
            menuColumn.childControlWidth = true;
            menuColumn.childControlHeight = true;
            menuColumn.childForceExpandWidth = true;
            menuColumn.childForceExpandHeight = false;
            AddSortChoice(sortMenu, "Name", ShopSort.Name);
            AddSortChoice(sortMenu, "Price, high to low", ShopSort.PriceHigh);
            AddSortChoice(sortMenu, "Price, low to high", ShopSort.PriceLow);
            AddSortChoice(sortMenu, "Demand", ShopSort.Demand);
            RaiseCanvas(sortMenu.gameObject, 8);
            sortMenu.gameObject.SetActive(false);

            var wagon = ImageOn(Child(page, "WagonFrame"), WagonGround, false);
            wagon.rectTransform.anchorMin = new Vector2(1f, 0f);
            wagon.rectTransform.anchorMax = Vector2.one;
            wagon.rectTransform.offsetMin = new Vector2(-(WagonPanelWidth + 12f), 8f);
            wagon.rectTransform.offsetMax = new Vector2(-12f, -MarketStrip);

            var wagonHead = Child(wagon.rectTransform, "Head");
            TopStrip(wagonHead, FrameHeader);
            var wagonRow = wagonHead.gameObject.AddComponent<HorizontalLayoutGroup>();
            wagonRow.padding = new RectOffset(8, 8, 6, 6);
            wagonRow.childAlignment = TextAnchor.MiddleRight;
            wagonRow.childControlWidth = true;
            wagonRow.childControlHeight = true;
            wagonRow.childForceExpandWidth = false;
            wagonRow.childForceExpandHeight = true;
            var wagonSpacer = Child(wagonHead, "Spacer");
            wagonSpacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            sellButton = Tab(wagonHead, "Sell", PressSell);
            var sellFit = sellButton.GetComponent<LayoutElement>();
            sellFit.preferredWidth = 160f;
            sellFit.preferredHeight = 40f;

            var weightBar = Child(wagon.rectTransform, "Weight");
            BottomStrip(weightBar, WeightLine);
            weightText = Body(weightBar, 20, TextAlignmentOptions.Center);
            Stretch(weightText.rectTransform, 4f);

            wagonHost = Child(wagon.rectTransform, "Slots");
            wagonHost.anchorMin = Vector2.zero;
            wagonHost.anchorMax = Vector2.one;
            wagonHost.offsetMin = new Vector2(8f, WeightLine);
            wagonHost.offsetMax = new Vector2(-8f, -FrameHeader);
            wagonGrid = wagonHost.gameObject.AddComponent<GridLayoutGroup>();
            wagonGrid.cellSize = new Vector2(WagonCellW, WagonCellH);
            wagonGrid.spacing = new Vector2(8f, 8f);
            wagonGrid.padding = new RectOffset(4, 4, 4, 4);
            wagonGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            wagonGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            wagonGrid.childAlignment = TextAnchor.UpperCenter;
            wagonGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            wagonGrid.constraintCount = WagonColumns;
            return page.gameObject;
        }

        void AddSortChoice(RectTransform menu, string label, ShopSort sort)
        {
            ShopSort captured = sort;
            Button button = Tab(menu, label, () => ChooseSort(captured));
            button.GetComponent<LayoutElement>().preferredHeight = 40f;
            sortChoices.Add(button);
            sortChoiceKinds.Add(sort);
        }

        GameObject BuildPlaza(RectTransform body)
        {
            var page = Child(body, "Plaza");
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
            innTab = Tab(sub, "Inn", OpenInn);
            gatesTab = Tab(sub, "Gates", OpenGates);
            tavernTab = Tab(sub, "Tavern", OpenTavern);
            stablesTab = Tab(sub, "Stables", OpenStables);
            innTab.GetComponent<LayoutElement>().preferredWidth = 160f;
            gatesTab.GetComponent<LayoutElement>().preferredWidth = 160f;
            tavernTab.GetComponent<LayoutElement>().preferredWidth = 170f;
            stablesTab.GetComponent<LayoutElement>().preferredWidth = 180f;

            var inn = Spot(page, "Inn");
            innSpot = inn.gameObject;
            var innColumn = SpotColumn(inn);
            restButton = StackButton(innColumn, "Rest", () => session.TryRest());
            innWaitButton = StackButton(innColumn, "Wait an hour", () => session.WaitOneHour());
            innDawnButton = StackButton(innColumn, "Wait until dawn", () => session.WaitUntilDawn());
            innDuskButton = StackButton(innColumn, "Wait until dusk", () => session.WaitUntilDusk());
            waterButton = StackButton(innColumn, "Fill water", () => session.TryFillWater());
            foodButton = StackButton(innColumn, "Fill food", () => session.TryFillFood());

            var gates = Spot(page, "Gates");
            gatesSpot = gates.gameObject;
            gatesColumn = Child(gates, "Column");
            gatesColumn.anchorMin = new Vector2(0f, 0f);
            gatesColumn.anchorMax = new Vector2(0f, 1f);
            gatesColumn.pivot = new Vector2(0f, 0.5f);
            gatesColumn.offsetMin = new Vector2(12f, 12f);
            gatesColumn.offsetMax = new Vector2(GatesPanelRight, -12f);
            ImageOn(gatesColumn, Panel, true);
            var gatesLayout = gatesColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            gatesLayout.spacing = 12f;
            gatesLayout.padding = new RectOffset(16, 16, 16, 16);
            gatesLayout.childAlignment = TextAnchor.UpperCenter;
            gatesLayout.childControlWidth = true;
            gatesLayout.childControlHeight = true;
            gatesLayout.childForceExpandWidth = true;
            gatesLayout.childForceExpandHeight = false;
            outlookText = Body(gatesColumn, 20, TextAlignmentOptions.Center);
            outlookText.color = Quiet;
            var fit = outlookText.gameObject.AddComponent<LayoutElement>();
            fit.preferredHeight = 90f;
            var spacer = Child(gatesColumn, "Spacer");
            spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var embarkPanel = Child(gatesColumn, "EmbarkPanel");
            ImageOn(embarkPanel, Bar, true);
            embarkPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 228f;
            var embarkLayout = embarkPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            embarkLayout.spacing = 10f;
            embarkLayout.padding = new RectOffset(12, 12, 12, 12);
            embarkLayout.childAlignment = TextAnchor.UpperCenter;
            embarkLayout.childControlWidth = true;
            embarkLayout.childControlHeight = true;
            embarkLayout.childForceExpandWidth = true;
            embarkLayout.childForceExpandHeight = false;
            var speeds = Child(embarkPanel, "Speeds");
            speeds.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            var speedRow = speeds.gameObject.AddComponent<HorizontalLayoutGroup>();
            speedRow.spacing = 8f;
            speedRow.childAlignment = TextAnchor.MiddleCenter;
            speedRow.childControlWidth = true;
            speedRow.childControlHeight = true;
            speedRow.childForceExpandWidth = true;
            speedRow.childForceExpandHeight = true;
            gatesCareful = Tab(speeds, "Careful", () => PickPace(RoadPace.Careful));
            gatesSteady = Tab(speeds, "Steady", () => PickPace(RoadPace.Steady));
            gatesRush = Tab(speeds, "Rush", () => PickPace(RoadPace.Rush));
            gatesCareful.GetComponent<LayoutElement>().flexibleWidth = 1f;
            gatesSteady.GetComponent<LayoutElement>().flexibleWidth = 1f;
            gatesRush.GetComponent<LayoutElement>().flexibleWidth = 1f;
            nextCampText = Body(embarkPanel, 20, TextAlignmentOptions.Center);
            nextCampText.richText = true;
            nextCampText.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            embarkButton = StackButton(embarkPanel, "Embark", Embark);

            var tavern = Spot(page, "Tavern");
            tavernSpot = tavern.gameObject;
            var tavernColumn = SpotColumn(tavern);
            listenButton = StackButton(tavernColumn, "Listen", () =>
            {
                listenLine = "Socialize is not in this slice.";
                Redraw();
            });
            listenText = Body(tavernColumn, 20, TextAlignmentOptions.Center);
            listenText.color = Ink;
            var listenFit = listenText.gameObject.AddComponent<LayoutElement>();
            listenFit.preferredHeight = 48f;

            var stables = Spot(page, "Stables");
            stablesSpot = stables.gameObject;
            var stablesColumn = SpotColumn(stables);
            var stablesText = Body(stablesColumn, 24, TextAlignmentOptions.Center);
            stablesText.text = "Mount upgrades are not in this slice.";
            var stablesFit = stablesText.gameObject.AddComponent<LayoutElement>();
            stablesFit.preferredHeight = 80f;
            return page.gameObject;
        }

        RectTransform Spot(RectTransform page, string name)
        {
            var spot = Child(page, name);
            Stretch(spot);
            spot.offsetMax = new Vector2(0f, -64f);
            return spot;
        }

        RectTransform SpotColumn(RectTransform spot)
        {
            var column = Child(spot, "Column");
            column.anchorMin = new Vector2(0.5f, 0f);
            column.anchorMax = new Vector2(0.5f, 1f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.offsetMin = new Vector2(-260f, 0f);
            column.offsetMax = new Vector2(260f, 0f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 28, 28);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        void OpenInn()
        {
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Inn;
            Redraw();
        }

        void OpenGates()
        {
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Gates;
            Redraw();
        }

        void OpenTavern()
        {
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Tavern;
            Redraw();
        }

        void OpenStables()
        {
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Stables;
            Redraw();
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
            mapFrame = Child(travel, "MapFrame");
            Stretch(mapFrame, 12f);
            mapRoot = Child(mapFrame, "Map");
            Stretch(mapRoot);
            var fit = mapRoot.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = RoadPath.Aspect;
            mapRoot.gameObject.AddComponent<RectMask2D>();

            mapSheet = Child(mapRoot, "Sheet");
            mapSheet.anchorMin = new Vector2(0.5f, 0.5f);
            mapSheet.anchorMax = new Vector2(0.5f, 0.5f);
            mapSheet.pivot = new Vector2(0.5f, 0.5f);
            mapSheet.anchoredPosition = Vector2.zero;

            mapImage = mapSheet.gameObject.AddComponent<RawImage>();
            mapImage.color = new Color(0.72f, 0.61f, 0.42f, 1f);
            mapImage.raycastTarget = true;

            var fogGo = Child(mapSheet, "Fog");
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

            routeRoot = Child(mapSheet, "Route");
            Stretch(routeRoot);
            routeRoot.SetSiblingIndex(fogGo.GetSiblingIndex());
            routeMark = ImageOn(Child(routeRoot, "Destination"), new Color(0.93f, 0.78f, 0.35f, 0.95f), false);
            routeMark.sprite = ring;
            routeMark.rectTransform.sizeDelta = new Vector2(72f, 72f);
            routeRoot.gameObject.SetActive(false);

            var tokenImage = ImageOn(Child(mapSheet, "Caravan"), new Color(0.93f, 0.82f, 0.55f, 1f), false);
            tokenImage.sprite = disc;
            token = tokenImage.rectTransform;
            token.sizeDelta = new Vector2(22f, 22f);

            for (int i = 0; i < 9; i++)
            {
                var npcImage = ImageOn(Child(mapSheet, "Npc" + i), Color.white, false);
                npcImage.sprite = disc;
                npcImage.rectTransform.sizeDelta = new Vector2(14f, 14f);
                npcTokens.Add(npcImage);
            }

            mapRoads = Child(travel, "MapRoads");
            mapRoads.anchorMin = new Vector2(0f, 0f);
            mapRoads.anchorMax = new Vector2(0f, 1f);
            mapRoads.pivot = new Vector2(0f, 0.5f);
            mapRoads.offsetMin = new Vector2(24f, 96f);
            mapRoads.offsetMax = new Vector2(444f, -52f);
            var roadLayout = mapRoads.gameObject.AddComponent<VerticalLayoutGroup>();
            roadLayout.spacing = 12f;
            roadLayout.padding = new RectOffset(0, 0, 8, 8);
            roadLayout.childAlignment = TextAnchor.UpperCenter;
            roadLayout.childControlWidth = true;
            roadLayout.childControlHeight = true;
            roadLayout.childForceExpandWidth = true;
            roadLayout.childForceExpandHeight = false;
            mapRoads.gameObject.SetActive(false);

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
            placeText = HudText(bar.rectTransform, 260f, TextAlignmentOptions.MidlineLeft);
            clockText = HudText(bar.rectTransform, 240f, TextAlignmentOptions.MidlineLeft);
            travelText = HudText(bar.rectTransform, 160f, TextAlignmentOptions.MidlineLeft);
            coinText = HudText(bar.rectTransform, 150f, TextAlignmentOptions.MidlineLeft);
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

        void BuildRoadChoice(RectTransform root)
        {
            cardRoot = Child(root, "RoadChoice");
            Stretch(cardRoot);
            RaiseCanvas(cardRoot.gameObject, 36);
            ImageOn(cardRoot, new Color(0.04f, 0.03f, 0.02f, 0.55f), true);
            var panel = Overlay(cardRoot, "Panel", new Vector2(820f, 460f), 0);
            Center(panel, new Vector2(820f, 460f));
            cardTitle = Body(panel, 28, TextAlignmentOptions.Center);
            cardTitle.text = "The road";
            var titleRt = cardTitle.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = Vector2.one;
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 64f);
            titleRt.anchoredPosition = Vector2.zero;
            cardBody = Body(panel, 22, TextAlignmentOptions.Center);
            var bodyRt = cardBody.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = Vector2.one;
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.sizeDelta = new Vector2(-56f, 72f);
            bodyRt.anchoredPosition = new Vector2(0f, -64f);
            rushButton = ChoiceButton(panel, "Rush", new Vector2(-240f, 20f), () => PickPace(RoadPace.Rush));
            steadyButton = ChoiceButton(panel, "Steady", new Vector2(0f, 20f), () => PickPace(RoadPace.Steady));
            carefulButton = ChoiceButton(panel, "Careful", new Vector2(240f, 20f), () => PickPace(RoadPace.Careful));
            dawnButton = ChoiceButton(panel, "Wait until dawn", new Vector2(-170f, -48f), () => session.WaitUntilDawn());
            duskButton = ChoiceButton(panel, "Wait until dusk", new Vector2(170f, -48f), () => session.WaitUntilDusk());
            confirmPaceButton = ChoiceButton(panel, "Take the road", new Vector2(0f, -160f), ConfirmPace);
            campButton = ChoiceButton(panel, "Make camp", new Vector2(0f, -20f), () => session.MakeCamp());
            continueButton = ChoiceButton(panel, "Continue", new Vector2(0f, -20f), () => session.ContinueEncounter());
            backPaceButton = ChoiceButton(panel, "Back", new Vector2(300f, -160f), CloseRoadChoice);
            dawnButton.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 56f);
            duskButton.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 56f);
            confirmPaceButton.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 88f);
            campButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 56f);
            continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 56f);
            backPaceButton.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 56f);
            var confirmLabel = confirmPaceButton.GetComponentInChildren<TextMeshProUGUI>();
            confirmLabel.color = Color.white;
            confirmLabel.richText = true;
        }

        void CloseRoadChoice()
        {
            choosingRoad = false;
            Redraw();
        }

        void PickPace(RoadPace pace)
        {
            draftPace = pace;
            Redraw();
        }

        Button ChoiceButton(RectTransform panel, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            Button button = CardButton(panel, label, position, action);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 56f);
            return button;
        }

        void ConfirmPace()
        {
            session.ChooseRoadPace(draftPace);
            if (!choosingRoad)
                return;
            PlaceId dest = pendingDest;
            choosingRoad = false;
            roadLit = false;
            lookingAtMap = false;
            session.DepartTo(dest);
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
            var close = CardButton(panel, "Close", new Vector2(0f, -40f), ToggleOptions);
            close.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 52f);
        }

        void Redraw()
        {
            drawn = session.Revision;
            ClearRaised();
            bool gatesMap = !session.Traveling && page == Page.Plaza && plazaSpot == PlazaSpot.Gates;
            dock.gameObject.SetActive(!session.Traveling && !lookingAtMap);
            travel.gameObject.SetActive(session.Traveling || lookingAtMap || gatesMap);
            FitMapFrame();
            marketPage.SetActive(page == Page.Market);
            plazaPage.SetActive(page == Page.Plaza);
            housePage.SetActive(page == Page.House);
            ShowPlazaSpot();
            houseTab.gameObject.SetActive(EconomyCatalog.IsCity(session.Dock));
            paceBar.SetActive(session.Traveling && !session.CampPending && !session.AwaitingPace && !session.EncounterPending);
            backButton.gameObject.SetActive(lookingAtMap && !session.Traveling);
            bool roadChoice = choosingRoad || session.CampPending || session.AwaitingPace || session.EncounterPending;
            cardRoot.gameObject.SetActive(roadChoice);
            optionsRoot.gameObject.SetActive(optionsOpen);
            logbookRoot.gameObject.SetActive(logbookOpen);
            dimmer.gameObject.SetActive(roadChoice && !optionsOpen && !choosingRoad);
            ShowRoadChoice(roadChoice);

            bannerName.text = CaravanSession.PlaceName(session.Dock);
            bannerTag.text = session.PatronLine();
            outlookText.text = session.OutlookLine();
            nextCampText.text = NextCampLine();
            listenText.text = listenLine;
            waterButton.GetComponentInChildren<TextMeshProUGUI>().text = FillLabel("Fill water", GoodId.Water, session.WaterLitres, EconomyRates.CanteenLitres);
            foodButton.GetComponentInChildren<TextMeshProUGUI>().text = FillLabel("Fill food", GoodId.Rations, session.RationKg, EconomyRates.RationBagKg);

            RebuildMarket();
            NoteSelection();
            RebuildLogbook();
            SyncRoads();
            PlaceRoadButtons();
            if (roadChoice && !optionsOpen)
            {
                LockButtons();
                EnableRoadChoice();
            }
            else if (optionsOpen)
            {
                LockButtons();
                EnableUnder(optionsRoot);
                gearButton.interactable = true;
            }
            else
            {
                ApplyFreeButtons();
            }

            UpdateLive();
        }

        void ShowRoadChoice(bool open)
        {
            bool encounter = session.EncounterPending;
            bool camp = session.CampPending && !encounter;
            bool pace = open && !camp && !encounter;
            bool resume = pace && session.AwaitingPace;
            cardTitle.text = encounter ? session.EncounterId : camp ? "Make camp" : "The road";
            cardBody.text = encounter ? "Nothing comes of this yet." : camp ? "The travel hours are spent." : "Choose a pace.";
            rushButton.gameObject.SetActive(pace);
            steadyButton.gameObject.SetActive(pace);
            carefulButton.gameObject.SetActive(pace);
            dawnButton.gameObject.SetActive(resume);
            duskButton.gameObject.SetActive(resume);
            confirmPaceButton.gameObject.SetActive(pace);
            backPaceButton.gameObject.SetActive(choosingRoad && !session.AwaitingPace && !encounter);
            campButton.gameObject.SetActive(camp);
            continueButton.gameObject.SetActive(encounter);
            ShowDeparture(pace);
            if (!pace)
                return;
            TintPace(rushButton, draftPace == RoadPace.Rush);
            TintPace(steadyButton, draftPace == RoadPace.Steady);
            TintPace(carefulButton, draftPace == RoadPace.Careful);
        }

        void ShowDeparture(bool pace)
        {
            var label = confirmPaceButton.GetComponentInChildren<TextMeshProUGUI>();
            label.richText = true;
            label.color = Color.white;
            if (!pace)
            {
                label.text = "<color=" + InkHex + ">Take the road</color>";
                return;
            }

            PlaceId dest = session.AwaitingPace ? session.LegDestination : pendingDest;
            float dayHours;
            float nightHours;
            session.SplitUntilStop(dest, draftPace, out dayHours, out nightHours);
            if (dayHours + nightHours <= 0.001f)
            {
                label.text = "<color=" + InkHex + ">Take the road</color>";
                return;
            }

            label.text = "<color=" + InkHex + ">Take the road</color>\n<color=" + DayHex + ">"
                + Amount(dayHours) + " h day</color>  <color=" + NightHex + ">"
                + Amount(nightHours) + " h night</color>";
        }

        void EnableRoadChoice()
        {
            if (session.EncounterPending)
            {
                continueButton.interactable = true;
                return;
            }

            if (session.CampPending)
            {
                campButton.interactable = true;
                return;
            }

            rushButton.interactable = true;
            steadyButton.interactable = true;
            carefulButton.interactable = true;
            confirmPaceButton.interactable = true;
            backPaceButton.interactable = choosingRoad;
            if (session.AwaitingPace)
            {
                dawnButton.interactable = session.CanWaitUntilDawn();
                duskButton.interactable = session.CanWaitUntilDusk();
            }
        }

        void RebuildMarket()
        {
            if (wagonPick >= session.CargoCount)
                wagonPick = -1;
            ClearChildren(shopHost);
            ClearChildren(wagonHost);
            buttons.RemoveAll(button => button == null);

            if (page != Page.Market || logbookOpen || optionsOpen || choosingRoad
                || session.CampPending || session.AwaitingPace || session.EncounterPending)
                sortMenuOpen = false;

            List<GoodId> goods = OrderShop();
            for (int i = 0; i < goods.Count; i++)
            {
                GoodId captured = goods[i];
                int stock = session.StockOf(session.Dock, captured);
                DemandBand band = EconomyCatalog.DemandOf(session.Dock, captured, stock);
                MakeSquare(shopHost, captured.ToString(), CaravanSession.GoodName(captured), stock.ToString(),
                    shopSelected && pickedGood == captured, band, stock <= 0, () => SelectShop(captured));
            }

            int slots = EconomyRates.WagonSpace;
            for (int i = 0; i < session.CargoCount && i < slots; i++)
            {
                int captured = i;
                GoodId good = session.CargoGoodAt(i);
                int stock = session.StockOf(session.Dock, good);
                DemandBand band = EconomyCatalog.DemandOf(session.Dock, good, stock);
                MakeSquare(wagonHost, "Slot" + i, CaravanSession.GoodName(good), null,
                    wagonPick == i, band, false, () => SelectWagon(captured));
            }

            for (int i = session.CargoCount; i < slots; i++)
                MakeSquare(wagonHost, "Empty" + i, null, null, false, DemandBand.None, true, null);

            weightText.text = "Weight " + session.CargoWeight() + "/" + EconomyRates.WagonWeight;
            detailText.text = DetailLine();
            sortMenu.gameObject.SetActive(sortMenuOpen);
            for (int i = 0; i < sortChoices.Count; i++)
                TintPace(sortChoices[i], sortChoiceKinds[i] == shopSort);
            FitGrids(goods.Count, slots);
        }

        List<GoodId> OrderShop()
        {
            var goods = new List<GoodId>();
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                if (good != GoodId.Rations)
                    goods.Add(good);
            }

            if (shopSort != ShopSort.Catalog)
                goods.Sort(CompareShop);
            return goods;
        }

        int CompareShop(GoodId a, GoodId b)
        {
            int order = 0;
            if (shopSort == ShopSort.Name)
                order = string.Compare(CaravanSession.GoodName(a), CaravanSession.GoodName(b), System.StringComparison.OrdinalIgnoreCase);
            else if (shopSort == ShopSort.PriceHigh)
                order = session.BuyCost(session.Dock, b).CompareTo(session.BuyCost(session.Dock, a));
            else if (shopSort == ShopSort.PriceLow)
                order = session.BuyCost(session.Dock, a).CompareTo(session.BuyCost(session.Dock, b));
            else if (shopSort == ShopSort.Demand)
                order = DemandRank(a).CompareTo(DemandRank(b));
            if (order != 0)
                return order;
            return ((int)a).CompareTo((int)b);
        }

        int DemandRank(GoodId good)
        {
            switch (EconomyCatalog.DemandOf(session.Dock, good, session.StockOf(session.Dock, good)))
            {
                case DemandBand.High:
                    return 0;
                case DemandBand.Mid:
                    return 1;
                case DemandBand.Low:
                    return 2;
                default:
                    return 3;
            }
        }

        void ToggleSortMenu()
        {
            sortMenuOpen = !sortMenuOpen;
            drawn = -1;
        }

        void ChooseSort(ShopSort sort)
        {
            shopSort = sort;
            sortMenuOpen = false;
            drawn = -1;
        }

        void FitGrids(int shopCount, int slots)
        {
            Canvas.ForceUpdateCanvases();
            FitGrid(shopGrid, shopHost, ShopColumns, RowsOf(shopCount, ShopColumns));
            FitGrid(wagonGrid, wagonHost, WagonColumns, RowsOf(slots, WagonColumns));
        }

        static int RowsOf(int count, int columns)
        {
            if (count <= 0 || columns <= 0)
                return 1;
            return (count + columns - 1) / columns;
        }

        static void FitGrid(GridLayoutGroup grid, RectTransform host, int columns, int rows)
        {
            float width = host.rect.width;
            float height = host.rect.height;
            if (width < 10f || height < 10f || columns <= 0 || rows <= 0)
                return;
            float cellW = (width - grid.padding.horizontal - (columns - 1) * grid.spacing.x) / columns;
            float cellH = (height - grid.padding.vertical - (rows - 1) * grid.spacing.y) / rows;
            if (cellW < 8f || cellH < 8f)
                return;
            grid.cellSize = new Vector2(cellW, cellH);
        }

        void SelectShop(GoodId good)
        {
            shopSelected = true;
            pickedGood = good;
            wagonPick = -1;
            sortMenuOpen = false;
            drawn = -1;
        }

        void SelectWagon(int index)
        {
            if (index < 0 || index >= session.CargoCount)
                return;
            shopSelected = false;
            wagonPick = index;
            pickedGood = session.CargoGoodAt(index);
            sortMenuOpen = false;
            drawn = -1;
        }

        void PressBuy()
        {
            if (!SelectionLive())
                return;
            GoodId good = SelectedGood();
            if (session.TryBuy(good))
                MarketSounds.PlayBuy(good);
        }

        void PressSell()
        {
            if (wagonPick >= 0 && wagonPick < session.CargoCount)
            {
                if (session.TrySellStack(wagonPick))
                {
                    wagonPick = -1;
                    MarketSounds.PlaySale();
                }
                return;
            }

            if (shopSelected && session.TrySell(pickedGood))
                MarketSounds.PlaySale();
        }

        bool SelectionLive()
        {
            if (wagonPick >= 0)
                return wagonPick < session.CargoCount;
            return shopSelected;
        }

        bool CanPressSell()
        {
            if (wagonPick >= 0 && wagonPick < session.CargoCount)
                return session.CanSellStack(wagonPick);
            if (shopSelected)
                return session.CanSell(pickedGood);
            return false;
        }

        GoodId SelectedGood()
        {
            if (wagonPick >= 0 && wagonPick < session.CargoCount)
                return session.CargoGoodAt(wagonPick);
            return pickedGood;
        }

        void NoteSelection()
        {
            if (!MarketInFront())
                return;
            if (wagonPick >= 0 && wagonPick < session.CargoCount)
            {
                session.NoteShownSale(wagonPick);
                return;
            }

            if (shopSelected)
                session.NoteShownBuy(pickedGood);
        }

        string DetailLine()
        {
            if (!SelectionLive())
                return string.Empty;
            GoodId good = SelectedGood();
            bool wagon = wagonPick >= 0 && wagonPick < session.CargoCount;
            var parts = new List<string>();
            if (wagon)
            {
                if (EconomyCatalog.Buys(session.Dock, good))
                    parts.Add(session.SellValueAt(wagonPick) + " coin");
            }
            else
            {
                parts.Add(session.BuyCost(session.Dock, good) + " coin");
            }

            if (EconomyCatalog.IsContraband(session.Dock, good))
                parts.Add("contraband");
            DemandBand band = EconomyCatalog.DemandOf(session.Dock, good, session.StockOf(session.Dock, good));
            if (band == DemandBand.High)
                parts.Add("high demand");
            else if (band == DemandBand.Mid)
                parts.Add("mid demand");
            else if (band == DemandBand.Low)
                parts.Add("low demand");
            return string.Join("  ·  ", parts);
        }

        void MakeSquare(RectTransform host, string name, string label, string count, bool selected, DemandBand band, bool dim, UnityEngine.Events.UnityAction action)
        {
            var rect = Child(host, name);
            var ring = rect.gameObject.AddComponent<Image>();
            ring.sprite = quad;
            ring.color = selected ? Ink : BandColor(band);
            ring.raycastTarget = action != null;
            if (action != null)
            {
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = ring;
                button.transition = Selectable.Transition.None;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.onClick.AddListener(action);
                buttons.Add(button);
            }

            var rim = ImageOn(Child(rect, "Rim"), BandColor(band), false);
            Stretch(rim.rectTransform, 4f);
            var face = ImageOn(Child(rect, "Face"), dim ? FaceDim : Field, false);
            Stretch(face.rectTransform, 8f);
            if (string.IsNullOrEmpty(label))
                return;

            var text = Body(rect, 18, TextAlignmentOptions.Center);
            text.text = label;
            text.color = dim ? Quiet : Ink;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 16f;
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            float bottom = string.IsNullOrEmpty(count) ? 6f : 20f;
            textRt.offsetMin = new Vector2(6f, bottom);
            textRt.offsetMax = new Vector2(-6f, -6f);
            if (string.IsNullOrEmpty(count))
                return;

            var countText = Body(rect, 14, TextAlignmentOptions.BottomRight);
            countText.text = count;
            countText.color = dim ? Quiet : Ink;
            countText.textWrappingMode = TextWrappingModes.NoWrap;
            var countRt = countText.rectTransform;
            countRt.anchorMin = new Vector2(1f, 0f);
            countRt.anchorMax = new Vector2(1f, 0f);
            countRt.pivot = new Vector2(1f, 0f);
            countRt.sizeDelta = new Vector2(48f, 20f);
            countRt.anchoredPosition = new Vector2(-4f, 3f);
        }

        static Color BandColor(DemandBand band)
        {
            switch (band)
            {
                case DemandBand.High:
                    return DemandHigh;
                case DemandBand.Mid:
                    return DemandMid;
                case DemandBand.Low:
                    return DemandLow;
                default:
                    return RimQuiet;
            }
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
            if (roadsReady && roadsDock == session.Dock)
                return;

            ClearNeighborRoads();
            IReadOnlyList<PlaceId> roads = session.RoadsOut();
            for (int i = 0; i < roads.Count; i++)
            {
                PlaceId dest = roads[i];
                string label = "Take the road to " + CaravanSession.PlaceName(dest);
                Button button = StackButton(gatesColumn, label, () => AskRoad(dest));
                button.transform.SetSiblingIndex(i);
                neighborRoads.Add(button);
                neighborDests.Add(dest);
            }

            roadsDock = session.Dock;
            roadsReady = true;
            PlaceRoadButtons();
        }

        void PlaceRoadButtons()
        {
            bool onMap = lookingAtMap && !session.Traveling;
            RectTransform host = onMap ? mapRoads : gatesColumn;
            for (int i = 0; i < neighborRoads.Count; i++)
            {
                Button button = neighborRoads[i];
                if (button == null || host == null)
                    continue;
                button.transform.SetParent(host, false);
                button.transform.SetSiblingIndex(i);
            }

            if (mapRoads != null)
                mapRoads.gameObject.SetActive(onMap);
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
            neighborDests.Clear();
            roadLit = false;
            roadsReady = false;
        }

        void AskRoad(PlaceId dest)
        {
            if (!session.CanTakeRoad())
                return;
            pendingDest = dest;
            roadLit = true;
            Redraw();
        }

        void Embark()
        {
            if (!roadLit || !session.CanTakeRoad())
                return;
            session.ChooseRoadPace(draftPace);
            PlaceId dest = pendingDest;
            roadLit = false;
            lookingAtMap = false;
            session.DepartTo(dest);
        }

        string NextCampLine()
        {
            if (!roadLit)
                return string.Empty;
            float dayHours;
            float nightHours;
            session.SplitUntilStop(pendingDest, draftPace, out dayHours, out nightHours);
            if (dayHours + nightHours <= 0.001f)
                return "Travel to next camp";
            return "Travel to next camp\n<color=" + DayHex + ">"
                + Amount(dayHours) + " h day</color>  <color=" + NightHex + ">"
                + Amount(nightHours) + " h night</color>";
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
                if (neighborRoads[i] == null)
                    continue;
                neighborRoads[i].interactable = session.CanTakeRoad();
                TintPace(neighborRoads[i], roadLit && neighborDests[i] == pendingDest);
            }
            gatesCareful.interactable = true;
            gatesSteady.interactable = true;
            gatesRush.interactable = true;
            TintPace(gatesCareful, draftPace == RoadPace.Careful);
            TintPace(gatesSteady, draftPace == RoadPace.Steady);
            TintPace(gatesRush, draftPace == RoadPace.Rush);
            embarkButton.interactable = roadLit && session.CanTakeRoad();
            buyButton.interactable = SelectionLive() && session.CanBuy(SelectedGood());
            sellButton.interactable = CanPressSell();
            sortButton.interactable = true;
            for (int i = 0; i < sortChoices.Count; i++)
                sortChoices[i].interactable = true;
            innTab.interactable = true;
            gatesTab.interactable = true;
            tavernTab.interactable = true;
            stablesTab.interactable = true;
            innWaitButton.interactable = session.CanWait();
            innDawnButton.interactable = session.CanWaitUntilDawn();
            innDuskButton.interactable = session.CanWaitUntilDusk();
            restButton.interactable = session.CanRest();
            waterButton.interactable = session.CanFillWater();
            foodButton.interactable = session.CanFillFood();
            listenButton.interactable = true;
            pauseButton.interactable = session.Traveling && !session.HoldTravel;
            pace1.interactable = session.Traveling && !session.HoldTravel;
            pace2.interactable = session.Traveling && !session.HoldTravel;
            pace4.interactable = session.Traveling && !session.HoldTravel;
            backButton.interactable = true;
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
            travelText.text = "Travel " + session.TravelHours.ToString("0.#") + " h";
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
            FitMapFrame();
            EnsureMap();
            float sun = TravelScale.Sunlight(session.Hour);
            if (mapImage.texture != null)
                mapImage.color = Color.Lerp(new Color(0.55f, 0.62f, 0.8f), Color.white, sun);

            Vector2 point = session.CaravanPoint;
            bool close = session.Traveling;
            float radius = session.VisionRadius();
            PlaceSheet(point, close, radius);
            UpdateRoute();
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

            bool shade = close && fogMaterial != null;
            if (fogImage != null)
                fogImage.gameObject.SetActive(shade);
            if (!shade)
                return;
            fogMaterial.SetVector("_Center", new Vector4(point.x, 1f - point.y, 0f, 0f));
            fogMaterial.SetFloat("_Radius", radius);
            fogMaterial.SetFloat("_Soft", radius * 0.1f);
            fogMaterial.SetFloat("_Aspect", RoadPath.Aspect);
        }

        void FitMapFrame()
        {
            if (mapFrame == null)
                return;
            if (session.Traveling || lookingAtMap)
            {
                Stretch(mapFrame, 12f);
                return;
            }

            mapFrame.anchorMin = Vector2.zero;
            mapFrame.anchorMax = Vector2.one;
            mapFrame.offsetMin = new Vector2(GatesPanelRight + 12f, TabHeight + 12f);
            mapFrame.offsetMax = new Vector2(-12f, -(BannerHeight + 64f + 12f));
        }

        void UpdateRoute()
        {
            if (routeRoot == null)
                return;
            PlaceId dest = session.Dock;
            bool show = false;
            if (session.Traveling || session.CampPending || session.AwaitingPace || session.EncounterPending)
            {
                dest = session.LegDestination;
                show = true;
            }
            else if (choosingRoad || roadLit)
            {
                dest = pendingDest;
                show = true;
            }

            if (!show)
            {
                routeRoot.gameObject.SetActive(false);
                return;
            }

            routeRoot.gameObject.SetActive(true);
            PlaceOnMap(routeMark.rectTransform, RoadGraph.DockPoint(dest), 72f);
            int count = 0;
            if (!RoadGraph.IsSea(session.Dock, dest))
            {
                IReadOnlyList<Vector2> samples = RoadGraph.Samples(session.Dock, dest);
                if (samples != null && samples.Count > 1)
                    count = samples.Count - 1;
                EnsureRouteSegments(count);
                for (int i = 0; i < count; i++)
                {
                    routeSegments[i].gameObject.SetActive(true);
                    PlaceSegment(routeSegments[i].rectTransform, samples[i], samples[i + 1], 8f);
                }
            }

            for (int i = count; i < routeSegments.Count; i++)
                routeSegments[i].gameObject.SetActive(false);
        }

        void EnsureRouteSegments(int count)
        {
            while (routeSegments.Count < count)
            {
                Image segment = ImageOn(Child(routeRoot, "Leg"), new Color(0.93f, 0.78f, 0.35f, 0.95f), false);
                routeSegments.Add(segment);
            }
        }

        void PlaceSegment(RectTransform rect, Vector2 fromTop, Vector2 toTop, float thickness)
        {
            var start = new Vector2(fromTop.x, 1f - fromTop.y);
            var end = new Vector2(toTop.x, 1f - toTop.y);
            var mid = (start + end) * 0.5f;
            rect.anchorMin = mid;
            rect.anchorMax = mid;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            Rect space = routeRoot.rect;
            var delta = new Vector2((end.x - start.x) * space.width, (end.y - start.y) * space.height);
            rect.sizeDelta = new Vector2(Mathf.Max(delta.magnitude, 1f), thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.localScale = Vector3.one;
        }

        void PlaceSheet(Vector2 topLeft, bool close, float radius)
        {
            if (mapSheet == null)
                return;
            Rect view = mapRoot.rect;
            if (view.width < 2f || view.height < 2f)
                return;
            mapSheet.sizeDelta = new Vector2(view.width, view.height);
            if (!close || radius < 0.0001f)
            {
                mapSheet.pivot = new Vector2(0.5f, 0.5f);
                mapSheet.localScale = Vector3.one;
                mapSheet.anchoredPosition = Vector2.zero;
                return;
            }

            mapSheet.pivot = new Vector2(topLeft.x, 1f - topLeft.y);
            mapSheet.localScale = new Vector3(1f / (2f * radius), 1f / (2f * radius), 1f);
            mapSheet.anchoredPosition = Vector2.zero;
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
            Redraw();
        }

        void OpenPlaza()
        {
            page = Page.Plaza;
            Redraw();
        }

        void ShowPlazaSpot()
        {
            innSpot.SetActive(plazaSpot == PlazaSpot.Inn);
            gatesSpot.SetActive(plazaSpot == PlazaSpot.Gates);
            tavernSpot.SetActive(plazaSpot == PlazaSpot.Tavern);
            stablesSpot.SetActive(plazaSpot == PlazaSpot.Stables);
            TintPace(innTab, plazaSpot == PlazaSpot.Inn);
            TintPace(gatesTab, plazaSpot == PlazaSpot.Gates);
            TintPace(tavernTab, plazaSpot == PlazaSpot.Tavern);
            TintPace(stablesTab, plazaSpot == PlazaSpot.Stables);
        }

        void OpenHouse()
        {
            page = Page.House;
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

        string FillLabel(string verb, GoodId good, float held, float capacity)
        {
            if (held >= capacity - 0.0001f)
                return verb + " · full";
            if (session.StockOf(session.Dock, good) <= 0)
                return verb + " · none on the shelf";
            return verb + " · " + session.BuyCost(session.Dock, good) + " coin";
        }

        string CargoLine()
        {
            var parts = new List<string>();
            parts.Add("Water " + Amount(session.WaterLitres) + "/" + Amount(EconomyRates.CanteenLitres) + " L");
            parts.Add("Rations " + Amount(session.RationKg) + "/" + Amount(EconomyRates.RationBagKg) + " kg");
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
                return string.Join("  ", parts);
            if (extra > 0)
                parts.Add("+" + extra);
            return string.Join("  ", parts);
        }

        static string Amount(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
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

        static Sprite MakeRing()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outer = size * 0.46f;
            float inner = size * 0.30f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 0f;
                    if (dist <= outer && dist >= inner)
                        alpha = 1f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
