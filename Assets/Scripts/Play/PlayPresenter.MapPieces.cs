using System.Collections.Generic;
using System.Text;
using Caravans.FrontDoor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caravans.Play
{
    public sealed partial class PlayPresenter
    {
        enum LandmarkDepart
        {
            None,
            Gate,
            Final,
            Return
        }

        GameObject tavernListen;
        GameObject tavernBoard;
        RectTransform tavernRows;
        readonly List<Button> tavernBuys = new List<Button>();
        string tavernCopy = string.Empty;
        Button sarnRoadButton;
        bool sarnRoadLit;
        LandmarkDepart landmarkDepart;
        RectTransform endRoot;
        TextMeshProUGUI endTitleText;
        TextMeshProUGUI endBodyText;

        void BuildTavernBoard(RectTransform spot)
        {
            var board = Child(spot, "MapBoard");
            Stretch(board, 24f);
            FrameSheet(board, false);
            var layout = board.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(28, 28, 18, 18);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            tavernRows = board;
            tavernBoard = board.gameObject;
        }

        void BuildEndScreen(RectTransform root)
        {
            endRoot = Child(root, "EndScreen");
            Stretch(endRoot);
            RaiseCanvas(endRoot.gameObject, 70);
            FrameSheet(endRoot, true);
            var layout = endRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(72, 72, 28, 24);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            endTitleText = Body(endRoot, UiType.Header, TextAlignmentOptions.Center);
            endTitleText.color = FrontUi.Gold;
            endTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            endBodyText = Body(endRoot, UiType.Small, TextAlignmentOptions.TopLeft);
            endBodyText.color = Ink;
            endBodyText.textWrappingMode = TextWrappingModes.Normal;
            endBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 820f;
            Button titleButton = StackButton(endRoot, "Return to the title", () => Gate.ReturnToTitle());
            titleButton.GetComponent<LayoutElement>().preferredHeight = 56f;
            endRoot.gameObject.SetActive(false);
        }

        void RefreshMapChrome()
        {
            bool kharun = session != null && session.Dock == PlaceId.Kharun;
            if (tavernListen != null)
                tavernListen.SetActive(!kharun);
            if (tavernBoard != null)
                tavernBoard.SetActive(kharun);
            if (kharun)
                RebuildTavernRows();
            if (endRoot != null)
            {
                bool show = session != null && session.EndScreenUp && !introOpen;
                endRoot.gameObject.SetActive(show);
                if (show)
                    PaintEndScreen();
            }
        }

        void RebuildTavernRows()
        {
            if (tavernRows == null || session == null)
                return;
            for (int i = tavernBuys.Count - 1; i >= 0; i--)
            {
                if (tavernBuys[i] != null)
                    buttons.Remove(tavernBuys[i]);
            }

            tavernBuys.Clear();
            ClearChildren(tavernRows);
            var copy = new StringBuilder();
            TextMeshProUGUI header = Body(tavernRows, UiType.Body, TextAlignmentOptions.Center);
            header.color = FrontUi.Gold;
            header.text = "Sarn's map";
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            copy.Append(header.text);
            int count = 6;
            for (int i = 1; i <= count; i++)
            {
                var row = Child(tavernRows, "Piece" + i);
                var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 0f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                bool owned = session.MapPieceOwned(i);
                bool onSale = session.MapPieceOnSale(i);
                string title = "Piece " + i + " of " + count + "  ·  " + session.MapPieceName(i);
                string body = session.MapPieceBody();
                string status;
                if (owned)
                    status = "Owned";
                else if (onSale)
                    status = LeadPriceLine();
                else
                    status = session.MapPieceLock(i);
                TextMeshProUGUI line = Body(row, UiType.Small, TextAlignmentOptions.MidlineLeft);
                line.color = Ink;
                line.text = title + "\n" + body + "\n" + status;
                line.textWrappingMode = TextWrappingModes.Normal;
                float height = onSale ? 118f : 78f;
                line.gameObject.AddComponent<LayoutElement>().preferredHeight = onSale ? 72f : height;
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
                copy.Append('\n');
                copy.Append(line.text);
                if (!onSale)
                    continue;
                int piece = i;
                Button buy = StackButton(row, "Buy", () => BuyShownPiece(piece));
                buy.interactable = session.Coin >= session.NextMapPiecePrice() && session.NextMapPiecePrice() >= 0;
                PaintTrade(buy);
                tavernBuys.Add(buy);
                copy.Append(buy.interactable ? "\nBuy" : "\nBuy grey");
            }

            tavernCopy = copy.ToString();
        }

        string LeadPriceLine()
        {
            int price = session.NextMapPiecePrice();
            int leads = session.MapPieceLeads;
            if (leads <= 0)
                return price + " coin";
            if (leads == 1)
                return "1 lead: " + price + " coin";
            return leads + " leads: " + price + " coin";
        }

        void BuyShownPiece(int oneBased)
        {
            session.TryBuyMapPiece(oneBased);
            Redraw();
        }

        void PaintEndScreen()
        {
            if (endTitleText != null)
                endTitleText.text = session.EndTitle;
            if (endBodyText != null)
                endBodyText.text = session.EndLedger();
            if (endRoot == null)
                return;
            var labels = endRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == endTitleText || labels[i] == endBodyText)
                    continue;
                labels[i].text = session.EndButtonLabel;
                labels[i].color = Ink;
            }
        }

        void ShowRestCard()
        {
            if (supplyGoButton != null)
                supplyGoButton.gameObject.SetActive(false);
            cardTitle.font = FrontArt.Display();
            cardBody.font = FrontArt.Regular();
            cardTitle.color = FrontUi.Gold;
            cardBody.color = Ink;
            cardBody.textWrappingMode = TextWrappingModes.Normal;
            cardTitle.text = session.RestTitle;
            cardBody.text = session.RestScene;
            rushButton.gameObject.SetActive(false);
            steadyButton.gameObject.SetActive(false);
            carefulButton.gameObject.SetActive(false);
            dawnButton.gameObject.SetActive(false);
            duskButton.gameObject.SetActive(false);
            confirmPaceButton.gameObject.SetActive(false);
            backPaceButton.gameObject.SetActive(false);
            campButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            if (encounterChoiceA != null)
                encounterChoiceA.gameObject.SetActive(true);
            if (encounterChoiceB != null)
                encounterChoiceB.gameObject.SetActive(true);
            SetChoiceLabel(encounterChoiceA, session.RestEmbarkLabel);
            SetChoiceLabel(encounterChoiceB, session.RestReturnLabel);
            FitEncounterCard(true, true);
        }

        void PressRoadChoice(int index)
        {
            if (session.RestPending)
            {
                if (index == 0)
                    PressRestEmbark();
                else
                    PressRestReturn();
                return;
            }

            session.ChooseEncounter(index);
        }

        void PressRestEmbark()
        {
            if (!session.RestPending)
                return;
            SupplyForecast forecast = session.ForecastFinal(session.ChosenPace);
            if (forecast.Red || forecast.Amber)
            {
                landmarkDepart = LandmarkDepart.Final;
                OpenDeparture(forecast, false);
                return;
            }

            session.TryStartFinalRun();
            Redraw();
        }

        void PressRestReturn()
        {
            if (!session.RestPending)
                return;
            SupplyForecast forecast = session.ForecastReturn(session.ChosenPace);
            if (forecast.Red || forecast.Amber)
            {
                landmarkDepart = LandmarkDepart.Return;
                OpenDeparture(forecast, false);
                return;
            }

            session.TryStartReturn();
            Redraw();
        }

        void EnsureSarnRoad(int sibling)
        {
            if (session == null || !session.MapJourneyAtGate || gatesColumn == null)
                return;
            sarnRoadButton = RoadRow(gatesColumn, "The road to Sarn's Rest", AskSarnRoad);
            sarnRoadButton.transform.SetSiblingIndex(sibling);
        }

        void ClearSarnRoad()
        {
            if (sarnRoadButton != null)
            {
                buttons.Remove(sarnRoadButton);
                DestroyImmediate(sarnRoadButton.gameObject);
                sarnRoadButton = null;
            }

            sarnRoadLit = false;
        }

        void AskSarnRoad()
        {
            if (session == null || !session.CanTakeRoad() || !session.MapJourneyAtGate)
                return;
            sarnRoadLit = true;
            roadLit = false;
            Redraw();
        }

        void PlaceSarnRoad(RectTransform host, int sibling)
        {
            if (sarnRoadButton == null || host == null)
                return;
            sarnRoadButton.transform.SetParent(host, false);
            sarnRoadButton.transform.SetSiblingIndex(sibling);
        }

        bool TryDrawLandmarkRoute()
        {
            if (routeRoot == null || session == null)
                return false;
            Vector2 from;
            Vector2 to;
            bool show = false;
            if (session.OnMapRoute(out from, out to))
                show = true;
            else if (sarnRoadLit && !session.Traveling && session.MapJourneyAtGate)
            {
                from = session.GateMapPoint();
                to = session.RestMapPoint();
                show = true;
            }

            if (!show)
                return false;
            routeRoot.gameObject.SetActive(true);
            PlaceOnMap(routeMark.rectTransform, to, 72f);
            EnsureRouteSegments(1);
            routeSegments[0].gameObject.SetActive(true);
            PlaceSegment(routeSegments[0].rectTransform, from, to, 8f);
            for (int i = 1; i < routeSegments.Count; i++)
                routeSegments[i].gameObject.SetActive(false);
            return true;
        }

        void EmbarkSarnRoad()
        {
            if (!sarnRoadLit || session == null || !session.CanTakeRoad() || !session.MapJourneyAtGate)
                return;
            SupplyForecast forecast = session.ForecastGate(draftPace);
            if (forecast.Red || forecast.Amber)
            {
                landmarkDepart = LandmarkDepart.Gate;
                OpenDeparture(forecast, false);
                return;
            }

            CommitEmbark();
        }

        bool CommitLandmark()
        {
            LandmarkDepart kind = landmarkDepart;
            bool sarn = sarnRoadLit && kind == LandmarkDepart.None;
            if (kind == LandmarkDepart.None && !sarn)
                return false;
            roadLit = false;
            sarnRoadLit = false;
            lookingAtMap = false;
            departureOpen = false;
            landmarkDepart = LandmarkDepart.None;
            if (kind == LandmarkDepart.Final)
                session.TryStartFinalRun();
            else if (kind == LandmarkDepart.Return)
                session.TryStartReturn();
            else
                session.TryStartGateRoad();
            return true;
        }

        string LandmarkDeparturePlace(PlaceId fallback)
        {
            if (landmarkDepart == LandmarkDepart.Return)
                return CaravanSession.PlaceName(PlaceId.Westmark);
            if (landmarkDepart == LandmarkDepart.Gate || landmarkDepart == LandmarkDepart.Final)
                return "Sarn's Rest";
            return CaravanSession.PlaceName(fallback);
        }

