using System.Collections.Generic;
using UnityEngine;

namespace Caravans.JarsRun
{
    public enum WeatherKind
    {
        Clear,
        Heat,
        Wind,
        Sandstorm
    }

    public enum TutorialStep
    {
        OpenMarket,
        BuyJars,
        LeaveForDraven,
        UsePace,
        RidingOut,
        RoadCard,
        ReadResult,
        ArriveDraven,
        SellJars,
        BuyFungus,
        LeaveForKharun,
        RidingHome,
        SellFungus,
        HouseChoice,
        Free
    }

    public sealed class WeatherMass
    {
        public WeatherKind Kind;
        public Vector2 Center;
        public float Radius;
        public Vector2 Velocity;
    }

    public struct NpcMark
    {
        public Vector2 Point;
        public HouseId House;
    }

    public sealed class CaravanSession
    {
        sealed class NpcCaravan
        {
            public HouseId House;
            public PlaceId Dock;
            public bool Traveling;
            public PlaceId From;
            public PlaceId To;
            public float Distance;
            public float EdgeLength;
            public readonly List<PlaceId> Path = new List<PlaceId>();
            public readonly int[] Cargo = new int[WorldIds.GoodCount];
        }

        struct CargoUnit
        {
            public GoodId Good;
            public float Integrity;
            public int DecayedOnDay;
        }

        readonly Shelf[,] shelves = new Shelf[WorldIds.PlaceCount, WorldIds.GoodCount];
        readonly float[,] eatAcc = new float[WorldIds.PlaceCount, WorldIds.GoodCount];
        readonly float[,] makeAcc = new float[WorldIds.PlaceCount, WorldIds.GoodCount];
        readonly float[,] influence = new float[WorldIds.PlaceCount, WorldIds.HouseCount];
        readonly int[] standingOf = new int[WorldIds.HouseCount];
        readonly HouseId?[] patron = new HouseId?[WorldIds.PlaceCount];
        readonly List<CargoUnit> stacks = new List<CargoUnit>();
        readonly List<NpcCaravan> npcs = new List<NpcCaravan>();
        readonly List<WeatherMass> masses = new List<WeatherMass>();

        PlaceId travelFrom;
        PlaceId travelTo;
        float edgeLength;

        public int Revision { get; private set; }
        public int Coin { get; private set; }
        public int Day { get; private set; }
        public float Hour { get; private set; }
        public PlaceId Dock { get; private set; }
        public bool Traveling { get; private set; }
        public bool TowardDraven { get; private set; }
        public float TravelDistance { get; private set; }
        public int Pace { get; private set; }
        public bool HoldTravel { get; private set; }
        public TutorialStep Step { get; private set; }
        public WeatherKind LegWeather { get; private set; }
        public float LegScale { get; private set; }
        public string HouseNote { get; private set; }
        public bool CardWasSteel { get; private set; }
        public int CardsResolved { get; private set; }
        public bool Skipped { get; private set; }
        public HouseId? House { get; private set; }
        public bool CardPending { get; private set; }
        public bool ResultPending { get; private set; }

        bool cardResolvedThisLeg;

        public IReadOnlyList<WeatherMass> Masses
        {
            get { return masses; }
        }

        public int CoinReserve
        {
            get { return NeedsCardReserve() ? ThrowawayEconomy.CardCoin : 0; }
        }

        public int NpcCount
        {
            get { return npcs.Count; }
        }

        public Vector2 CaravanPoint
        {
            get
            {
                if (!Traveling)
                    return RoadGraph.DockPoint(Dock);
                return RoadGraph.PointAlong(travelFrom, travelTo, TravelDistance);
            }
        }

        public CaravanSession()
        {
            ResetCaravan();
            Step = TutorialStep.OpenMarket;
        }

