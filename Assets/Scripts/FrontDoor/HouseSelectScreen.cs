using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    public sealed class HouseChoice
    {
        public string Id;
        public string Name;
        public string Mark;
        public string Flavour;
        public bool Available;
    }

    public static class Houses
    {
        public static readonly HouseChoice[] All =
        {
            new HouseChoice { Id = "house_kharun", Name = "House Kharûn", Mark = "K", Flavour = "Scrubstone, contracts, quiet leverage.", Available = true },
            new HouseChoice { Id = "house_zamath", Name = "House Zamath", Mark = "Z", Flavour = "Forest, witching rods, pragmatic deals.", Available = false },
            new HouseChoice { Id = "house_thalor", Name = "House Thalor", Mark = "T", Flavour = "Proud, formal, dependent on the west.", Available = false },
            new HouseChoice { Id = "house_veythar", Name = "House Veythar", Mark = "V", Flavour = "Isolationist, militant, agrarian.", Available = false },
            new HouseChoice { Id = "house_ghorath", Name = "House Ghorath", Mark = "G", Flavour = "Debts, secrets, second chances.", Available = false },
        };
    }

    public sealed class HouseSelectScreen : MonoBehaviour
    {
        readonly List<Card> cards = new List<Card>();
        Button confirm;
        string selected = "";

        public static void Spawn()
        {
            var title = FindAnyObjectByType<TitleScreen>();
            if (title != null)
            {
                title.gameObject.SetActive(false);
                Destroy(title.gameObject);
            }

            var existing = FindAnyObjectByType<HouseSelectScreen>();
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            var host = new GameObject("HouseSelect");
            host.AddComponent<HouseSelectScreen>();
        }

        void Start()
        {
            PaintSky();
            FrontUi.EnsureEventSystem();
            Build();
            StormDirector.Ensure().BeginFade();
        }

        void PaintSky()
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 1f);
        }

        void Build()
        {
            var canvas = FrontUi.Canvas("HouseCanvas", 20);
            canvas.SetParent(transform, false);
            var column = FrontUi.Box(canvas, "Column", new Vector2(1400f, 820f));
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 0.5f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 28f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            FrontUi.Label(column, "Choose Your House", 40f, true, FrontUi.HouseTitle, new Vector2(1400f, 56f));
            FrontUi.Label(column, "The market does not forgive the uncommitted.", 16f, false, FrontUi.HouseSubtitle, new Vector2(1400f, 28f));

            var row = FrontUi.Box(column, "Houses", new Vector2(1200f, 270f));
            var rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredWidth = 1200f;
            rowElement.preferredHeight = 270f;
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 18f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            for (int i = 0; i < Houses.All.Length; i++)
                cards.Add(MakeCard(row, Houses.All[i]));

            confirm = FrontUi.MenuButton(column, "CONFIRM", false, Confirm);
            var back = FrontUi.Box(column, "Back", new Vector2(200f, 36f));
            var backElement = back.gameObject.AddComponent<LayoutElement>();
            backElement.preferredWidth = 200f;
            backElement.preferredHeight = 36f;
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0f, 0f, 0f, 0f);
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.transition = Selectable.Transition.None;
            backButton.onClick.AddListener(Gate.ShowTitle);
            var backLabel = FrontUi.Label(back, "Back", 16f, false, FrontUi.Back, new Vector2(200f, 36f));
            var backHost = (RectTransform)backLabel.transform.parent;
            backHost.anchorMin = Vector2.zero;
            backHost.anchorMax = Vector2.one;
            backHost.offsetMin = Vector2.zero;
            backHost.offsetMax = Vector2.zero;
            var backHostElement = backHost.GetComponent<LayoutElement>();
            if (backHostElement != null)
                backHostElement.ignoreLayout = true;
        }

        Card MakeCard(RectTransform row, HouseChoice house)
        {
            var rect = FrontUi.Box(row, house.Name, new Vector2(210f, 270f));
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 210f;
            element.preferredHeight = 270f;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = FrontArt.PlateQuiet();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = house.Available ? Color.white : new Color(0.55f, 0.5f, 0.45f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = house.Available;

            var body = FrontUi.Box(rect, "Body", new Vector2(210f, 270f));
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(14f, 14f);
            body.offsetMax = new Vector2(-14f, -14f);
            var bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.childAlignment = TextAnchor.MiddleCenter;
            bodyLayout.spacing = 10f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;
            body.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            bool open = house.Available;
            var mark = FrontUi.Label(body, house.Mark, 48f, true, open ? FrontUi.HouseTitle : FrontUi.LockedMark, new Vector2(180f, 64f));
            var name = FrontUi.Label(body, house.Name, 16f, true, open ? FrontUi.Bone : FrontUi.LockedName, new Vector2(180f, 28f));
            var flavour = FrontUi.Label(body, house.Flavour, 12f, false, open ? FrontUi.Flavour : FrontUi.LockedFlavour, new Vector2(180f, 72f), true);
            var status = FrontUi.Label(
                body,
                open ? "Available" : "Locked in Demo",
                12f,
                false,
                open ? FrontUi.Available : FrontUi.Locked,
                new Vector2(180f, 24f));

            IgnoreRaycasts(mark, name, flavour, status);
            var card = new Card { Id = house.Id, Plate = image, Available = open };
            if (open)
            {
                string id = house.Id;
                button.onClick.AddListener(() => Select(id));
            }

            return card;
        }

        void Select(string houseId)
        {
            selected = houseId;
            confirm.interactable = true;
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (!card.Available || card.Plate == null)
                    continue;
                bool chosen = card.Id == houseId;
                var hot = FrontArt.PlateHot();
                var quiet = FrontArt.PlateQuiet();
                if (hot != null && quiet != null)
                    card.Plate.sprite = chosen ? hot : quiet;
            }
        }

        void Confirm()
        {
            if (string.IsNullOrEmpty(selected))
                return;
            Gate.ConfirmRun(selected);
        }

        static void IgnoreRaycasts(params TMP_Text[] labels)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null)
                    continue;
                labels[i].raycastTarget = false;
            }
        }

        sealed class Card
        {
            public string Id;
            public Image Plate;
            public bool Available;
        }
    }
}
