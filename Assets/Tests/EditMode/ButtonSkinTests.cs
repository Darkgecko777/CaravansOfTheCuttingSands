using System.Collections.Generic;
using Caravans.FrontDoor;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caravans.Play.Tests
{
    public class ButtonSkinTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                    Object.DestroyImmediate(spawned[i]);
            }

            spawned.Clear();
        }

        [Test]
        public void WideAndCompactPlatesAreSlicedSpriteSwaps()
        {
            // FrontUi.MenuItem, MenuLine, IntroBegin, the options backdrop, the pause
            // backdrop, and MakeSquare goods cells are not resin seals. They stay off
            // this skin: text rows, a full-screen dimmer, or a goods tile. A full
            // PlayPresenter is not built here. Awake starts a caravan. BarSeal,
            // StackButton, and RoadRow call ApplyWide. TradeSeal calls ApplyCompact.
            // Tab calls ApplyWide, and Sort plus the gates paces call ApplyCompact.
            Sprite wide = Resources.Load<Sprite>("Ui/Buttons/wide_normal");
            Sprite compact = Resources.Load<Sprite>("Ui/Buttons/compact_normal");
            Assert.IsNotNull(wide);
            Assert.IsNotNull(compact);
            Assert.AreEqual(24f, wide.border.x, 0.5f);
            Assert.AreEqual(24f, wide.border.y, 0.5f);
            Assert.AreEqual(24f, wide.border.z, 0.5f);
            Assert.AreEqual(24f, wide.border.w, 0.5f);
            Assert.AreEqual(24f, compact.border.x, 0.5f);

            Button wideButton = Make("Logbook", 64f);
            ButtonSkin.ApplyWide(wideButton);
            AssertPlate(wideButton, "Ui/Buttons/wide_");
            Assert.Greater(wideButton.GetComponent<Image>().pixelsPerUnitMultiplier, 1.5f);

            Button compactButton = Make("Buy", 112f);
            ButtonSkin.ApplyCompact(compactButton);
            AssertPlate(compactButton, "Ui/Buttons/compact_");
            Assert.AreEqual(1f, compactButton.GetComponent<Image>().pixelsPerUnitMultiplier, 0.05f);
        }

        [Test]
        public void ChosenPlateUsesTheSelectedFaceAndLabel()
        {
            Button button = Make("Steady", 64f);
            ButtonSkin.ApplyCompact(button);
            ButtonSkin.SetChosen(button, true);
            Assert.AreEqual(Resources.Load<Sprite>("Ui/Buttons/compact_selected"), button.GetComponent<Image>().sprite);
            Assert.AreEqual(ButtonSkin.LabelSelected, LabelColor(button));
            ButtonSkin.SetChosen(button, false);
            Assert.AreEqual(Resources.Load<Sprite>("Ui/Buttons/compact_normal"), button.GetComponent<Image>().sprite);
            Assert.AreEqual(ButtonSkin.LabelNormal, LabelColor(button));
        }

        [Test]
        public void DisabledBuyUsesTheDisabledInk()
        {
            Button button = Make("Buy", 112f);
            ButtonSkin.ApplyCompact(button);
            button.interactable = false;
            ButtonSkin.Refresh(button);
            Assert.AreEqual(ButtonSkin.LabelDisabled, LabelColor(button));
            Assert.AreEqual(Resources.Load<Sprite>("Ui/Buttons/compact_disabled"), button.spriteState.disabledSprite);
        }

        [Test]
        public void AnUnskinnedButtonStaysAPlainHit()
        {
            Button button = Make("GoodsCell", 120f);
            Assert.AreEqual(Selectable.Transition.ColorTint, button.transition);
            Assert.AreNotEqual(Image.Type.Sliced, button.GetComponent<Image>().type);
        }

        static void AssertPlate(Button button, string prefix)
        {
            Image image = button.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced, image.type);
            Assert.AreEqual(Color.white, image.color);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, button.transition);
            Assert.AreEqual(Navigation.Mode.None, button.navigation.mode);
            Assert.AreEqual(image, button.targetGraphic);
            Assert.AreEqual(Resources.Load<Sprite>(prefix + "normal"), image.sprite);
            Assert.AreEqual(Resources.Load<Sprite>(prefix + "hover"), button.spriteState.highlightedSprite);
            Assert.AreEqual(Resources.Load<Sprite>(prefix + "pressed"), button.spriteState.pressedSprite);
            Assert.AreEqual(Resources.Load<Sprite>(prefix + "disabled"), button.spriteState.disabledSprite);
            Assert.AreEqual(Resources.Load<Sprite>(prefix + "selected"), button.spriteState.selectedSprite);
        }

        static Color LabelColor(Button button)
        {
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>();
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].gameObject != button.gameObject)
                    return graphics[i].color;
            }

            return Color.clear;
        }

        Button Make(string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            spawned.Add(go);
            go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = name;
            text.raycastTarget = false;
            return button;
        }
    }
}
