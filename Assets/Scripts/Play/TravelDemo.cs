using System.Collections.Generic;
using Caravans.FrontDoor;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caravans.Play
{
    // Editor travel sandbox. It does not start a run and does not retune live travel.
    public sealed class TravelDemo : MonoBehaviour
    {
        // Placeholder rates. Systems will balance them later.
        [SerializeField] float campScroll = 0f;
        [SerializeField] float steadyScroll = 2.8f;
        [SerializeField] float pushScroll = 5.2f;
        [SerializeField] float campFoodPerSecond = 0.005f;
        [SerializeField] float campWaterPerSecond = 0.007f;
        [SerializeField] float steadyFoodPerSecond = 0.014f;
        [SerializeField] float steadyWaterPerSecond = 0.018f;
        [SerializeField] float pushFoodPerSecond = 0.032f;
        [SerializeField] float pushWaterPerSecond = 0.042f;

        static readonly string[] TerrainNames =
        {
            "Dune Sea", "Salt Flats", "Badlands", "Dry Scrub", "Highlands", "Coast",
        };

        TravelDemoChart chart;
        TravelDemoView view;
        Camera cam;
        RectTransform canvas;
        RectTransform supply;
        Image foodFill;
        Image waterFill;
        TextMeshProUGUI message;
        TextMeshProUGUI dayLabel;
        TMP_Dropdown routeDrop;
        TMP_Dropdown directionDrop;
        TMP_Dropdown terrainDrop;
        TMP_InputField seedField;
        Slider clockSlider;
        readonly TextMeshProUGUI[] paceLabels = new TextMeshProUGUI[3];
        int pace = 1;
        float hour = 9.5f;
        float clockSpeed = 0.35f;
        float food = 1f;
        float water = 1f;
        float distance;
        string shownMessage = "";

        public bool ShotReady { get; private set; }

        void Start()
        {
            FrontUi.EnsureEventSystem();
            cam = Camera.main;
            if (cam == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cam = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            var world = new GameObject("TravelWorld");
            world.transform.SetParent(transform, false);
            view = world.AddComponent<TravelDemoView>();
            view.Build(cam);
            canvas = FrontUi.Canvas("TravelDemoCanvas", 40);
            canvas.SetParent(transform, false);
            try
            {
                chart = TravelDemoChart.Load();
                if (chart.Routes.Length != 18)
                    Debug.LogWarning("Travel demo expected 18 roads, read " + chart.Routes.Length + ".");
                BuildHud();
                Debug.Log("Travel demo ready. " + chart.Routes.Length + " roads. Run confirmed: " + Gate.RunConfirmed);
            }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                FrontUi.Label(canvas, error.Message, UiType.Body, false, UiType.Cream, new Vector2(1200f, 80f));
            }

            ShotReady = chart != null;
        }

        void Update()
        {
            if (chart == null || view == null)
                return;
            float delta = Time.deltaTime;
            hour = Mathf.Repeat(hour + clockSpeed * delta, 24f);
            distance += ScrollSpeed() * Time.smoothDeltaTime;
            if (distance < 0f)
                distance = 0f;
            food = Mathf.Max(0f, food - FoodRate() * delta);
            water = Mathf.Max(0f, water - WaterRate() * delta);
            view.Show(chart, distance, hour, ScrollSpeed());
            if (foodFill != null)
                foodFill.fillAmount = food;
            if (waterFill != null)
                waterFill.fillAmount = water;
            string line = SupplyLine();
            if (message != null && line != shownMessage)
            {
                shownMessage = line;
                message.text = line;
                if (supply != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(supply);
            }

            if (dayLabel != null)
            {
                int minutes = Mathf.FloorToInt(Mathf.Repeat(hour, 24f) * 60f);
                dayLabel.text = WorldClock.DayPart(hour) + "  " + (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
            }

            PollBiomeKey();

            PlaceSupply();
        }

        void OnDestroy()
        {
            TravelDemoSprites.Clear();
        }

        public void PrepareStill()
        {
            if (chart == null)
                return;
            chart.SelectRoute(TravelDemoChart.DefaultRouteId);
            chart.SetForward(true);
            chart.SetOverride(null);
            chart.SetSeed(112);
            pace = 1;
            hour = 9.6f;
            clockSpeed = 0f;
            food = 0.78f;
            water = 0.64f;
            distance = chart.WorldLength * 0.48f;
            if (clockSlider != null)
                clockSlider.SetValueWithoutNotify(0f);
            if (seedField != null)
                seedField.SetTextWithoutNotify("112");
            RefreshRouteDropdown();
            RefreshDirectionDropdown();
            if (terrainDrop != null)
            {
                terrainDrop.SetValueWithoutNotify(0);
                terrainDrop.RefreshShownValue();
            }

            PaintPace();
            view.Show(chart, distance, hour, ScrollSpeed());
        }

        float ScrollSpeed()
        {
            if (pace == 0)
                return campScroll;
            if (pace == 2)
                return pushScroll;
            return steadyScroll;
        }

        float FoodRate()
        {
            if (pace == 0)
                return campFoodPerSecond;
            if (pace == 2)
                return pushFoodPerSecond;
            return steadyFoodPerSecond;
        }

        float WaterRate()
        {
            if (pace == 0)
                return campWaterPerSecond;
            if (pace == 2)
                return pushWaterPerSecond;
            return steadyWaterPerSecond;
        }

        string SupplyLine()
        {
            if (food <= 0f && water <= 0f)
                return "The food and the water are gone.";
            if (food <= 0f)
                return "The food is gone.";
            if (water <= 0f)
                return "The water is gone.";
            return "";
        }

        void BuildHud()
        {
            var bar = FrontUi.Box(canvas, "DebugBar", new Vector2(1920f, 96f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 96f);
            var backing = bar.gameObject.AddComponent<Image>();
            backing.color = new Color(0.08f, 0.05f, 0.03f, 0.88f);
            backing.raycastTarget = false;
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 14, 14);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            FrontUi.MenuItem(bar, "BACK", true, Gate.ReturnToTitle, 200f, 64f);
            routeDrop = Dropdown(bar, "Route", 320f);
            directionDrop = Dropdown(bar, "Direction", 240f);
            terrainDrop = Dropdown(bar, "Terrain", 190f);
            BiomeButton(bar);
            seedField = SeedBox(bar);
            dayLabel = Plain(bar, "DayPart", WorldClock.DayPart(hour), UiType.Hud, UiType.Cream, TextAlignmentOptions.MidlineLeft);
            var dayFit = dayLabel.rectTransform.gameObject.AddComponent<LayoutElement>();
            dayFit.preferredWidth = 210f;
            dayFit.preferredHeight = 40f;
            clockSlider = ClockBox(bar);
            RefreshRouteDropdown();
            RefreshDirectionDropdown();
            RefreshTerrainDropdown();

            supply = FrontUi.Box(canvas, "Supplies", new Vector2(440f, 220f));
            supply.anchorMin = supply.anchorMax = new Vector2(0.5f, 0.5f);
            supply.pivot = new Vector2(0.5f, 1f);
            var supplyBack = supply.gameObject.AddComponent<Image>();
            supplyBack.color = new Color(0.08f, 0.05f, 0.03f, 0.82f);
            supplyBack.raycastTarget = false;
            var supplyLayout = supply.gameObject.AddComponent<VerticalLayoutGroup>();
            supplyLayout.padding = new RectOffset(16, 16, 14, 14);
            supplyLayout.spacing = 8f;
            supplyLayout.childAlignment = TextAnchor.UpperCenter;
            supplyLayout.childControlWidth = true;
            supplyLayout.childControlHeight = true;
            supplyLayout.childForceExpandWidth = true;
            supplyLayout.childForceExpandHeight = false;
            var supplyFit = supply.gameObject.AddComponent<ContentSizeFitter>();
            supplyFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            supplyFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var supplyWidth = supply.gameObject.AddComponent<LayoutElement>();
            supplyWidth.preferredWidth = 440f;

            foodFill = Bar(supply, "FOOD", new Color(0.75f, 0.16f, 0.12f, 1f));
            waterFill = Bar(supply, "WATER", new Color(0.20f, 0.45f, 0.82f, 1f));
            var paces = FrontUi.Box(supply, "Paces", new Vector2(400f, 52f));
            var paceFit = paces.gameObject.AddComponent<LayoutElement>();
            paceFit.preferredHeight = 52f;
            paceFit.preferredWidth = 400f;
            var paceRow = paces.gameObject.AddComponent<HorizontalLayoutGroup>();
            paceRow.spacing = 8f;
            paceRow.childAlignment = TextAnchor.MiddleCenter;
            paceRow.childControlWidth = true;
            paceRow.childControlHeight = true;
            paceRow.childForceExpandWidth = true;
            paceRow.childForceExpandHeight = false;
            paceLabels[0] = PaceButton(paces, "Camp", 0);
            paceLabels[1] = PaceButton(paces, "Steady", 1);
            paceLabels[2] = PaceButton(paces, "Push", 2);
            message = Plain(supply, "SupplyMessage", "", UiType.Body, FrontUi.Gold, TextAlignmentOptions.Center);
            var messageFit = message.rectTransform.gameObject.AddComponent<LayoutElement>();
            messageFit.preferredHeight = 36f;
            messageFit.preferredWidth = 400f;
            PaintPace();
            PlaceSupply();
        }

        void PlaceSupply()
        {
            if (supply == null || cam == null || canvas == null || view == null)
                return;
            Bounds mount = view.MountBounds();
            float crest;
            float ground = view.TryCrest(mount.center.x, out crest) ? crest : mount.min.y;
            float width = LayoutUtility.GetPreferredWidth(supply);
            if (width < 1f)
                width = 440f;
            float height = LayoutUtility.GetPreferredHeight(supply);
            if (height < 1f)
                height = supply.rect.height;

            // Beside the thrask, bottom on the crest, so the panel sits in the sky.
            Vector3 world = new Vector3(mount.max.x + 0.2f, ground, 0f);
            Vector3 screen = cam.WorldToScreenPoint(world);
            Vector2 local;
            if (screen.z < 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out local))
                return;
            bool onRight = local.x + width <= canvas.rect.xMax - 12f;
            if (!onRight)
            {
                world = new Vector3(mount.min.x - 0.2f, ground, 0f);
                screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out local))
                    return;
            }

            if (local.y + height > canvas.rect.yMax - 12f)
                local.y = canvas.rect.yMax - 12f - height;
            if (local.y < canvas.rect.yMin + 12f)
                local.y = canvas.rect.yMin + 12f;
            supply.anchorMin = new Vector2(0.5f, 0.5f);
            supply.anchorMax = new Vector2(0.5f, 0.5f);
            supply.pivot = onRight ? Vector2.zero : Vector2.right;
            supply.anchoredPosition = local;
        }

        Image Bar(RectTransform parent, string caption, Color fill)
        {
            var row = FrontUi.Box(parent, caption, new Vector2(400f, 28f));
            var fit = row.gameObject.AddComponent<LayoutElement>();
            fit.preferredHeight = 28f;
            fit.preferredWidth = 400f;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var label = Plain(row, "Caption", caption, UiType.Small, UiType.Cream, TextAlignmentOptions.MidlineLeft);
            var labelFit = label.rectTransform.gameObject.AddComponent<LayoutElement>();
            labelFit.preferredWidth = 84f;
            labelFit.preferredHeight = 28f;
            var track = FrontUi.Box(row, "Track", new Vector2(280f, 18f));
            var trackFit = track.gameObject.AddComponent<LayoutElement>();
            trackFit.preferredWidth = 280f;
            trackFit.preferredHeight = 18f;
            trackFit.flexibleWidth = 1f;
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.16f, 0.12f, 0.09f, 1f);
            trackImage.raycastTarget = false;
            var fillRect = FrontUi.Box(track, "Fill", new Vector2(280f, 18f));
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.color = fill;
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.sprite = TrackSprite();
            return fillImage;
        }

        TextMeshProUGUI PaceButton(RectTransform parent, string caption, int index)
        {
            var rect = FrontUi.Box(parent, caption, new Vector2(120f, 48f));
            var fit = rect.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 120f;
            fit.preferredHeight = 48f;
            fit.flexibleWidth = 1f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.11f, 0.07f, 1f);
            var label = Plain(rect, "Text", caption, UiType.Tab, UiType.Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            int captured = index;
            button.onClick.AddListener(delegate { SetPace(captured); });
            return label;
        }

        void SetPace(int index)
        {
            pace = index;
            PaintPace();
        }

        void PaintPace()
        {
            for (int i = 0; i < paceLabels.Length; i++)
            {
                if (paceLabels[i] != null)
                    paceLabels[i].color = i == pace ? FrontUi.Gold : UiType.Cream;
            }
        }

        void OnRoute(int index)
        {
            chart.SelectRoute(index);
            distance = 0f;
            RefreshDirectionDropdown();
        }

        void OnDirection(int index)
        {
            chart.SetForward(index == 0);
            distance = 0f;
        }

        void OnTerrain(int index)
        {
            chart.SetOverride(index <= 0 ? null : TerrainNames[index - 1]);
        }

        void PollBiomeKey()
        {
            if (chart == null || (seedField != null && seedField.isFocused))
                return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.bKey.wasPressedThisFrame || keyboard.rightBracketKey.wasPressedThisFrame)
                CycleBiome(1);
            else if (keyboard.leftBracketKey.wasPressedThisFrame)
                CycleBiome(-1);
        }

        void CycleBiome(int direction)
        {
            if (chart == null)
                return;
            int current = -1;
            for (int i = 0; i < TerrainNames.Length; i++)
            {
                if (TerrainNames[i] == chart.TerrainOverride)
                    current = i;
            }

            int count = TerrainNames.Length;
            int next = current < 0 ? 0 : current + direction;
            if (next >= count)
                next = 0;
            if (next < 0)
                next = count - 1;
            chart.SetOverride(TerrainNames[next]);
            if (terrainDrop == null)
                return;
            terrainDrop.SetValueWithoutNotify(next + 1);
            terrainDrop.RefreshShownValue();
        }

        void BiomeButton(RectTransform parent)
        {
            var rect = FrontUi.Box(parent, "NextBiome", new Vector2(150f, 40f));
            var fit = rect.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 150f;
            fit.preferredHeight = 40f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.11f, 0.07f, 1f);
            var label = Plain(rect, "Text", "Next biome", UiType.Small, UiType.Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate { CycleBiome(1); });
        }

        void OnSeed(string text)
        {
            int seed;
            if (int.TryParse(text, out seed))
                chart.SetSeed(seed);
        }

        void OnClock(float value)
        {
            clockSpeed = value;
        }

        void RefreshRouteDropdown()
        {
            var labels = new List<string>();
            for (int i = 0; i < chart.Routes.Length; i++)
                labels.Add(chart.RouteLabel(i));
            Fill(routeDrop, labels, chart.RouteIndex);
            routeDrop.onValueChanged.AddListener(OnRoute);
        }

        void RefreshDirectionDropdown()
        {
            var labels = new List<string>
            {
                chart.DirectionLabel(true),
                chart.DirectionLabel(false),
            };
            Fill(directionDrop, labels, chart.Forward ? 0 : 1);
            directionDrop.onValueChanged.AddListener(OnDirection);
        }

        void RefreshTerrainDropdown()
        {
            var labels = new List<string> { "Chart" };
            for (int i = 0; i < TerrainNames.Length; i++)
                labels.Add(TerrainNames[i]);
            int index = 0;
            if (!string.IsNullOrEmpty(chart.TerrainOverride))
            {
                for (int i = 0; i < TerrainNames.Length; i++)
                {
                    if (TerrainNames[i] == chart.TerrainOverride)
                        index = i + 1;
                }
            }

            Fill(terrainDrop, labels, index);
            terrainDrop.onValueChanged.AddListener(OnTerrain);
        }

        static void Fill(TMP_Dropdown dropdown, List<string> labels, int index)
        {
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(index, 0, Mathf.Max(0, labels.Count - 1)));
            dropdown.RefreshShownValue();
        }

        TMP_Dropdown Dropdown(RectTransform parent, string name, float width)
        {
            var rect = FrontUi.Box(parent, name, new Vector2(width, 40f));
            var fit = rect.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = width;
            fit.preferredHeight = 40f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.10f, 0.07f, 1f);
            var caption = Plain(rect, "Label", name, UiType.Small, UiType.Cream, TextAlignmentOptions.MidlineLeft);
            Stretch(caption.rectTransform, 10f, 8f, -28f, -8f);
            var arrow = Plain(rect, "Arrow", "v", UiType.Small, FrontUi.Gold, TextAlignmentOptions.MidlineRight);
            var arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(24f, 0f);
            arrowRect.anchoredPosition = new Vector2(-8f, 0f);

            var template = FrontUi.Box(rect, "Template", new Vector2(width, 280f));
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = Vector2.zero;
            template.sizeDelta = new Vector2(0f, 280f);
            var templateImage = template.gameObject.AddComponent<Image>();
            templateImage.color = new Color(0.10f, 0.07f, 0.05f, 0.98f);
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = FrontUi.Box(template, "Viewport", new Vector2(width, 280f));
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = FrontUi.Box(viewport, "Content", new Vector2(width, 32f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 32f);
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var contentFit = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = FrontUi.Box(content, "Item", new Vector2(width, 32f));
            var itemFit = item.gameObject.AddComponent<LayoutElement>();
            itemFit.preferredHeight = 32f;
            var itemBack = item.gameObject.AddComponent<Image>();
            itemBack.color = new Color(0.18f, 0.13f, 0.09f, 1f);
            var toggle = item.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = itemBack;
            var itemLabel = Plain(item, "Item Label", name, UiType.Small, UiType.Cream, TextAlignmentOptions.MidlineLeft);
            Stretch(itemLabel.rectTransform, 10f, 4f, -10f, -4f);

            scroll.viewport = viewport;
            scroll.content = content;
            var dropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.template.gameObject.SetActive(false);
            return dropdown;
        }

        TMP_InputField SeedBox(RectTransform parent)
        {
            var rect = FrontUi.Box(parent, "Seed", new Vector2(120f, 40f));
            var fit = rect.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 120f;
            fit.preferredHeight = 40f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.10f, 0.07f, 1f);
            var area = FrontUi.Box(rect, "Text Area", new Vector2(100f, 32f));
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(8f, 4f);
            area.offsetMax = new Vector2(-8f, -4f);
            area.gameObject.AddComponent<RectMask2D>();
            var text = Plain(area, "Text", "112", UiType.Small, UiType.Cream, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);
            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.fontAsset = FrontArt.Regular();
            input.pointSize = UiType.Small;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.text = "112";
            input.onEndEdit.AddListener(OnSeed);
            return input;
        }

        Slider ClockBox(RectTransform parent)
        {
            var rect = FrontUi.Box(parent, "Clock", new Vector2(180f, 40f));
            var fit = rect.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 180f;
            fit.preferredHeight = 40f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.10f, 0.07f, 1f);
            var fillArea = FrontUi.Box(rect, "Fill Area", new Vector2(160f, 8f));
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(10f, -4f);
            fillArea.offsetMax = new Vector2(-10f, 4f);
            var fill = FrontUi.Box(fillArea, "Fill", new Vector2(160f, 8f));
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = FrontUi.Gold;
            fillImage.sprite = TrackSprite();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            var handle = FrontUi.Box(rect, "Handle", new Vector2(14f, 28f));
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = UiType.Cream;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 4f;
            slider.value = clockSpeed;
            slider.onValueChanged.AddListener(OnClock);
            return slider;
        }

        static Sprite trackSprite;

        static Sprite TrackSprite()
        {
            if (trackSprite != null)
                return trackSprite;
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply(false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            trackSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            trackSprite.hideFlags = HideFlags.HideAndDontSave;
            return trackSprite;
        }

        static TextMeshProUGUI Plain(RectTransform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            var rect = FrontUi.Box(parent, name, new Vector2(40f, 28f));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = FrontArt.Regular();
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}
