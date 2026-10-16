using System.Collections.Generic;
using UnityEngine;

namespace Caravans.Play
{
    public enum WeatherKind
    {
        Clear,
        Heat,
        Wind,
        Sandstorm
    }

    public enum RoadPace
    {
        Careful,
        Steady,
        Rush
    }

    public sealed class WeatherMass
    {
        public WeatherKind Kind;
        public Vector2 Center;
        public float Radius;
        public Vector2 Velocity;
    }

    public struct Shelf
    {
        public int Stock;
        public int Cap;
        public int BasePrice;
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

        struct LoggedPrice
        {
            public bool Seen;
            public int Coin;
            public PlaceId Place;
        }

        readonly Shelf[,] shelves = new Shelf[WorldIds.PlaceCount, WorldIds.GoodCount];
        readonly float[,] eatAcc = new float[WorldIds.PlaceCount, WorldIds.GoodCount];
        readonly float[,] makeAcc = new float[WorldIds.PlaceCount, WorldIds.GoodCount];
        readonly float[,] influence = new float[WorldIds.PlaceCount, WorldIds.HouseCount];
        readonly int[] standingOf = new int[WorldIds.HouseCount];
        readonly HouseId?[] patron = new HouseId?[WorldIds.PlaceCount];
        readonly List<CargoUnit> stacks = new List<CargoUnit>();
        readonly LoggedPrice[] loggedBuy = new LoggedPrice[WorldIds.GoodCount];
        readonly LoggedPrice[] loggedSale = new LoggedPrice[WorldIds.GoodCount];
        readonly List<NpcCaravan> npcs = new List<NpcCaravan>();
        readonly List<WeatherMass> masses = new List<WeatherMass>();

        PlaceId travelFrom;
        PlaceId travelTo;
        float edgeLength;

        public int Revision { get; private set; }
        public int Coin { get; private set; }
        public float WaterLitres { get; private set; }
        public float RationKg { get; private set; }
        public int Day { get; private set; }
        public float Hour { get; private set; }
        public PlaceId Dock { get; private set; }
        public bool Traveling { get; private set; }
        public float TravelDistance { get; private set; }
        public float TravelHours { get; private set; }
        public int Pace { get; private set; }
        public bool HoldTravel { get; private set; }
        public RoadPace ChosenPace { get; private set; }
        public bool TravelsByDay { get; private set; }
        public bool CampPending { get; private set; }
        public bool AwaitingPace { get; private set; }
        public WeatherKind LegWeather { get; private set; }
        public float LegScale { get; private set; }
        public HouseId? House { get; private set; }

        float kitFactor = 1f;

        public IReadOnlyList<WeatherMass> Masses
        {
            get { return masses; }
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
            SitAt(HouseId.Kharun);
        }

        public static PlaceId HomeOf(HouseId house)
        {
            switch (house)
            {
                case HouseId.Zamath:
                    return PlaceId.Zamath;
                case HouseId.Thalor:
                    return PlaceId.Thalor;
                case HouseId.Veythar:
                    return PlaceId.Veythar;
                case HouseId.Ghorath:
                    return PlaceId.Ghorath;
                default:
                    return PlaceId.Kharun;
            }
        }

