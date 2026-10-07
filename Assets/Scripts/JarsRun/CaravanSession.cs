using System.Collections.Generic;
using UnityEngine;

namespace Caravans.JarsRun
{
    public enum GoodId
    {
        ShimmersteelJars,
        FingerFungus,
        Water,
        Rations
    }

    public enum PlaceId
    {
        Kharun,
        Draven
    }

    public enum HouseId
    {
        Kharun,
        Zamath,
        Thalor,
        Veythar,
        Ghorath
    }

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

    public sealed class CaravanSession
    {
        readonly Shelf[,] shelves = new Shelf[2, 4];
        readonly int[] cargo = new int[4];
        readonly List<WeatherMass> masses = new List<WeatherMass>();

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

        public Vector2 CaravanPoint
        {
            get
            {
                if (!Traveling)
                    return Dock == PlaceId.Kharun ? RoadPath.Kharun : RoadPath.Draven;
                return RoadPath.PointAtDistance(TravelDistance);
            }
        }

        public CaravanSession()
        {
            ResetCaravan();
            Step = TutorialStep.OpenMarket;
        }

        public int CargoOf(GoodId good)
        {
            return cargo[(int)good];
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
            return ThrowawayEconomy.Price(shelf.BasePrice, shelf.Stock, shelf.Cap);
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
            int price = PriceOf(Dock, good);
            if (stock <= 0 || Coin - price < CoinReserve)
                return false;

            shelves[(int)Dock, (int)good].Stock = stock - 1;
            cargo[(int)good] += 1;
            Coin -= price;
            Touch();
            return true;
        }

        public bool TrySell(GoodId good)
        {
            if (Traveling || !SellAllowed(good) || cargo[(int)good] <= 0)
                return false;

            int price = PriceOf(Dock, good);
            cargo[(int)good] -= 1;
            shelves[(int)Dock, (int)good].Stock += 1;
            Coin += price;

            if (Step == TutorialStep.SellJars && good == GoodId.ShimmersteelJars && cargo[(int)GoodId.ShimmersteelJars] == 0)
                Step = TutorialStep.BuyFungus;
            if (Step == TutorialStep.SellFungus && good == GoodId.FingerFungus && cargo[(int)GoodId.FingerFungus] == 0)
                Step = TutorialStep.HouseChoice;

            Touch();
            return true;
        }

        public bool Depart()
        {
            if (Traveling)
                return false;
            if (Step != TutorialStep.LeaveForDraven
                && Step != TutorialStep.LeaveForKharun
                && Step != TutorialStep.Free)
                return false;

            TowardDraven = Dock == PlaceId.Kharun;
            TravelDistance = TowardDraven ? 0f : RoadPath.Length;
            Traveling = true;
            cardResolvedThisLeg = false;
            LegWeather = WeatherAlongRoad();
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

            return true;
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

            float duration = ThrowawayEconomy.RealSeconds * Mathf.Max(0.01f, LegScale);
            float step = RoadPath.Length / duration * Pace * realSeconds;
            float next = TowardDraven ? TravelDistance + step : TravelDistance - step;
            float half = RoadPath.Length * 0.5f;

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

            if (TowardDraven && next >= RoadPath.Length)
            {
                CommitMove(RoadPath.Length - TravelDistance);
                Arrive(PlaceId.Draven);
                return;
            }

            if (!TowardDraven && next <= 0f)
            {
                CommitMove(TravelDistance);
                Arrive(PlaceId.Kharun);
                return;
            }

            float moved = Mathf.Abs(next - TravelDistance);
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
            MoveWeather(1f);
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
                HouseNote = "That package is not written. This slice stays at Kharûn.";
                Touch();
                return false;
            }

            House = HouseId.Kharun;
            HouseNote = string.Empty;
            Advance(TutorialStep.Free);
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
            WeatherKind worst = WeatherKind.Clear;
            IReadOnlyList<Vector2> samples = RoadPath.Samples;
            for (int i = 0; i < samples.Count; i++)
            {
                WeatherKind kind = WeatherAt(samples[i]);
                if (ThrowawayEconomy.Severity(kind) > ThrowawayEconomy.Severity(worst))
                    worst = kind;
            }

            return worst;
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
            return place == PlaceId.Kharun ? "Kharûn" : "Draven";
        }

        public static string GoodName(GoodId good)
        {
            switch (good)
            {
                case GoodId.ShimmersteelJars:
                    return "Shimmersteel Jars";
                case GoodId.FingerFungus:
                    return "Finger Fungus";
                case GoodId.Water:
                    return "Water";
                default:
                    return "Rations";
            }
        }

        public static string HouseName(HouseId house)
        {
            switch (house)
            {
                case HouseId.Kharun:
                    return "Kharûn";
                case HouseId.Zamath:
                    return "Zamath";
                case HouseId.Thalor:
                    return "Thalor";
                case HouseId.Veythar:
                    return "Veythar";
                default:
                    return "Ghorath";
            }
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

        void Arrive(PlaceId place)
        {
            Dock = place;
            Traveling = false;
            HoldTravel = false;
            Pace = 1;
            TravelDistance = place == PlaceId.Kharun ? 0f : RoadPath.Length;
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
            MoveWeather(hours);
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

        void AdvanceClock(float hours)
        {
            Hour += hours;
            while (Hour >= 24f)
            {
                Hour -= 24f;
                Day += 1;
                // Dawn refill and the daily eat are not locked. The day turns. Shelves stay.
            }
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
            ThrowawayEconomy.ApplyStart(shelves);
            for (int i = 0; i < cargo.Length; i++)
                cargo[i] = 0;
            Coin = ThrowawayEconomy.StartingCoin;
            Day = 1;
            Hour = 6f;
            Dock = PlaceId.Kharun;
            Traveling = false;
            TowardDraven = true;
            TravelDistance = 0f;
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
