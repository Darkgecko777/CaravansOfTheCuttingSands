using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caravans.FrontDoor
{
    public sealed class ButtonPlate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Button button;
        Image image;
        TextMeshProUGUI label;
        Sprite normal;
        Sprite selected;
        bool over;
        bool down;
        bool chosen;

        public void Bind(Button host, Image face, Sprite quiet, Sprite chosenFace)
        {
            button = host;
            image = face;
            normal = quiet;
            selected = chosenFace != null ? chosenFace : quiet;
            label = host.GetComponentInChildren<TextMeshProUGUI>();
            Paint();
        }

        public void SetChosen(bool value)
        {
            chosen = value;
            Paint();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            over = true;
            Paint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            over = false;
            down = false;
            Paint();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            down = true;
            Paint();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            down = false;
            Paint();
        }

        void OnEnable()
        {
            Paint();
        }

        void Update()
        {
            Paint();
        }

        public void Paint()
        {
            if (image != null && normal != null)
            {
                Sprite face = chosen && selected != null ? selected : normal;
                if (image.sprite != face)
                    image.sprite = face;
            }

            if (label == null)
                return;
            Color ink = LabelInk();
            if (label.color != ink)
                label.color = ink;
        }

        Color LabelInk()
        {
            if (button != null && !button.interactable)
                return ButtonSkin.LabelDisabled;
            if (down)
                return ButtonSkin.LabelPressed;
            if (over)
                return ButtonSkin.LabelHover;
            if (chosen)
                return ButtonSkin.LabelSelected;
            return ButtonSkin.LabelNormal;
        }
    }
}
