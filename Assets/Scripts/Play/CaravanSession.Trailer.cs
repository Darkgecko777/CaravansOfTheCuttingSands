#if UNITY_EDITOR
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Caravans.Play
{
    public sealed partial class CaravanSession
    {
        internal bool TrailerQuietHeld { get; private set; }

        internal int TrailerRushIndex
        {
            get { return rushCargoIndex; }
        }

        internal void TrailerReleaseQuiet()
        {
            TrailerQuietHeld = false;
        }

        internal void TrailerSetClock(int day, float hour)
        {
            Day = day;
            Hour = hour;
            TravelHours = TravelScale.WindowHours;
            Touch();
        }

        internal void TrailerQuietRoad()
        {
            if (!EncounterPending)
            {
                encounterArmed = false;
                EncounterHour = -1f;
            }

            TravelHours = TravelScale.WindowHours;
            bool held = TrailerQuietHeld;
            TrailerQuietHeld = true;
            if (!held)
                Touch();
        }

        internal void TrailerAdvanceLeg(float fraction)
        {
            if (!Traveling || edgeLength <= 0.000001f)
                return;
            float target = edgeLength * fraction;
            if (target < 0f)
                target = 0f;
            int guard = 0;
            while (TravelDistance < target - 0.000001f && guard++ < 20000)
            {
                TrailerQuietRoad();
                float before = TravelDistance;
                Tick(1f / 60f);
                if (TravelDistance <= before + 0.0000001f)
                    break;
                if (!Traveling || CampPending || EncounterPending)
                    break;
            }
        }

        internal bool TrailerPlaceWeather(WeatherKind kind, Vector2 center, float radius, Vector2 velocity)
        {
            if (!ShotPlaceWeather(kind, center, radius))
                return false;
            for (int i = 0; i < masses.Count; i++)
            {
                if (masses[i].Kind != kind)
                    continue;
                masses[i].Velocity = velocity;
                Touch();
                return true;
            }

            return false;
        }

        internal void TrailerGiveCargo(GoodId good, float integrity, int basis)
        {
            GiveCargo(good, integrity);
            CargoUnit unit = stacks[stacks.Count - 1];
            unit.Basis = basis;
            stacks[stacks.Count - 1] = unit;
            Touch();
        }

        internal void TrailerSetFlipEdge(PlaceId village, HouseId rival, int lead)
        {
            int place = (int)village;
            for (int house = 0; house < WorldIds.HouseCount; house++)
                influence[place, house] = 0f;
            if (lead < 1)
                lead = 1;
            influence[place, (int)rival] = lead;
            RefreshPatron(village);
            Touch();
        }

        internal string TrailerResolveTone(int choice, bool good)
        {
            string want = good ? "good" : "bad";
            EncounterCard card = EncounterCards.Find(EncounterId);
            if (card == null || card.choices == null || choice < 0 || choice >= card.choices.Length)
                return null;
            EncounterOutcome[] outcomes = EncounterCards.Adjust(
                card,
                card.choices[choice],
                HasTrait(card.traitTag),
                HasSkill(card.skillTag));
            for (int i = 0; i < outcomes.Length; i++)
            {
                if (!string.Equals(outcomes[i].tone, want, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                ResolveOutcome(choice, outcomes[i].key);
                return EncounterResult;
            }

            return null;
        }

        internal string TrailerFingerprint()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            var line = new StringBuilder();
            line.Append(RunSeed);
            for (int i = 0; i < npcs.Count; i++)
            {
                line.Append('|');
                line.Append(npcs[i].Dock);
            }

            line.Append('|').Append(Day);
            line.Append('|').Append(Hour.ToString("0.00", culture));
            line.Append('|').Append(Dock);
            line.Append('|').Append(Traveling);
            line.Append('|').Append(TravelDistance.ToString("0.000", culture));
            line.Append('|').Append(Coin);
            for (int i = 0; i < stacks.Count; i++)
            {
                line.Append('|');
                line.Append(stacks[i].Good);
            }

            line.Append('|').Append(kharunPoints.ToString("0.00", culture));
            line.Append('|').Append(GetKharunRank());
            HouseId? holder = PatronOf(PlaceId.Draven);
            line.Append('|').Append(holder.HasValue ? holder.Value.ToString() : "none");
            line.Append('|').Append(OwnedMapPieces);
            line.Append('|').Append(EncounterId ?? string.Empty);
            line.Append('|').Append(EncounterResult ?? string.Empty);
            line.Append('|').Append(RoadNoteCount);
            line.Append('|').Append(ScoutHired);
            line.Append('|').Append(HagglerHired);
            line.Append('|').Append(GuardHired);
            line.Append('|').Append(WaterLitres.ToString("0.00", culture));
            line.Append('|').Append(RationKg.ToString("0.00", culture));
            Vector2 storm = Vector2.zero;
            for (int i = 0; i < masses.Count; i++)
            {
                if (masses[i].Kind != WeatherKind.Sandstorm)
                    continue;
                storm = masses[i].Center;
                break;
            }

            line.Append('|').Append(storm.x.ToString("0.000", culture));
            line.Append('|').Append(storm.y.ToString("0.000", culture));
            return line.ToString();
        }

        internal bool TrailerApply(string shotId)
        {
            TrailerReleaseQuiet();
            switch (shotId)
            {
                case "02_kharun_market":
                    TrailerSetClock(1, 7f);
                    return Dock == PlaceId.Kharun && CargoCount == 0;
                case "03_depart_draven":
                    TrailerSetClock(1, 8f);
                    FillKit();
                    return TrailerBuyPacks(2);
                case "04_road_day":
                    if (!TrailerOpenLeg(PlaceId.Draven, 2, 0.30f, 2, 10f))
                        return false;
                    ShotShowNpcTokens();
                    return TrailerRivalInSight();
                case "05_market_buy":
                    stacks.Clear();
                    SetCoin(600);
                    TrailerSetClock(1, EconomyRates.MorningHour);
                    ShotDockAt(PlaceId.Kharun);
                    return CargoCount == 0 && Coin == 600;
                case "06_market_sell":
                    return TrailerGiveBought(PlaceId.Draven, 3, 20);
                case "07_inn_fill":
                    ShotDockAt(PlaceId.Draven);
                    SetKit(KitWaterCapacity * 0.4f, KitRationCapacity * 0.4f);
                    SetCoin(300);
                    return StockOf(PlaceId.Draven, GoodId.Water) > 0
                        && StockOf(PlaceId.Draven, GoodId.Rations) > 0
                        && Coin >= BuyCost(PlaceId.Draven, GoodId.Water) + BuyCost(PlaceId.Draven, GoodId.Rations);
                case "08_road_dusk":
                    return TrailerOpenLeg(PlaceId.Brineford, 2, 0.25f, 3, 16.5f);
                case "09_tavern_hire":
                    ShotDockAt(PlaceId.Kharun);
                    SetCoin(400);
                    return !ScoutHired && !GuardHired && !HagglerHired && Coin >= 100;
                case "10_gates_scout_rush":
                    ShotDockAt(PlaceId.Kharun);
                    FillKit();
                    SetCoin(Mathf.Max(Coin, 400));
                    if (!TryHireAgent("Scout"))
                        return false;
                    return TrailerBuyPacks(2) && ScoutHired;
                case "11_encounter_good":
                    FillKit();
                    if (!ShotDepartPaused(PlaceId.Draven))
                        return false;
                    ShotEncounter("CA-11");
                    return EncounterPending && EncounterId == "CA-11" && RationKg >= 3f;
                case "12_skins_low":
                    if (!ShotDepartPaused(PlaceId.Draven))
                        return false;
                    ShotDropDrinkPacks();
                    SetKit(3.5f, 1.4f);
                    ShotOpenCritical();
                    return CriticalPending && HoldTravel;
                case "13_guard_save":
                    ShotDockAt(PlaceId.Kharun);
                    SetCoin(Mathf.Max(Coin, 200));
                    if (!TryHireAgent("Guard"))
                        return false;
                    if (!TrailerGiveDistinct(3))
                        return false;
                    if (!ShotDepartPaused(PlaceId.Draven))
                        return false;
                    ChooseRoadPace(RoadPace.Rush);
                    ShotEncounter("CA-03");
                    return GuardHired && ChosenPace == RoadPace.Rush && EncounterId == "CA-03" && CargoCount == 3;
                case "14_sandstorm_in":
                    if (!TrailerOpenLeg(PlaceId.Draven, 2, 0.20f, 2, 11f))
                        return false;
                    return TrailerPlaceIncomingSandstorm();
                case "15_rush_cargo_loss":
                    return TrailerArmRushLoss();
                case "16_logbook_road_note":
                    if (!TrailerArmRushLoss())
                        return false;
                    string result = TrailerResolveTone(0, false);
                    if (string.IsNullOrEmpty(result) || result.IndexOf("The rush cost you one", System.StringComparison.Ordinal) < 0)
                        return false;
                    if (RoadNoteCount < 1)
                        return false;
                    ShotDockAt(PlaceId.Draven);
                    return !Traveling && RoadNoteCount >= 1;
                case "17_village_flip":
                    return TrailerArmFlip();
                case "18_rank_up":
                    return TrailerArmRank();
                case "19_tavern_map_piece":
                    ShotDockAt(PlaceId.Kharun);
                    SetKharunPoints(100f);
                    SetCoin(2000);
                    SetMapPieceLeads(1);
                    return GetKharunRank() == 1 && MapPieceLeads == 1 && !MapPieceOwned(1);
                case "20_westmark_gate":
                    return TrailerArmWestmark();
                case "21_sarn_road":
                    return TrailerArmWestmark();
                case "22_embarked":
                    ShotOwnPieces(6, true);
                    SetKharunPoints(1000f);
                    ShotShowEnd();
                    return EndScreenUp;
                case "23_camp_card":
                    if (!ShotDepartPaused(PlaceId.Draven))
                        return false;
                    ShotCamp();
                    return CampPending;
                case "24_departure_short":
                    ShotDockAt(PlaceId.Kharun);
                    ShotDropDrinkPacks();
                    SetKit(1f, 0.5f);
                    return Dock == PlaceId.Kharun && CanTakeRoad();
                case "25_rescue_dry":
                    if (!ShotDepartPaused(PlaceId.Draven))
                        return false;
                    ShotOpenDeath();
                    return DeathPending;
                case "26_sarns_rest":
                    ShotOwnPieces(6, true);
                    SetKharunPoints(1000f);
                    ShotDockAt(PlaceId.Westmark);
                    if (!TryStartGateRoad())
                        return false;
                    CompleteMapLeg();
                    return RestPending;
                case "27_ridgewatch_market":
                    ShotDockAt(PlaceId.Ridgewatch);
                    TrailerSetClock(1, EconomyRates.MorningHour);
                    return Dock == PlaceId.Ridgewatch && CargoCount == 0;
                case "28_road_night_4x":
                    return TrailerOpenLeg(PlaceId.Draven, 4, 0.40f, 2, 22f);
                default:
                    return false;
            }
        }

        internal bool TrailerRivalInSight()
        {
            if (NpcCount < 3 || !Traveling)
                return false;
            float vision = VisionRadius();
            Vector2 point = CaravanPoint;
            for (int i = 0; i < 3; i++)
            {
                NpcMark mark = NpcMarkAt(i);
                if (!mark.Traveling)
                    continue;
                if (RoadPath.MapDistance(point, mark.Point) <= vision + 0.0001f)
                    return true;
            }

            return false;
        }

        bool TrailerOpenLeg(PlaceId dest, int pace, float fraction, int day, float hour)
        {
            if (Traveling)
                return false;
            if (Dock != PlaceId.Kharun)
                ShotDockAt(PlaceId.Kharun);
            FillKit();
            if (!TrailerParkClear())
                return false;
            ChooseRoadPace(RoadPace.Steady);
            if (!DepartTo(dest))
                return false;
            ChoosePace(pace);
            if (LegWeather != WeatherKind.Clear)
                return false;
            if (fraction > 0f)
                TrailerAdvanceLeg(fraction);
            if (!Traveling || CampPending || EncounterPending)
                return false;
            TrailerSetClock(day, hour);
            if (!TrailerParkClear())
                return false;
            TrailerQuietRoad();
            return TrailerSkyClear();
        }

        bool TrailerParkClear()
        {
            bool storm = TrailerPlaceWeather(WeatherKind.Sandstorm, new Vector2(0.98f, 0.98f), 0.02f, Vector2.zero);
            bool heat = TrailerPlaceWeather(WeatherKind.Heat, new Vector2(0.02f, 0.98f), 0.02f, Vector2.zero);
            bool wind = TrailerPlaceWeather(WeatherKind.Wind, new Vector2(0.98f, 0.02f), 0.02f, Vector2.zero);
            return storm && heat && wind;
        }

        bool TrailerSkyClear()
        {
            if (WeatherAt(CaravanPoint) != WeatherKind.Clear)
                return false;
            float vision = VisionRadius();
            for (int i = 0; i < masses.Count; i++)
            {
                WeatherMass mass = masses[i];
                if (RoadPath.MapDistance(CaravanPoint, mass.Center) < vision + mass.Radius)
                    return false;
            }

            return true;
        }

        bool TrailerPlaceIncomingSandstorm()
        {
            const float radius = 0.10f;
            const int horizon = 420;
            float vision = TravelScale.VisionRadius(Hour, WeatherKind.Clear);
            float distance = TravelDistance;
            int bestScore = int.MaxValue;
            Vector2 bestCenter = Vector2.zero;
            Vector2 bestVelocity = Vector2.zero;
            bool found = false;
            float[] starts = { 0.12f, 0.13f, 0.14f, 0.15f };
            float[] ends = { 0.04f, 0.05f, 0.07f };
            for (int s = 0; s < starts.Length; s++)
            {
                if (starts[s] <= radius + 0.004f || starts[s] >= vision - 0.008f)
                    continue;
                for (int e = 0; e < ends.Length; e++)
                {
                    for (int step = 0; step < 24; step++)
                    {
                        float angle = step * Mathf.PI * 2f / 24f;
                        Vector2 origin = RoadGraph.PointAlong(travelFrom, travelTo, distance);
                        Vector2 later = RoadPointAfter(distance, horizon);
                        Vector2 center = origin + MapOffset(angle, starts[s]);
                        Vector2 final = later + MapOffset(angle, ends[e]);
                        float hours = HoursForTicks(horizon);
                        if (hours <= 0.0001f)
                            continue;
                        Vector2 velocity = (final - center) / hours;
                        int entry;
                        bool held;
                        if (!ScoreStorm(distance, center, velocity, radius, vision, out entry, out held))
                            continue;
                        int score = Mathf.Abs(entry - 144);
                        if (!held)
                            score += 1000;
                        if (entry < 120 || entry > 180)
                            score += 500;
                        if (score >= bestScore)
                            continue;
                        bestScore = score;
                        bestCenter = center;
                        bestVelocity = velocity;
                        found = true;
                    }
                }
            }

            if (!found || bestScore >= 500)
                return false;
            if (WeatherAt(CaravanPoint) != WeatherKind.Clear)
                return false;
            return TrailerPlaceWeather(WeatherKind.Sandstorm, bestCenter, radius, bestVelocity)
                && WeatherAt(CaravanPoint) == WeatherKind.Clear;
        }

        bool ScoreStorm(float distance, Vector2 center, Vector2 velocity, float radius, float vision, out int entry, out bool held)
        {
            entry = -1;
            held = false;
            float cursor = distance;
            Vector2 storm = center;
            float opened = RoadPath.MapDistance(RoadGraph.PointAlong(travelFrom, travelTo, cursor), storm);
            if (opened <= radius + 0.002f || opened >= vision - 0.004f)
                return false;
            bool inside = false;
            for (int tick = 0; tick < 420; tick++)
            {
                StepStorm(ref cursor, ref storm, velocity);
                float gap = RoadPath.MapDistance(RoadGraph.PointAlong(travelFrom, travelTo, cursor), storm);
                if (gap <= radius)
                {
                    if (entry < 0)
                        entry = tick;
                    inside = true;
                }
                else if (entry >= 0)
                {
                    inside = false;
                }
            }

            held = entry >= 0 && inside;
            return entry >= 0 && entry < 300;
        }

        void StepStorm(ref float distance, ref Vector2 center, Vector2 velocity)
        {
            float speed = Mathf.Max(0.01f, TravelScale.Speed(ChosenPace));
            float scale = Mathf.Max(0.01f, LegScale);
            float duration = TravelScale.RealSeconds * (edgeLength / RoadPath.Length) * scale / speed;
            float step = edgeLength / duration * Pace * (1f / 60f);
            float next = distance + step;
            if (next > edgeLength)
                next = edgeLength;
            float moved = next - distance;
            float hours = 0f;
            if (moved > 0f)
                hours = moved / RoadPath.Length * TravelScale.BaseLegHours * scale / speed;
            distance = next;
            center += velocity * hours;
            center.x = Mathf.Repeat(center.x, 1f);
            center.y = Mathf.Repeat(center.y, 1f);
        }

        Vector2 RoadPointAfter(float distance, int ticks)
        {
            float cursor = distance;
            Vector2 unused = Vector2.zero;
            for (int i = 0; i < ticks; i++)
                StepStorm(ref cursor, ref unused, Vector2.zero);
            return RoadGraph.PointAlong(travelFrom, travelTo, cursor);
        }

        float HoursForTicks(int ticks)
        {
            float speed = Mathf.Max(0.01f, TravelScale.Speed(ChosenPace));
            float scale = Mathf.Max(0.01f, LegScale);
            float duration = TravelScale.RealSeconds * (edgeLength / RoadPath.Length) * scale / speed;
            float step = edgeLength / duration * Pace * (1f / 60f);
            if (step <= 0f)
                return 0f;
            float hours = step / RoadPath.Length * TravelScale.BaseLegHours * scale / speed;
            return hours * ticks;
        }

        static Vector2 MapOffset(float angle, float mapDist)
        {
            float aspect = RoadPath.Aspect;
            return new Vector2(Mathf.Cos(angle) * mapDist / aspect, Mathf.Sin(angle) * mapDist);
        }

        bool TrailerArmRushLoss()
        {
            ShotDockAt(PlaceId.Kharun);
            if (!TrailerGiveDistinct(4))
                return false;
            if (!ShotDepartPaused(PlaceId.Draven))
                return false;
            ChooseRoadPace(RoadPace.Rush);
            int best = 0;
            int value = -1;
            for (int i = 0; i < CargoCount; i++)
            {
                int sell = SellValueAt(i);
                if (sell <= value)
                    continue;
                value = sell;
                best = i;
            }

            ForceRushCargoIndex(best);
            ShotEncounter("CA-04");
            return ChosenPace == RoadPace.Rush && !GuardHired && EncounterId == "CA-04" && CargoCount == 4 && rushCargoIndex == best;
        }

        bool TrailerArmFlip()
        {
            ShotDockAt(PlaceId.Draven);
            SetKharunPoints(0f);
            HouseId rival = HouseId.Zamath;
            HouseId? current = PatronOf(PlaceId.Draven);
            if (current.HasValue && current.Value != HouseId.Kharun)
                rival = current.Value;
            if (!TrailerGiveBought(PlaceId.Draven, 1, 8))
                return false;
            TrailerSetFlipEdge(PlaceId.Draven, rival, 1);
            return PatronOf(PlaceId.Draven) == rival && CargoCount == 1 && GetKharunRank() == 0 && SellValueAt(0) > 1;
        }

        bool TrailerArmRank()
        {
            ShotDockAt(PlaceId.Kharun);
            SetKharunPoints(499f);
            stacks.Clear();
            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                if (!Sells(PlaceId.Kharun, good))
                    continue;
                TrailerGiveCargo(good, 1f, 0);
                int worth = SellValueAt(CargoCount - 1);
                if (worth < 70)
                {
                    stacks.RemoveAt(CargoCount - 1);
                    continue;
                }

                CargoUnit unit = stacks[0];
                unit.Basis = worth - 60;
                stacks[0] = unit;
                Touch();
                return GetKharunRank() == 4 && CargoCount == 1;
            }

            return false;
        }

        bool TrailerArmWestmark()
        {
            ShotOwnPieces(6, true);
            SetKharunPoints(1000f);
            FillKit();
            ShotDockAt(PlaceId.Westmark);
            return Dock == PlaceId.Westmark && OwnedMapPieces >= 6 && MapJourneyAtGate && CanTakeRoad();
        }

        bool TrailerGiveBought(PlaceId place, int count, int profit)
        {
            ShotDockAt(place);
            stacks.Clear();
            int given = 0;
            for (int i = 0; i < WorldIds.GoodCount && given < count; i++)
            {
                GoodId good = (GoodId)i;
                if (!Sells(place, good))
                    continue;
                TrailerGiveCargo(good, 1f, 0);
                int index = CargoCount - 1;
                int worth = SellValueAt(index);
                if (worth <= profit)
                {
                    stacks.RemoveAt(index);
                    continue;
                }

                CargoUnit unit = stacks[index];
                unit.Basis = worth - profit;
                stacks[index] = unit;
                given++;
            }

            Touch();
            return given == count;
        }

        bool TrailerGiveDistinct(int count)
        {
            stacks.Clear();
            int given = 0;
            for (int i = 0; i < WorldIds.GoodCount && given < count; i++)
            {
                GoodId good = (GoodId)i;
                if (good == GoodId.Water || good == GoodId.Rations || good == GoodId.SeaWater)
                    continue;
                TrailerGiveCargo(good, 1f, 0);
                given++;
            }

            Touch();
            return given == count;
        }

        bool TrailerBuyPacks(int count)
        {
            int bought = 0;
            int guard = 0;
            while (bought < count && guard++ < WorldIds.GoodCount)
            {
                GoodId pick = GoodId.ShimmersteelJars;
                int cost = int.MaxValue;
                bool found = false;
                for (int i = 0; i < WorldIds.GoodCount; i++)
                {
                    GoodId good = (GoodId)i;
                    if (good == GoodId.Rations || good == GoodId.Water || good == GoodId.SeaWater)
                        continue;
                    if (!CanBuy(good))
                        continue;
                    int price = BuyCost(Dock, good);
                    if (price >= cost)
                        continue;
                    cost = price;
                    pick = good;
                    found = true;
                }

                if (!found || !TryBuy(pick))
                    return false;
                bought++;
            }

            return bought == count;
        }

        void FillKit()
        {
            SetKit(KitWaterCapacity, KitRationCapacity);
        }

        static bool Sells(PlaceId place, GoodId good)
        {
            if (good == GoodId.Water || good == GoodId.Rations || good == GoodId.SeaWater)
                return false;
            return EconomyCatalog.Buys(place, good);
        }
    }
}
#endif
