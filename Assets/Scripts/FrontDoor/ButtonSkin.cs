using UnityEngine;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    // Option A plates. Wide is the dock, the road, and a card. Compact is Sort, Buy, Sell, and the gates paces.
    public static class ButtonSkin
    {
        public static readonly Color LabelNormal = new Color32(0xEA, 0xDC, 0xC0, 0xFF);
        public static readonly Color LabelHover = new Color32(0xF4, 0xE8, 0xC8, 0xFF);
        public static readonly Color LabelPressed = new Color32(0xC8, 0xBA, 0x9E, 0xFF);
        public static readonly Color LabelDisabled = new Color32(0x8C, 0x80, 0x6B, 220);
        public static readonly Color LabelSelected = new Color32(0xE8, 0xC7, 0x6B, 0xFF);

        const string Wide = "Ui/Buttons/wide_";
        const string Compact = "Ui/Buttons/compact_";

        static Sprite wideNormal;
        static Sprite wideHover;
        static Sprite widePressed;
        static Sprite wideDisabled;
        static Sprite wideSelected;
        static Sprite compactNormal;
        static Sprite compactHover;
        static Sprite compactPressed;
        static Sprite compactDisabled;
        static Sprite compactSelected;

        public static void ApplyWide(Button button)
        {
            Apply(button, true);
        }

        public static void ApplyCompact(Button button)
        {
            Apply(button, false);
        }

        public static void SetChosen(Button button, bool chosen)
        {
            if (button == null)
                return;
            ButtonPlate plate = button.GetComponent<ButtonPlate>();
            if (plate == null)
                return;
            plate.SetChosen(chosen);
        }

        public static void Refresh(Button button)
        {
            if (button == null)
                return;
            ButtonPlate plate = button.GetComponent<ButtonPlate>();
            if (plate != null)
                plate.Paint();
        }

        static void Apply(Button button, bool wide)
        {
            if (button == null)
                return;
            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            Sprite normal = Plate(wide, "normal");
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            if (normal != null)
                image.pixelsPerUnitMultiplier = UiType.Tuck(normal, EdgeFor(button));
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Plate(wide, "hover"),
                pressedSprite = Plate(wide, "pressed"),
                disabledSprite = Plate(wide, "disabled"),
                selectedSprite = Plate(wide, "selected"),
            };
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            ButtonPlate plate = button.GetComponent<ButtonPlate>();
            if (plate == null)
                plate = button.gameObject.AddComponent<ButtonPlate>();
            plate.Bind(button, image, normal, Plate(wide, "selected"));
        }

        // A 24 px border on a short plate eats the face. Tuck it to 12 px under 96 reference pixels.
        static float EdgeFor(Button button)
        {
            float height = 0f;
            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element != null && element.preferredHeight > 1f)
                height = element.preferredHeight;
            if (height <= 1f)
            {
                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                    height = Mathf.Abs(rect.sizeDelta.y);
            }

            return height >= 96f ? 24f : 12f;
        }

        static Sprite Plate(bool wide, string face)
        {
            if (wide)
            {
                if (face == "hover")
                    return wideHover = wideHover != null ? wideHover : Load(Wide + "hover");
                if (face == "pressed")
                    return widePressed = widePressed != null ? widePressed : Load(Wide + "pressed");
                if (face == "disabled")
                    return wideDisabled = wideDisabled != null ? wideDisabled : Load(Wide + "disabled");
                if (face == "selected")
                    return wideSelected = wideSelected != null ? wideSelected : Load(Wide + "selected");
                return wideNormal = wideNormal != null ? wideNormal : Load(Wide + "normal");
            }

            if (face == "hover")
                return compactHover = compactHover != null ? compactHover : Load(Compact + "hover");
            if (face == "pressed")
                return compactPressed = compactPressed != null ? compactPressed : Load(Compact + "pressed");
            if (face == "disabled")
                return compactDisabled = compactDisabled != null ? compactDisabled : Load(Compact + "disabled");
            if (face == "selected")
                return compactSelected = compactSelected != null ? compactSelected : Load(Compact + "selected");
            return compactNormal = compactNormal != null ? compactNormal : Load(Compact + "normal");
        }

        static Sprite Load(string path)
        {
            return Resources.Load<Sprite>(path);
        }
    }
}