        public static CaravanSession AtHouse(HouseId house)
        {
            var session = new CaravanSession();
            session.BeginAt(house);
            return session;
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
            if (good == GoodId.SeaWater)
                good = GoodId.Water;
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

        public bool HasLoggedBuy(GoodId good)
        {
            return loggedBuy[(int)good].Seen;
        }

        public int LoggedBuy(GoodId good)
        {
            return loggedBuy[(int)good].Coin;
        }

        public PlaceId LoggedBuyPlace(GoodId good)
        {
            return loggedBuy[(int)good].Place;
        }

        public bool HasLoggedSale(GoodId good)
        {
            return loggedSale[(int)good].Seen;
        }

        public int LoggedSale(GoodId good)
        {
            return loggedSale[(int)good].Coin;
        }

        public PlaceId LoggedSalePlace(GoodId good)
        {
            return loggedSale[(int)good].Place;
        }

        public void NoteMarketShown(bool selling)
        {
            if (Traveling || CampPending || AwaitingPace)
                return;

            for (int i = 0; i < WorldIds.GoodCount; i++)
            {
                GoodId good = (GoodId)i;
                if (good == GoodId.Rations)
                    continue;
                if (selling)
                {
                    if (CargoOf(good) <= 0)
                        continue;
                    int price = SellValue(Dock, good);
                    LoggedPrice mark = loggedSale[i];
                    if (mark.Seen && price <= mark.Coin)
                        continue;
                    mark.Seen = true;
                    mark.Coin = price;
                    mark.Place = Dock;
                    loggedSale[i] = mark;
                }
                else
                {
                    if (StockOf(Dock, good) <= 0)
                        continue;
                    int price = BuyCost(Dock, good);
                    LoggedPrice mark = loggedBuy[i];
                    if (mark.Seen && price >= mark.Coin)
                        continue;
                    mark.Seen = true;
                    mark.Coin = price;
                    mark.Place = Dock;
                    loggedBuy[i] = mark;
                }
            }
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

        public bool TryBuy(GoodId good)
        {
            if (Traveling || good == GoodId.Rations)
                return false;

            int stock = StockOf(Dock, good);
            int price = BuyCost(Dock, good);
            if (stock <= 0 || Coin < price)
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

        public bool CanFillWater()
        {
            return CanFill(GoodId.Water, WaterLitres, EconomyRates.CanteenLitres);
        }

        public bool CanFillFood()
        {
            return CanFill(GoodId.Rations, RationKg, EconomyRates.RationBagKg);
        }

        public bool TryFillWater()
        {
            return TryFill(GoodId.Water, WaterLitres, EconomyRates.CanteenLitres, EconomyRates.WaterLitresPerUnit, value => WaterLitres = value);
        }

        public bool TryFillFood()
        {
            return TryFill(GoodId.Rations, RationKg, EconomyRates.RationBagKg, EconomyRates.RationsKgPerUnit, value => RationKg = value);
        }

        bool CanFill(GoodId good, float held, float capacity)
        {
            if (Traveling || held >= capacity - 0.0001f)
                return false;
            int stock = StockOf(Dock, good);
            int price = BuyCost(Dock, good);
            return stock > 0 && Coin >= price;
        }

        bool TryFill(GoodId good, float held, float capacity, float perUnit, System.Action<float> store)
        {
            if (perUnit <= 0f || !CanFill(good, held, capacity))
                return false;
            int stock = StockOf(Dock, good);
            int price = BuyCost(Dock, good);
            PutStock(Dock, good, stock - 1);
            Coin -= price;
            store(Mathf.Min(capacity, held + perUnit));
            Touch();
            return true;
        }

        public bool TrySell(GoodId good)
        {
            if (Traveling || good == GoodId.Rations || !EconomyCatalog.Buys(Dock, good))
                return false;

            int index = WorstStack(good);
            if (index < 0)
                return false;

            int gross = Gross(Dock, good, stacks[index].Integrity);
            int net = Mathf.Max(0, gross - CutCoin(gross, TariffPercent(Dock)));
            stacks.RemoveAt(index);
            GoodId shelf = good == GoodId.SeaWater ? GoodId.Water : good;
            PutStock(Dock, shelf, StockOf(Dock, shelf) + 1);
            Coin += net;
            if (House != null && !EconomyCatalog.IsCity(Dock))
            {
                influence[(int)Dock, (int)House.Value] += gross;
                RefreshPatron(Dock);
            }

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

        public bool CanTakeRoad()
        {
            return !Traveling && TravelHours > TravelScale.ReserveHours + 0.0001f;
        }

        public bool CanWait()
        {
            return !Traveling && TravelHours - 1f >= TravelScale.ReserveHours - 0.0001f;
        }

        public bool CanRest()
        {
            return !Traveling && TravelHours < TravelScale.WindowHours - 0.001f;
        }

        public bool DepartTo(PlaceId destination)
        {
            if (!CanTakeRoad() || destination == Dock)
                return false;
            return BeginTravel(destination);
        }

        public void ChooseRoadPace(RoadPace pace, bool byDay)
        {
            ChosenPace = pace;
            // Day or night is stored for a later encounter pass. No stretch reads it yet.
            TravelsByDay = byDay;
            if (AwaitingPace)
            {
                AwaitingPace = false;
                HoldTravel = false;
                if (Pace <= 0)
                    Pace = 1;
            }

            Touch();
        }

        public void ChoosePace(int pace)
        {
            if (pace != 1 && pace != 2 && pace != 4)
                return;
            Pace = pace;
            Touch();
        }

        public void SetPaused(bool paused)
        {
            if (HoldTravel)
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

            float speed = Mathf.Max(0.01f, TravelScale.Speed(ChosenPace));
            float duration = TravelScale.RealSeconds * (edgeLength / RoadPath.Length) * Mathf.Max(0.01f, LegScale) / speed;
            float step = edgeLength / duration * Pace * realSeconds;
            float next = TravelDistance + step;
            if (next > edgeLength)
                next = edgeLength;

            float moved = next - TravelDistance;
            float hours = HoursForDistance(moved);
            float room = TravelHours - TravelScale.ReserveHours;
            if (room <= 0.00001f)
            {
                OpenCamp();
                return;
            }

            if (hours > room + 0.00001f)
            {
                TravelDistance += DistanceForHours(room);
                SpendWindow(room, TravelScale.Kit(ChosenPace));
                OpenCamp();
                return;
            }

            TravelDistance = next;
            SpendWindow(hours, TravelScale.Kit(ChosenPace));
            if (TravelDistance >= edgeLength - 0.000001f)
                Arrive(travelTo);
        }

        public void MakeCamp()
        {
            if (!CampPending)
                return;

            CampPending = false;
            kitFactor = 1f;
            AdvanceClock(TravelScale.CampHours);
            TravelHours = TravelScale.WindowHours;
            AwaitingPace = true;
            HoldTravel = true;
            Touch();
        }

        public bool TryRest()
        {
            if (!CanRest())
                return false;

            kitFactor = 1f;
            AdvanceClock(TravelScale.TownRestHours);
            TravelHours = TravelScale.WindowHours;
            Touch();
            return true;
        }

        public void WaitOneHour()
        {
            if (!CanWait())
                return;
            SpendWindow(1f, 1f);
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
                int severity = TravelScale.Severity(mass.Kind);
                int bestSeverity = TravelScale.Severity(best);
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
            if (Traveling)
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
            return TravelScale.VisionRadius(Hour, WeatherAt(CaravanPoint));
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
            Traveling = true;
            CampPending = false;
            AwaitingPace = false;
            LegWeather = WeatherOn(travelFrom, travelTo);
            LegScale = TravelScale.TimeScale(LegWeather);
            Pace = 1;
            HoldTravel = false;
            Touch();

            if (edgeLength <= 0.000001f)
                Arrive(destination);
            return true;
        }

        void Arrive(PlaceId place)
        {
            Dock = place;
            Traveling = false;
            HoldTravel = false;
            CampPending = false;
            AwaitingPace = false;
            Pace = 1;
            TravelDistance = 0f;
            Touch();
        }

        // The hour reserve stops the leg. A later weather pass will also force camp in a sandstorm.
        void OpenCamp()
        {
            HoldTravel = true;
            CampPending = true;
            ResampleWeather();
            Touch();
        }

        void SpendWindow(float hours, float kit)
        {
            if (hours <= 0f)
                return;
            kitFactor = kit;
            AdvanceClock(hours);
            kitFactor = 1f;
            TravelHours -= hours;
        }

        float HoursForDistance(float distance)
        {
            if (distance <= 0f)
                return 0f;
            float speed = Mathf.Max(0.01f, TravelScale.Speed(ChosenPace));
            return distance / RoadPath.Length * TravelScale.BaseLegHours * Mathf.Max(0.01f, LegScale) / speed;
        }

        float DistanceForHours(float hours)
        {
            float speed = Mathf.Max(0.01f, TravelScale.Speed(ChosenPace));
            float perHour = RoadPath.Length * speed / (TravelScale.BaseLegHours * Mathf.Max(0.01f, LegScale));
            return hours * perHour;
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
                Drink(step);
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

        void Drink(float hours)
        {
            float day = EconomyRates.DayHours;
            if (hours <= 0f || day <= 0f)
                return;
            float days = hours / day * kitFactor;
            WaterLitres = Mathf.Max(0f, WaterLitres - days * EconomyRates.WaterLitresPerDay);
            RationKg = Mathf.Max(0f, RationKg - days * EconomyRates.RationsKgPerDay);
        }

        float HoursUntil(int mark)
        {
            float day = EconomyRates.DayHours;
            if (mark == 0)
            {
                if (Hour <= 0.0001f || Hour >= day - 0.0001f)
                    return day;
                return day - Hour;
            }

            if (Mathf.Abs(Hour - mark) <= 0.0001f)
                return day;
            if (Hour < mark)
                return mark - Hour;
            return day - Hour + mark;
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
            float want = EconomyCatalog.MakeAmount(place, good);
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
                SellCargo(npc);
                Plan(npc);
            }
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
                if (dest == origin)
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
                    if (!EconomyCatalog.Buys(dest, id))
                        continue;
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
                if (!EconomyCatalog.Buys(npc.Dock, id))
                    continue;
                GoodId shelf = id == GoodId.SeaWater ? GoodId.Water : id;
                int price = PriceOf(npc.Dock, id);
                PutStock(npc.Dock, shelf, shelves[(int)npc.Dock, (int)shelf].Stock + count);
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
            SellCargo(npc);
            Plan(npc);
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

        void ResampleWeather()
        {
            WeatherKind again = WeatherAlongRoad();
            if (TravelScale.Severity(again) > TravelScale.Severity(LegWeather))
            {
                LegWeather = again;
                LegScale = TravelScale.TimeScale(again);
            }
        }

        WeatherKind WeatherOn(PlaceId from, PlaceId to)
        {
            WeatherKind worst = WeatherKind.Clear;
            IReadOnlyList<Vector2> samples = RoadGraph.Samples(from, to);
            for (int i = 0; i < samples.Count; i++)
            {
                WeatherKind kind = WeatherAt(samples[i]);
                if (TravelScale.Severity(kind) > TravelScale.Severity(worst))
                    worst = kind;
            }

            return worst;
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

            Coin = TravelScale.StartingCoin;
            WaterLitres = EconomyRates.CanteenLitres;
            RationKg = EconomyRates.RationBagKg;
            Day = 1;
            Hour = EconomyRates.MorningHour;
            TravelHours = TravelScale.WindowHours;
            Dock = PlaceId.Kharun;
            Traveling = false;
            TravelDistance = 0f;
            travelFrom = PlaceId.Kharun;
            travelTo = PlaceId.Draven;
            edgeLength = RoadGraph.Length(PlaceId.Kharun, PlaceId.Draven);
            Pace = 1;
            HoldTravel = false;
            ChosenPace = RoadPace.Steady;
            TravelsByDay = true;
            CampPending = false;
            AwaitingPace = false;
            kitFactor = 1f;
            LegWeather = WeatherKind.Clear;
            LegScale = 1f;
            House = null;
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

        void BeginAt(HouseId house)
        {
            SitAt(house);
            Touch();
        }

        void SitAt(HouseId house)
        {
            House = house;
            Dock = HomeOf(house);
            Traveling = false;
            HoldTravel = false;
            CampPending = false;
            AwaitingPace = false;
            TravelDistance = 0f;
            travelFrom = Dock;
            Pace = 1;
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
