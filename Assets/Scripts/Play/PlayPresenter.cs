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
        const float TopBarHeight = UiType.Bar;
        const float TabHeight = UiType.Bar;
        const float WagonPanelWidth = 440f;
        const float DetailWidth = 300f;
        const float FrameGap = UiType.Gap;
        const float FrameHeader = 72f;
        const float WeightLine = 40f;
        const float MeterHeight = 72f;
        const float ShopCellW = 158f;
        const float ShopCellH = 120f;
        const float WagonCellW = 236f;
        const float WagonCellH = 140f;
        const float RoadPanelWidth = 540f;
        const float GatesPanelRight = UiType.Gap + RoadPanelWidth;
        const float LogGoodW = 240f;
        const float LogPriceW = 300f;

        // Off hides the tavern, the stables, the house room, and Gear. Those rooms are still placeholders.
        public static bool ShowUnfinishedFeatures;
        const int ShopColumns = 8;
        const int WagonColumns = 2;

        static readonly Color Ink = UiType.Cream;
        static readonly Color Quiet = new Color(0.78f, 0.7f, 0.58f, 1f);
        static readonly Color Panel = new Color(0.2f, 0.15f, 0.11f, 0.94f);
        static readonly Color Bar = new Color(0.12f, 0.09f, 0.07f, 0.96f);
        static readonly Color ButtonFace = new Color(0.45f, 0.32f, 0.22f, 1f);
        static readonly Color ButtonHot = new Color(0.66f, 0.46f, 0.28f, 1f);
        static readonly Color ButtonDown = new Color(0.3f, 0.2f, 0.14f, 1f);
        static readonly Color ButtonOff = new Color(0.24f, 0.19f, 0.16f, 0.85f);
        static readonly Color Field = new Color(0.28f, 0.21f, 0.16f, 1f);
        static readonly Color FaceDim = new Color(0.18f, 0.14f, 0.11f, 1f);
        static readonly Color WagonGround = new Color(0.32f, 0.23f, 0.16f, 1f);
        static readonly Color ParchmentInk = new Color(0.28f, 0.16f, 0.09f, 1f);
        const string InkHex = "#EADCC0";
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
        TextMeshProUGUI coinText;
        TextMeshProUGUI cargoText;
        TextMeshProUGUI outlookText;
        TextMeshProUGUI listenText;
        TextMeshProUGUI legText;
        TextMeshProUGUI housePageText;
        RectTransform shopHost;
        RectTransform wagonHost;
        GridLayoutGroup shopGrid;
        GridLayoutGroup wagonGrid;
        RectTransform sortMenu;
        RectTransform hoverChip;
        TextMeshProUGUI hoverLabel;
        TextMeshProUGUI detailText;
        TextMeshProUGUI detailName;
        TextMeshProUGUI weightText;
        Image detailCell;
        RawImage detailIcon;
        Image meterFill;
        RawImage groundImage;
        AspectRatioFitter groundFit;
        RectTransform gatesColumn;
        GameObject innSpot;
        GameObject gatesSpot;
        GameObject tavernSpot;
        GameObject stablesSpot;
        PlaceId roadsDock;
        bool roadsReady;
        Button marketTab;
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
        TextMeshProUGUI logbookDate;
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
            session = new CaravanSession();
            EnsureEventSystem();
            quad = MakeSprite(false);
            disc = MakeSprite(true);
            ring = MakeRing();
            font = MakeFont();
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.62f, 0.48f, 0.32f, 1f);
            }
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

            var ground = Child(root, "Ground");
            Stretch(ground);
            var plate = Child(ground, "Plate");
            Stretch(plate);
            groundImage = plate.gameObject.AddComponent<RawImage>();
            groundImage.raycastTarget = false;
            groundFit = plate.gameObject.AddComponent<AspectRatioFitter>();
            groundFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            groundFit.aspectRatio = 16f / 9f;
            ApplyGround(session.Dock);

            dock = Child(root, "Dock");
            FillBelowBar(dock);
            travel = Child(root, "Travel");
            FillBelowBar(travel);

            BuildDock();
            BuildTravel();
            BuildTopBar(root);
            BuildLogbook(root);
            dimmer = ImageOn(Child(root, "Dimmer"), new Color(0.05f, 0.04f, 0.03f, 0.72f), true);
            Stretch(dimmer.rectTransform);
            RaiseCanvas(dimmer.gameObject, 12);
            BuildRoadChoice(root);
            BuildOptions(root);
        }

        void BuildDock()
        {
            var body = Child(dock, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(0f, TabHeight);
            body.offsetMax = Vector2.zero;

            marketPage = BuildMarket(body);
            plazaPage = BuildPlaza(body);
            housePage = BuildHousePage(body);

            var tabs = DarkBar(Child(dock, "Tabs"), false);
            BottomStrip(tabs.rectTransform, TabHeight);
            var row = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = UiType.Gap;
            row.padding = new RectOffset(28, 28, 6, 6);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            ApprovedArt art = ApprovedArt.Load();
            marketTab = BarSeal(tabs.rectTransform, "Market", art != null ? art.sealMarket : null, art != null ? art.sealMarketHover : null, art != null ? art.sealMarketPressed : null, OpenMarket, UiType.Tab);
            innTab = BarSeal(tabs.rectTransform, "Inn", art != null ? art.sealInn : null, art != null ? art.sealInnHover : null, art != null ? art.sealInnPressed : null, OpenInn, UiType.Tab);
            houseTab = BarSeal(tabs.rectTransform, "House", art != null ? art.sealHouse : null, art != null ? art.sealHouseHover : null, art != null ? art.sealHousePressed : null, OpenHouse, UiType.Tab);
            gatesTab = BarSeal(tabs.rectTransform, "Gates", art != null ? art.sealGates : null, art != null ? art.sealGatesHover : null, art != null ? art.sealGatesPressed : null, OpenGates, UiType.Tab);
            tavernTab = BarSeal(tabs.rectTransform, "Tavern", null, null, null, OpenTavern, UiType.Tab);
            stablesTab = BarSeal(tabs.rectTransform, "Stables", null, null, null, OpenStables, UiType.Tab);
        }

        GameObject BuildMarket(RectTransform body)
        {
            var page = Child(body, "Market");
            Stretch(page);

            var panel = FrameSheet(Child(page, "MarketPanel"), false);
            Stretch(panel.rectTransform, FrameGap);

            var columns = Child(panel.rectTransform, "Columns");
            Stretch(columns, 44f);
            var row = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 0f;
            row.padding = new RectOffset(8, 8, 8, 8);
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var shop = Child(columns, "Shop");
            var shopFit = shop.gameObject.AddComponent<LayoutElement>();
            shopFit.minWidth = 0f;
            shopFit.preferredWidth = 0f;
            shopFit.flexibleWidth = 1f;
            var shopCol = shop.gameObject.AddComponent<VerticalLayoutGroup>();
            shopCol.spacing = UiType.Space;
            shopCol.padding = new RectOffset(8, 8, 0, 0);
            shopCol.childAlignment = TextAnchor.UpperLeft;
            shopCol.childControlWidth = true;
            shopCol.childControlHeight = true;
            shopCol.childForceExpandWidth = true;
            shopCol.childForceExpandHeight = false;

            var shopHead = Child(shop, "Head");
            shopHead.gameObject.AddComponent<LayoutElement>().preferredHeight = FrameHeader;
            var shopRow = shopHead.gameObject.AddComponent<HorizontalLayoutGroup>();
            shopRow.spacing = UiType.Space;
            shopRow.padding = new RectOffset(4, 4, 4, 4);
            shopRow.childAlignment = TextAnchor.MiddleLeft;
            shopRow.childControlWidth = true;
            shopRow.childControlHeight = true;
            shopRow.childForceExpandWidth = false;
            shopRow.childForceExpandHeight = false;
            var shopTitle = Body(shopHead, UiType.Header, TextAlignmentOptions.MidlineLeft);
            shopTitle.color = FrontUi.Gold;
            shopTitle.text = "Settlement Sells";
            shopTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            sortButton = Tab(shopHead, "Sort", ToggleSortMenu);
            SquareSeal(sortButton, 64f);

            shopHost = Child(shop, "Cells");
            var shopHostFit = shopHost.gameObject.AddComponent<LayoutElement>();
            shopHostFit.minWidth = 0f;
            shopHostFit.preferredWidth = 0f;
            shopHostFit.flexibleHeight = 1f;
            shopHostFit.flexibleWidth = 1f;
            shopHost.gameObject.AddComponent<RectMask2D>();
            shopGrid = shopHost.gameObject.AddComponent<GridLayoutGroup>();
            shopGrid.cellSize = new Vector2(ShopCellW, ShopCellH);
            shopGrid.spacing = new Vector2(8f, 8f);
            shopGrid.padding = new RectOffset(4, 4, 4, 4);
            shopGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            shopGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            shopGrid.childAlignment = TextAnchor.UpperCenter;
            shopGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            shopGrid.constraintCount = ShopColumns;

            ColumnRule(columns);

            var detail = Child(columns, "Detail");
            var detailFit = detail.gameObject.AddComponent<LayoutElement>();
            detailFit.minWidth = DetailWidth;
            detailFit.preferredWidth = DetailWidth;
            detailFit.flexibleWidth = 0f;
            var detailColumn = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            detailColumn.spacing = UiType.Space;
            detailColumn.padding = new RectOffset(8, 8, 4, 4);
            detailColumn.childAlignment = TextAnchor.UpperCenter;
            detailColumn.childControlWidth = true;
            detailColumn.childControlHeight = true;
            detailColumn.childForceExpandWidth = false;
            detailColumn.childForceExpandHeight = false;
            detailCell = CellFace(Child(detail, "Preview"), false);
            var previewFit = detailCell.gameObject.AddComponent<LayoutElement>();
            previewFit.preferredHeight = 168f;
            previewFit.preferredWidth = 168f;
            detailIcon = GoodsPicture(detailCell.rectTransform, null);
            detailName = Body(detail, UiType.DetailName, TextAlignmentOptions.Center);
            detailName.color = Quiet;
            detailName.text = "Select a good";
            detailName.overflowMode = TextOverflowModes.Ellipsis;
            var nameFit = detailName.gameObject.AddComponent<LayoutElement>();
            nameFit.preferredHeight = 80f;
            nameFit.preferredWidth = 268f;
            detailText = Body(detail, UiType.Price, TextAlignmentOptions.Center);
            detailText.color = Ink;
            detailText.overflowMode = TextOverflowModes.Ellipsis;
            var lineFit = detailText.gameObject.AddComponent<LayoutElement>();
            lineFit.preferredHeight = 96f;
            lineFit.preferredWidth = 268f;
            var trades = Child(detail, "Trades");
            trades.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
            var tradeRow = trades.gameObject.AddComponent<HorizontalLayoutGroup>();
            tradeRow.spacing = UiType.Gap;
            tradeRow.childAlignment = TextAnchor.MiddleCenter;
            tradeRow.childControlWidth = true;
            tradeRow.childControlHeight = true;
            tradeRow.childForceExpandWidth = true;
            tradeRow.childForceExpandHeight = true;
            ApprovedArt art = ApprovedArt.Load();
            buyButton = TradeSeal(trades, "Buy", art != null ? art.sealBuy : null, art != null ? art.sealBuyHover : null, art != null ? art.sealBuyPressed : null, PressBuy);
            sellButton = TradeSeal(trades, "Sell", art != null ? art.sealSell : null, art != null ? art.sealSellHover : null, art != null ? art.sealSellPressed : null, PressSell);
            var detailSpacer = Child(detail, "Spacer");
            detailSpacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var meter = Child(detail, "Meter");
            meter.gameObject.AddComponent<LayoutElement>().preferredHeight = MeterHeight;
            BuildMeter(meter);

            ColumnRule(columns);

            var wagon = Child(columns, "Wagon");
            var wagonFit = wagon.gameObject.AddComponent<LayoutElement>();
            wagonFit.minWidth = WagonPanelWidth;
            wagonFit.preferredWidth = WagonPanelWidth;
            wagonFit.flexibleWidth = 0f;
            var wagonCol = wagon.gameObject.AddComponent<VerticalLayoutGroup>();
            wagonCol.spacing = UiType.Space;
            wagonCol.padding = new RectOffset(8, 8, 0, 0);
            wagonCol.childAlignment = TextAnchor.UpperCenter;
            wagonCol.childControlWidth = true;
            wagonCol.childControlHeight = true;
            wagonCol.childForceExpandWidth = true;
            wagonCol.childForceExpandHeight = false;
            var wagonTitle = Body(wagon, UiType.Header, TextAlignmentOptions.Center);
            wagonTitle.color = FrontUi.Gold;
            wagonTitle.text = "Your Wagon";
            wagonTitle.textWrappingMode = TextWrappingModes.NoWrap;
            var wagonTitleFit = wagonTitle.gameObject.AddComponent<LayoutElement>();
            wagonTitleFit.preferredHeight = FrameHeader;
            wagonTitleFit.flexibleWidth = 1f;

            wagonHost = Child(wagon, "Slots");
            var wagonHostFit = wagonHost.gameObject.AddComponent<LayoutElement>();
            wagonHostFit.minWidth = 0f;
            wagonHostFit.preferredWidth = 0f;
            wagonHostFit.flexibleHeight = 1f;
            wagonHostFit.flexibleWidth = 1f;
            wagonHost.gameObject.AddComponent<RectMask2D>();
            wagonGrid = wagonHost.gameObject.AddComponent<GridLayoutGroup>();
            wagonGrid.cellSize = new Vector2(WagonCellW, WagonCellH);
            wagonGrid.spacing = new Vector2(8f, 8f);
            wagonGrid.padding = new RectOffset(4, 4, 4, 4);
            wagonGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            wagonGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            wagonGrid.childAlignment = TextAnchor.UpperCenter;
            wagonGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            wagonGrid.constraintCount = WagonColumns;

            weightText = Body(wagon, UiType.Body, TextAlignmentOptions.Center);
            weightText.color = Ink;
            weightText.gameObject.AddComponent<LayoutElement>().preferredHeight = WeightLine;

            sortMenu = Child(shopHead, "SortMenu");
            sortMenu.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            DarkBar(sortMenu, true);
            sortMenu.anchorMin = new Vector2(1f, 0f);
            sortMenu.anchorMax = new Vector2(1f, 0f);
            sortMenu.pivot = new Vector2(1f, 1f);
            sortMenu.anchoredPosition = new Vector2(0f, -4f);
            sortMenu.sizeDelta = new Vector2(420f, 248f);
            var menuColumn = sortMenu.gameObject.AddComponent<VerticalLayoutGroup>();
            menuColumn.spacing = 4f;
            menuColumn.padding = new RectOffset(12, 12, 12, 12);
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

            hoverChip = Child(panel.rectTransform, "Hover");
            DarkBar(hoverChip, false);
            hoverChip.anchorMin = new Vector2(0.5f, 0.5f);
            hoverChip.anchorMax = new Vector2(0.5f, 0.5f);
            hoverChip.pivot = new Vector2(0.5f, 0f);
            hoverChip.sizeDelta = new Vector2(220f, 40f);
            hoverLabel = Body(hoverChip, UiType.Small, TextAlignmentOptions.Center);
            hoverLabel.color = Ink;
            Stretch(hoverLabel.rectTransform, 8f);
            RaiseCanvas(hoverChip.gameObject, 9);
            hoverChip.gameObject.SetActive(false);
            return page.gameObject;
        }

        void ColumnRule(RectTransform row)
        {
            var rule = Child(row, "Rule");
            var image = rule.gameObject.AddComponent<Image>();
            image.sprite = quad;
            image.color = UiType.Umber;
            image.raycastTarget = false;
            var fit = rule.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 2f;
            fit.flexibleWidth = 0f;
            fit.flexibleHeight = 1f;
        }

        void AddSortChoice(RectTransform menu, string label, ShopSort sort)
        {
            ShopSort captured = sort;
            Button button = MenuLine(menu, label, () => ChooseSort(captured));
            button.GetComponent<LayoutElement>().preferredHeight = 52f;
            sortChoices.Add(button);
            sortChoiceKinds.Add(sort);
        }

        GameObject BuildPlaza(RectTransform body)
        {
            var page = Child(body, "Plaza");
            Stretch(page);

            var inn = Spot(page, "Inn");
            innSpot = inn.gameObject;
            var innColumn = CompactCard(inn, 560f);
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
            gatesColumn.offsetMin = new Vector2(FrameGap, FrameGap);
            gatesColumn.offsetMax = new Vector2(GatesPanelRight, -FrameGap);
            FrameSheet(gatesColumn, true);
            var gatesLayout = gatesColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            gatesLayout.spacing = UiType.Space;
            gatesLayout.padding = new RectOffset(24, 24, 24, 24);
            gatesLayout.childAlignment = TextAnchor.UpperCenter;
            gatesLayout.childControlWidth = true;
            gatesLayout.childControlHeight = true;
            gatesLayout.childForceExpandWidth = true;
            gatesLayout.childForceExpandHeight = false;
            outlookText = Body(gatesColumn, UiType.Body, TextAlignmentOptions.Center);
            outlookText.color = Ink;
            var fit = outlookText.gameObject.AddComponent<LayoutElement>();
            fit.preferredHeight = 120f;
            var spacer = Child(gatesColumn, "Spacer");
            spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var speeds = Child(gatesColumn, "Speeds");
            speeds.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
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
            nextCampText = Body(gatesColumn, UiType.Body, TextAlignmentOptions.Center);
            nextCampText.color = Ink;
            nextCampText.richText = true;
            nextCampText.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;
            embarkButton = StackButton(gatesColumn, "Embark", Embark);

            var tavern = Spot(page, "Tavern");
            tavernSpot = tavern.gameObject;
            var tavernColumn = CompactCard(tavern, UiType.CardMinH);
            listenButton = StackButton(tavernColumn, "Listen", () =>
            {
                listenLine = "Socialize is not in this slice.";
                Redraw();
            });
            listenText = Body(tavernColumn, UiType.Body, TextAlignmentOptions.Center);
            listenText.color = Ink;
            var listenFit = listenText.gameObject.AddComponent<LayoutElement>();
            listenFit.preferredHeight = 72f;

            var stables = Spot(page, "Stables");
            stablesSpot = stables.gameObject;
            var stablesColumn = CompactCard(stables, UiType.CardMinH);
            var stablesText = Body(stablesColumn, UiType.Body, TextAlignmentOptions.Center);
            stablesText.color = Ink;
            stablesText.text = "Mount upgrades are not in this slice.";
            var stablesFit = stablesText.gameObject.AddComponent<LayoutElement>();
            stablesFit.preferredHeight = 80f;
            return page.gameObject;
        }

        RectTransform Spot(RectTransform page, string name)
        {
            var spot = Child(page, name);
            Stretch(spot);
            return spot;
        }

        RectTransform CompactCard(RectTransform spot, float height)
        {
            var card = Child(spot, "Card");
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(UiType.CardMinW, Mathf.Max(height, UiType.CardMinH));
            card.anchoredPosition = Vector2.zero;
            DarkBar(card, false);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiType.Space;
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return card;
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
            var card = CompactCard(page, UiType.CardMinH);
            housePageText = Body(card, UiType.Body, TextAlignmentOptions.Center);
            housePageText.color = Ink;
            housePageText.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            housePageText.text = "Jobs and standing are not in this slice.";
            return page.gameObject;
        }

        void BuildTravel()
        {
            mapFrame = Child(travel, "MapFrame");
            Stretch(mapFrame, 12f);
            var rod = mapFrame.gameObject.AddComponent<Image>();
            rod.sprite = quad;
            rod.color = Color.clear;
            rod.raycastTarget = false;
            mapRoot = Child(mapFrame, "Map");
            Stretch(mapRoot);
            var edge = mapRoot.gameObject.AddComponent<Image>();
            edge.sprite = quad;
            edge.color = UiType.Rod;
            edge.raycastTarget = false;
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
            FrameSheet(mapRoads, false);
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

            var legBar = DarkBar(Child(travel, "Leg"), false);
            TopStrip(legBar.rectTransform, 48f);
            legText = Body(legBar.rectTransform, UiType.Body, TextAlignmentOptions.Center);
            legText.color = Ink;
            Stretch(legText.rectTransform, 4f);

            paceBar = new GameObject("Pace", typeof(RectTransform));
            paceBar.transform.SetParent(travel, false);
            var paceRt = paceBar.GetComponent<RectTransform>();
            BottomStrip(paceRt, UiType.Bar);
            DarkBar(paceRt, false);
            var paceLayout = paceBar.AddComponent<HorizontalLayoutGroup>();
            paceLayout.spacing = 12f;
            paceLayout.childAlignment = TextAnchor.MiddleCenter;
            paceLayout.childControlWidth = true;
            paceLayout.childControlHeight = true;
            paceLayout.childForceExpandWidth = false;
            paceLayout.childForceExpandHeight = false;
            paceLayout.padding = new RectOffset(0, 0, 10, 10);
            pauseButton = Tab(paceRt, "Pause", () => session.SetPaused(true));
            pace1 = Tab(paceRt, "1×", () => session.ChoosePace(1));
            pace2 = Tab(paceRt, "2×", () => session.ChoosePace(2));
            pace4 = Tab(paceRt, "4×", () => session.ChoosePace(4));
            FitSeal(pauseButton, 120f, 60f);
            FitSeal(pace1, 96f, 60f);
            FitSeal(pace2, 96f, 60f);
            FitSeal(pace4, 96f, 60f);

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
            var bar = DarkBar(Child(root, "TopBar"), false);
            TopStrip(bar.rectTransform, TopBarHeight);
            RaiseCanvas(bar.gameObject, 20);

            placeText = Body(bar.rectTransform, UiType.Header, TextAlignmentOptions.MidlineLeft);
            placeText.color = FrontUi.Gold;
            var placeRt = placeText.rectTransform;
            placeRt.anchorMin = Vector2.zero;
            placeRt.anchorMax = new Vector2(0.32f, 1f);
            placeRt.offsetMin = new Vector2(UiType.Gap, 8f);
            placeRt.offsetMax = new Vector2(-8f, -8f);

            clockText = Body(bar.rectTransform, UiType.Hud, TextAlignmentOptions.Center);
            clockText.color = Ink;
            var clockRt = clockText.rectTransform;
            clockRt.anchorMin = new Vector2(0.32f, 0f);
            clockRt.anchorMax = new Vector2(0.62f, 1f);
            clockRt.offsetMin = new Vector2(8f, 8f);
            clockRt.offsetMax = new Vector2(-8f, -8f);

            var cluster = Child(bar.rectTransform, "Resources");
            cluster.anchorMin = new Vector2(0.62f, 0f);
            cluster.anchorMax = Vector2.one;
            cluster.offsetMin = new Vector2(8f, 6f);
            cluster.offsetMax = new Vector2(-UiType.Gap, -6f);
            var row = cluster.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleRight;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            ApprovedArt art = ApprovedArt.Load();
            HudIcon(cluster, art != null ? art.iconScrubstone : null);
            coinText = HudText(cluster, 110f, TextAlignmentOptions.MidlineLeft);
            coinText.color = Ink;
            coinText.fontSize = UiType.Hud;
            HudIcon(cluster, art != null ? art.iconCapacity : null);
            cargoText = HudText(cluster, 72f, TextAlignmentOptions.MidlineLeft);
            cargoText.color = Ink;
            cargoText.fontSize = UiType.Hud;
            logbookButton = BarSeal(cluster, "Logbook", null, null, null, ToggleLogbook);
            gearButton = BarSeal(cluster, "Gear", null, null, null, ToggleOptions);
        }

        void BuildLogbook(RectTransform root)
        {
            logbookRoot = Child(root, "Logbook");
            logbookRoot.anchorMin = Vector2.zero;
            logbookRoot.anchorMax = Vector2.one;
            logbookRoot.offsetMin = new Vector2(UiType.Gap, TabHeight + UiType.Gap);
            logbookRoot.offsetMax = new Vector2(-UiType.Gap, -(TopBarHeight + UiType.Gap));
            DocumentOn(logbookRoot, true);
            RaiseCanvas(logbookRoot.gameObject, 18);

            var dateBand = Child(logbookRoot, "Date");
            TopStrip(dateBand, 48f);
            logbookDate = Body(dateBand, UiType.Body, TextAlignmentOptions.Center);
            logbookDate.color = ParchmentInk;
            Stretch(logbookDate.rectTransform, 4f);

            var header = Child(logbookRoot, "Header");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 52f);
            header.anchoredPosition = new Vector2(0f, -48f);
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.padding = new RectOffset(16, 16, 8, 8);
            headerRow.spacing = 12f;
            headerRow.childAlignment = TextAnchor.MiddleCenter;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = true;
            var iconGap = Child(header, "IconGap");
            iconGap.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
            ChartLabel(header, "Good", LogGoodW);
            ChartLabel(header, "Lowest buy", LogPriceW);
            ChartLabel(header, "Highest sale", LogPriceW);

            var scrollRt = Child(logbookRoot, "Scroll");
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, -108f);
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
            ImageOn(cardRoot, UiType.Backdrop, true);
            var panel = Child(cardRoot, "Panel");
            Center(panel, new Vector2(860f, 520f));
            FrameSheet(panel, true);
            cardTitle = Body(panel, UiType.Header, TextAlignmentOptions.Center);
            cardTitle.color = FrontUi.Gold;
            cardTitle.text = "The road";
            var titleRt = cardTitle.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = Vector2.one;
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-96f, 56f);
            titleRt.anchoredPosition = new Vector2(0f, -48f);
            cardBody = Body(panel, UiType.Body, TextAlignmentOptions.Center);
            cardBody.color = Ink;
            var bodyRt = cardBody.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = Vector2.one;
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.sizeDelta = new Vector2(-96f, 72f);
            bodyRt.anchoredPosition = new Vector2(0f, -112f);
            carefulButton = ChoiceButton(panel, "Careful", new Vector2(-240f, 10f), () => PickPace(RoadPace.Careful));
            steadyButton = ChoiceButton(panel, "Steady", new Vector2(0f, 10f), () => PickPace(RoadPace.Steady));
            rushButton = ChoiceButton(panel, "Rush", new Vector2(240f, 10f), () => PickPace(RoadPace.Rush));
            dawnButton = ChoiceButton(panel, "Wait until dawn", new Vector2(-200f, -70f), () => session.WaitUntilDawn());
            duskButton = ChoiceButton(panel, "Wait until dusk", new Vector2(200f, -70f), () => session.WaitUntilDusk());
            confirmPaceButton = ChoiceButton(panel, "Take the road", new Vector2(0f, -165f), ConfirmPace);
            campButton = ChoiceButton(panel, "Make camp", new Vector2(0f, -20f), () => session.MakeCamp());
            continueButton = ChoiceButton(panel, "Continue", new Vector2(0f, -20f), () => session.ContinueEncounter());
            backPaceButton = ChoiceButton(panel, "Back", new Vector2(300f, -120f), CloseRoadChoice);
            dawnButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 64f);
            duskButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 64f);
            SplitSealLabel(dawnButton, 48f);
            SplitSealLabel(duskButton, 48f);
            confirmPaceButton.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 96f);
            backPaceButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(300f, -165f);
            campButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 56f);
            continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 56f);
            backPaceButton.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 56f);
            var confirmImage = confirmPaceButton.GetComponent<Image>();
            Sprite paceSeal = confirmImage.sprite;
            confirmImage.sprite = quad;
            confirmImage.preserveAspect = false;
            confirmImage.color = new Color(1f, 1f, 1f, 0f);
            Image seal = SealIcon(confirmPaceButton.transform, paceSeal, 48f);
            var sealRt = seal.rectTransform;
            sealRt.anchorMin = sealRt.anchorMax = new Vector2(0.5f, 1f);
            sealRt.pivot = new Vector2(0.5f, 1f);
            sealRt.sizeDelta = new Vector2(48f, 48f);
            sealRt.anchoredPosition = new Vector2(0f, -4f);
            seal.transform.SetAsFirstSibling();
            var confirmLabel = confirmPaceButton.GetComponentInChildren<TextMeshProUGUI>();
            confirmLabel.color = Color.white;
            confirmLabel.richText = true;
            var labelRt = confirmLabel.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(-12f, 44f);
            labelRt.anchoredPosition = new Vector2(0f, 6f);
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

        void SplitSealLabel(Button button, float sealSize)
        {
            var image = button.GetComponent<Image>();
            Sprite sprite = image.sprite;
            image.sprite = quad;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            Image seal = SealIcon(button.transform, sprite, sealSize);
            var sealRt = seal.rectTransform;
            sealRt.anchorMin = sealRt.anchorMax = new Vector2(0f, 0.5f);
            sealRt.pivot = new Vector2(0f, 0.5f);
            sealRt.sizeDelta = new Vector2(sealSize, sealSize);
            sealRt.anchoredPosition = new Vector2(4f, 0f);
            seal.transform.SetAsFirstSibling();
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(sealSize + 18f, 2f);
            labelRt.offsetMax = new Vector2(-4f, -2f);
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

        void BuildOptions(RectTransform root)
        {
            optionsRoot = Child(root, "Options");
            Stretch(optionsRoot);
            RaiseCanvas(optionsRoot.gameObject, 50);
            var backdrop = ImageOn(optionsRoot, UiType.Backdrop, true);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(ToggleOptions);
            var panel = Child(optionsRoot, "Panel");
            Center(panel, new Vector2(UiType.CardMinW, UiType.CardMinH));
            FrameSheet(panel, true);
            var title = Body(panel, UiType.Body, TextAlignmentOptions.Center);
            title.color = Ink;
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
            HideUnfinished();
            bool gatesMap = !session.Traveling && page == Page.Plaza && plazaSpot == PlazaSpot.Gates;
            dock.gameObject.SetActive(!session.Traveling && !lookingAtMap);
            travel.gameObject.SetActive(session.Traveling || lookingAtMap || gatesMap);
            FitMapFrame();
            marketPage.SetActive(page == Page.Market);
            plazaPage.SetActive(page == Page.Plaza);
            housePage.SetActive(page == Page.House);
            ShowPlazaSpot();
            houseTab.gameObject.SetActive(ShowUnfinishedFeatures && EconomyCatalog.IsCity(session.Dock));
            tavernTab.gameObject.SetActive(ShowUnfinishedFeatures);
            stablesTab.gameObject.SetActive(ShowUnfinishedFeatures);
            gearButton.gameObject.SetActive(ShowUnfinishedFeatures);
            TintPace(marketTab, page == Page.Market);
            TintPace(houseTab, page == Page.House);
            paceBar.SetActive(session.Traveling && !session.CampPending && !session.AwaitingPace && !session.EncounterPending);
            backButton.gameObject.SetActive(lookingAtMap && !session.Traveling);
            bool roadChoice = session.CampPending || session.AwaitingPace || session.EncounterPending;
            cardRoot.gameObject.SetActive(roadChoice);
            optionsRoot.gameObject.SetActive(optionsOpen);
            logbookRoot.gameObject.SetActive(logbookOpen);
            dimmer.gameObject.SetActive(false);
            ShowRoadChoice(roadChoice);

            ApplyGround(session.Dock);
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

            PaintTrade(buyButton);
            PaintTrade(sellButton);
            UpdateLive();
        }

        void HideUnfinished()
        {
            if (ShowUnfinishedFeatures)
                return;
            optionsOpen = false;
            if (page == Page.House)
                page = Page.Market;
            if (plazaSpot == PlazaSpot.Tavern || plazaSpot == PlazaSpot.Stables)
                plazaSpot = PlazaSpot.Inn;
        }

        void PaintTrade(Button button)
        {
            if (button == null)
                return;
            bool on = button.interactable;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && !label.richText)
                label.color = on ? Ink : FrontUi.Disabled;
            Transform seal = button.transform.Find("Seal");
            if (seal == null)
                return;
            var image = seal.GetComponent<Image>();
            if (image != null)
                image.color = on ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        void ShowRoadChoice(bool open)
        {
            bool encounter = session.EncounterPending;
            bool camp = session.CampPending && !encounter;
            bool pace = open && !camp && !encounter;
            bool resume = pace && session.AwaitingPace;
            cardTitle.text = encounter ? "On the Road" : camp ? "Make camp" : "The road";
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
            HideHover();
            buttons.RemoveAll(button => button == null);

            if (page != Page.Market || logbookOpen || optionsOpen || choosingRoad
                || session.CampPending || session.AwaitingPace || session.EncounterPending)
                sortMenuOpen = false;

            List<GoodId> goods = OrderShop();
            int slots = EconomyRates.WagonSpace;
            Canvas.ForceUpdateCanvases();
            FitGrids(goods.Count, slots);
            for (int i = 0; i < goods.Count; i++)
            {
                GoodId captured = goods[i];
                int stock = session.StockOf(session.Dock, captured);
                MakeSquare(shopHost, captured.ToString(), CaravanSession.GoodName(captured), stock.ToString(),
                    shopSelected && pickedGood == captured, () => SelectShop(captured), captured);
            }

            for (int i = 0; i < session.CargoCount && i < slots; i++)
            {
                int captured = i;
                GoodId good = session.CargoGoodAt(i);
                MakeSquare(wagonHost, "Slot" + i, CaravanSession.GoodName(good), null,
                    wagonPick == i, () => SelectWagon(captured), good);
            }

            for (int i = session.CargoCount; i < slots; i++)
                MakeSquare(wagonHost, "Empty" + i, null, null, false, null);

            weightText.text = "Weight " + session.CargoWeight() + "/" + EconomyRates.WagonWeight;
            detailText.text = DetailLine();
            ShowDetail();
            sortMenu.gameObject.SetActive(sortMenuOpen);
            for (int i = 0; i < sortChoices.Count; i++)
                TintPace(sortChoices[i], sortChoiceKinds[i] == shopSort);
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

        void MakeSquare(RectTransform host, string name, string label, string count, bool selected, UnityEngine.Events.UnityAction action, GoodId? icon = null)
        {
            var rect = Child(host, name);
            bool empty = string.IsNullOrEmpty(label);
            Image face = CellFace(rect, selected && !empty);
            face.raycastTarget = action != null;
            if (empty)
                face.color = new Color(1f, 1f, 1f, 0.5f);
            if (action != null)
            {
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.onClick.AddListener(action);
                buttons.Add(button);
            }

            if (empty)
                return;

            if (icon.HasValue)
                GoodsIcon(rect, icon.Value);

            float cellWidth = rect.parent == shopHost && shopGrid != null
                ? shopGrid.cellSize.x
                : wagonGrid != null ? wagonGrid.cellSize.x : ShopCellW;
            bool showName = NameFits(label, cellWidth);
            if (showName)
            {
                var text = Body(rect, UiType.Small, TextAlignmentOptions.Center);
                text.text = label;
                text.color = Ink;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                Shade(text);
                var textRt = text.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = new Vector2(1f, 0.28f);
                textRt.offsetMin = new Vector2(6f, string.IsNullOrEmpty(count) ? 4f : 28f);
                textRt.offsetMax = new Vector2(-6f, 0f);
            }
            else if (action != null)
            {
                var hover = rect.gameObject.AddComponent<CellHover>();
                hover.Bind(this, label);
            }

            if (string.IsNullOrEmpty(count))
                return;

            var countText = Body(rect, UiType.Small, TextAlignmentOptions.BottomRight);
            countText.text = count;
            countText.color = Ink;
            countText.textWrappingMode = TextWrappingModes.NoWrap;
            Shade(countText);
            var countRt = countText.rectTransform;
            countRt.anchorMin = new Vector2(1f, 0f);
            countRt.anchorMax = new Vector2(1f, 0f);
            countRt.pivot = new Vector2(1f, 0f);
            countRt.sizeDelta = new Vector2(72f, 28f);
            countRt.anchoredPosition = new Vector2(-6f, 4f);
        }

        static bool NameFits(string label, float cellWidth)
        {
            if (string.IsNullOrEmpty(label) || cellWidth < 16f)
                return false;
            return label.Length * UiType.Small * 0.7f <= cellWidth - 16f;
        }

        public void ShowCellName(string name, RectTransform cell)
        {
            if (hoverChip == null || string.IsNullOrEmpty(name) || cell == null)
                return;
            hoverLabel.text = name;
            var panel = (RectTransform)hoverChip.parent;
            Vector3 top = cell.TransformPoint(new Vector3(cell.rect.center.x, cell.rect.yMax, 0f));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, top);
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screen, null, out local))
                return;
            hoverChip.anchoredPosition = local + new Vector2(0f, 6f);
            float width = Mathf.Clamp(name.Length * UiType.Small * 0.58f + 28f, 140f, 520f);
            hoverChip.sizeDelta = new Vector2(width, 44f);
            hoverChip.gameObject.SetActive(true);
            hoverChip.SetAsLastSibling();
        }

        public void HideHover()
        {
            if (hoverChip != null)
                hoverChip.gameObject.SetActive(false);
        }

        void GoodsIcon(RectTransform parent, GoodId good)
        {
            Texture2D texture = Resources.Load<Texture2D>("Goods/" + good);
            if (texture == null)
                return;
            GoodsPicture(parent, texture);
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
                Button button = RoadRow(gatesColumn, label, () => AskRoad(dest));
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
            clockText.text = WorldClock.Format(session.Day, session.Hour);
            if (logbookDate != null)
                logbookDate.text = clockText.text;
            coinText.text = session.Coin.ToString();
            int used = session.CargoSpace();
            int cap = EconomyRates.WagonSpace;
            cargoText.text = used + "/" + cap;
            if (meterFill != null)
                meterFill.fillAmount = cap <= 0 ? 0f : Mathf.Clamp01(used / (float)cap);
            if (legText != null)
                legText.text = session.Traveling ? session.LegWeatherLine() : string.Empty;
            TintPace(pauseButton, session.Pace == 0 && session.Traveling);
            TintPace(pace1, session.Pace == 1);
            TintPace(pace2, session.Pace == 2);
            TintPace(pace4, session.Pace == 4);
            TintPace(logbookButton, logbookOpen);
            TintPace(gearButton, optionsOpen);
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
                float bottom = session.Traveling ? UiType.Bar + UiType.Gap : UiType.Gap;
                mapFrame.anchorMin = Vector2.zero;
                mapFrame.anchorMax = Vector2.one;
                mapFrame.offsetMin = new Vector2(UiType.Gap, bottom);
                mapFrame.offsetMax = new Vector2(-UiType.Gap, -56f);
                return;
            }

            mapFrame.anchorMin = Vector2.zero;
            mapFrame.anchorMax = Vector2.one;
            mapFrame.offsetMin = new Vector2(GatesPanelRight + UiType.Gap, TabHeight + UiType.Gap);
            mapFrame.offsetMax = new Vector2(-UiType.Gap, -UiType.Gap);
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
            float inset = !close || radius < 0.0001f ? 8f : 0f;
            mapSheet.sizeDelta = new Vector2(Mathf.Max(2f, view.width - inset), Mathf.Max(2f, view.height - inset));
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
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 12f;
                layout.padding = new RectOffset(8, 8, 4, 4);
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                LogbookIcon(row, good);
                ChartLabel(row, CaravanSession.GoodName(good), LogGoodW);
                ChartLabel(row, LoggedBuyText(good), LogPriceW).color = LogInk(session.HasLoggedBuy(good));
                ChartLabel(row, LoggedSaleText(good), LogPriceW).color = LogInk(session.HasLoggedSale(good));
            }
        }

        string LoggedBuyText(GoodId good)
        {
            if (!session.HasLoggedBuy(good))
                return "\u2014";
            return session.LoggedBuy(good) + " coin, " + CaravanSession.PlaceName(session.LoggedBuyPlace(good));
        }

        string LoggedSaleText(GoodId good)
        {
            if (!session.HasLoggedSale(good))
                return "\u2014";
            return session.LoggedSale(good) + " coin, " + CaravanSession.PlaceName(session.LoggedSalePlace(good));
        }

        static Color LogInk(bool filled)
        {
            return filled
                ? ParchmentInk
                : new Color(ParchmentInk.r, ParchmentInk.g, ParchmentInk.b, 0.45f);
        }

        TextMeshProUGUI ChartLabel(RectTransform parent, string label, float width)
        {
            var text = Body(parent, UiType.LogRow, TextAlignmentOptions.MidlineLeft);
            text.color = ParchmentInk;
            text.text = label;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            var element = text.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = 0f;
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

        static string Amount(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        void TintPace(Button button, bool selected)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && !label.richText)
                label.color = selected ? FrontUi.Gold : Ink;
            if (button.transition == Selectable.Transition.SpriteSwap)
                return;
            var colors = button.colors;
            colors.normalColor = selected ? ButtonHot : Color.white;
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

        Button BarSeal(RectTransform parent, string label, Sprite normal, Sprite hover, Sprite pressed, UnityEngine.Events.UnityAction action, float labelSize = 0f)
        {
            Sprite quiet;
            Sprite hot;
            Sprite down;
            SealSprites(normal, hover, pressed, out quiet, out hot, out down);
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hit = go.AddComponent<Image>();
            hit.sprite = quad;
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(action);
            float size = labelSize > 0f ? labelSize : UiType.Body;
            float textW = Mathf.Ceil(label.Length * size * 0.62f);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 64f;
            element.minWidth = 64f + textW;
            element.preferredWidth = element.minWidth;
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.padding = new RectOffset(4, 8, 2, 2);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            Image seal = SealIcon(go.transform, quiet, 52f);
            var text = Body(go.GetComponent<RectTransform>(), size, TextAlignmentOptions.MidlineLeft);
            text.text = label;
            text.color = Ink;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var textFit = text.gameObject.AddComponent<LayoutElement>();
            textFit.minWidth = textW;
            textFit.preferredWidth = textW;
            textFit.preferredHeight = 40f;
            go.AddComponent<SealSwap>().Bind(seal, quiet, hot, down);
            buttons.Add(button);
            return button;
        }

        Button TradeSeal(RectTransform parent, string label, Sprite normal, Sprite hover, Sprite pressed, UnityEngine.Events.UnityAction action)
        {
            Sprite quiet;
            Sprite hot;
            Sprite down;
            SealSprites(normal, hover, pressed, out quiet, out hot, out down);
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hit = go.AddComponent<Image>();
            hit.sprite = quad;
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(action);
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 110f;
            element.preferredHeight = 112f;
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            Image seal = SealIcon(go.transform, quiet, 72f);
            var text = Body(go.GetComponent<RectTransform>(), UiType.Trade, TextAlignmentOptions.Center);
            text.text = label;
            text.color = Ink;
            Shade(text);
            text.raycastTarget = false;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            go.AddComponent<SealSwap>().Bind(seal, quiet, hot, down);
            buttons.Add(button);
            return button;
        }

        Button MenuLine(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = quad;
            image.color = new Color(1f, 1f, 1f, 0.04f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.04f);
            colors.highlightedColor = new Color(1f, 0.94f, 0.82f, 0.35f);
            colors.pressedColor = new Color(0.78f, 0.68f, 0.48f, 0.5f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.disabledColor = ButtonOff;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(action);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 52f;
            var text = Body(go.GetComponent<RectTransform>(), UiType.Body, TextAlignmentOptions.MidlineLeft);
            text.text = label;
            text.color = Ink;
            text.raycastTarget = false;
            Stretch(text.rectTransform, 8f);
            buttons.Add(button);
            return button;
        }

        Button RoadRow(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = BarSeal(parent, label, null, null, null, action);
            var element = button.GetComponent<LayoutElement>();
            element.minWidth = 0f;
            element.preferredWidth = 0f;
            element.flexibleWidth = 1f;
            element.preferredHeight = 72f;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                var textFit = text.GetComponent<LayoutElement>();
                if (textFit != null)
                {
                    textFit.minWidth = 0f;
                    textFit.flexibleWidth = 1f;
                }
            }

            return button;
        }

        static void SealSprites(Sprite normal, Sprite hover, Sprite pressed, out Sprite quiet, out Sprite hot, out Sprite down)
        {
            quiet = normal;
            if (quiet == null)
            {
                ApprovedArt kit = ApprovedArt.Load();
                quiet = kit != null && kit.sealBase != null ? kit.sealBase : FrontArt.ResinSeal();
            }

            hot = hover != null ? hover : quiet;
            down = pressed != null ? pressed : quiet;
        }

        Image SealIcon(Transform parent, Sprite sprite, float size)
        {
            var go = new GameObject("Seal", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : quad;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var fit = go.AddComponent<LayoutElement>();
            fit.preferredWidth = size;
            fit.preferredHeight = size;
            return image;
        }

        void FitSeal(Button button, float width, float height)
        {
            SquareSeal(button, height);
            var element = button.GetComponent<LayoutElement>();
            if (element != null)
                element.preferredWidth = width;
            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
        }

        Button Tab(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            ApprovedArt kit = ApprovedArt.Load();
            Sprite panel = kit != null && kit.sealBase != null ? kit.sealBase : FrontArt.ButtonPanel();
            image.sprite = panel != null ? panel : quad;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.82f, 1f);
            colors.pressedColor = new Color(0.78f, 0.68f, 0.48f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = ButtonOff;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(action);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 64f;
            var text = Body(go.GetComponent<RectTransform>(), UiType.Body, TextAlignmentOptions.Center);
            text.text = label;
            text.color = Ink;
            Shade(text);
            text.raycastTarget = false;
            Stretch(text.rectTransform, 4f);
            buttons.Add(button);
            return button;
        }

        Button StackButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = BarSeal(parent, label, null, null, null, action);
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
            var text = Body(parent, UiType.Hud, alignment);
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
            text.fontSize = Mathf.Max(size, UiType.Small);
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
            rect.offsetMax = new Vector2(0f, -TopBarHeight);
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

        void LogbookIcon(RectTransform row, GoodId good)
        {
            var slot = Child(row, "Icon");
            var element = slot.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 48f;
            element.preferredHeight = 44f;
            CellFace(slot, false);
            Texture2D texture = Resources.Load<Texture2D>("Goods/" + good);
            if (texture != null)
                GoodsPicture(slot, texture);
        }

        Image DocumentOn(RectTransform rect, bool raycast)
        {
            return SheetOn(rect, FrontArt.Parchment(), raycast);
        }

        Image DarkBar(RectTransform rect, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = quad;
            image.color = Bar;
            image.raycastTarget = raycast;
            return image;
        }

        Image FrameSheet(RectTransform rect, bool raycast)
        {
            Image sheet = FrontUi.FrameSheet(rect);
            sheet.raycastTarget = raycast;
            return sheet;
        }

        static void Shade(TextMeshProUGUI text)
        {
            if (text == null)
                return;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = UiType.Shade;
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        Image SheetOn(RectTransform rect, Sprite sprite, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : quad;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = raycast;
            return image;
        }

        void SquareSeal(Button button, float height)
        {
            if (button == null)
                return;
            var element = button.GetComponent<LayoutElement>();
            if (element == null)
                element = button.gameObject.AddComponent<LayoutElement>();
            var image = button.GetComponent<Image>();
            float aspect = 1f;
            if (image != null && image.sprite != null && image.sprite.rect.height > 1f)
                aspect = image.sprite.rect.width / image.sprite.rect.height;
            element.preferredHeight = height;
            element.preferredWidth = height * aspect;
            element.flexibleWidth = 0f;
            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(height * aspect, height);
        }

        void HudIcon(RectTransform parent, Sprite sprite)
        {
            var go = new GameObject("Icon", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : quad;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 52f;
            element.preferredHeight = 52f;
        }

        Image CellFace(RectTransform rect, bool selected)
        {
            ApprovedArt art = ApprovedArt.Load();
            Sprite sprite = null;
            if (art != null)
                sprite = selected && art.goodsCellSelected != null ? art.goodsCellSelected : art.goodsCell;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : quad;
            image.type = Image.Type.Simple;
            image.preserveAspect = sprite != null;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        RawImage GoodsPicture(RectTransform parent, Texture2D texture)
        {
            var picture = Child(parent, "Icon");
            picture.anchorMin = new Vector2(0.18f, 0.18f);
            picture.anchorMax = new Vector2(0.82f, 0.82f);
            picture.offsetMin = Vector2.zero;
            picture.offsetMax = Vector2.zero;
            var raw = picture.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = texture != null ? Color.white : Color.clear;
            raw.raycastTarget = false;
            var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = texture != null ? texture.width / (float)Mathf.Max(texture.height, 1) : 1f;
            return raw;
        }

        void ShowDetail()
        {
            if (detailName == null)
                return;
            bool live = SelectionLive();
            detailName.text = live ? CaravanSession.GoodName(SelectedGood()) : "Select a good";
            detailName.color = live ? FrontUi.Gold : Quiet;
            if (detailCell != null)
            {
                ApprovedArt art = ApprovedArt.Load();
                Sprite cell = null;
                if (art != null)
                    cell = live && art.goodsCellSelected != null ? art.goodsCellSelected : art.goodsCell;
                if (cell != null)
                    detailCell.sprite = cell;
                detailCell.color = live ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }

            if (detailIcon == null)
                return;
            Texture2D texture = live ? Resources.Load<Texture2D>("Goods/" + SelectedGood()) : null;
            detailIcon.texture = texture;
            detailIcon.color = texture != null ? Color.white : Color.clear;
            var fit = detailIcon.GetComponent<AspectRatioFitter>();
            if (fit != null && texture != null)
                fit.aspectRatio = texture.width / (float)Mathf.Max(texture.height, 1);
        }

        void BuildMeter(RectTransform meter)
        {
            ApprovedArt art = ApprovedArt.Load();
            Sprite frameSprite = art != null ? art.meterFrame : null;
            var frame = Child(meter, "Frame");
            Stretch(frame);
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = frameSprite != null ? frameSprite : quad;
            frameImage.type = frameSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            frameImage.fillCenter = true;
            frameImage.raycastTarget = false;
            var fill = Child(meter, "Fill");
            fill.anchorMin = new Vector2(0f, 0.32f);
            fill.anchorMax = new Vector2(1f, 0.67f);
            float inset = 0f;
            if (frameSprite != null && frameSprite.pixelsPerUnit > 0f)
                inset = frameSprite.border.x / frameSprite.pixelsPerUnit;
            fill.offsetMin = new Vector2(inset, 0f);
            fill.offsetMax = new Vector2(-inset, 0f);
            meterFill = fill.gameObject.AddComponent<Image>();
            meterFill.sprite = art != null && art.meterFill != null ? art.meterFill : quad;
            meterFill.type = Image.Type.Filled;
            meterFill.fillMethod = Image.FillMethod.Horizontal;
            meterFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            meterFill.fillAmount = 0f;
            meterFill.preserveAspect = false;
            meterFill.raycastTarget = false;
        }

        Texture2D BackdropFor(PlaceId place)
        {
            ApprovedArt art = ApprovedArt.Load();
            Texture2D shared = art != null ? art.settlementGeneric : null;
            if (shared != null)
                return shared;
            return FrontArt.SettlementDesert();
        }

        void ApplyGround(PlaceId place)
        {
            if (groundImage == null)
                return;
            bool settlement = session == null || !session.Traveling;
            Texture2D plate = settlement ? BackdropFor(place) : null;
            groundImage.texture = plate;
            groundImage.color = plate != null ? Color.white : new Color(0.62f, 0.48f, 0.32f, 1f);
            if (groundFit != null && plate != null && plate.height > 0)
                groundFit.aspectRatio = plate.width / (float)plate.height;
        }

#if UNITY_EDITOR
        bool shotSeeded;

        public bool ShotMapReady => mapImage != null && mapImage.texture != null;

        public bool Shot(string id)
        {
            switch (id)
            {
                case "seed":
                    if (!shotSeeded)
                    {
                        session.ShotSeed();
                        shotSeeded = true;
                    }

                    FinishShot();
                    return session.CargoCount > 0;
                case "market-empty":
                    ShotMarket(false, false, false);
                    return page == Page.Market && !shopSelected && wagonPick < 0 && !sortMenuOpen;
                case "market-buy":
                    ShotMarket(true, false, false);
                    return shopSelected && wagonPick < 0;
                case "market-wagon":
                    if (session.CargoCount < 1)
                        return false;
                    ShotMarket(false, true, false);
                    return wagonPick == 0;
                case "market-sort":
                    ShotMarket(false, false, true);
                    return sortMenuOpen && page == Page.Market;
                case "ridgewatch":
                    ShotClearOverlays();
                    if (session.Dock != PlaceId.Ridgewatch || session.Traveling)
                        session.ShotDockAt(PlaceId.Ridgewatch);
                    page = Page.Market;
                    FinishShot();
                    return session.Dock == PlaceId.Ridgewatch && !session.Traveling;
                case "inn":
                    return ShotPlaza(PlazaSpot.Inn, false, false);
                case "gates":
                    return ShotPlaza(PlazaSpot.Gates, false, false);
                case "tavern":
                    return ShotPlaza(PlazaSpot.Tavern, false, false);
                case "stables":
                    return ShotPlaza(PlazaSpot.Stables, false, false);
                case "house":
                    ShotClearOverlays();
                    if (session.Dock != PlaceId.Kharun || session.Traveling)
                        session.ShotDockAt(PlaceId.Kharun);
                    page = Page.House;
                    FinishShot();
                    return page == Page.House && EconomyCatalog.IsCity(session.Dock);
                case "logbook":
                    ShotClearOverlays();
                    page = Page.Market;
                    logbookOpen = true;
                    FinishShot();
                    return logbookOpen;
                case "options":
                    ShotClearOverlays();
                    page = Page.Market;
                    optionsOpen = true;
                    FinishShot();
                    return optionsOpen;
                case "travel":
                    return ShotPlaza(PlazaSpot.Gates, true, false);
                case "road":
                    return ShotPlaza(PlazaSpot.Gates, true, false);
                case "depart":
                    ShotClearOverlays();
                    if (!session.Traveling)
                    {
                        if (!session.ShotDepartPaused(PlaceId.Draven))
                            return false;
                    }

                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    FinishShot();
                    return session.Traveling && session.Pace == 0 && session.TravelDistance <= 0.000001f;
                case "encounter":
                    ShotClearOverlays();
                    if (!session.EncounterPending)
                        session.ShotEncounter("WD-01");
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    FinishShot();
                    return session.EncounterPending && session.EncounterId == "WD-01";
                case "camp":
                    ShotClearOverlays();
                    if (!session.CampPending)
                        session.ShotCamp();
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    FinishShot();
                    return session.CampPending && !session.EncounterPending;
                case "resume":
                    ShotClearOverlays();
                    if (!session.AwaitingPace)
                        session.ShotResumePace();
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    FinishShot();
                    return session.AwaitingPace && !session.CampPending;
                default:
                    return false;
            }
        }

        bool ShotPlaza(PlazaSpot spot, bool lit, bool choice)
        {
            ShotClearOverlays();
            if (session.Traveling || session.Dock != PlaceId.Kharun)
                session.ShotDockAt(PlaceId.Kharun);
            page = Page.Plaza;
            plazaSpot = spot;
            if (lit)
            {
                if (!session.CanTakeRoad())
                    return false;
                pendingDest = PlaceId.Draven;
                roadLit = true;
                draftPace = RoadPace.Steady;
            }

            choosingRoad = false;
            FinishShot();
            if (choice)
                return false;
            if (lit)
                return roadLit && pendingDest == PlaceId.Draven && !choosingRoad;
            return plazaSpot == spot && !roadLit;
        }

        void ShotMarket(bool shop, bool wagon, bool sort)
        {
            ShotClearOverlays();
            page = Page.Market;
            if (shop)
                SelectShop(ShotStocked());
            if (wagon)
                SelectWagon(0);
            if (sort)
                sortMenuOpen = true;
            FinishShot();
        }

        void ShotClearOverlays()
        {
            sortMenuOpen = false;
            shopSelected = false;
            wagonPick = -1;
            optionsOpen = false;
            logbookOpen = false;
            choosingRoad = false;
            lookingAtMap = false;
            roadLit = false;
            listenLine = string.Empty;
        }

        GoodId ShotStocked()
        {
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                if (good == GoodId.Rations)
                    continue;
                if (session.StockOf(session.Dock, good) > 0)
                    return good;
            }

            return GoodId.ShimmersteelJars;
        }

        void FinishShot()
        {
            Canvas.ForceUpdateCanvases();
            Redraw();
            Canvas.ForceUpdateCanvases();
        }
#endif
    }

    sealed class CellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        PlayPresenter owner;
        string goodsName;

        public void Bind(PlayPresenter host, string name)
        {
            owner = host;
            goodsName = name;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner != null)
                owner.ShowCellName(goodsName, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner != null)
                owner.HideHover();
        }
    }

    sealed class SealSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Image seal;
        Sprite quiet;
        Sprite hot;
        Sprite down;

        public void Bind(Image image, Sprite normal, Sprite hover, Sprite pressed)
        {
            seal = image;
            quiet = normal;
            hot = hover != null ? hover : normal;
            down = pressed != null ? pressed : normal;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (seal != null)
                seal.sprite = hot;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (seal != null)
                seal.sprite = quiet;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (seal != null)
                seal.sprite = down;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (seal == null)
                return;
            bool over = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, eventData.position, eventData.pressEventCamera);
            seal.sprite = over ? hot : quiet;
        }
    }
}
