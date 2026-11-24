using Caravans.FrontDoor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caravans.Play
{
    public sealed partial class PlayPresenter
    {
        TextMeshProUGUI waterText;
        TextMeshProUGUI rationText;
        Button supplyGoButton;
        bool departureOpen;
        bool departureFromCamp;
        bool shotDeparture;
        string shotDepartureTitle = string.Empty;
        string shotDepartureBody = string.Empty;
        SupplyForecast departureForecast;

        void ContinueCard()
        {
            if (session.DeathPending)
            {
                departureOpen = false;
                session.ContinueDeath();
                return;
            }

            if (session.CriticalPending)
            {
                session.ContinueCritical();
                return;
            }

            session.ContinueEncounter();
        }

        void OpenDeparture(SupplyForecast forecast, bool fromCamp)
        {
            departureForecast = forecast;
            departureFromCamp = fromCamp;
            departureOpen = true;
            shotDeparture = false;
            Redraw();
        }

        void ConfirmDeparture()
        {
            bool fromCamp = departureFromCamp;
            departureOpen = false;
            shotDeparture = false;
            if (fromCamp)
            {
                session.ChooseRoadPace(draftPace);
                return;
            }

            CommitEmbark();
        }

        void CommitEmbark()
        {
            session.ChooseRoadPace(draftPace);
            PlaceId dest = pendingDest;
            if (CommitLandmark())
                return;
            roadLit = false;
            lookingAtMap = false;
            departureOpen = false;
            session.DepartTo(dest);
        }

        bool SupplyCardUp()
        {
            if (session.DeathPending)
                return true;
            if (departureOpen)
                return true;
            return session.CriticalPending && !(session.EncounterPending && !session.EncounterResolved);
        }

        void ShowSupplyCard(bool death, bool critical)
        {
            cardTitle.font = FrontArt.Display();
            cardBody.font = FrontArt.Regular();
            cardTitle.color = FrontUi.Gold;
            cardBody.color = Ink;
            cardBody.textWrappingMode = TextWrappingModes.Normal;
            cardBody.alignment = TextAlignmentOptions.Center;
            rushButton.gameObject.SetActive(false);
            steadyButton.gameObject.SetActive(false);
            carefulButton.gameObject.SetActive(false);
            dawnButton.gameObject.SetActive(false);
            duskButton.gameObject.SetActive(false);
            confirmPaceButton.gameObject.SetActive(false);
            campButton.gameObject.SetActive(false);
            if (encounterChoiceA != null)
                encounterChoiceA.gameObject.SetActive(false);
            if (encounterChoiceB != null)
                encounterChoiceB.gameObject.SetActive(false);

            bool departure = !death && !critical;
            continueButton.gameObject.SetActive(!departure);
            if (supplyGoButton != null)
                supplyGoButton.gameObject.SetActive(departure);
            backPaceButton.gameObject.SetActive(departure);
            if (death)
            {
                cardTitle.text = session.RescueTitle;
                cardBody.text = session.RescueLead + "\n\n" + session.RescueBody;
            }
            else if (critical)
            {
                cardTitle.text = "The Skins Run Low";
                cardBody.text = "One more watch of water, perhaps two. Then nothing.";
            }
            else if (shotDeparture)
            {
                cardTitle.text = shotDepartureTitle;
                cardBody.text = shotDepartureBody;
            }
            else if (departureForecast.Red)
            {
                cardTitle.text = "You will run dry";
                cardBody.text = DepartureCopy(departureForecast);
            }
            else
            {
                cardTitle.text = "The road";
                cardBody.text = DepartureCopy(departureForecast);
            }

            FitSupplyCard(departure);
        }

        string DepartureCopy(SupplyForecast forecast)
        {
            PlaceId dest = departureFromCamp ? session.LegDestination : pendingDest;
            string place = LandmarkDeparturePlace(dest);
            if (forecast.Red)
            {
                return "You will run dry before " + place + ". Haul " + forecast.HaulWater
                    + " water and " + forecast.HaulRations + " rations more.";
            }

            return "This road needs about " + Amount(forecast.WaterNeed) + " L and " + Amount(forecast.RationNeed)
                + " kg. You carry " + Amount(forecast.SupplyWater) + " L and " + Amount(forecast.SupplyRations)
                + " kg (with " + forecast.PackUnits + " in the packs).";
        }

        void FitSupplyCard(bool twoButtons)
        {
            if (cardPanel == null || cardBody == null || continueButton == null)
                return;
            const float width = 764f;
            string copy = cardBody.text ?? "";
            float textH = cardBody.GetPreferredValues(copy, width, 0f).y;
            float line = UiType.Body * 1.25f;
            float guess = Mathf.Max(line, Mathf.Ceil(Mathf.Max(copy.Length, 1) / 42f) * line);
            if (textH < guess * 0.6f)
                textH = guess;
            textH = Mathf.Max(96f, textH + 8f);
            var bodyRt = cardBody.rectTransform;
            bodyRt.sizeDelta = new Vector2(-96f, textH);
            bodyRt.anchoredPosition = new Vector2(0f, -112f);
            float buttonBlock = twoButtons ? 148f : 72f;
            float panelH = 112f + textH + 28f + buttonBlock + 48f;
            if (panelH < 520f)
                panelH = 520f;
            cardPanel.sizeDelta = new Vector2(860f, panelH);
            float top = -112f - textH - 24f;
            if (!twoButtons)
            {
                PlaceSupplyButton(continueButton, top, 320f);
                return;
            }

            PlaceSupplyButton(supplyGoButton, top, 460f);
            PlaceSupplyButton(backPaceButton, top - 76f, 200f);
        }

        static void PlaceSupplyButton(Button button, float top, float width)
        {
            if (button == null)
                return;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, 56f);
            rect.anchoredPosition = new Vector2(0f, top);
        }

        Color KitTint(float hours)
        {
            if (hours <= session.CriticalHours + 0.0001f)
            {
#if UNITY_EDITOR
                float wave = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Caravans.FrontDoor.TrailerClock.Time(Time.unscaledTime) * 4f));
#else
                float wave = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
#endif
                return Color.Lerp(new Color(0.55f, 0.18f, 0.14f, 1f), new Color(0.86f, 0.32f, 0.24f, 1f), wave);
            }

            if (hours <= session.LowHours + 0.0001f)
                return new Color(0.93f, 0.72f, 0.38f, 1f);
            return Ink;
        }
    }
}