        public int CargoOf(GoodId good)
        {
            int count = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].Good == good)
                    count += 1;
            }

            return count;
        }

        public int CargoSpace()
        {
            int used = 0;
            for (int i = 0; i < stacks.Count; i++)
                used += EconomyCatalog.Space(stacks[i].Good);
            return used;
        }

        public int CargoWeight()
        {
            int used = 0;
            for (int i = 0; i < stacks.Count; i++)
                used += EconomyCatalog.Weight(stacks[i].Good);
            return used;
        }

        public float IntegrityOf(GoodId good)
        {
            int index = WorstStack(good);
            if (index < 0)
                return 1f;
            return stacks[index].Integrity;
        }

        public int StockOf(PlaceId place, GoodId good)
        {
            return shelves[(int)place, (int)good].Stock;
        }

        public int CapOf(PlaceId place, GoodId good)
        {
            return shelves[(int)place, (int)good].Cap;
        }

        public int PriceOf(PlaceId place, GoodId good)
        {
            Shelf shelf = shelves[(int)place, (int)good];
            return EconomyCatalog.ShelfPrice(shelf.BasePrice, shelf.Stock, EconomyCatalog.DayUnit(place, good));
        }

        public int BuyCost(PlaceId place, GoodId good)
        {
            int shelf = PriceOf(place, good);
            return shelf + CutCoin(shelf, TariffPercent(place));
        }

        public int SellValue(PlaceId place, GoodId good)
        {
            int gross = Gross(place, good, IntegrityOf(good));
            return Mathf.Max(0, gross - CutCoin(gross, TariffPercent(place)));
        }

        public int TariffPercent(PlaceId place)
        {
            if (House == null)
                return 0;
            HouseId? holder = PatronOf(place);
            if (holder == null || holder.Value == House.Value)
                return 0;
            int percent = EconomyRates.TariffBase - standingOf[(int)holder.Value];
            if (percent < EconomyRates.TariffFloor)
                percent = EconomyRates.TariffFloor;
            return percent;
        }

        public HouseId? PatronOf(PlaceId place)
        {
            return patron[(int)place];
        }

        public float InfluenceOf(PlaceId place, HouseId house)
        {
            return influence[(int)place, (int)house];
        }

        public string PatronLine()
        {
            HouseId? holder = PatronOf(Dock);
            int percent = TariffPercent(Dock);
            if (holder == null)
                return "No patron · " + percent + "%";
            return HouseName(holder.Value) + " · " + percent + "%";
        }

        public string BoundLabel()
        {
            if (!Traveling)
                return PlaceName(Dock);
            return "Road to " + PlaceName(travelTo);
        }

        public IReadOnlyList<PlaceId> RoadsOut()
        {
            return RoadGraph.Neighbors(Dock);
        }

        public NpcMark NpcMarkAt(int index)
        {
            NpcCaravan npc = npcs[index];
            Vector2 point = npc.Traveling
                ? RoadGraph.PointAlong(npc.From, npc.To, npc.Distance)
                : RoadGraph.DockPoint(npc.Dock);
            return new NpcMark { Point = point, House = npc.House };
        }

        public int NpcSpace(int index)
        {
            return UsedSpace(npcs[index]);
        }

        public int NpcWeight(int index)
        {
            return UsedWeight(npcs[index]);
        }

        public bool NpcTraveling(int index)
        {
            return npcs[index].Traveling;
        }

        public int NpcHopsRemaining(int index)
        {
            NpcCaravan npc = npcs[index];
            if (!npc.Traveling)
                return 0;
            return 1 + npc.Path.Count;
        }

        public void NoteMarketOpened()
        {
            if (Step == TutorialStep.OpenMarket)
                Advance(TutorialStep.BuyJars);
        }

        public void NotePlazaOpened()
        {
            if (Step == TutorialStep.BuyJars && CargoOf(GoodId.ShimmersteelJars) >= 1)
                Advance(TutorialStep.LeaveForDraven);
            if (Step == TutorialStep.BuyFungus && CargoOf(GoodId.FingerFungus) >= 1)
                Advance(TutorialStep.LeaveForKharun);
        }

        public void NoteSellOpened()
        {
            if (Step == TutorialStep.ArriveDraven)
                Advance(TutorialStep.SellJars);
        }

        public bool TryBuy(GoodId good)
        {
            if (Traveling || !BuyAllowed(good))
                return false;

            int stock = StockOf(Dock, good);
            int price = BuyCost(Dock, good);
            if (stock <= 0 || Coin - price < CoinReserve)
                return false;
            if (CargoSpace() + EconomyCatalog.Space(good) > EconomyRates.WagonSpace)
                return false;
            if (CargoWeight() + EconomyCatalog.Weight(good) > EconomyRates.WagonWeight)
                return false;

            PutStock(Dock, good, stock - 1);
            stacks.Add(new CargoUnit
            {
                Good = good,
                Integrity = 1f,
                DecayedOnDay = DecayStamp()
            });
            Coin -= price;
            Touch();
            return true;
        }

        public bool TrySell(GoodId good)
        {
            if (Traveling || !SellAllowed(good))
                return false;

            int index = WorstStack(good);
            if (index < 0)
                return false;

            int gross = Gross(Dock, good, stacks[index].Integrity);
            int net = Mathf.Max(0, gross - CutCoin(gross, TariffPercent(Dock)));
            stacks.RemoveAt(index);
            PutStock(Dock, good, StockOf(Dock, good) + 1);
            Coin += net;
            if (House != null && !EconomyCatalog.IsCity(Dock))
            {
                influence[(int)Dock, (int)House.Value] += gross;
                RefreshPatron(Dock);
            }

            if (Step == TutorialStep.SellJars && good == GoodId.ShimmersteelJars && CargoOf(GoodId.ShimmersteelJars) == 0)
                Step = TutorialStep.BuyFungus;
            if (Step == TutorialStep.SellFungus && good == GoodId.FingerFungus && CargoOf(GoodId.FingerFungus) == 0)
                Step = TutorialStep.HouseChoice;

            Touch();
            return true;
        }

        public void ApplyIntegrityHit(GoodId good, float loss)
        {
            if (loss <= 0f)
                return;
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                if (stacks[i].Good != good)
                    continue;
                CargoUnit unit = stacks[i];
                unit.Integrity -= loss;
                if (unit.Integrity <= 0.001f)
                    stacks.RemoveAt(i);
                else
                    stacks[i] = unit;
            }

            Touch();
        }

        public bool Depart()
        {
            if (Traveling)
                return false;
            if (Step != TutorialStep.LeaveForDraven
                && Step != TutorialStep.LeaveForKharun
                && Step != TutorialStep.Free)
                return false;

            if (Dock == PlaceId.Kharun && (Step == TutorialStep.LeaveForDraven || Step == TutorialStep.Free))
                return BeginTravel(PlaceId.Draven);
            if (Dock == PlaceId.Draven && (Step == TutorialStep.LeaveForKharun || Step == TutorialStep.Free))
                return BeginTravel(PlaceId.Kharun);
            return false;
        }

        public bool DepartTo(PlaceId destination)
        {
            if (Traveling || Step != TutorialStep.Free || destination == Dock)
                return false;
            return BeginTravel(destination);
        }

        public void ChoosePace(int pace)
        {
            if (pace != 1 && pace != 2 && pace != 4)
                return;
            Pace = pace;
            if (Step == TutorialStep.UsePace)
            {
                HoldTravel = false;
                Advance(TutorialStep.RidingOut);
            }
            else
            {
                Touch();
            }
        }

        public void SetPaused(bool paused)
        {
            if (Step == TutorialStep.UsePace || HoldTravel)
                return;
            if (paused)
                Pace = 0;
            else if (Pace == 0)
                Pace = 1;
            Touch();
        }

        public void Tick(float realSeconds)
        {
            if (!Traveling || HoldTravel || Pace <= 0 || realSeconds <= 0f)
                return;
            if (edgeLength <= 0.000001f)
            {
                Arrive(travelTo);
                return;
            }

            float duration = ThrowawayEconomy.RealSeconds * (edgeLength / RoadPath.Length) * Mathf.Max(0.01f, LegScale);
            float step = edgeLength / duration * Pace * realSeconds;
            float next = TravelDistance + step;
            float half = edgeLength * 0.5f;

            if (TowardDraven && !cardResolvedThisLeg && TravelDistance < half && next >= half)
            {
                CommitMove(half - TravelDistance);
                TravelDistance = half;
                HoldTravel = true;
                CardPending = true;
                if (Step == TutorialStep.RidingOut)
                    Step = TutorialStep.RoadCard;
                ResampleWeather();
                Touch();
                return;
            }

            if (next >= edgeLength)
            {
                CommitMove(edgeLength - TravelDistance);
                Arrive(travelTo);
                return;
            }

            float moved = next - TravelDistance;
            TravelDistance = next;
            CommitMove(moved);
        }

        public void ResolveCard(bool steel)
        {
            if (!CardPending)
                return;

            ResampleWeather();
            int cost = Mathf.Min(ThrowawayEconomy.CardCoin, Coin);
            Coin -= cost;
            CardWasSteel = steel;
            CardsResolved += 1;
            cardResolvedThisLeg = true;
            CardPending = false;
            ResultPending = true;
            HoldTravel = true;
            if (Step == TutorialStep.RoadCard)
                Step = TutorialStep.ReadResult;
            Touch();
        }

        public void ContinueFromCard()
        {
            if (!ResultPending)
                return;
            ResultPending = false;
            HoldTravel = false;
            if (Pace <= 0)
                Pace = 1;
            if (Step == TutorialStep.ReadResult)
                Step = TutorialStep.RidingOut;
            Touch();
        }

        public void WaitOneHour()
        {
            if (Traveling || Step != TutorialStep.Free)
                return;
            AdvanceClock(1f);
            Touch();
        }

        public void AdvanceHours(float hours)
        {
            if (hours <= 0f)
                return;
            AdvanceClock(hours);
            Touch();
        }

        public void TickMorning()
        {
            RunMorning();
            Touch();
        }

        public void TickNight()
        {
            RunNight();
            Touch();
        }

        public void SetShelf(PlaceId place, GoodId good, int stock)
        {
            PutStock(place, good, Mathf.Max(0, stock));
            Touch();
        }

        public void SetStanding(HouseId house, int steps)
        {
            if (steps < 0)
                steps = 0;
            if (steps > EconomyRates.StandingMax)
                steps = EconomyRates.StandingMax;
            standingOf[(int)house] = steps;
            Touch();
        }

        public void AddPresence(PlaceId place, HouseId house, int coin)
        {
            if (coin <= 0 || EconomyCatalog.IsCity(place))
                return;
            influence[(int)place, (int)house] += coin;
            RefreshPatron(place);
            Touch();
        }

        public void SkipTutorial()
        {
            ResetCaravan();
            Skipped = true;
            Step = TutorialStep.HouseChoice;
            HouseNote = string.Empty;
            Touch();
        }

        public bool ChooseHouse(HouseId house)
        {
            if (Step != TutorialStep.HouseChoice)
                return false;
            if (house != HouseId.Kharun)
            {
                HouseNote = "That package is not written. This slice stays at Khar\u00fbn.";
                Touch();
                return false;
            }

            House = HouseId.Kharun;
            HouseNote = string.Empty;
            Step = TutorialStep.Free;
            ReplanDocked();
            Touch();
            return true;
        }

        public WeatherKind WeatherAt(Vector2 topLeft)
        {
            WeatherKind best = WeatherKind.Clear;
            float bestDist = float.MaxValue;
            for (int i = 0; i < masses.Count; i++)
            {
                WeatherMass mass = masses[i];
                float dist = RoadPath.MapDistance(topLeft, mass.Center);
                if (dist > mass.Radius)
                    continue;
                int severity = ThrowawayEconomy.Severity(mass.Kind);
                int bestSeverity = ThrowawayEconomy.Severity(best);
                if (severity > bestSeverity || (severity == bestSeverity && dist < bestDist))
                {
                    best = mass.Kind;
                    bestDist = dist;
                }
            }

            return best;
        }

        public WeatherKind WeatherAlongRoad()
        {
            if (Traveling && !LessonRoad(travelFrom, travelTo))
                return WeatherOn(travelFrom, travelTo);
            return WeatherOn(PlaceId.Kharun, PlaceId.Draven);
        }

        public string OutlookLine()
        {
            return Describe(WeatherAlongRoad(), "The road ahead is ");
        }

        public string LegWeatherLine()
        {
            return Describe(LegWeather, "On this leg: ");
        }

        public float VisionRadius()
        {
            return ThrowawayEconomy.VisionRadius(Hour, WeatherAt(CaravanPoint));
        }

        public static string PlaceName(PlaceId place)
        {
            return EconomyCatalog.PlaceName(place);
        }

        public static string GoodName(GoodId good)
        {
            return EconomyCatalog.GoodName(good);
        }

        public static string HouseName(HouseId house)
        {
            return EconomyCatalog.HouseName(house);
        }

        public static string WeatherName(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Heat:
                    return "Heat";
                case WeatherKind.Wind:
                    return "Wind";
                case WeatherKind.Sandstorm:
                    return "Sandstorm";
                default:
                    return "Clear";
            }
        }

        bool BeginTravel(PlaceId destination)
        {
            if (!RoadGraph.Connects(Dock, destination))
                return false;

            travelFrom = Dock;
            travelTo = destination;
            edgeLength = RoadGraph.Length(Dock, destination);
            TravelDistance = 0f;
            TowardDraven = Dock == PlaceId.Kharun && destination == PlaceId.Draven;
            Traveling = true;
            cardResolvedThisLeg = false;
            LegWeather = WeatherOn(travelFrom, travelTo);
            LegScale = ThrowawayEconomy.TimeScale(LegWeather);
            Pace = 1;

            if (Step == TutorialStep.LeaveForDraven)
            {
                HoldTravel = true;
                Pace = 0;
                Advance(TutorialStep.UsePace);
            }
            else if (Step == TutorialStep.LeaveForKharun)
            {
                HoldTravel = false;
                Advance(TutorialStep.RidingHome);
            }
            else
            {
                HoldTravel = false;
                Touch();
            }

            if (edgeLength <= 0.000001f)
                Arrive(destination);
            return true;
        }

        void Arrive(PlaceId place)
        {
            Dock = place;
            Traveling = false;
            HoldTravel = false;
            Pace = 1;
            TravelDistance = 0f;
            if (place == PlaceId.Draven && Step == TutorialStep.RidingOut)
                Step = TutorialStep.ArriveDraven;
            if (place == PlaceId.Kharun && Step == TutorialStep.RidingHome)
                Step = TutorialStep.SellFungus;
            Touch();
        }

        void CommitMove(float distanceMoved)
        {
            if (distanceMoved <= 0f)
                return;
            float hours = distanceMoved / RoadPath.Length * ThrowawayEconomy.BaseLegHours * LegScale;
            AdvanceClock(hours);
        }

        void AdvanceClock(float hours)
        {
            float left = hours;
            int guard = 0;
            while (left > 0.00005f && guard++ < 64)
            {
                float toMorning = HoursUntil(EconomyRates.MorningHour);
                float toNight = HoursUntil(EconomyRates.NightHour);
                float toMidnight = HoursUntil(0);
                float step = left;
                int hit = 0;
                Consider(ref step, ref hit, toMidnight, 3);
                Consider(ref step, ref hit, toMorning, 1);
                Consider(ref step, ref hit, toNight, 2);
                MoveNpcs(step);
                MoveWeather(step);
                left -= step;
                if (hit == 3)
                {
                    Hour = 0f;
                    Day += 1;
                }
                else if (hit == 1)
                {
                    Hour = EconomyRates.MorningHour;
                    RunMorning();
                }
                else if (hit == 2)
                {
                    Hour = EconomyRates.NightHour;
                    RunNight();
                }
                else
                {
                    Hour += step;
                }
            }
        }

        static void Consider(ref float step, ref int hit, float threshold, int id)
        {
            if (threshold < step - 0.00001f)
            {
                step = threshold;
                hit = id;
            }
            else if (hit == 0 && Mathf.Abs(threshold - step) <= 0.00001f)
            {
                step = threshold;
                hit = id;
            }
        }

        float HoursUntil(int mark)
        {
            if (mark == 0)
            {
                if (Hour <= 0.0001f || Hour >= 24f - 0.0001f)
                    return 24f;
                return 24f - Hour;
            }

            if (Mathf.Abs(Hour - mark) <= 0.0001f)
                return 24f;
            if (Hour < mark)
                return mark - Hour;
            return 24f - Hour + mark;
        }

        void RunMorning()
        {
            for (int place = 0; place < WorldIds.PlaceCount; place++)
            {
                for (int good = 0; good < WorldIds.GoodCount; good++)
                {
                    float amount = EconomyCatalog.Eat((PlaceId)place, (GoodId)good);
                    if (amount <= 0f)
                        continue;
                    if (shelves[place, good].Stock <= 0)
                    {
                        eatAcc[place, good] = 0f;
                        continue;
                    }

                    eatAcc[place, good] += amount;
                    int want = Mathf.FloorToInt(eatAcc[place, good]);
                    int take = Mathf.Min(shelves[place, good].Stock, want);
                    PutStock((PlaceId)place, (GoodId)good, shelves[place, good].Stock - take);
                    eatAcc[place, good] -= take;
                    if (shelves[place, good].Stock <= 0)
                        eatAcc[place, good] = 0f;
                }
            }

            DecayCargo();
            for (int place = 0; place < WorldIds.PlaceCount; place++)
            {
                if (EconomyCatalog.IsCity((PlaceId)place))
                    continue;
                for (int house = 0; house < WorldIds.HouseCount; house++)
                {
                    influence[place, house] *= EconomyRates.InfluenceKeep;
                    if (influence[place, house] < 0.001f)
                        influence[place, house] = 0f;
                }

                RefreshPatron((PlaceId)place);
            }

            ReplanDocked();
        }

        void RunNight()
        {
            for (int good = 0; good < WorldIds.GoodCount; good++)
            {
                GoodId id = (GoodId)good;
                if (EconomyCatalog.IsRecipeOutput(id))
                    continue;
                for (int place = 0; place < WorldIds.PlaceCount; place++)
                {
                    if (EconomyCatalog.IsProducer((PlaceId)place, id))
                        Make((PlaceId)place, id, false, default);
                }
            }

            for (int i = 0; i < EconomyRates.Recipes.Length; i++)
            {
                Recipe recipe = EconomyRates.Recipes[i];
                Make(recipe.Place, recipe.Output, true, recipe);
            }

            ReplanDocked();
        }

        void Make(PlaceId place, GoodId good, bool crafted, Recipe recipe)
        {
            int p = (int)place;
            int g = (int)good;
            float want = EconomyCatalog.Desired(good);
            makeAcc[p, g] += want;
            int limit = shelves[p, g].Cap;
            if (shelves[p, g].Stock >= limit)
            {
                makeAcc[p, g] = 0f;
                return;
            }

            int room = limit - shelves[p, g].Stock;
            int ready = Mathf.FloorToInt(makeAcc[p, g]);
            int affordable = int.MaxValue;
            if (crafted)
            {
                affordable = EconomyCatalog.Affordable(recipe, input => shelves[p, (int)input].Stock);
            }

            int make = ready;
            if (make > room)
                make = room;
            if (make > affordable)
                make = affordable;
            if (make < 0)
                make = 0;

            PutStock(place, good, shelves[p, g].Stock + make);
            makeAcc[p, g] -= make;
            if (crafted && make > 0)
            {
                PutStock(place, recipe.InputA, shelves[p, (int)recipe.InputA].Stock - recipe.CountA * make);
                if (recipe.HasSecond)
                    PutStock(place, recipe.InputB, shelves[p, (int)recipe.InputB].Stock - recipe.CountB * make);
            }

            if (shelves[p, g].Stock >= limit)
                makeAcc[p, g] = 0f;
            else if (crafted && affordable <= make)
                makeAcc[p, g] = Mathf.Min(makeAcc[p, g], want);
        }

        void DecayCargo()
        {
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                float rate = EconomyCatalog.DecayOf(stacks[i].Good);
                if (rate <= 0f || stacks[i].DecayedOnDay == Day)
                    continue;
                CargoUnit unit = stacks[i];
                unit.Integrity -= rate;
                unit.DecayedOnDay = Day;
                if (unit.Integrity <= 0.001f)
                    stacks.RemoveAt(i);
                else
                    stacks[i] = unit;
            }
        }

        int DecayStamp()
        {
            if (Hour + 0.001f >= EconomyRates.MorningHour)
                return Day;
            return Day - 1;
        }

        void RefreshPatron(PlaceId place)
        {
            if (EconomyCatalog.IsCity(place))
            {
                patron[(int)place] = EconomyCatalog.CityHouse(place);
                return;
            }

            int index = (int)place;
            HouseId? current = patron[index];
            HouseId? best = null;
            float bestValue = 0f;
            if (current.HasValue && influence[index, (int)current.Value] > 0.001f)
            {
                best = current;
                bestValue = influence[index, (int)current.Value];
            }

            for (int house = 0; house < WorldIds.HouseCount; house++)
            {
                float value = influence[index, house];
                if (value <= 0.001f)
                    continue;
                if (best == null || value > bestValue + 0.001f)
                {
                    best = (HouseId)house;
                    bestValue = value;
                }
            }

            patron[index] = best;
        }

        bool Tradable(PlaceId place)
        {
            if (Step == TutorialStep.Free)
                return true;
            return place != PlaceId.Kharun && place != PlaceId.Draven;
        }

        void SpawnNpcs()
        {
            npcs.Clear();
            var rng = new System.Random(EconomyRates.NpcSeed);
            HouseId[] houses =
            {
                HouseId.Kharun,
                HouseId.Zamath, HouseId.Zamath,
                HouseId.Thalor, HouseId.Thalor,
                HouseId.Veythar, HouseId.Veythar,
                HouseId.Ghorath, HouseId.Ghorath
            };
            for (int i = 0; i < houses.Length; i++)
            {
                var npc = new NpcCaravan();
                npc.House = houses[i];
                npc.Dock = (PlaceId)rng.Next(WorldIds.PlaceCount);
                npcs.Add(npc);
            }

            ReplanDocked();
        }

        void ReplanDocked()
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                NpcCaravan npc = npcs[i];
                if (npc.Traveling)
                    continue;
                if (!Tradable(npc.Dock))
                    Flee(npc);
                else
                {
                    SellCargo(npc);
                    Plan(npc);
                }
            }
        }

        void Flee(NpcCaravan npc)
        {
            IReadOnlyList<PlaceId> neighbors = RoadGraph.Neighbors(npc.Dock);
            if (neighbors.Count == 0)
            {
                npc.Traveling = false;
                return;
            }

            PlaceId best = neighbors[0];
            for (int i = 1; i < neighbors.Count; i++)
            {
                if (PreferFlee(neighbors[i], best))
                    best = neighbors[i];
            }

            var path = new List<PlaceId> { npc.Dock, best };
            StartPath(npc, path);
        }

        bool PreferFlee(PlaceId candidate, PlaceId best)
        {
            bool candidateClosed = !Tradable(candidate);
            bool bestClosed = !Tradable(best);
            if (bestClosed && !candidateClosed)
                return true;
            if (candidateClosed && !bestClosed)
                return false;
            return (int)candidate < (int)best;
        }

        void Plan(NpcCaravan npc)
        {
            PlaceId origin = npc.Dock;
            float bestScore = 0f;
            int bestHops = 99;
            PlaceId bestDest = origin;
            int[] bestLoad = null;
            for (int place = 0; place < WorldIds.PlaceCount; place++)
            {
                var dest = (PlaceId)place;
                if (dest == origin || !Tradable(dest))
                    continue;
                int hops = RoadGraph.Hops(origin, dest);
                if (hops <= 0 || hops > EconomyRates.NpcHopLimit)
                    continue;
                List<PlaceId> path = RoadGraph.Path(origin, dest, EconomyRates.NpcHopLimit);
                if (path.Count < 2)
                    continue;
                var load = new int[WorldIds.GoodCount];
                int space = UsedSpace(npc);
                int weight = UsedWeight(npc);
                int profit = FillLoad(origin, dest, npc.House, load, ref space, ref weight);
                if (profit <= 0)
                    continue;
                float score = profit / Mathf.Max(TripHours(path), EconomyRates.SeaHourFloor);
                bool better = bestLoad == null
                    || score > bestScore + 0.001f
                    || (Mathf.Abs(score - bestScore) <= 0.001f && hops < bestHops)
                    || (Mathf.Abs(score - bestScore) <= 0.001f && hops == bestHops && (int)dest < (int)bestDest);
                if (!better)
                    continue;
                bestScore = score;
                bestHops = hops;
                bestDest = dest;
                bestLoad = load;
            }

            if (bestLoad == null)
            {
                npc.Traveling = false;
                return;
            }

            BuyLoad(npc, bestLoad);
            StartPath(npc, RoadGraph.Path(origin, bestDest, EconomyRates.NpcHopLimit));
        }

        int FillLoad(PlaceId origin, PlaceId dest, HouseId house, int[] load, ref int space, ref int weight)
        {
            int profit = 0;
            int guard = 0;
            int limit = EconomyRates.WagonSpace + 4;
            while (guard++ < limit)
            {
                int bestGood = -1;
                int bestUnit = 0;
                for (int good = 0; good < WorldIds.GoodCount; good++)
                {
                    if (shelves[(int)origin, good].Stock - load[good] <= 0)
                        continue;
                    var id = (GoodId)good;
                    int unitSpace = EconomyCatalog.Space(id);
                    int unitWeight = EconomyCatalog.Weight(id);
                    if (space + unitSpace > EconomyRates.WagonSpace)
                        continue;
                    if (weight + unitWeight > EconomyRates.WagonWeight)
                        continue;
                    int unit = UnitProfit(origin, dest, house, id);
                    if (unit > bestUnit)
                    {
                        bestUnit = unit;
                        bestGood = good;
                    }
                }

                if (bestGood < 0)
                    break;
                load[bestGood] += 1;
                space += EconomyCatalog.Space((GoodId)bestGood);
                weight += EconomyCatalog.Weight((GoodId)bestGood);
                profit += bestUnit;
            }

            return profit;
        }

        int UnitProfit(PlaceId origin, PlaceId dest, HouseId house, GoodId good)
        {
            int buy = PriceOf(origin, good);
            int sell = PriceOf(dest, good);
            int paid = buy + CutCoin(buy, NpcPercent(origin, house));
            int received = sell - CutCoin(sell, NpcPercent(dest, house));
            return received - paid;
        }

        static float TripHours(List<PlaceId> path)
        {
            float sum = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                float hours = RoadGraph.Hours(path[i - 1], path[i]);
                if (hours <= 0f)
                    hours = EconomyRates.SeaHourFloor;
                sum += hours;
            }

            return sum;
        }

        void BuyLoad(NpcCaravan npc, int[] load)
        {
            for (int good = 0; good < load.Length; good++)
            {
                int count = load[good];
                if (count <= 0)
                    continue;
                int have = shelves[(int)npc.Dock, good].Stock;
                if (count > have)
                    count = have;
                var id = (GoodId)good;
                while (count > 0 && UsedSpace(npc) + EconomyCatalog.Space(id) <= EconomyRates.WagonSpace
                    && UsedWeight(npc) + EconomyCatalog.Weight(id) <= EconomyRates.WagonWeight)
                {
                    PutStock(npc.Dock, id, shelves[(int)npc.Dock, good].Stock - 1);
                    npc.Cargo[good] += 1;
                    count -= 1;
                }
            }
        }

        void SellCargo(NpcCaravan npc)
        {
            bool touched = false;
            for (int good = 0; good < npc.Cargo.Length; good++)
            {
                int count = npc.Cargo[good];
                if (count <= 0)
                    continue;
                var id = (GoodId)good;
                int price = PriceOf(npc.Dock, id);
                PutStock(npc.Dock, id, shelves[(int)npc.Dock, good].Stock + count);
                npc.Cargo[good] = 0;
                if (!EconomyCatalog.IsCity(npc.Dock))
                {
                    influence[(int)npc.Dock, (int)npc.House] += price * count;
                    touched = true;
                }
            }

            if (touched)
                RefreshPatron(npc.Dock);
        }

        void StartPath(NpcCaravan npc, List<PlaceId> path)
        {
            if (path == null || path.Count < 2)
            {
                npc.Traveling = false;
                return;
            }

            npc.Path.Clear();
            for (int i = 2; i < path.Count; i++)
                npc.Path.Add(path[i]);
            npc.From = path[0];
            npc.To = path[1];
            npc.Distance = 0f;
            npc.EdgeLength = RoadGraph.Length(npc.From, npc.To);
            npc.Traveling = true;
        }

        void MoveNpcs(float hours)
        {
            if (hours <= 0f)
                return;
            for (int i = 0; i < npcs.Count; i++)
            {
                float left = hours;
                int guard = 0;
                while (npcs[i].Traveling && left > 0.00001f && guard++ < 32)
                    StepNpc(npcs[i], ref left);
            }
        }

        void StepNpc(NpcCaravan npc, ref float left)
        {
            if (npc.EdgeLength <= 0.000001f)
            {
                float cost = Mathf.Min(left, EconomyRates.SeaHourFloor);
                left -= cost;
                if (cost <= 0f)
                    left = 0f;
                FinishEdge(npc);
                return;
            }

            float hours = RoadGraph.Hours(npc.From, npc.To);
            if (hours <= 0.0001f)
                hours = EconomyRates.SeaHourFloor;
            float remain = Mathf.Max(0f, npc.EdgeLength - npc.Distance);
            float remainHours = hours * (remain / npc.EdgeLength);
            if (remainHours > left + 0.00001f)
            {
                npc.Distance += npc.EdgeLength * (left / hours);
                left = 0f;
                return;
            }

            left -= remainHours;
            npc.Distance = npc.EdgeLength;
            FinishEdge(npc);
        }

        void FinishEdge(NpcCaravan npc)
        {
            PlaceId arrived = npc.To;
            npc.Dock = arrived;
            if (npc.Path.Count > 0)
            {
                npc.From = arrived;
                npc.To = npc.Path[0];
                npc.Path.RemoveAt(0);
                npc.Distance = 0f;
                npc.EdgeLength = RoadGraph.Length(npc.From, npc.To);
                npc.Traveling = true;
                return;
            }

            npc.Traveling = false;
            npc.Distance = 0f;
            if (Tradable(arrived))
            {
                SellCargo(npc);
                Plan(npc);
            }
            else
            {
                Flee(npc);
            }
        }

        int NpcPercent(PlaceId place, HouseId house)
        {
            HouseId? holder = PatronOf(place);
            if (holder == null || holder.Value == house)
                return 0;
            return EconomyRates.TariffBase;
        }

        static int UsedSpace(NpcCaravan npc)
        {
            int used = 0;
            for (int good = 0; good < npc.Cargo.Length; good++)
            {
                if (npc.Cargo[good] > 0)
                    used += npc.Cargo[good] * EconomyCatalog.Space((GoodId)good);
            }

            return used;
        }

        static int UsedWeight(NpcCaravan npc)
        {
            int used = 0;
            for (int good = 0; good < npc.Cargo.Length; good++)
            {
                if (npc.Cargo[good] > 0)
                    used += npc.Cargo[good] * EconomyCatalog.Weight((GoodId)good);
            }

            return used;
        }

        void PutStock(PlaceId place, GoodId good, int stock)
        {
            Shelf shelf = shelves[(int)place, (int)good];
            shelf.Stock = stock;
            shelves[(int)place, (int)good] = shelf;
        }

        int WorstStack(GoodId good)
        {
            int best = -1;
            float worst = 2f;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].Good != good || stacks[i].Integrity >= worst)
                    continue;
                worst = stacks[i].Integrity;
                best = i;
            }

            return best;
        }

        int Gross(PlaceId place, GoodId good, float integrity)
        {
            return Mathf.Max(0, Mathf.RoundToInt(PriceOf(place, good) * Mathf.Clamp01(integrity)));
        }

        static int CutCoin(int price, int percent)
        {
            if (price <= 0 || percent <= 0)
                return 0;
            return (price * percent + 50) / 100;
        }

        bool BuyAllowed(GoodId good)
        {
            if (Step == TutorialStep.Free)
                return true;
            if (Step == TutorialStep.BuyJars)
                return good == GoodId.ShimmersteelJars;
            if (Step == TutorialStep.BuyFungus)
                return good == GoodId.FingerFungus;
            return false;
        }

        bool SellAllowed(GoodId good)
        {
            if (Step == TutorialStep.Free)
                return true;
            if (Step == TutorialStep.SellJars)
                return good == GoodId.ShimmersteelJars;
            if (Step == TutorialStep.SellFungus)
                return good == GoodId.FingerFungus;
            return false;
        }

        bool NeedsCardReserve()
        {
            return Step == TutorialStep.OpenMarket
                || Step == TutorialStep.BuyJars
                || Step == TutorialStep.LeaveForDraven
                || Step == TutorialStep.UsePace
                || Step == TutorialStep.RidingOut
                || Step == TutorialStep.RoadCard;
        }

        void ResampleWeather()
        {
            WeatherKind again = WeatherAlongRoad();
            if (ThrowawayEconomy.Severity(again) > ThrowawayEconomy.Severity(LegWeather))
            {
                LegWeather = again;
                LegScale = ThrowawayEconomy.TimeScale(again);
            }
        }

        WeatherKind WeatherOn(PlaceId from, PlaceId to)
        {
            WeatherKind worst = WeatherKind.Clear;
            IReadOnlyList<Vector2> samples = RoadGraph.Samples(from, to);
            for (int i = 0; i < samples.Count; i++)
            {
                WeatherKind kind = WeatherAt(samples[i]);
                if (ThrowawayEconomy.Severity(kind) > ThrowawayEconomy.Severity(worst))
                    worst = kind;
            }

            return worst;
        }

        static bool LessonRoad(PlaceId a, PlaceId b)
        {
            return (a == PlaceId.Kharun && b == PlaceId.Draven) || (a == PlaceId.Draven && b == PlaceId.Kharun);
        }

        void MoveWeather(float hours)
        {
            for (int i = 0; i < masses.Count; i++)
            {
                WeatherMass mass = masses[i];
                Vector2 next = mass.Center + mass.Velocity * hours;
                next.x = Mathf.Repeat(next.x, 1f);
                next.y = Mathf.Repeat(next.y, 1f);
                mass.Center = next;
            }
        }

        void ResetCaravan()
        {
            EconomyCatalog.EnsureLoaded();
            Step = TutorialStep.OpenMarket;
            stacks.Clear();
            for (int place = 0; place < WorldIds.PlaceCount; place++)
            {
                var id = (PlaceId)place;
                patron[place] = EconomyCatalog.IsCity(id) ? EconomyCatalog.CityHouse(id) : (HouseId?)null;
                for (int house = 0; house < WorldIds.HouseCount; house++)
                    influence[place, house] = 0f;
                for (int good = 0; good < WorldIds.GoodCount; good++)
                {
                    var item = (GoodId)good;
                    eatAcc[place, good] = 0f;
                    makeAcc[place, good] = 0f;
                    shelves[place, good] = new Shelf
                    {
                        Stock = EconomyCatalog.StartingStock(id, item),
                        Cap = EconomyCatalog.Cap(id, item),
                        BasePrice = EconomyCatalog.BasePrice(item)
                    };
                }
            }

            for (int i = 0; i < standingOf.Length; i++)
                standingOf[i] = 0;

            Coin = ThrowawayEconomy.StartingCoin;
            Day = 1;
            Hour = 6f;
            Dock = PlaceId.Kharun;
            Traveling = false;
            TowardDraven = true;
            TravelDistance = 0f;
            travelFrom = PlaceId.Kharun;
            travelTo = PlaceId.Draven;
            edgeLength = RoadGraph.Length(PlaceId.Kharun, PlaceId.Draven);
            Pace = 1;
            HoldTravel = false;
            LegWeather = WeatherKind.Clear;
            LegScale = 1f;
            CardWasSteel = false;
            cardResolvedThisLeg = false;
            CardPending = false;
            ResultPending = false;
            House = null;
            HouseNote = string.Empty;
            masses.Clear();
            masses.Add(new WeatherMass
            {
                Kind = WeatherKind.Sandstorm,
                Center = new Vector2(0.82f, 0.72f),
                Radius = 0.16f,
                Velocity = new Vector2(-0.004f, 0.002f)
            });
            masses.Add(new WeatherMass
            {
                Kind = WeatherKind.Heat,
                Center = new Vector2(0.16f, 0.88f),
                Radius = 0.18f,
                Velocity = new Vector2(0.006f, -0.002f)
            });
            masses.Add(new WeatherMass
            {
                Kind = WeatherKind.Wind,
                Center = new Vector2(0.72f, 0.08f),
                Radius = 0.14f,
                Velocity = new Vector2(-0.004f, 0.003f)
            });
            SpawnNpcs();
        }

        void Advance(TutorialStep step)
        {
            Step = step;
            Touch();
        }

        void Touch()
        {
            Revision += 1;
        }

        static string Describe(WeatherKind kind, string lead)
        {
            switch (kind)
            {
                case WeatherKind.Heat:
                    return lead + "Heat. The leg takes longer. Cargo loss is not rated yet.";
                case WeatherKind.Wind:
                    return lead + "Wind. The leg takes longer. Cargo loss is not rated yet.";
                case WeatherKind.Sandstorm:
                    return lead + "Sandstorm. The leg takes longer. Cargo loss is not rated yet.";
                default:
                    return lead + "Clear. Cargo loss is not rated yet.";
            }
        }
    }
}