#if UNITY_EDITOR
        bool ShotTavernLocked()
        {
            ShotClearOverlays();
            if (session.Traveling || session.Dock != PlaceId.Kharun)
                session.ShotDockAt(PlaceId.Kharun);
            session.SetKharunPoints(0f);
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Tavern;
            FinishShot();
            return plazaSpot == PlazaSpot.Tavern
                && tavernTab != null
                && tavernTab.gameObject.activeSelf
                && tavernCopy.Contains("Needs Kharûn rank 1")
                && tavernCopy.Contains("A fragment of Sarn's map.")
                && !tavernCopy.Contains("\nBuy");
        }

        bool ShotTavernBuy()
        {
            ShotClearOverlays();
            if (session.Traveling || session.Dock != PlaceId.Kharun)
                session.ShotDockAt(PlaceId.Kharun);
            session.SetKharunPoints(100f);
            session.SetCoin(2000);
            session.SetMapPieceLeads(1);
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Tavern;
            FinishShot();
            return tavernCopy.Contains("1 lead: 540 coin")
                && tavernCopy.Contains("\nBuy")
                && !tavernCopy.Contains("Buy grey")
                && tavernCopy.Contains("Needs Kharûn rank 3");
        }

        bool ShotTavernOwned()
        {
            ShotClearOverlays();
            if (session.Traveling || session.Dock != PlaceId.Kharun)
                session.ShotDockAt(PlaceId.Kharun);
            session.SetKharunPoints(100f);
            session.SetCoin(2000);
            session.TryBuyMapPiece(1);
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Tavern;
            FinishShot();
            return session.MapPieceOwned(1)
                && tavernCopy.Contains("Owned")
                && tavernCopy.Contains("Needs Kharûn rank 3")
                && !tavernCopy.Contains("\nBuy");
        }

        bool ShotWestmarkGate()
        {
            ShotClearOverlays();
            session.ShotOwnPieces(6, true);
            session.SetKharunPoints(1000f);
            session.ShotDockAt(PlaceId.Westmark);
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Gates;
            FinishShot();
            return session.Dock == PlaceId.Westmark
                && session.MapJourneyUnlocked
                && sarnRoadButton != null
                && sarnRoadButton.gameObject.activeInHierarchy
                && !session.RestPending
                && !session.EndScreenUp;
        }

        bool ShotSarnsRest()
        {
            ShotClearOverlays();
            session.ShotOwnPieces(6, true);
            session.SetKharunPoints(1000f);
            session.ShotDockAt(PlaceId.Westmark);
            if (!session.TryStartGateRoad())
                return false;
            session.CompleteMapLeg();
            page = Page.Plaza;
            plazaSpot = PlazaSpot.Gates;
            FinishShot();
            return session.RestPending
                && cardTitle != null
                && cardTitle.text == "Sarn's Rest"
                && cardBody != null
                && cardBody.text.Contains("Plain lilies at the water")
                && encounterChoiceA != null
                && encounterChoiceA.gameObject.activeSelf
                && encounterChoiceB != null
                && encounterChoiceB.gameObject.activeSelf;
        }

        bool ShotEndScreen()
        {
            ShotClearOverlays();
            session.ShotOwnPieces(6, true);
            session.SetKharunPoints(1000f);
            session.ShotShowEnd();
            page = Page.Market;
            FinishShot();
            return session.EndScreenUp
                && endRoot != null
                && endRoot.gameObject.activeSelf
                && endTitleText != null
                && endTitleText.text == "Embarked"
                && endBodyText != null
                && endBodyText.text.Contains("The ledger")
                && endBodyText.text.Contains("Rank 10")
                && endBodyText.text.Contains("0 of 15")
                && endBodyText.text.Contains("30500 coin")
                && endBodyText.text.Contains("1 Vekmar, Year 112")
                && endBodyText.text.Contains("Return to the title") == false
                && session.EndButtonLabel == "Return to the title";
        }
#endif
    }
}
