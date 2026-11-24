#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Caravans.Play
{
    public sealed partial class PlayPresenter
    {
        struct TrailerEvent
        {
            public string Name;
            public int Frame;
        }

        bool trailerLive;
        int trailerFrame;
        int trailerLength;
        string trailerShot = string.Empty;
        string trailerFault = string.Empty;
        bool trailerSawNight;
        bool trailerSawVision;
        bool trailerSawHaze;
        int trailerBuys;
        readonly List<TrailerEvent> trailerEvents = new List<TrailerEvent>();

        public bool TrailerShotDone
        {
            get { return trailerLive == false && trailerFrame >= trailerLength && trailerLength > 0; }
        }

        public int TrailerFrameCount
        {
            get { return trailerFrame; }
        }

        public string TrailerFault
        {
            get { return trailerFault; }
        }

        public string TrailerFingerprint
        {
            get { return session == null ? string.Empty : session.TrailerFingerprint(); }
        }

        public string TrailerEventsJson
        {
            get
            {
                var line = new StringBuilder();
                line.Append('[');
                for (int i = 0; i < trailerEvents.Count; i++)
                {
                    if (i > 0)
                        line.Append(',');
                    line.Append("{\"name\":\"");
                    line.Append(trailerEvents[i].Name);
                    line.Append("\",\"t\":");
                    line.Append((trailerEvents[i].Frame / 60d).ToString("0.00", CultureInfo.InvariantCulture));
                    line.Append('}');
                }

                line.Append(']');
                return line.ToString();
            }
        }

        public int TrailerEventCount
        {
            get { return trailerEvents.Count; }
        }

        public string TrailerEventName(int index)
        {
            return trailerEvents[index].Name;
        }

        public int TrailerEventFrame(int index)
        {
            return trailerEvents[index].Frame;
        }

        public bool TrailerBegin(string shotId)
        {
            trailerLive = false;
            trailerFrame = 0;
            trailerLength = 0;
            trailerFault = string.Empty;
            trailerShot = shotId ?? string.Empty;
            trailerSawNight = false;
            trailerSawVision = false;
            trailerSawHaze = false;
            trailerBuys = 0;
            trailerEvents.Clear();
            ShotClearOverlays();
            session.ShotPinRun();
            if (session.RunSeed != CaravanSession.EditorShotSeed)
            {
                trailerFault = "Run seed is not 4817.";
                return false;
            }

            session.TrailerReleaseQuiet();
            shotHidesHover = true;
            HideHover();
            if (!session.TrailerApply(trailerShot))
            {
                trailerFault = "Setup returned false.";
                return false;
            }

            if (!ApplyTrailerUi(trailerShot))
            {
                trailerFault = "The screen for this shot did not open.";
                return false;
            }

            SnapWeatherLook();
            FinishShot();
            trailerLength = TrailerFrames(trailerShot);
            if (trailerLength <= 0)
            {
                trailerFault = "This shot has no length.";
                return false;
            }

            return true;
        }

        public void TrailerStartFrames()
        {
            Caravans.FrontDoor.TrailerClock.ResetFrames();
            trailerFrame = 0;
            trailerLive = true;
            trailerSawNight = false;
            trailerSawVision = false;
            trailerSawHaze = false;
            trailerBuys = 0;
            trailerEvents.Clear();
            trailerFault = string.Empty;
        }

        public void TrailerUpdate()
        {
            if (!Caravans.FrontDoor.TrailerClock.Active || session == null)
                return;
            Caravans.FrontDoor.TrailerClock.Advance();
            if (session.TrailerQuietHeld)
                session.TrailerQuietRoad();
            if (!trailerLive)
                return;
            RunTrailerActions(trailerFrame);
        }

        public void TrailerSample()
        {
            if (!trailerLive || session == null)
                return;
            SampleTrailerFrame();
            trailerFrame++;
            if (trailerFrame >= trailerLength)
                trailerLive = false;
        }

        bool ApplyTrailerUi(string shotId)
        {
            switch (shotId)
            {
                case "02_kharun_market":
                case "05_market_buy":
                case "06_market_sell":
                case "16_logbook_road_note":
                case "17_village_flip":
                case "18_rank_up":
                case "22_embarked":
                case "27_ridgewatch_market":
                    page = Page.Market;
                    return true;
                case "07_inn_fill":
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Inn;
                    return true;
                case "09_tavern_hire":
                case "19_tavern_map_piece":
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Tavern;
                    return true;
                case "21_sarn_road":
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    Redraw();
                    AskSarnRoad();
                    return sarnRoadLit;
                case "24_departure_short":
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    Redraw();
                    AskRoad(PlaceId.Brineford);
                    if (!roadLit)
                        return false;
                    Embark();
                    return departureOpen && cardTitle != null && cardTitle.text == "You will run dry";
                default:
                    page = Page.Plaza;
                    plazaSpot = PlazaSpot.Gates;
                    return true;
            }
        }

        void RunTrailerActions(int frame)
        {
            switch (trailerShot)
            {
                case "02_kharun_market":
                    if (frame == TrailerAt(2.5d))
                        TrailerSelectStocked(true);
                    break;
                case "03_depart_draven":
                    if (frame == TrailerAt(0.6d))
                        TrailerAskDraven();
                    else if (frame == TrailerAt(1.6d))
                        TrailerEmbark();
                    else if (frame == TrailerAt(1.7d))
                        TrailerPace(2);
                    break;
                case "05_market_buy":
                    if (frame == TrailerAt(0.5d))
                        TrailerSelectPriced();
                    else if (frame == TrailerAt(1.2d) || frame == TrailerAt(1.8d))
                        TrailerBuy();
                    break;
                case "06_market_sell":
                    if (frame == TrailerAt(0.5d))
                        TrailerSelectUnit();
                    else if (frame == TrailerAt(1.2d))
                        TrailerSell("sell");
                    break;
                case "07_inn_fill":
                    if (frame == TrailerAt(0.8d))
                        TrailerFillWater();
                    else if (frame == TrailerAt(1.6d))
                        TrailerFillFood();
                    break;
                case "09_tavern_hire":
                    if (frame == TrailerAt(1.0d))
                        TrailerHire("Guard", "hire-guard");
                    else if (frame == TrailerAt(2.6d))
                        TrailerHire("Scout", "hire-scout");
                    break;
                case "10_gates_scout_rush":
                    if (frame == TrailerAt(0.5d))
                        TrailerAskDraven();
                    else if (frame == TrailerAt(1.3d))
                        TrailerPickRush();
                    break;
                case "11_encounter_good":
                    if (frame == TrailerAt(2.0d))
                        TrailerResolve(true);
                    break;
                case "13_guard_save":
                    if (frame == TrailerAt(1.0d))
                        TrailerResolve(false);
                    break;
                case "15_rush_cargo_loss":
                    if (frame == TrailerAt(2.0d))
                        TrailerResolve(false);
                    break;
                case "16_logbook_road_note":
                    if (frame == TrailerAt(0.4d))
                        TrailerOpenLogbook();
                    break;
                case "17_village_flip":
                    if (frame == TrailerAt(1.0d))
                        TrailerSelectUnit();
                    else if (frame == TrailerAt(1.8d))
                        TrailerSell("flip");
                    break;
                case "18_rank_up":
                    if (frame == TrailerAt(0.6d))
                        TrailerSelectUnit();
                    else if (frame == TrailerAt(1.2d))
                        TrailerSell("toast");
                    break;
                case "19_tavern_map_piece":
                    if (frame == TrailerAt(1.5d))
                        TrailerBuyPiece();
                    break;
                case "20_westmark_gate":
                    if (frame == TrailerAt(0.8d))
                        TrailerAskSarn();
                    break;
                case "21_sarn_road":
                    if (frame == TrailerAt(0.3d))
                        TrailerEmbarkSarn();
                    else if (frame == TrailerAt(0.4d))
                        TrailerPaceQuiet();
                    break;
                case "27_ridgewatch_market":
                    if (frame == TrailerAt(1.5d))
                        TrailerSelectStocked(true);
                    break;
            }
        }

        void SampleTrailerFrame()
        {
            if (trailerShot == "08_road_dusk" && !trailerSawNight && session.Hour >= EconomyRates.NightHour)
            {
                trailerSawNight = true;
                NoteTrailer("night");
            }

            if (trailerShot != "14_sandstorm_in")
                return;
            WeatherKind here = session.WeatherAt(session.CaravanPoint);
            if (trailerFrame == 0)
            {
                if (here != WeatherKind.Clear || SandstormLookWeight > 0.001f)
                    FailTrailer("Frame 0 is not a clear sky with the sandstorm look at rest.");
                if (!TrailerStormOverlapsVision())
                    Debug.Log("Trailer sandstorm patch is outside the vision circle at frame 0 of 14_sandstorm_in.");
            }

            if (!trailerSawVision
                && here == WeatherKind.Sandstorm
                && Mathf.Abs(session.VisionRadius() - TravelScale.VisionSandstorm) <= 0.0001f)
            {
                trailerSawVision = true;
                NoteTrailer("vision-snap");
            }

            if (trailerSawVision && !trailerSawHaze && SandstormLookWeight >= 0.999f)
            {
                trailerSawHaze = true;
                NoteTrailer("haze-full");
            }

            if (trailerFrame == 300
                && (here != WeatherKind.Sandstorm || SandstormLookWeight < 0.999f))
                FailTrailer("Frame 300 is not inside the sandstorm with the haze full.");
        }

        bool TrailerStormOverlapsVision()
        {
            IReadOnlyList<WeatherMass> masses = session.Masses;
            float vision = session.VisionRadius();
            Vector2 point = session.CaravanPoint;
            for (int i = 0; i < masses.Count; i++)
            {
                WeatherMass mass = masses[i];
                if (mass.Kind != WeatherKind.Sandstorm)
                    continue;
                return RoadPath.MapDistance(point, mass.Center) < vision + mass.Radius;
            }

            return false;
        }

        void TrailerSelectStocked(bool note)
        {
            SelectShop(ShotStocked());
            FinishShot();
            if (!shopSelected)
            {
                FailTrailer("No stocked good could be selected.");
                return;
            }

            if (note)
                NoteTrailer("select");
        }

        void TrailerSelectPriced()
        {
            GoodId pick = GoodId.ShimmersteelJars;
            bool found = false;
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                if (good == GoodId.Rations || good == GoodId.Water || good == GoodId.SeaWater)
                    continue;
                if (session.StockOf(session.Dock, good) < 2)
                    continue;
                if (session.BuyCost(session.Dock, good) < 20)
                    continue;
                pick = good;
                found = true;
                break;
            }

            if (!found)
            {
                FailTrailer("No stocked good costs 20 or more.");
                return;
            }

            SelectShop(pick);
            FinishShot();
        }

        void TrailerBuy()
        {
            int before = session.CargoCount;
            PressBuy();
            FinishShot();
            if (session.CargoCount <= before)
            {
                FailTrailer("Buy did not take a unit.");
                return;
            }

            trailerBuys++;
            NoteTrailer(trailerBuys == 1 ? "buy-1" : "buy-2");
        }

        void TrailerSelectUnit()
        {
            SelectWagon(0);
            FinishShot();
            if (wagonPick != 0)
                FailTrailer("The first pack unit is not selected.");
        }

        void TrailerSell(string eventName)
        {
            int before = session.CargoCount;
            PressSell();
            FinishShot();
            if (session.CargoCount >= before)
            {
                FailTrailer("Sell did not leave the pack.");
                return;
            }

            if (eventName == "flip" && session.PatronOf(PlaceId.Draven) != HouseId.Kharun)
            {
                FailTrailer("The sale did not make House Kharûn patron at Draven.");
                return;
            }

            if (eventName == "toast")
            {
                if (standingToast == null || standingToast.text.IndexOf("rank 5", System.StringComparison.Ordinal) < 0)
                {
                    FailTrailer("The rank toast did not say rank 5.");
                    return;
                }
            }

            NoteTrailer(eventName);
        }

        void TrailerFillWater()
        {
            if (!session.TryFillWater())
            {
                FailTrailer("Fill water did not run.");
                return;
            }

            FinishShot();
            NoteTrailer("fill-water");
        }

        void TrailerFillFood()
        {
            if (!session.TryFillFood())
            {
                FailTrailer("Fill food did not run.");
                return;
            }

            FinishShot();
            NoteTrailer("fill-food");
        }

        void TrailerHire(string role, string eventName)
        {
            HireShownAgent(role);
            FinishShot();
            bool hired = role == "Guard" ? session.GuardHired : session.ScoutHired;
            if (!hired)
            {
                FailTrailer(role + " was not hired.");
                return;
            }

            NoteTrailer(eventName);
        }

        void TrailerAskDraven()
        {
            AskRoad(PlaceId.Draven);
            FinishShot();
            if (!roadLit || pendingDest != PlaceId.Draven)
            {
                FailTrailer("The Draven road did not light.");
                return;
            }

            NoteTrailer("road-lit");
        }

        void TrailerEmbark()
        {
            Embark();
            FinishShot();
            if (!session.Traveling || departureOpen)
            {
                FailTrailer("Embark did not leave the dock.");
                return;
            }

            NoteTrailer("embark");
        }

        void TrailerPace(int pace)
        {
            session.ChoosePace(pace);
            FinishShot();
        }

        void TrailerPickRush()
        {
            PickPace(RoadPace.Rush);
            FinishShot();
            string line = session.ScoutLine(draftPace, roadLit || sarnRoadLit);
            if (string.IsNullOrEmpty(line) || line.IndexOf("Rush", System.StringComparison.Ordinal) < 0)
            {
                FailTrailer("The scout line did not name Rush.");
                return;
            }

            NoteTrailer("scout-line");
        }

        void TrailerResolve(bool good)
        {
            int before = session.CargoCount;
            string result = session.TrailerResolveTone(0, good);
            FinishShot();
            if (string.IsNullOrEmpty(result))
            {
                FailTrailer("The encounter did not resolve.");
                return;
            }

            if (trailerShot == "13_guard_save")
            {
                if (result != "Your guard handled it." || session.CargoCount != before)
                {
                    FailTrailer("The guard did not keep the packs.");
                    return;
                }
            }

            if (trailerShot == "15_rush_cargo_loss")
            {
                if (result.IndexOf("The rush cost you one", System.StringComparison.Ordinal) < 0 || session.CargoCount != before - 1)
                {
                    FailTrailer("The rush did not cost one pack unit.");
                    return;
                }

                NoteTrailer("result");
                NoteTrailer("cargo-lost");
                return;
            }

            NoteTrailer("result");
        }

        void TrailerOpenLogbook()
        {
            ToggleLogbook();
            FinishShot();
            if (!logbookOpen)
            {
                FailTrailer("The logbook did not open.");
                return;
            }

            NoteTrailer("logbook");
        }

        void TrailerBuyPiece()
        {
            BuyShownPiece(1);
            FinishShot();
            if (!session.MapPieceOwned(1))
            {
                FailTrailer("Piece 1 was not bought.");
                return;
            }

            NoteTrailer("piece-bought");
        }

        void TrailerAskSarn()
        {
            AskSarnRoad();
            FinishShot();
            if (!sarnRoadLit)
            {
                FailTrailer("The road to Sarn's Rest did not light.");
                return;
            }

            NoteTrailer("sarn-road-lit");
        }

        void TrailerEmbarkSarn()
        {
            EmbarkSarnRoad();
            FinishShot();
            if (!session.Traveling || departureOpen)
            {
                FailTrailer("The caravan did not leave on the Sarn road.");
                return;
            }

            NoteTrailer("embark");
        }

        void TrailerPaceQuiet()
        {
            session.ChoosePace(2);
            session.TrailerQuietRoad();
            FinishShot();
        }

        void NoteTrailer(string name)
        {
            for (int i = 0; i < trailerEvents.Count; i++)
            {
                if (trailerEvents[i].Name == name)
                    return;
            }

            trailerEvents.Add(new TrailerEvent { Name = name, Frame = trailerFrame });
        }

        void FailTrailer(string reason)
        {
            if (!string.IsNullOrEmpty(trailerFault))
                return;
            trailerFault = reason;
        }

        static int TrailerAt(double seconds)
        {
            return (int)System.Math.Round(seconds * 60d, System.MidpointRounding.AwayFromZero);
        }

        static int TrailerFrames(string shotId)
        {
            switch (shotId)
            {
                case "02_kharun_market":
                case "03_depart_draven":
                case "09_tavern_hire":
                case "28_road_night_4x":
                    return 360;
                case "04_road_day":
                case "08_road_dusk":
                case "11_encounter_good":
                case "17_village_flip":
                case "19_tavern_map_piece":
                    return 390;
                case "05_market_buy":
                case "06_market_sell":
                case "07_inn_fill":
                case "10_gates_scout_rush":
                case "12_skins_low":
                case "13_guard_save":
                case "16_logbook_road_note":
                case "20_westmark_gate":
                case "22_embarked":
                case "23_camp_card":
                case "24_departure_short":
                case "25_rescue_dry":
                case "26_sarns_rest":
                    return 240;
                case "14_sandstorm_in":
                case "15_rush_cargo_loss":
                    return 420;
                case "18_rank_up":
                case "21_sarn_road":
                case "27_ridgewatch_market":
                    return 300;
                default:
                    return 0;
            }
        }
    }
}
#endif
