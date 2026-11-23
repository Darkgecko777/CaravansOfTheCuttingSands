using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Caravans.Play.Tests
{
    public class PlayTests
    {
        [Test]
        public void FullShelfIsCheaperThanAScarceOne()
        {
            var session = new CaravanSession();
            int jarsAtKharun = session.PriceOf(PlaceId.Kharun, GoodId.ShimmersteelJars);
            int jarsAtDraven = session.PriceOf(PlaceId.Draven, GoodId.ShimmersteelJars);
            int fungusAtDraven = session.PriceOf(PlaceId.Draven, GoodId.FingerFungus);
            int fungusAtKharun = session.PriceOf(PlaceId.Kharun, GoodId.FingerFungus);

            Assert.Less(jarsAtKharun, jarsAtDraven);
            Assert.Less(fungusAtDraven, fungusAtKharun);
        }

        [Test]
        public void TheRoadStartsClear()
        {
            var session = new CaravanSession();
            Assert.AreEqual(WeatherKind.Clear, session.WeatherAlongRoad());
        }

        [Test]
        public void HalfwaySitsBetweenTheTwoDocks()
        {
            Vector2 mid = RoadPath.PointAtDistance(RoadPath.Length * 0.5f);
            Assert.Less(RoadPath.Draven.y, mid.y);
            Assert.Less(mid.y, RoadPath.Kharun.y);
        }

        [Test]
        public void PriceAnchorsSitAtEmptyOrdinaryAndFull()
        {
            Assert.AreEqual(2f, EconomyCatalog.CoverMultiplier(0f), 0.001f);
            Assert.AreEqual(1f, EconomyCatalog.CoverMultiplier(3f), 0.001f);
            Assert.AreEqual(0.5f, EconomyCatalog.CoverMultiplier(6f), 0.001f);
            Assert.AreEqual(0.5f, EconomyCatalog.CoverMultiplier(9f), 0.001f);
            Assert.AreEqual(100, EconomyCatalog.ShelfPrice(50, 0, 1f));
            Assert.AreEqual(50, EconomyCatalog.ShelfPrice(50, 3, 1f));
            Assert.AreEqual(25, EconomyCatalog.ShelfPrice(50, 6, 1f));
            Assert.AreEqual(25, EconomyCatalog.ShelfPrice(50, 12, 1f));
            Assert.AreEqual(1, EconomyCatalog.ShelfPrice(1, 6, 1f));
        }

        [Test]
        public void PaintedKharunDravenMatchesTheLessonRoad()
        {
            float length = RoadGraph.Length(PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(RoadPath.Length, length, 0.0001f);
            Assert.AreEqual(24f, RoadGraph.Hours(PlaceId.Kharun, PlaceId.Draven), 0.01f);
            Vector2 mid = RoadGraph.PointAlong(PlaceId.Kharun, PlaceId.Draven, length * 0.5f);
            Vector2 lesson = RoadPath.PointAtDistance(RoadPath.Length * 0.5f);
            Assert.Less(Vector2.Distance(mid, lesson), 0.0001f);
            Assert.AreEqual(0f, RoadGraph.Hours(PlaceId.Zamath, PlaceId.Neth), 0.001f);
            Assert.AreEqual(1, RoadGraph.Hops(PlaceId.Zamath, PlaceId.Neth));
            Assert.AreEqual(1, RoadGraph.Hops(PlaceId.Westmark, PlaceId.Rukh));
        }

        [Test]
        public void ShelvesStartOnTheHopGradient()
        {
            EconomyCatalog.EnsureLoaded();
            var session = new CaravanSession();
            Assert.AreEqual(21f, EconomyCatalog.WorldEat(GoodId.ShimmersteelJars), 0.01f);
            Assert.AreEqual(21f, EconomyCatalog.WorldEat(GoodId.Water), 0.01f);
            Assert.AreEqual(6, session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars));
            Assert.AreEqual(10, session.StockOf(PlaceId.Draven, GoodId.ShimmersteelJars));
            Assert.AreEqual(6, session.StockOf(PlaceId.Draven, GoodId.FingerFungus));
            Assert.AreEqual(5, session.StockOf(PlaceId.Kharun, GoodId.FingerFungus));
            Assert.AreEqual(0f, EconomyCatalog.Eat(PlaceId.Zamath, GoodId.WaterWitchingRods), 0.001f);
            Assert.Greater(session.StockOf(PlaceId.Zamath, GoodId.WaterWitchingRods), 0);
        }

        [Test]
        public void MorningEatsAndNightJarsWaitOnBars()
        {
            var session = new CaravanSession();
            int rods = session.StockOf(PlaceId.Zamath, GoodId.WaterWitchingRods);
            int jars = session.StockOf(PlaceId.Draven, GoodId.ShimmersteelJars);
            session.TickMorning();
            Assert.AreEqual(jars - 2, session.StockOf(PlaceId.Draven, GoodId.ShimmersteelJars));
            Assert.AreEqual(rods, session.StockOf(PlaceId.Zamath, GoodId.WaterWitchingRods));

            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 0);
            session.SetShelf(PlaceId.Kharun, GoodId.Shimmersteel, 0);
            session.TickNight();
            Assert.AreEqual(0, session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Kharun, GoodId.Shimmersteel, 5);
            session.TickNight();
            Assert.AreEqual(2, session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars));
            Assert.AreEqual(1, session.StockOf(PlaceId.Kharun, GoodId.Shimmersteel));
        }

        [Test]
        public void ARivalCutStaysAtTwelve()
        {
            var session = new CaravanSession();
            session.AddPresence(PlaceId.Draven, HouseId.Zamath, 80);
            session.AddPresence(PlaceId.Draven, HouseId.Thalor, 80);
            Assert.AreEqual(HouseId.Zamath, session.PatronOf(PlaceId.Draven));
            Assert.AreEqual(EconomyCatalog.CityHouse(PlaceId.Kharun), session.PatronOf(PlaceId.Kharun));

            int dravenAsk = SpreadAsk(session, PlaceId.Draven, GoodId.ShimmersteelJars);
            Assert.AreEqual(0, session.GetKharunRank());
            Assert.AreEqual(12, session.TariffPercent(PlaceId.Draven));
            Assert.AreEqual(dravenAsk + Cut(dravenAsk, 12), session.BuyCost(PlaceId.Draven, GoodId.ShimmersteelJars));
            session.SetKharunPoints(1000f);
            Assert.AreEqual(10, session.GetKharunRank());
            Assert.AreEqual(12, session.TariffPercent(PlaceId.Draven));
            Assert.AreEqual(dravenAsk + Cut(dravenAsk, 12), session.BuyCost(PlaceId.Draven, GoodId.ShimmersteelJars));
            Assert.AreEqual(0, session.TariffPercent(PlaceId.Kharun));
            int kharunAsk = SpreadAsk(session, PlaceId.Kharun, GoodId.ShimmersteelJars);
            Assert.AreEqual(kharunAsk, session.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars));
            session.AddPresence(PlaceId.Westmark, HouseId.Kharun, 40);
            Assert.AreEqual(HouseId.Kharun, session.PatronOf(PlaceId.Westmark));
            Assert.AreEqual(0, session.TariffPercent(PlaceId.Westmark));
            Assert.IsFalse(session.KharunFlipSeen(PlaceId.Westmark));
            Assert.AreEqual(0, session.TariffPercent(PlaceId.Ashar));

            float before = session.InfluenceOf(PlaceId.Draven, HouseId.Zamath);
            float points = session.KharunPoints;
            session.TickMorning();
            Assert.AreEqual(before * 0.75f, session.InfluenceOf(PlaceId.Draven, HouseId.Zamath), 0.01f);
            Assert.AreEqual(points, session.KharunPoints, 0.001f);
            Assert.IsFalse(session.KharunFlipSeen(PlaceId.Draven));
            Assert.IsTrue(string.IsNullOrEmpty(session.RankUpNotice));
        }

        [Test]
        public void ARoundTripInOneMarketDoesNotMakeCoin()
        {
            var probe = new CaravanSession();
            Assert.AreEqual(0.15f, EconomyRates.MerchantSpread, 0.0001f);
            Assert.AreEqual(EconomyRates.MerchantSpread, probe.MerchantSpread(), 0.0001f);
            Assert.AreEqual(0, probe.TariffPercent(PlaceId.Kharun));

            GoodId[] goods =
            {
                GoodId.Water,
                GoodId.MorRoot,
                GoodId.FingerFungus,
                GoodId.Shimmersteel
            };
            for (int i = 0; i < goods.Length; i++)
            {
                RoundTrip(goods[i], 1, 1);
                RoundTrip(goods[i], CoverStock(PlaceId.Kharun, goods[i], EconomyRates.OrdinaryCoverDays), 1);
                RoundTrip(goods[i], CoverStock(PlaceId.Kharun, goods[i], EconomyRates.FullCoverDays), 1);
                RoundTrip(goods[i], 1, 10);
                RoundTrip(goods[i], CoverStock(PlaceId.Kharun, goods[i], EconomyRates.OrdinaryCoverDays), 10);
                RoundTrip(goods[i], CoverStock(PlaceId.Kharun, goods[i], EconomyRates.FullCoverDays), 10);
            }
        }

        [Test]
        public void ASameVisitResaleAddsNoInfluence()
        {
            var session = new CaravanSession();
            session.ShotDockAt(PlaceId.Draven);
            session.SetShelf(PlaceId.Draven, GoodId.ShimmersteelJars, 4);
            int coin = session.Coin;
            Assert.AreEqual(0f, session.InfluenceOf(PlaceId.Draven, HouseId.Kharun), 0.001f);
            Assert.IsTrue(session.TryBuy(GoodId.ShimmersteelJars));
            Assert.IsTrue(session.TrySell(GoodId.ShimmersteelJars));
            Assert.LessOrEqual(session.Coin, coin);
            Assert.AreEqual(0f, session.InfluenceOf(PlaceId.Draven, HouseId.Kharun), 0.001f);
            Assert.AreEqual(4, session.StockOf(PlaceId.Draven, GoodId.ShimmersteelJars));

            var carried = new CaravanSession();
            carried.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 4);
            Assert.IsTrue(carried.TryBuy(GoodId.ShimmersteelJars));
            Leave(carried, PlaceId.Draven);
            float before = carried.InfluenceOf(PlaceId.Draven, HouseId.Kharun);
            Assert.IsTrue(carried.TrySell(GoodId.ShimmersteelJars));
            Assert.Greater(carried.InfluenceOf(PlaceId.Draven, HouseId.Kharun), before);
        }

        static int CoverStock(PlaceId place, GoodId good, int days)
        {
            int stock = Mathf.CeilToInt(EconomyCatalog.DayUnit(place, good) * days);
            if (stock < 1)
                stock = 1;
            return stock;
        }

        static void RoundTrip(GoodId good, int stock, int units)
        {
            var session = new CaravanSession();
            session.SetShelf(PlaceId.Kharun, good, stock);
            int purse = session.Coin;
            int moved = 0;
            while (moved < units)
            {
                int held = 0;
                int mark = session.Coin;
                while (moved + held < units && session.CanBuy(good))
                {
                    int quote = session.BuyCost(PlaceId.Kharun, good);
                    int before = session.Coin;
                    Assert.IsTrue(session.TryBuy(good), good + " at stock " + stock);
                    Assert.AreEqual(before - quote, session.Coin);
                    held++;
                }

                Assert.Greater(held, 0, good + " bought nothing at stock " + stock);
                for (int n = 0; n < held; n++)
                {
                    int quote = session.SellValue(PlaceId.Kharun, good);
                    int before = session.Coin;
                    Assert.IsTrue(session.TrySell(good));
                    Assert.AreEqual(before + quote, session.Coin);
                }

                Assert.LessOrEqual(session.Coin, mark);
                moved += held;
            }

            Assert.LessOrEqual(session.Coin, purse);
            Assert.AreEqual(stock, session.StockOf(PlaceId.Kharun, good));
            Assert.AreEqual(0, session.CargoOf(good));
        }

        static int SpreadAsk(CaravanSession session, PlaceId place, GoodId good)
        {
            GoodId shelf = good == GoodId.SeaWater ? GoodId.Water : good;
            int stock = session.StockOf(place, shelf) - 1;
            if (stock < 0)
                stock = 0;
            int mid = EconomyCatalog.ShelfPrice(
                EconomyCatalog.BasePrice(shelf),
                stock,
                EconomyCatalog.DayUnit(place, shelf));
            int ask = Mathf.RoundToInt(mid * (1f + EconomyRates.MerchantSpread));
            if (ask < 1)
                ask = 1;
            return ask;
        }

        [Test]
        public void FoodDecaysAndSweetBlissIsGoneAfterFourMornings()
        {
            var session = new CaravanSession();
            session.SetShelf(PlaceId.Kharun, GoodId.SweetBliss, 4);
            session.SetShelf(PlaceId.Kharun, GoodId.FingerFungus, 4);
            Assert.IsTrue(session.TryBuy(GoodId.SweetBliss));
            Assert.IsTrue(session.TryBuy(GoodId.FingerFungus));
            Assert.IsTrue(session.TryBuy(GoodId.Water));
            session.GiveCargo(GoodId.Rations, 1f);

            session.AdvanceHours(24f);
            Assert.AreEqual(0.75f, session.IntegrityOf(GoodId.SweetBliss), 0.001f);
            Assert.AreEqual(0.90f, session.IntegrityOf(GoodId.FingerFungus), 0.001f);
            session.AdvanceHours(72f);
            Assert.AreEqual(0, session.CargoOf(GoodId.SweetBliss));
            Assert.AreEqual(1, session.CargoOf(GoodId.FingerFungus));

            session.AdvanceHours(24f * 6f);
            Assert.AreEqual(0, session.CargoOf(GoodId.FingerFungus));
            Assert.AreEqual(1, session.CargoOf(GoodId.Water));
            Assert.AreEqual(1f, session.IntegrityOf(GoodId.Water), 0.001f);
            Assert.AreEqual(1, session.CargoOf(GoodId.Rations));
            Assert.AreEqual(1f, session.IntegrityOf(GoodId.Rations), 0.001f);
            Assert.IsFalse(session.DeathPending);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
        }

        [Test]
        public void NpcLoadsFitInsideTwoHops()
        {
            var session = new CaravanSession();
            AssertNpcLoads(session);
            session.AdvanceHours(48f);
            AssertNpcLoads(session);
        }

        [Test]
        public void ContrabandStaysOnTheShelf()
        {
            var session = new CaravanSession();
            Assert.IsTrue(EconomyCatalog.IsContraband(PlaceId.Kharun, GoodId.DreamLotusNectar));
            Assert.IsTrue(EconomyCatalog.IsContraband(PlaceId.Kharun, GoodId.EyesOfArkhul));
            Assert.IsFalse(EconomyCatalog.IsContraband(PlaceId.Draven, GoodId.EyesOfArkhul));
            Assert.IsTrue(EconomyCatalog.IsContraband(PlaceId.Kharun, GoodId.SweetBliss));
            Assert.IsFalse(EconomyCatalog.IsContraband(PlaceId.Draven, GoodId.SweetBliss));
            Assert.Greater(session.PriceOf(PlaceId.Ghorath, GoodId.DreamLotusNectar), 0);
            Assert.Greater(session.StockOf(PlaceId.Ghorath, GoodId.DreamLotusNectar), 0);
        }

        [Test]
        public void APlainSessionStartsFreeAtKharun()
        {
            var session = new CaravanSession();
            Assert.AreEqual(HouseId.Kharun, session.House.Value);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.IsFalse(session.Traveling);
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            Assert.AreEqual(RoadPace.Steady, session.ChosenPace);
        }

        [Test]
        public void AHouseStartIsFreeAtThatCityWithTheSharedPurse()
        {
            var session = new CaravanSession();
            Assert.IsTrue(session.House.HasValue);
            Assert.AreEqual(HouseId.Kharun, session.House.Value);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(TravelScale.StartingCoin, session.Coin);
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));
            Assert.IsFalse(session.Traveling);
        }

        [Test]
        public void TheOnlyStartIsKharun()
        {
            var session = new CaravanSession();
            Assert.AreEqual(HouseId.Kharun, session.House.Value);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(TravelScale.StartingCoin, session.Coin);
            Assert.AreEqual(0, session.CargoCount);
        }

        [Test]
        public void TheKitStartsFullAndFollowsTheClock()
        {
            var session = new CaravanSession();
            Assert.AreEqual(session.KitWaterCapacity, session.WaterLitres, 0.001f);
            Assert.AreEqual(session.KitRationCapacity, session.RationKg, 0.001f);
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));

            session.AdvanceHours(EconomyRates.DayHours);
            Assert.AreEqual(session.KitWaterCapacity - session.DailyWater, session.WaterLitres, 0.02f);
            Assert.AreEqual(session.KitRationCapacity - session.DailyRations, session.RationKg, 0.02f);

            float water = session.WaterLitres;
            float food = session.RationKg;
            session.WaitOneHour();
            float hourShare = 1f / EconomyRates.DayHours;
            Assert.AreEqual(water - session.DailyWater * hourShare, session.WaterLitres, 0.02f);
            Assert.AreEqual(food - session.DailyRations * hourShare, session.RationKg, 0.02f);
        }

        [Test]
        public void AClearCrossingDrinksDayRoadHoursAtTheHigherRate()
        {
            var session = new CaravanSession();
            int day = session.Day;
            float hour = session.Hour;
            float water = session.WaterLitres;
            float food = session.RationKg;
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            Ride(session);
            float elapsed = (session.Day - day) * EconomyRates.DayHours + (session.Hour - hour);
            Assert.AreEqual(36f, elapsed, 0.05f);
            float kitHours = 9f * TravelScale.DayKit + 6f + 9f + 6f + 6f * TravelScale.DayKit;
            Assert.AreEqual(water - kitHours / EconomyRates.DayHours * session.DailyWater, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - kitHours / EconomyRates.DayHours * session.DailyRations, session.RationKg, 0.05f);
        }

        [Test]
        public void FillPoursOneShelfUnitIntoTheKit()
        {
            var session = new CaravanSession();
            session.AdvanceHours(1f);
            int stock = session.StockOf(PlaceId.Kharun, GoodId.Water);
            int rations = session.StockOf(PlaceId.Kharun, GoodId.Rations);
            int coin = session.Coin;
            Assert.IsTrue(session.CanFillWater());
            Assert.IsTrue(session.TryFillWater());
            Assert.AreEqual(session.KitWaterCapacity, session.WaterLitres, 0.001f);
            Assert.AreEqual(stock - 1, session.StockOf(PlaceId.Kharun, GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.Less(session.Coin, coin);
            Assert.IsFalse(session.CanFillWater());
            Assert.IsFalse(session.TryFillWater());

            Assert.IsTrue(session.TryFillFood());
            Assert.AreEqual(session.KitRationCapacity, session.RationKg, 0.001f);
            Assert.AreEqual(rations - 1, session.StockOf(PlaceId.Kharun, GoodId.Rations));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));
            Assert.IsFalse(session.TryFillFood());
        }

        static void AssertNpcLoads(CaravanSession session)
        {
            Assert.AreEqual(9, session.NpcCount);
            for (int i = 0; i < session.NpcCount; i++)
            {
                Assert.LessOrEqual(session.NpcSpace(i), EconomyRates.WagonSpace);
                Assert.LessOrEqual(session.NpcWeight(i), EconomyRates.WagonWeight);
                if (session.NpcTraveling(i))
                    Assert.LessOrEqual(session.NpcHopsRemaining(i), EconomyRates.NpcHopLimit);
            }
        }

        [Test]
        public void LogbookKeepsTheCheapestBuyAndTheDearestSale()
        {
            var session = new CaravanSession();
            Assert.IsFalse(session.HasLoggedBuy(GoodId.ShimmersteelJars));
            Assert.IsFalse(session.HasLoggedSale(GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Kharun, GoodId.FrostIron, 0);
            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 0);
            session.NoteMarketShown(false);
            Assert.IsFalse(session.HasLoggedBuy(GoodId.FrostIron));
            Assert.IsFalse(session.HasLoggedBuy(GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 1);
            int dear = session.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars);
            session.NoteMarketShown(false);
            Assert.AreEqual(dear, session.LoggedBuy(GoodId.ShimmersteelJars));
            Assert.AreEqual(PlaceId.Kharun, session.LoggedBuyPlace(GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 40);
            int cheap = session.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars);
            Assert.Less(cheap, dear);
            session.NoteMarketShown(false);
            Assert.AreEqual(cheap, session.LoggedBuy(GoodId.ShimmersteelJars));
            Assert.AreEqual(PlaceId.Kharun, session.LoggedBuyPlace(GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 1);
            session.NoteMarketShown(false);
            Assert.AreEqual(cheap, session.LoggedBuy(GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Kharun, GoodId.FrostIron, 1);
            int ironAtHome = session.BuyCost(PlaceId.Kharun, GoodId.FrostIron);
            session.NoteMarketShown(false);
            Assert.AreEqual(ironAtHome, session.LoggedBuy(GoodId.FrostIron));
            Assert.AreEqual(PlaceId.Kharun, session.LoggedBuyPlace(GoodId.FrostIron));

            Assert.IsTrue(session.TryBuy(GoodId.ShimmersteelJars));
            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 40);
            int homeSale = session.SellValue(PlaceId.Kharun, GoodId.ShimmersteelJars);
            session.NoteMarketShown(true);
            Assert.AreEqual(homeSale, session.LoggedSale(GoodId.ShimmersteelJars));
            Assert.AreEqual(PlaceId.Kharun, session.LoggedSalePlace(GoodId.ShimmersteelJars));
            Assert.IsFalse(session.HasLoggedSale(GoodId.Water));

            session.SetShelf(PlaceId.Kharun, GoodId.ShimmersteelJars, 1);
            int dearerHomeSale = session.SellValue(PlaceId.Kharun, GoodId.ShimmersteelJars);
            Assert.Greater(dearerHomeSale, homeSale);
            session.NoteMarketShown(true);
            Assert.AreEqual(dearerHomeSale, session.LoggedSale(GoodId.ShimmersteelJars));

            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.NoteMarketShown(false);
            Assert.AreEqual(PlaceId.Kharun, session.LoggedBuyPlace(GoodId.FrostIron));
            Ride(session);
            Assert.AreEqual(PlaceId.Draven, session.Dock);

            session.SetShelf(PlaceId.Draven, GoodId.ShimmersteelJars, 1);
            int awaySale = session.SellValue(PlaceId.Draven, GoodId.ShimmersteelJars);
            Assert.Greater(awaySale, homeSale);
            session.NoteMarketShown(true);
            Assert.AreEqual(awaySale, session.LoggedSale(GoodId.ShimmersteelJars));
            Assert.AreEqual(PlaceId.Draven, session.LoggedSalePlace(GoodId.ShimmersteelJars));

            session.SetShelf(PlaceId.Draven, GoodId.FrostIron, 80);
            int ironAway = session.BuyCost(PlaceId.Draven, GoodId.FrostIron);
            Assert.Less(ironAway, ironAtHome);
            session.NoteMarketShown(false);
            Assert.AreEqual(ironAway, session.LoggedBuy(GoodId.FrostIron));
            Assert.AreEqual(PlaceId.Draven, session.LoggedBuyPlace(GoodId.FrostIron));
            Assert.AreEqual(cheap, session.LoggedBuy(GoodId.ShimmersteelJars));
        }

        [Test]
        public void GradesOpenFoodAndWaterAtDifferentPrices()
        {
            var session = new CaravanSession();
            Assert.Less(session.PriceOf(PlaceId.Veythar, GoodId.Rations), session.PriceOf(PlaceId.Thalor, GoodId.Rations));
            Assert.Less(session.PriceOf(PlaceId.Thalor, GoodId.Rations), session.PriceOf(PlaceId.Ashar, GoodId.Rations));
            Assert.Less(session.PriceOf(PlaceId.Ghorath, GoodId.Water), session.PriceOf(PlaceId.Kharun, GoodId.Water));
            Assert.Less(session.PriceOf(PlaceId.Kharun, GoodId.Water), session.PriceOf(PlaceId.Moraq, GoodId.Water));
            Assert.AreEqual(6, session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars));
            Assert.AreEqual(6, session.StockOf(PlaceId.Draven, GoodId.FingerFungus));

            session.SetShelf(PlaceId.Westmark, GoodId.ShimmersteelJars, 0);
            int water = session.StockOf(PlaceId.Westmark, GoodId.Water);
            session.TickNight();
            Assert.AreEqual(0, session.StockOf(PlaceId.Westmark, GoodId.ShimmersteelJars));
            Assert.Greater(session.StockOf(PlaceId.Westmark, GoodId.Water), water);
            Assert.IsFalse(EconomyCatalog.IsProducer(PlaceId.Westmark, GoodId.ShimmersteelJars));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Westmark, GoodId.Water));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Westmark, GoodId.Rations));
        }

        [Test]
        public void RationsAreKitAndWaterStillSellsAtKharun()
        {
            var session = new CaravanSession();
            Assert.IsFalse(session.TryBuy(GoodId.Rations));
            Assert.IsFalse(session.TrySell(GoodId.Rations));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));

            session.AdvanceHours(1f);
            int rations = session.StockOf(PlaceId.Kharun, GoodId.Rations);
            Assert.IsTrue(session.TryFillFood());
            Assert.AreEqual(rations - 1, session.StockOf(PlaceId.Kharun, GoodId.Rations));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));

            Assert.IsTrue(session.TryBuy(GoodId.Water));
            Assert.IsTrue(session.TrySell(GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
        }

        [Test]
        public void SeaWaterIsPricedAndReceivedAsPureWater()
        {
            Assert.AreEqual(0, EconomyCatalog.StartingStock(PlaceId.Kharun, GoodId.SeaWater));
            Assert.AreEqual(EconomyRates.SeaStart, EconomyCatalog.StartingStock(PlaceId.Zamath, GoodId.SeaWater));
            Assert.AreEqual(EconomyCatalog.Weight(GoodId.Water), EconomyCatalog.Weight(GoodId.SeaWater));
            Assert.AreEqual(EconomyCatalog.Space(GoodId.Water), EconomyCatalog.Space(GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Westmark, GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Neth, GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Sorel, GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Zamath, GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Ghul, GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Kaleth, GoodId.SeaWater));
            Assert.IsTrue(EconomyCatalog.IsProducer(PlaceId.Brineford, GoodId.SeaWater));
            Assert.IsFalse(EconomyCatalog.IsProducer(PlaceId.Rukh, GoodId.SeaWater));
            Assert.IsFalse(EconomyCatalog.IsProducer(PlaceId.Ghorath, GoodId.SeaWater));

            var session = new CaravanSession();
            Assert.AreEqual(0, session.StockOf(PlaceId.Kharun, GoodId.SeaWater));
            Assert.AreEqual(session.BuyCost(PlaceId.Zamath, GoodId.Water), session.BuyCost(PlaceId.Zamath, GoodId.SeaWater));

            var zamath = new CaravanSession();
            zamath.ShotDockAt(PlaceId.Zamath);
            zamath.SetShelf(PlaceId.Zamath, GoodId.SeaWater, 4);
            int waterAtZamath = zamath.StockOf(PlaceId.Zamath, GoodId.Water);
            Assert.IsTrue(zamath.TryBuy(GoodId.SeaWater));
            Assert.IsFalse(zamath.TrySell(GoodId.SeaWater));
            Assert.AreEqual(1, zamath.CargoOf(GoodId.SeaWater));
            Assert.AreEqual(waterAtZamath, zamath.StockOf(PlaceId.Zamath, GoodId.Water));

            Leave(session, PlaceId.Westmark);
            session.SetShelf(PlaceId.Westmark, GoodId.SeaWater, 4);
            Assert.IsTrue(session.TryBuy(GoodId.SeaWater));
            Leave(session, PlaceId.Kharun);
            int water = session.StockOf(PlaceId.Kharun, GoodId.Water);
            int coin = session.Coin;
            int pay = session.SellValue(PlaceId.Kharun, GoodId.SeaWater);
            Assert.AreEqual(session.SellValue(PlaceId.Kharun, GoodId.Water), pay);
            Assert.IsTrue(session.TrySell(GoodId.SeaWater));
            Assert.AreEqual(0, session.CargoOf(GoodId.SeaWater));
            Assert.AreEqual(0, session.StockOf(PlaceId.Kharun, GoodId.SeaWater));
            Assert.AreEqual(water + 1, session.StockOf(PlaceId.Kharun, GoodId.Water));
            Assert.AreEqual(coin + pay, session.Coin);

            Leave(session, PlaceId.Brineford);
            session.SetShelf(PlaceId.Brineford, GoodId.SeaWater, 4);
            session.SetShelf(PlaceId.Brineford, GoodId.Water, 4);
            Assert.IsTrue(session.TryBuy(GoodId.SeaWater));
            Assert.IsTrue(session.TryBuy(GoodId.Water));
            Leave(session, PlaceId.Moraq);
            Leave(session, PlaceId.Ghorath);
            int atGhorath = session.StockOf(PlaceId.Ghorath, GoodId.Water);
            Assert.IsFalse(session.TrySell(GoodId.Water));
            Assert.IsFalse(session.TrySell(GoodId.SeaWater));
            Assert.AreEqual(atGhorath, session.StockOf(PlaceId.Ghorath, GoodId.Water));
            Assert.AreEqual(1, session.CargoOf(GoodId.Water));
            Assert.AreEqual(1, session.CargoOf(GoodId.SeaWater));
        }

        static void Leave(CaravanSession session, PlaceId dest)
        {
            if (!session.CanTakeRoad())
                Assert.IsTrue(session.TryRest());
            session.SetKit(session.KitWaterCapacity, session.KitRationCapacity);
            Assert.IsTrue(session.DepartTo(dest));
            Ride(session);
        }

        static void Release(CaravanSession session)
        {
            if (session.EncounterPending)
                session.SkipEncounter();
        }

        static void Ride(CaravanSession session)
        {
            for (int i = 0; i < 4000 && session.Traveling; i++)
            {
                if (session.DeathPending)
                    break;
                Release(session);
                if (session.CampPending)
                    session.MakeCamp();
                if (session.AwaitingPace)
                {
                    session.ChooseRoadPace(session.ChosenPace);
                    Release(session);
                }
                session.Tick(0.25f);
            }

            Assert.IsFalse(session.DeathPending);
            Assert.IsFalse(session.Traveling);
            Assert.IsFalse(session.CampPending);
        }

        static int Cut(int price, int percent)
        {
            return (price * percent + 50) / 100;
        }

        [Test]
        public void TheTravelWindowStopsAtThreeHours()
        {
            var session = new CaravanSession();
            Assert.IsTrue(session.CanWait());
            for (int i = 0; i < 9; i++)
                session.WaitOneHour();
            Assert.AreEqual(TravelScale.ReserveHours, session.TravelHours, 0.001f);
            Assert.IsFalse(session.CanWait());
            Assert.IsFalse(session.CanTakeRoad());
            float hour = session.Hour;
            session.WaitOneHour();
            Assert.AreEqual(hour, session.Hour, 0.001f);

            float water = session.WaterLitres;
            Assert.IsTrue(session.TryRest());
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            Assert.AreEqual(water - TravelScale.TownRestHours / EconomyRates.DayHours * session.DailyWater, session.WaterLitres, 0.02f);
            Assert.IsTrue(session.CanTakeRoad());
        }

        [Test]
        public void CampRestoresTheWindowAfterNineTravelHours()
        {
            var session = new CaravanSession();
            float water = session.WaterLitres;
            int day = session.Day;
            float hour = session.Hour;
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
            {
                Release(session);
                session.Tick(0.25f);
            }

            Assert.IsTrue(session.CampPending);
            Assert.AreEqual(TravelScale.ReserveHours, session.TravelHours, 0.05f);
            float elapsed = Elapsed(session, day, hour);
            Assert.AreEqual(TravelScale.WindowHours - TravelScale.ReserveHours, elapsed, 0.05f);
            float travelDays = elapsed / EconomyRates.DayHours;
            Assert.AreEqual(water - travelDays * TravelScale.DayKit * session.DailyWater, session.WaterLitres, 0.05f);
            float covered = session.TravelDistance / RoadGraph.Length(PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(travelDays, covered, 0.02f);

            session.MakeCamp();
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            Assert.IsTrue(session.AwaitingPace);
            float afterCamp = Elapsed(session, day, hour);
            Assert.AreEqual(elapsed + TravelScale.CampHours, afterCamp, 0.05f);
            session.ChooseRoadPace(RoadPace.Steady);
            Release(session);
            Assert.IsFalse(session.AwaitingPace);
            Assert.IsFalse(session.HoldTravel);
        }

        [Test]
        public void RushCoversMoreRoadAndCarefulDrinksLess()
        {
            float steady = DistanceUntilCamp(RoadPace.Steady);
            float rush = DistanceUntilCamp(RoadPace.Rush);
            float careful = DistanceUntilCamp(RoadPace.Careful);
            Assert.AreEqual(steady * 1.5f, rush, steady * 0.02f);
            Assert.AreEqual(steady * 0.5f, careful, steady * 0.02f);

            float steadyKit = KitUntilCamp(RoadPace.Steady);
            float rushKit = KitUntilCamp(RoadPace.Rush);
            float carefulKit = KitUntilCamp(RoadPace.Careful);
            Assert.AreEqual(steadyKit * 1.25f, rushKit, 0.05f);
            Assert.AreEqual(steadyKit * 0.75f, carefulKit, 0.05f);
        }

        [Test]
        public void ASeaOpeningSpendsNoTravelHours()
        {
            var session = new CaravanSession();
            session.ShotDockAt(PlaceId.Zamath);
            float hours = session.TravelHours;
            session.SplitUntilStop(PlaceId.Neth, RoadPace.Steady, out float day, out float night);
            Assert.AreEqual(0f, day, 0.001f);
            Assert.AreEqual(0f, night, 0.001f);
            Assert.IsTrue(session.DepartTo(PlaceId.Neth));
            Assert.IsFalse(session.Traveling);
            Assert.AreEqual(PlaceId.Neth, session.Dock);
            Assert.AreEqual(hours, session.TravelHours, 0.001f);
        }

        [Test]
        public void TheDepartureSplitFollowsTheClockUntilTheNextStop()
        {
            TravelScale.SplitDayNight(14f, 9f, out float walkedDay, out float walkedNight);
            Assert.AreEqual(4f, walkedDay, 0.001f);
            Assert.AreEqual(5f, walkedNight, 0.001f);

            var session = new CaravanSession();
            Assert.IsFalse(session.CanWaitUntilDawn());
            session.SplitUntilStop(PlaceId.Draven, RoadPace.Steady, out float day, out float night);
            Assert.AreEqual(9f, day, 0.05f);
            Assert.AreEqual(0f, night, 0.05f);
            session.SplitUntilStop(PlaceId.Draven, RoadPace.Rush, out day, out night);
            Assert.AreEqual(9f, day, 0.05f);
            Assert.AreEqual(0f, night, 0.05f);

            float kaleth = RoadGraph.Hours(PlaceId.Kharun, PlaceId.Kaleth) / TravelScale.Speed(RoadPace.Rush);
            Assert.Less(kaleth, TravelScale.WindowHours - TravelScale.ReserveHours);
            session.SplitUntilStop(PlaceId.Kaleth, RoadPace.Rush, out day, out night);
            Assert.AreEqual(kaleth, day, 0.05f);
            Assert.AreEqual(0f, night, 0.05f);
            session.SplitUntilStop(PlaceId.Kaleth, RoadPace.Steady, out day, out night);
            Assert.AreEqual(9f, day + night, 0.05f);

            session.AdvanceHours(8f);
            session.SplitUntilStop(PlaceId.Draven, RoadPace.Steady, out day, out night);
            Assert.AreEqual(4f, day, 0.05f);
            Assert.AreEqual(5f, night, 0.05f);
            session.AdvanceHours(4f);
            session.SplitUntilStop(PlaceId.Draven, RoadPace.Steady, out day, out night);
            Assert.AreEqual(0f, day, 0.05f);
            Assert.AreEqual(9f, night, 0.05f);
        }

        [Test]
        public void DayHoursOnTheRoadDrinkMoreThanNightHours()
        {
            var session = new CaravanSession();
            session.AdvanceHours(8f);
            float water = session.WaterLitres;
            float food = session.RationKg;
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
            {
                Release(session);
                session.Tick(0.25f);
            }
            Assert.IsTrue(session.CampPending);
            float kitHours = (4f * TravelScale.DayKit + 5f) / EconomyRates.DayHours;
            Assert.AreEqual(water - kitHours * session.DailyWater, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - kitHours * session.DailyRations, session.RationKg, 0.05f);
        }

        [Test]
        public void CampCanWaitUntilDawnWithoutSpendingTheTravelStore()
        {
            var session = new CaravanSession();
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
            {
                Release(session);
                session.Tick(0.25f);
            }
            session.MakeCamp();
            Assert.AreEqual(21f, session.Hour, 0.05f);
            float water = session.WaterLitres;
            float food = session.RationKg;
            Assert.IsTrue(session.CanWaitUntilDawn());
            session.WaitUntilDawn();
            Assert.AreEqual(EconomyRates.MorningHour, session.Hour, 0.05f);
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            float waited = 9f / EconomyRates.DayHours;
            Assert.AreEqual(water - waited * session.DailyWater, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - waited * session.DailyRations, session.RationKg, 0.05f);
            session.SplitUntilStop(PlaceId.Draven, RoadPace.Steady, out float day, out float night);
            Assert.AreEqual(9f, day, 0.05f);
            Assert.AreEqual(0f, night, 0.05f);
            Assert.IsFalse(session.CanWaitUntilDawn());

            water = session.WaterLitres;
            food = session.RationKg;
            Assert.IsTrue(session.CanWaitUntilDusk());
            session.WaitUntilDusk();
            Assert.AreEqual(EconomyRates.NightHour, session.Hour, 0.05f);
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            waited = 12f / EconomyRates.DayHours;
            Assert.AreEqual(water - waited * session.DailyWater, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - waited * session.DailyRations, session.RationKg, 0.05f);
            session.SplitUntilStop(PlaceId.Draven, RoadPace.Steady, out day, out night);
            Assert.AreEqual(0f, day, 0.05f);
            Assert.AreEqual(9f, night, 0.05f);
            Assert.IsFalse(session.CanWaitUntilDusk());
        }

        [Test]
        public void TownCanWaitUntilDuskWithoutSpendingTheTravelStore()
        {
            var session = new CaravanSession();
            Assert.IsFalse(session.CanWaitUntilDawn());
            Assert.IsTrue(session.CanWaitUntilDusk());
            float travel = session.TravelHours;
            float water = session.WaterLitres;
            session.WaitUntilDusk();
            Assert.IsFalse(session.Traveling);
            Assert.AreEqual(EconomyRates.NightHour, session.Hour, 0.001f);
            Assert.AreEqual(travel, session.TravelHours, 0.001f);
            float waited = 12f / EconomyRates.DayHours;
            Assert.AreEqual(water - waited * session.DailyWater, session.WaterLitres, 0.05f);
            Assert.IsFalse(session.CanWaitUntilDusk());
            Assert.IsTrue(session.CanWaitUntilDawn());
        }

        [Test]
        public void StretchChancesFollowThePace()
        {
            Assert.AreEqual(0.35f, EncounterTable.Chance(RoadPace.Careful), 0.0001f);
            Assert.AreEqual(0.50f, EncounterTable.Chance(RoadPace.Steady), 0.0001f);
            Assert.AreEqual(0.65f, EncounterTable.Chance(RoadPace.Rush), 0.0001f);
        }

        [Test]
        public void TheEncounterChartStoresDayAndNightAndTheWrittenRows()
        {
            IReadOnlyList<EncounterRow> rows = EncounterTable.All;
            Assert.AreEqual(87, rows.Count);
            var seen = new HashSet<string>();
            int day = 0;
            int night = 0;
            int either = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.IsTrue(seen.Add(rows[i].Id));
                if (rows[i].When == DayNight.Day)
                    day++;
                else if (rows[i].When == DayNight.Night)
                    night++;
                else
                    either++;
            }

            Assert.AreEqual(33, day);
            Assert.AreEqual(34, night);
            Assert.AreEqual(20, either);
            Assert.AreEqual("SS-01", EncounterTable.Rows(EncounterCategory.WeatherSandstorm)[0].Id);
            Assert.AreEqual("WS-01", EncounterTable.Rows(EncounterCategory.WildlifeSea)[0].Id);
            Assert.AreEqual(DayNight.Night, ChartWhen("CA-06"));
            Assert.AreEqual(DayNight.Night, ChartWhen("CA-07"));
            Assert.AreEqual(DayNight.Either, ChartWhen("CA-08"));
            Assert.AreEqual(DayNight.Either, ChartWhen("CA-09"));
            Assert.AreEqual(DayNight.Either, ChartWhen("CA-10"));
            Assert.AreEqual(DayNight.Day, ChartWhen("CA-11"));
            Assert.AreEqual(DayNight.Either, ChartWhen("FU-01"));
            Assert.AreEqual(EncounterCategory.Catchall, ChartCategory("CA-11"));
            Assert.AreEqual(EncounterCategory.WildlifeSea, ChartCategory("FU-01"));
            Assert.IsTrue(seen.Contains("BA-04"));
            Assert.IsTrue(seen.Contains("BA-05"));
            Assert.IsFalse(EncounterCards.IsRoadPool("BA-04"));
            Assert.IsFalse(EncounterCards.IsRoadPool("BA-05"));
            Assert.IsFalse(EncounterCards.IsRoadPool("FU-01"));
        }

        [Test]
        public void RoadPoolsFollowTheRouteAndLeaveRivalRowsOut()
        {
            List<EncounterCategory> kaleth = EncounterTable.Eligible(PlaceId.Kharun, PlaceId.Kaleth, WeatherKind.Clear);
            Assert.IsTrue(kaleth.Contains(EncounterCategory.WildlifeDesert));
            Assert.IsTrue(kaleth.Contains(EncounterCategory.Bandits));
            Assert.IsTrue(kaleth.Contains(EncounterCategory.Catchall));
            Assert.IsTrue(kaleth.Contains(EncounterCategory.CityKharun));
            Assert.IsTrue(kaleth.Contains(EncounterCategory.WeatherClear));
            Assert.IsFalse(kaleth.Contains(EncounterCategory.WildlifeSea));
            Assert.IsFalse(kaleth.Contains(EncounterCategory.WildlifeMountain));
            Assert.AreEqual(5, kaleth.Count);

            List<EncounterCategory> sea = EncounterTable.Eligible(PlaceId.Kaleth, PlaceId.Zamath, WeatherKind.Heat);
            Assert.IsTrue(sea.Contains(EncounterCategory.WildlifeSea));
            Assert.IsTrue(sea.Contains(EncounterCategory.CityZamath));
            Assert.IsTrue(sea.Contains(EncounterCategory.WeatherHeat));
            Assert.IsFalse(sea.Contains(EncounterCategory.WildlifeMountain));
            Assert.IsFalse(sea.Contains(EncounterCategory.CityKharun));

            List<EncounterCategory> mountain = EncounterTable.Eligible(PlaceId.Kharun, PlaceId.Draven, WeatherKind.Sandstorm);
            Assert.IsTrue(mountain.Contains(EncounterCategory.WildlifeMountain));
            Assert.IsTrue(mountain.Contains(EncounterCategory.CityKharun));
            Assert.IsTrue(mountain.Contains(EncounterCategory.WeatherSandstorm));
            Assert.IsFalse(mountain.Contains(EncounterCategory.WeatherClear));
            Assert.IsFalse(mountain.Contains(EncounterCategory.WildlifeSea));

            List<EncounterCategory> both = EncounterTable.Eligible(PlaceId.Veythar, PlaceId.Ghorath, WeatherKind.Wind);
            Assert.IsTrue(both.Contains(EncounterCategory.CityVeythar));
            Assert.IsTrue(both.Contains(EncounterCategory.CityGhorath));
            Assert.IsTrue(both.Contains(EncounterCategory.WildlifeMountain));
            Assert.IsTrue(both.Contains(EncounterCategory.WeatherWind));

            var from = new List<PlaceId>();
            var to = new List<PlaceId>();
            RoadGraph.CopyRoads(from, to);
            Assert.AreEqual(from.Count, EncounterTable.Routes.Count);
            var weather = new[] { WeatherKind.Clear, WeatherKind.Heat, WeatherKind.Wind, WeatherKind.Sandstorm };
            for (int i = 0; i < from.Count; i++)
            {
                Assert.IsTrue(EncounterTable.HasRoute(from[i], to[i]));
                Assert.IsTrue(RoadGraph.Connects(from[i], to[i]));
                for (int w = 0; w < weather.Length; w++)
                {
                    List<EncounterCategory> pool = EncounterTable.Eligible(from[i], to[i], weather[w]);
                    Assert.IsFalse(pool.Contains(EncounterCategory.RivalHouses));
                    Assert.IsFalse(pool.Contains(EncounterCategory.AncientMysteries));
                }
            }

            for (int i = 0; i < EncounterTable.Routes.Count; i++)
                Assert.IsTrue(RoadGraph.Connects(EncounterTable.Routes[i].A, EncounterTable.Routes[i].B));
        }

        [Test]
        public void GhulAndGhorathShareAWildlifeSeaRoad()
        {
            Assert.IsTrue(RoadGraph.Connects(PlaceId.Ghul, PlaceId.Ghorath));
            Assert.IsFalse(RoadGraph.IsSea(PlaceId.Ghul, PlaceId.Ghorath));
            Assert.Greater(RoadGraph.Hours(PlaceId.Ghul, PlaceId.Ghorath), 0f);
            IReadOnlyList<Vector2> samples = RoadGraph.Samples(PlaceId.Ghul, PlaceId.Ghorath);
            Assert.AreEqual(RoadGraph.DockPoint(PlaceId.Ghul), samples[0]);
            Assert.AreEqual(RoadGraph.DockPoint(PlaceId.Ghorath), samples[samples.Count - 1]);
            List<EncounterCategory> pool = EncounterTable.Eligible(PlaceId.Ghul, PlaceId.Ghorath, WeatherKind.Clear);
            Assert.IsTrue(pool.Contains(EncounterCategory.WildlifeSea));
            Assert.IsTrue(pool.Contains(EncounterCategory.CityGhorath));
            Assert.IsFalse(pool.Contains(EncounterCategory.WildlifeMountain));
            Assert.IsFalse(pool.Contains(EncounterCategory.CityKharun));
        }

        [Test]
        public void ALegChecksOnceAndTheCardWaitsForItsHour()
        {
            var session = new CaravanSession();
            Assert.AreEqual(0, session.StretchChecks);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            Assert.AreEqual(1, session.StretchChecks);
            Assert.IsFalse(session.EncounterPending);
            Assert.IsFalse(session.HoldTravel);
            Assert.AreEqual(0f, session.TravelDistance, 0.001f);
            Assert.AreEqual(0f, session.LegTravelHours, 0.001f);

            if (session.EncounterHour >= 0f)
            {
                Assert.IsNull(session.EncounterId);
                Assert.GreaterOrEqual(session.EncounterHour, 1f);
                Assert.LessOrEqual(session.EncounterHour, 6f);
                for (int i = 0; i < 4000 && !session.EncounterPending; i++)
                    session.Tick(0.25f);
                Assert.IsTrue(session.EncounterPending);
                Assert.IsTrue(session.HoldTravel);
                Assert.AreEqual(session.EncounterHour, session.LegTravelHours, 0.05f);
                Assert.Greater(session.TravelDistance, 0f);
            }

            if (session.EncounterPending)
            {
                Assert.IsTrue(EncounterCards.IsRoadPool(session.EncounterId));
                Assert.AreEqual(2, session.EncounterChoiceCount);
                Assert.IsFalse(session.EncounterResolved);
                int coin = session.Coin;
                float travel = session.TravelHours;
                float water = session.WaterLitres;
                float food = session.RationKg;
                float hour = session.Hour;
                int day = session.Day;
                float distance = session.TravelDistance;
                session.ContinueEncounter();
                Assert.IsTrue(session.EncounterPending);
                Assert.AreEqual(coin, session.Coin);
                Assert.AreEqual(travel, session.TravelHours, 0.001f);
                Assert.AreEqual(water, session.WaterLitres, 0.001f);
                Assert.AreEqual(food, session.RationKg, 0.001f);
                Assert.AreEqual(hour, session.Hour, 0.001f);
                Assert.AreEqual(day, session.Day);
                Assert.AreEqual(distance, session.TravelDistance, 0.001f);
                session.SkipEncounter();
            }

            var stub = new CaravanSession();
            int stubCoin = stub.Coin;
            stub.OpenEncounter("WD-01");
            stub.ContinueEncounter();
            Assert.IsFalse(stub.EncounterPending);
            Assert.AreEqual(stubCoin, stub.Coin);

            for (int i = 0; i < 4000 && !session.CampPending; i++)
            {
                Release(session);
                session.Tick(0.25f);
            }

            Assert.IsTrue(session.CampPending);
            Assert.AreEqual(1, session.StretchChecks);
            session.MakeCamp();
            Assert.AreEqual(1, session.StretchChecks);
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.AreEqual(1, session.StretchChecks);
            Assert.IsFalse(session.EncounterPending);

            var sea = new CaravanSession();
            sea.ShotDockAt(PlaceId.Zamath);
            Assert.IsTrue(sea.DepartTo(PlaceId.Neth));
            Assert.AreEqual(0, sea.StretchChecks);
            Assert.IsFalse(sea.EncounterPending);
            Assert.AreEqual(-1f, sea.EncounterHour, 0.001f);
            Assert.AreEqual(PlaceId.Neth, sea.Dock);
        }

        [Test]
        public void EncounterCardsLoadSixteenWrittenRows()
        {
            IReadOnlyList<EncounterCard> cards = EncounterCards.All;
            Assert.AreEqual(16, cards.Count);
            var seen = new HashSet<string>();
            for (int i = 0; i < cards.Count; i++)
            {
                EncounterCard card = cards[i];
                Assert.IsTrue(seen.Add(card.id));
                Assert.IsFalse(string.IsNullOrEmpty(card.title));
                Assert.IsFalse(string.IsNullOrEmpty(card.scene));
                Assert.AreEqual(2, card.choices.Length);
                for (int c = 0; c < card.choices.Length; c++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(card.choices[c].label));
                    Assert.AreEqual(2, card.choices[c].outcomes.Length);
                    int weight = card.choices[c].outcomes[0].weight + card.choices[c].outcomes[1].weight;
                    Assert.AreEqual(100, weight);
                }
            }

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data", "config", "encounter_cards.json"));
            string bad = File.ReadAllText(path).Replace("\"skillTag\": \"Scout\"", "\"skillTag\": \"Courier\"");
            Assert.Throws<System.InvalidOperationException>(() => EncounterCards.LoadText(bad));
            Assert.AreEqual(16, EncounterCards.All.Count);
        }

        [Test]
        public void RoadFiltersKeepWrittenCardsOnTheirRoads()
        {
            Assert.IsTrue(EncounterCards.Fits(EncounterCards.Find("CA-03"), PlaceId.Kharun, PlaceId.Draven, 8f, flag => false));
            Assert.IsTrue(EncounterCards.Fits(EncounterCards.Find("BA-01"), PlaceId.Kharun, PlaceId.Draven, 8f, flag => false));
            Assert.IsFalse(EncounterCards.Fits(EncounterCards.Find("CA-01"), PlaceId.Kharun, PlaceId.Draven, 20f, flag => false));
            EncounterCard kulva = EncounterCards.Find("CZ-03");
            Assert.IsTrue(EncounterCards.Fits(kulva, PlaceId.Kaleth, PlaceId.Zamath, 10f, flag => false));
            Assert.IsTrue(EncounterCards.Fits(kulva, PlaceId.Zamath, PlaceId.Southmark, 10f, flag => false));
            Assert.IsFalse(EncounterCards.Fits(kulva, PlaceId.Kharun, PlaceId.Draven, 10f, flag => false));
            Assert.IsFalse(EncounterCards.Fits(kulva, PlaceId.Kaleth, PlaceId.Zamath, 10f, flag => flag == "prophet_ignored"));
            Assert.AreEqual("scripted", EncounterCards.Find("FU-01").pool);
            Assert.IsFalse(EncounterCards.IsRoadPool("FU-01"));

            var random = new System.Random(3);
            List<EncounterCategory> pool = EncounterTable.Eligible(PlaceId.Kharun, PlaceId.Draven, WeatherKind.Clear);
            for (int i = 0; i < 24; i++)
            {
                string id = EncounterCards.Pick(pool, PlaceId.Kharun, PlaceId.Draven, 8f, flag => false, random);
                EncounterCard card = EncounterCards.Find(id);
                EncounterCategory category = EncounterCards.CategoryOf(card);
                Assert.IsTrue(category == EncounterCategory.Catchall || category == EncounterCategory.Bandits);
                Assert.AreNotEqual("FU-01", id);
                Assert.AreNotEqual("BA-04", id);
                Assert.AreNotEqual("WD-01", id);
            }
        }

        [Test]
        public void BanditsTakeTheMostValuableUnitOrCoin()
        {
            var loaded = new CaravanSession();
            loaded.GiveCargo(GoodId.ShimmersteelJars, 1f);
            loaded.GiveCargo(GoodId.ShimmersteelJars, 0.4f);
            loaded.OpenEncounter("BA-01");
            loaded.ResolveOutcome(0, "a2");
            Assert.AreEqual(1, loaded.CargoCount);
            Assert.AreEqual(0.4f, loaded.CargoIntegrityAt(0), 0.001f);

            var empty = new CaravanSession();
            int coin = empty.Coin;
            empty.OpenEncounter("BA-03");
            empty.ResolveOutcome(0, "a2");
            Assert.AreEqual(0, empty.CargoCount);
            Assert.AreEqual(coin - 25, empty.Coin);
        }

        [Test]
        public void TheGodUnderTheWaterReplacesTheRollOnlyAfterKulvaIsIgnored()
        {
            var plain = new CaravanSession();
            plain.ShotDockAt(PlaceId.Kaleth);
            Assert.IsTrue(plain.DepartTo(PlaceId.Zamath));
            Assert.AreNotEqual("FU-01", plain.EncounterId);
            Assert.AreEqual(1, plain.StretchChecks);

            var armed = new CaravanSession();
            armed.SetRunFlag("prophet_ignored");
            armed.ShotDockAt(PlaceId.Kaleth);
            Assert.IsTrue(armed.DepartTo(PlaceId.Zamath));
            Assert.AreEqual("FU-01", armed.EncounterId);
            Assert.GreaterOrEqual(armed.EncounterHour, 1f);
            Assert.LessOrEqual(armed.EncounterHour, 6f);
            Assert.IsFalse(armed.EncounterPending);
            Assert.AreEqual(1, armed.StretchChecks);

            var other = new CaravanSession();
            other.SetRunFlag("prophet_ignored");
            Assert.IsTrue(other.DepartTo(PlaceId.Draven));
            Assert.AreNotEqual("FU-01", other.EncounterId);
            Assert.IsTrue(other.HasRunFlag("prophet_ignored"));

            var sea = new CaravanSession();
            sea.SetRunFlag("prophet_ignored");
            sea.ShotDockAt(PlaceId.Zamath);
            Assert.IsTrue(sea.DepartTo(PlaceId.Neth));
            Assert.AreEqual(0, sea.StretchChecks);
            Assert.AreNotEqual("FU-01", sea.EncounterId);
            Assert.IsTrue(sea.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(PlaceId.Neth, sea.Dock);

            var resolved = new CaravanSession();
            resolved.SetRunFlag("prophet_ignored");
            resolved.GiveCargo(GoodId.Water, 1f);
            resolved.GiveCargo(GoodId.ShimmersteelJars, 1f);
            resolved.OpenEncounter("FU-01");
            resolved.ResolveOutcome(0, "a2");
            Assert.IsFalse(resolved.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(GoodId.Water, resolved.CargoGoodAt(0));
            Assert.AreEqual(1f, resolved.CargoIntegrityAt(0), 0.001f);
            Assert.AreEqual(0.75f, resolved.CargoIntegrityAt(1), 0.001f);
        }

        [Test]
        public void CardKitLossStopsAtTheSixHourReserve()
        {
            var session = new CaravanSession();
            session.ChooseRoadPace(RoadPace.Steady);
            float kit = TravelScale.Kit(RoadPace.Steady);
            float waterReserve = (6f / EconomyRates.DayHours) * session.DailyWater * kit;
            float foodReserve = (6f / EconomyRates.DayHours) * session.DailyRations * kit;
            session.ApplyCardKit(-1000f, -1000f);
            Assert.AreEqual(waterReserve, session.WaterLitres, 0.02f);
            Assert.AreEqual(foodReserve, session.RationKg, 0.02f);
            session.ApplyCardKit(-5f, -5f);
            Assert.AreEqual(waterReserve, session.WaterLitres, 0.02f);
            Assert.AreEqual(foodReserve, session.RationKg, 0.02f);
            session.SetKit(1f, 0.5f);
            session.ApplyCardKit(-5f, -5f);
            Assert.AreEqual(1f, session.WaterLitres, 0.001f);
            Assert.AreEqual(0.5f, session.RationKg, 0.001f);
        }

        [Test]
        public void HeedingKulvaBanksAMapLeadAndAFriendCamps()
        {
            var heed = new CaravanSession();
            heed.OpenEncounter("CZ-03");
            heed.ResolveOutcome(0, "a1");
            Assert.IsTrue(heed.HasRunFlag("prophet_heeded"));
            Assert.IsFalse(heed.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(1, heed.MapPieceLeads);
            heed.OpenEncounter("CZ-03");
            heed.ResolveOutcome(0, "a1");
            Assert.AreEqual(1, heed.MapPieceLeads);

            var friend = new CaravanSession();
            friend.SetFriendOfBandits(true);
            friend.SetKit(10f, 1f);
            friend.GiveCargo(GoodId.ShimmersteelJars, 1f);
            friend.OpenEncounter("BA-01");
            friend.ResolveOutcome(0, "t1");
            Assert.IsFalse(friend.HasSkill("Guard"));
            Assert.IsTrue(friend.HasTrait("FriendOfBandits"));
            Assert.AreEqual(1, friend.CargoCount);
            Assert.IsTrue(friend.CampPending);
            Assert.AreEqual(friend.KitWaterCapacity, friend.WaterLitres, 0.01f);
            Assert.AreEqual(friend.KitRationCapacity, friend.RationKg, 0.01f);
        }

        [Test]
        public void StarterPacksHoldFourSlotsAndWeightTen()
        {
            Assert.AreEqual(8, EconomyRates.WagonSpace);
            Assert.AreEqual(20, EconomyRates.WagonWeight);
            Assert.AreEqual(1, EconomyCatalog.Space(GoodId.LivingLumber));
            Assert.AreEqual(EconomyCatalog.Space(GoodId.Water), EconomyCatalog.Space(GoodId.SeaWater));

            var heavy = new CaravanSession();
            heavy.SetShelf(PlaceId.Kharun, GoodId.TitansHeartRock, 12);
            for (int i = 0; i < 2; i++)
                Assert.IsTrue(heavy.TryBuy(GoodId.TitansHeartRock));
            Assert.AreEqual(2, heavy.CargoOf(GoodId.TitansHeartRock));
            Assert.AreEqual(2, heavy.CargoSpace());
            Assert.AreEqual(10, heavy.CargoWeight());
            Assert.LessOrEqual(10 + 5, EconomyRates.WagonWeight);
            Assert.LessOrEqual(3, EconomyRates.WagonSpace);
            Assert.Less(heavy.CargoSpace(), EconomyRates.WagonSpace);
            Assert.GreaterOrEqual(heavy.Coin, heavy.BuyCost(PlaceId.Kharun, GoodId.TitansHeartRock));
            Assert.Greater(heavy.StockOf(PlaceId.Kharun, GoodId.TitansHeartRock), 0);
            Assert.IsFalse(heavy.CanBuy(GoodId.TitansHeartRock));
            Assert.IsFalse(heavy.TryBuy(GoodId.TitansHeartRock));
            Assert.AreEqual(2, heavy.CargoOf(GoodId.TitansHeartRock));

            var light = new CaravanSession();
            light.SetShelf(PlaceId.Kharun, GoodId.MorRoot, 20);
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(light.TryBuy(GoodId.MorRoot));
            Assert.AreEqual(4, light.CargoSpace());
            Assert.AreEqual(4, light.CargoWeight());
            Assert.LessOrEqual(5, EconomyRates.WagonSpace);
            Assert.LessOrEqual(light.CargoWeight(), EconomyRates.WagonWeight);
            Assert.GreaterOrEqual(light.Coin, light.BuyCost(PlaceId.Kharun, GoodId.MorRoot));
            Assert.Greater(light.StockOf(PlaceId.Kharun, GoodId.MorRoot), 0);
            Assert.IsFalse(light.CanBuy(GoodId.MorRoot));
            Assert.IsFalse(light.TryBuy(GoodId.MorRoot));

            var mixed = new CaravanSession();
            mixed.SetShelf(PlaceId.Kharun, GoodId.MorRoot, 5);
            mixed.SetShelf(PlaceId.Kharun, GoodId.PalestoneBricks, 5);
            Assert.IsTrue(mixed.TryBuy(GoodId.MorRoot));
            Assert.IsTrue(mixed.TryBuy(GoodId.PalestoneBricks));
            Assert.AreEqual(GoodId.MorRoot, mixed.CargoGoodAt(0));
            Assert.IsTrue(mixed.TrySellStack(0));
            Assert.AreEqual(0, mixed.CargoOf(GoodId.MorRoot));
            Assert.AreEqual(GoodId.PalestoneBricks, mixed.CargoGoodAt(0));

            var notes = new CaravanSession();
            notes.SetShelf(PlaceId.Kharun, GoodId.FrostIron, 0);
            notes.NoteShownBuy(GoodId.FrostIron);
            Assert.IsFalse(notes.HasLoggedBuy(GoodId.FrostIron));
            notes.SetShelf(PlaceId.Kharun, GoodId.FrostIron, 1);
            notes.NoteShownBuy(GoodId.FrostIron);
            Assert.AreEqual(notes.BuyCost(PlaceId.Kharun, GoodId.FrostIron), notes.LoggedBuy(GoodId.FrostIron));

            var dry = new CaravanSession();
            dry.ShotDockAt(PlaceId.Ghorath);
            dry.SetShelf(PlaceId.Ghorath, GoodId.Water, 4);
            Assert.IsTrue(dry.TryBuy(GoodId.Water));
            dry.NoteShownSale(0);
            Assert.IsFalse(dry.HasLoggedSale(GoodId.Water));
        }

        [Test]
        public void DemandBandsFollowTheShelfKnees()
        {
            Assert.AreEqual(1f, EconomyCatalog.DayUnit(PlaceId.Kharun, GoodId.Water), 0.001f);
            int mid = EconomyRates.OrdinaryCoverDays;
            int low = EconomyRates.FullCoverDays;
            Assert.AreEqual(DemandBand.High, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.Water, 0));
            Assert.AreEqual(DemandBand.High, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.Water, mid - 1));
            Assert.AreEqual(DemandBand.Mid, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.Water, mid));
            Assert.AreEqual(DemandBand.Mid, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.Water, low - 1));
            Assert.AreEqual(DemandBand.Low, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.Water, low));
            Assert.AreEqual(DemandBand.None, EconomyCatalog.DemandOf(PlaceId.Ghorath, GoodId.Water, 0));
            Assert.AreEqual(DemandBand.None, EconomyCatalog.DemandOf(PlaceId.Zamath, GoodId.SeaWater, 0));
            Assert.AreEqual(DemandBand.High, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.SeaWater, 0));
            Assert.AreEqual(DemandBand.None, EconomyCatalog.DemandOf(PlaceId.Kharun, GoodId.Rations, 0));
        }

        [Test]
        public void MarketSoundsHaveAWavForEveryBuyAndTheSale()
        {
            string folder = Path.Combine(Application.dataPath, "Resources", "Audio");
            foreach (GoodId good in System.Enum.GetValues(typeof(GoodId)))
            {
                string resource = MarketSounds.BuyResource(good);
                if (good == GoodId.Rations)
                {
                    Assert.IsNull(resource);
                    continue;
                }

                Assert.IsFalse(string.IsNullOrEmpty(resource));
                Assert.IsTrue(resource.StartsWith("Audio/"));
                string file = Path.Combine(folder, resource.Substring("Audio/".Length) + ".wav");
                Assert.IsTrue(File.Exists(file), file);
            }

            Assert.AreEqual("Audio/ore-market-to-caravan-ui", MarketSounds.BuyResource(GoodId.Shimmersteel));
            Assert.AreEqual("Audio/sale-scrubstone", MarketSounds.SaleResource);
            Assert.IsTrue(File.Exists(Path.Combine(folder, "sale-scrubstone.wav")));
        }

        [Test]
        public void VisionRadiiAreHalfTheFirstSight()
        {
            Assert.AreEqual(0.17f, TravelScale.VisionDay, 0.0001f);
            Assert.AreEqual(0.09f, TravelScale.VisionNight, 0.0001f);
            Assert.AreEqual(0.07f, TravelScale.VisionSandstorm, 0.0001f);
        }

        [Test]
        public void StandardCaravanMatchesTheResolvedSheet()
        {
            var session = new CaravanSession();
            AssertResolved(session, "standard");
            Assert.AreEqual(session.KitWaterCapacity, session.WaterLitres, 0.001f);
            Assert.AreEqual(session.KitRationCapacity, session.RationKg, 0.001f);
            Assert.AreEqual("40 L · 4 d", session.HudWater);
            Assert.AreEqual("16 kg · 4 d", session.HudRations);
            Assert.AreEqual(25f, EconomyRates.WaterLitresPerDay, 0.001f);
            Assert.AreEqual(10f, EconomyRates.RationsKgPerDay, 0.001f);

            float water = session.WaterLitres;
            float food = session.RationKg;
            session.SetCaravan(2, 0);
            AssertResolved(session, "duzhu");
            Assert.AreEqual(water, session.WaterLitres, 0.001f);
            Assert.AreEqual(food, session.RationKg, 0.001f);

            session.SetCaravan(3, 0);
            AssertResolved(session, "pincer");
            session.SetCaravan(2, 2);
            AssertResolved(session, "duzhu+2");
            session.SetCaravan(3, 3);
            AssertResolved(session, "big");
            session.SetCaravan(1, 2);
            AssertResolved(session, "thrask+2");

            session.SetCaravan(0, -1);
            Assert.AreEqual(1, session.MerchantMountTier);
            Assert.AreEqual(ResolvedRowOf("standard").dailyWater, session.DailyWater, 0.001f);
            session.SetCaravan(9, 1);
            Assert.AreEqual(3, session.MerchantMountTier);
            Assert.Greater(session.DailyWater, ResolvedRowOf("pincer").dailyWater);

            KitTier[] tiers = GameConfig.Kit.tiers;
            for (int i = 0; i < tiers.Length; i++)
                Assert.LessOrEqual(tiers[i].tier, 3);
        }

        [Test]
        public void ALowWaterKitPoursOneSkinOnTheRoad()
        {
            var session = new CaravanSession();
            session.GiveCargo(GoodId.Water, 1f);
            session.SetKit(8f, 16f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.AreEqual(32.375f, session.WaterLitres, 0.02f);
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.AreEqual("Poured a skin of water from the packs (0 left).", session.LastNotice);
        }

        [Test]
        public void ALowRationKitOpensOneBundleOnTheRoad()
        {
            var session = new CaravanSession();
            session.GiveCargo(GoodId.Rations, 1f);
            session.SetKit(40f, 3.2f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.AreEqual(12.95f, session.RationKg, 0.02f);
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));
            Assert.AreEqual("Opened a ration bundle from the packs (0 left).", session.LastNotice);
        }

        [Test]
        public void SeaWaterNeverPoursIntoTheKit()
        {
            var session = new CaravanSession();
            session.GiveCargo(GoodId.SeaWater, 1f);
            session.SetKit(8f, 16f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.AreEqual(1, session.CargoOf(GoodId.SeaWater));
            Assert.AreEqual(7.375f, session.WaterLitres, 0.02f);
        }

        [Test]
        public void ADockedKitDoesNotPourFromThePacks()
        {
            var session = new CaravanSession();
            session.GiveCargo(GoodId.Water, 1f);
            session.SetKit(8f, 16f);
            session.AdvanceHours(2f);
            Assert.AreEqual(8f - session.DailyWater * 2f / EconomyRates.DayHours, session.WaterLitres, 0.02f);
            Assert.AreEqual(1, session.CargoOf(GoodId.Water));
            Assert.AreEqual(string.Empty, session.LastNotice);
            Assert.IsFalse(session.DeathPending);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
        }

        [Test]
        public void AnEmptyStoreOnTheRoadIsDeathAndASoftReset()
        {
            var session = new CaravanSession();
            session.SetShelf(PlaceId.Kharun, GoodId.FrostIron, 3);
            session.NoteShownBuy(GoodId.FrostIron);
            int logged = session.LoggedBuy(GoodId.FrostIron);
            Assert.IsTrue(session.HasLoggedBuy(GoodId.FrostIron));
            session.SetRunFlag("prophet_ignored");
            session.AddPresence(PlaceId.Draven, HouseId.Zamath, 100);
            Assert.AreEqual(EconomyRates.TariffBase, session.TariffPercent(PlaceId.Draven));
            Assert.AreEqual(HouseId.Zamath, session.PatronOf(PlaceId.Draven));
            session.GiveCargo(GoodId.MorRoot, 1f);
            session.SetKit(0.05f, 0.05f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);

            Assert.IsTrue(session.DeathPending);
            Assert.IsTrue(session.Traveling);
            Assert.AreEqual(TravelScale.StartingCoin, session.Coin);
            Assert.AreEqual(1, session.CargoOf(GoodId.MorRoot));
            Assert.AreEqual("Dry", session.RescueTitle);
            Assert.AreEqual("The skins are empty. The sand is not.", session.RescueLead);
            Assert.IsFalse(string.IsNullOrEmpty(session.RescueBody));
            string firstText = session.RescueBody;
            Assert.AreEqual(1, session.Day);
            Assert.AreEqual(7f, session.Hour, 0.02f);

            session.ContinueDeath();
            Assert.IsFalse(session.DeathPending);
            Assert.IsFalse(session.Traveling);
            Assert.AreEqual(0, session.CargoCount);
            Assert.AreEqual(TravelScale.StartingCoin / 2, session.Coin);
            Assert.AreEqual(session.DailyWater, session.WaterLitres, 0.01f);
            Assert.AreEqual(session.DailyRations, session.RationKg, 0.01f);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            Assert.AreEqual(HouseId.Kharun, session.House.Value);
            Assert.AreEqual(1, session.MerchantMountTier);
            Assert.AreEqual(3, session.Day);
            Assert.AreEqual(7f, session.Hour, 0.02f);
            Assert.AreEqual(3, session.StockOf(PlaceId.Kharun, GoodId.FrostIron));
            Assert.AreEqual(logged, session.LoggedBuy(GoodId.FrostIron));
            Assert.IsTrue(session.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(EconomyRates.TariffBase, session.TariffPercent(PlaceId.Draven));
            Assert.AreEqual(HouseId.Zamath, session.PatronOf(PlaceId.Draven));
            Assert.AreEqual(1, session.RescueCount);

            session.SetKit(0f, 0f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.IsTrue(session.DeathPending);
            Assert.AreEqual("Dry", session.RescueTitle);
            Assert.AreNotEqual(firstText, session.RescueBody);
            session.ContinueDeath();
            Assert.AreEqual(2, session.RescueCount);
        }

        [Test]
        public void PaupersPurseLiftsAHalvedPurseUnderForty()
        {
            var session = new CaravanSession();
            session.SetCoin(50);
            session.SetKit(0f, 0f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            session.ContinueDeath();
            Assert.AreEqual(40, session.Coin);
        }

        [Test]
        public void ADuzhuWakesWithOneDayOfItsOwnKit()
        {
            var session = new CaravanSession();
            session.SetCaravan(2, 0);
            session.SetKit(0f, 0f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            session.ContinueDeath();
            Assert.AreEqual(2, session.MerchantMountTier);
            Assert.AreEqual(21f, session.WaterLitres, 0.01f);
            Assert.AreEqual(8.5f, session.RationKg, 0.01f);
            Assert.AreEqual(8, session.CargoSpaceCap);
            Assert.AreEqual(20, session.CargoWeightCap);
            Assert.AreEqual(HouseId.Kharun, session.House.Value);
        }

        [Test]
        public void AnEmptyKitAtADockOrOnTheSeaDoesNotKill()
        {
            var docked = new CaravanSession();
            docked.SetKit(0f, 0f);
            docked.AdvanceHours(4f);
            Assert.IsFalse(docked.DeathPending);
            Assert.AreEqual(PlaceId.Kharun, docked.Dock);
            Assert.AreEqual("Buy water", docked.HudWater);
            Assert.AreEqual("Buy food", docked.HudRations);

            var sea = new CaravanSession();
            sea.ShotDockAt(PlaceId.Zamath);
            sea.SetKit(0f, 0f);
            Assert.IsTrue(sea.DepartTo(PlaceId.Neth));
            Assert.IsFalse(sea.DeathPending);
            Assert.IsFalse(sea.Traveling);
            Assert.AreEqual(PlaceId.Neth, sea.Dock);
        }

        [Test]
        public void DepartureWarnsOnTheClockTheCaravanActuallyTravels()
        {
            var session = new CaravanSession();
            SupplyForecast careful = session.ForecastRoad(PlaceId.Brineford, RoadPace.Careful);
            Assert.IsFalse(careful.Red);
            Assert.IsFalse(careful.Sea);
            Assert.Greater(careful.DaysAfter, 0.5f);
            SupplyForecast steady = session.ForecastRoad(PlaceId.Brineford, RoadPace.Steady);
            Assert.IsFalse(steady.Red);
            Assert.Greater(steady.DaysAfter, 1.4f);
            Assert.Less(steady.DaysAfter, 2.3f);

            session.SetKit(careful.WaterNeed - 0.4f, careful.RationNeed - 0.4f);
            SupplyForecast red = session.ForecastRoad(PlaceId.Brineford, RoadPace.Careful);
            Assert.IsTrue(red.Red);
            Assert.AreEqual(1, red.HaulWater);
            Assert.AreEqual(1, red.HaulRations);
            Assert.IsTrue(session.DepartTo(PlaceId.Brineford));
            Assert.IsTrue(session.Traveling);

            var amber = new CaravanSession();
            amber.SetKit(36f, 14.5f);
            SupplyForecast warn = amber.ForecastRoad(PlaceId.Brineford, RoadPace.Careful);
            Assert.IsTrue(warn.Amber);
            Assert.IsFalse(warn.Red);

            var opening = new CaravanSession();
            opening.ShotDockAt(PlaceId.Zamath);
            SupplyForecast sea = opening.ForecastRoad(PlaceId.Neth, RoadPace.Steady);
            Assert.IsTrue(sea.Sea);
            Assert.IsFalse(sea.Red);
            Assert.IsFalse(sea.Amber);
            Assert.AreEqual(0f, sea.WaterNeed, 0.001f);
            Assert.AreEqual(0f, sea.RationNeed, 0.001f);
        }

        [Test]
        public void AWaterPackCoversTheCardFloorSoTheKitCanEmpty()
        {
            var session = new CaravanSession();
            session.GiveCargo(GoodId.Water, 1f);
            session.ApplyCardKit(-1000f, 0f);
            Assert.AreEqual(0f, session.WaterLitres, 0.001f);
            Assert.AreEqual(1, session.CargoOf(GoodId.Water));
            Assert.AreEqual(session.KitRationCapacity, session.RationKg, 0.001f);
            Assert.IsFalse(session.DeathPending);
        }

        static DayNight ChartWhen(string id)
        {
            return ChartRow(id).When;
        }

        static EncounterCategory ChartCategory(string id)
        {
            return ChartRow(id).Category;
        }

        static EncounterRow ChartRow(string id)
        {
            IReadOnlyList<EncounterRow> rows = EncounterTable.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Id == id)
                    return rows[i];
            }

            Assert.Fail("Missing chart row " + id);
            return null;
        }

        static float DistanceUntilCamp(RoadPace pace)
        {
            var session = new CaravanSession();
            session.ChooseRoadPace(pace);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
            {
                Release(session);
                session.Tick(0.25f);
            }
            Assert.IsTrue(session.CampPending);
            return session.TravelDistance;
        }

        static float KitUntilCamp(RoadPace pace)
        {
            var session = new CaravanSession();
            float water = session.WaterLitres;
            session.ChooseRoadPace(pace);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
            {
                Release(session);
                session.Tick(0.25f);
            }
            Assert.IsTrue(session.CampPending);
            return water - session.WaterLitres;
        }

        [Test]
        public void ABuyKeepsItsOwnBasis()
        {
            var session = new CaravanSession();
            session.SetCoin(4000);
            session.SetShelf(PlaceId.Kharun, GoodId.DreamLotusNectar, 30);
            int cost = session.BuyCost(PlaceId.Kharun, GoodId.DreamLotusNectar);
            Assert.IsTrue(session.TryBuy(GoodId.DreamLotusNectar));
            Assert.AreEqual(cost, session.BasisAt(0));
            session.ApplyIntegrityHit(GoodId.DreamLotusNectar, 0.15f);
            Assert.AreEqual(cost, session.BasisAt(0));
            session.SetShelf(PlaceId.Kharun, GoodId.DreamLotusNectar, 1);
            int later = session.BuyCost(PlaceId.Kharun, GoodId.DreamLotusNectar);
            Assert.AreNotEqual(cost, later);
            Assert.IsTrue(session.TryBuy(GoodId.DreamLotusNectar));
            Assert.AreEqual(later, session.BasisAt(1));
            Assert.Less(session.CargoIntegrityAt(0), session.CargoIntegrityAt(1));

            session.ShotDockAt(PlaceId.Zamath);
            session.SetShelf(PlaceId.Zamath, GoodId.DreamLotusNectar, 0);
            int net = session.SellValue(PlaceId.Zamath, GoodId.DreamLotusNectar);
            int profit = net - cost;
            Assert.Greater(profit, 0);
            float before = session.KharunPoints;
            Assert.IsTrue(session.TrySell(GoodId.DreamLotusNectar));
            float divisor = EconomyRates.StandingCoinPerPointBase;
            Assert.AreEqual(before + profit / divisor, session.KharunPoints, 0.001f);
            Assert.AreEqual(later, session.BasisAt(0));
            Assert.AreEqual(0, session.GetKharunRank());
        }

        [Test]
        public void FillsAndPoursAwardNoStanding()
        {
            var session = new CaravanSession();
            session.SetCoin(400);
            session.SetKit(10f, 16f);
            float points = session.KharunPoints;
            Assert.IsTrue(session.TryFillWater());
            Assert.AreEqual(points, session.KharunPoints, 0.001f);
            session.SetKit(40f, 4f);
            Assert.IsTrue(session.TryFillFood());
            Assert.AreEqual(points, session.KharunPoints, 0.001f);

            session.GiveCargo(GoodId.Water, 1f);
            Assert.AreEqual(-1, session.BasisAt(0));
            session.SetKit(8f, 16f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.AreEqual(points, session.KharunPoints, 0.001f);
            Assert.IsFalse(session.KharunFlipSeen(PlaceId.Draven));

            var food = new CaravanSession();
            food.GiveCargo(GoodId.Rations, 1f);
            food.SetKit(40f, 3.2f);
            Assert.IsTrue(food.DepartTo(PlaceId.Draven));
            food.SpendRoadHours(1f);
            Assert.AreEqual(0, food.CargoOf(GoodId.Rations));
            Assert.AreEqual(0f, food.KharunPoints, 0.001f);
        }

        [Test]
        public void AProfitableSaleAnywhereRaisesKharunPoints()
        {
            var kharun = new CaravanSession();
            float city = SellDear(kharun, PlaceId.Kharun, PlaceId.Zamath);
            Assert.Greater(city, 0f);
            Assert.AreEqual(city, kharun.KharunPoints, 0.001f);
            Assert.IsFalse(kharun.KharunFlipSeen(PlaceId.Zamath));
            Assert.IsFalse(kharun.KharunFlipSeen(PlaceId.Kharun));

            var rival = new CaravanSession();
            float away = SellDear(rival, PlaceId.Draven, PlaceId.Zamath);
            Assert.Greater(away, 0f);
            Assert.AreEqual(away, rival.KharunPoints, 0.001f);
            Assert.AreEqual(12, rival.TariffPercent(PlaceId.Zamath));

            var held = new CaravanSession();
            held.AddPresence(PlaceId.Draven, HouseId.Kharun, 80);
            Assert.AreEqual(HouseId.Kharun, held.PatronOf(PlaceId.Draven));
            Assert.IsFalse(held.KharunFlipSeen(PlaceId.Draven));
            float home = SellDear(held, PlaceId.Kharun, PlaceId.Draven);
            Assert.Greater(home, 0f);
            Assert.AreEqual(home, held.KharunPoints, 0.001f);
            Assert.IsFalse(held.KharunFlipSeen(PlaceId.Draven));
            Assert.AreEqual(0, held.TariffPercent(PlaceId.Draven));

            var open = new CaravanSession();
            Assert.IsNull(open.PatronOf(PlaceId.Draven));
            float flipped = SellDear(open, PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(flipped + EconomyRates.StandingFlipBonus, open.KharunPoints, 0.001f);
            Assert.IsTrue(open.KharunFlipSeen(PlaceId.Draven));
            Assert.AreEqual(HouseId.Kharun, open.PatronOf(PlaceId.Draven));
        }

        [Test]
        public void EighteenHundredCoinOfProfitReachesRankOne()
        {
            var session = new CaravanSession();
            float profitCoin = 0f;
            float points = 0f;
            for (int n = 0; n < 40 && session.GetKharunRank() < 1; n++)
            {
                int rankBefore = session.GetKharunRank();
                Assert.AreEqual(0, rankBefore);
                float added = SellDear(session, PlaceId.Draven, PlaceId.Kharun);
                Assert.Greater(added, 0f);
                profitCoin += added * (EconomyRates.StandingCoinPerPointBase + EconomyRates.StandingCoinPerPointStep * rankBefore);
                points += added;
                Assert.AreEqual(points, session.KharunPoints, 0.02f);
                Assert.IsFalse(session.KharunFlipSeen(PlaceId.Kharun));
            }

            Assert.GreaterOrEqual(profitCoin, 1800f);
            Assert.GreaterOrEqual(session.KharunPoints, 100f);
            Assert.AreEqual(1, session.GetKharunRank());
            Assert.IsFalse(session.KharunFlipSeen(PlaceId.Draven));
        }

        [Test]
        public void StandingSkipsLossesResalesFindsAndRations()
        {
            var loss = new CaravanSession();
            loss.SetCoin(4000);
            loss.ShotDockAt(PlaceId.Kharun);
            loss.SetShelf(PlaceId.Kharun, GoodId.DreamLotusNectar, 1);
            Assert.IsTrue(loss.TryBuy(GoodId.DreamLotusNectar));
            loss.ShotDockAt(PlaceId.Zamath);
            loss.SetShelf(PlaceId.Zamath, GoodId.DreamLotusNectar, 40);
            int basis = loss.BasisAt(0);
            int net = loss.SellValueAt(0);
            Assert.LessOrEqual(net, basis);
            Assert.IsTrue(loss.TrySell(GoodId.DreamLotusNectar));
            Assert.AreEqual(0f, loss.KharunPoints, 0.001f);

            var resale = new CaravanSession();
            resale.SetCoin(4000);
            resale.ShotDockAt(PlaceId.Draven);
            resale.SetShelf(PlaceId.Draven, GoodId.ShimmersteelJars, 6);
            Assert.IsTrue(resale.TryBuy(GoodId.ShimmersteelJars));
            Assert.IsTrue(resale.TrySell(GoodId.ShimmersteelJars));
            Assert.AreEqual(0f, resale.KharunPoints, 0.001f);
            Assert.IsFalse(resale.KharunFlipSeen(PlaceId.Draven));
            Assert.IsNull(resale.PatronOf(PlaceId.Draven));

            var found = new CaravanSession();
            found.GiveCargo(GoodId.ShimmersteelJars, 1f);
            Assert.AreEqual(-1, found.BasisAt(0));
            found.ShotDockAt(PlaceId.Kharun);
            int purse = found.Coin;
            int sold = found.SellValueAt(0);
            Assert.IsTrue(found.TrySellStack(0));
            Assert.AreEqual(purse + sold, found.Coin);
            Assert.AreEqual(0f, found.KharunPoints, 0.001f);
            Assert.IsFalse(found.KharunFlipSeen(PlaceId.Kharun));
        }

        [Test]
        public void TheFirstPlayerFlipPaysOnce()
        {
            var village = new CaravanSession();
            float sale = SellDear(village, PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(sale + EconomyRates.StandingFlipBonus, village.KharunPoints, 0.001f);
            Assert.IsTrue(village.KharunFlipSeen(PlaceId.Draven));
            float hold = village.InfluenceOf(PlaceId.Draven, HouseId.Kharun);
            village.AddPresence(PlaceId.Draven, HouseId.Zamath, Mathf.CeilToInt(hold) + 1);
            Assert.AreEqual(HouseId.Zamath, village.PatronOf(PlaceId.Draven));
            float again = SellDear(village, PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(sale + EconomyRates.StandingFlipBonus + again, village.KharunPoints, 0.02f);
            Assert.IsTrue(village.KharunFlipSeen(PlaceId.Draven));
            Assert.AreEqual(HouseId.Kharun, village.PatronOf(PlaceId.Draven));

            var post = new CaravanSession();
            float west = SellDear(post, PlaceId.Kharun, PlaceId.Westmark);
            Assert.AreEqual(west + EconomyRates.StandingFlipBonus, post.KharunPoints, 0.001f);
            Assert.IsTrue(post.KharunFlipSeen(PlaceId.Westmark));
            Assert.AreEqual(HouseId.Kharun, post.PatronOf(PlaceId.Westmark));

            var npc = new CaravanSession();
            npc.AddPresence(PlaceId.Draven, HouseId.Kharun, 40);
            Assert.AreEqual(HouseId.Kharun, npc.PatronOf(PlaceId.Draven));
            Assert.AreEqual(0f, npc.KharunPoints, 0.001f);
            Assert.IsFalse(npc.KharunFlipSeen(PlaceId.Draven));
            npc.AddPresence(PlaceId.Draven, HouseId.Zamath, 200);
            Assert.AreEqual(HouseId.Zamath, npc.PatronOf(PlaceId.Draven));
            float later = SellDear(npc, PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(later + EconomyRates.StandingFlipBonus, npc.KharunPoints, 0.001f);
            Assert.IsTrue(npc.KharunFlipSeen(PlaceId.Draven));
        }

        [Test]
        public void RankCapsAtTenAndDoesNotDrop()
        {
            var session = new CaravanSession();
            Assert.AreEqual(0, session.GetKharunRank());
            Assert.AreEqual("House Kharûn  ·  Rank 0  ·  0 / 100", session.StandingLine());
            session.SetKharunPoints(540f);
            Assert.AreEqual(5, session.GetKharunRank());
            Assert.AreEqual("House Kharûn  ·  Rank 5  ·  40 / 100", session.StandingLine());
            Assert.IsTrue(string.IsNullOrEmpty(session.RankUpNotice));
            session.SetKharunPoints(2500f);
            Assert.AreEqual(10, session.GetKharunRank());
            Assert.AreEqual(EconomyRates.StandingCap, session.GetKharunRank());
            Assert.AreEqual("House Kharûn  ·  Rank 10", session.StandingLine());
            float points = session.KharunPoints;
            session.AddKharunPoints(80f);
            Assert.AreEqual(points + 80f, session.KharunPoints, 0.001f);
            Assert.AreEqual(10, session.GetKharunRank());
            session.AddKharunPoints(0f);
            Assert.AreEqual(points + 80f, session.KharunPoints, 0.001f);
            session.SetCoin(4000);
            session.ShotDockAt(PlaceId.Kharun);
            session.SetShelf(PlaceId.Kharun, GoodId.DreamLotusNectar, 1);
            Assert.IsTrue(session.TryBuy(GoodId.DreamLotusNectar));
            session.ShotDockAt(PlaceId.Zamath);
            session.SetShelf(PlaceId.Zamath, GoodId.DreamLotusNectar, 40);
            int basis = session.BasisAt(0);
            int net = session.SellValueAt(0);
            Assert.LessOrEqual(net, basis);
            Assert.IsTrue(session.TrySell(GoodId.DreamLotusNectar));
            Assert.AreEqual(points + 80f, session.KharunPoints, 0.001f);
        }

        [Test]
        public void ASoftResetKeepsKharunStanding()
        {
            var session = new CaravanSession();
            SellDear(session, PlaceId.Kharun, PlaceId.Draven);
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Draven));
            session.ShotDockAt(PlaceId.Kharun);
            session.SetKharunPoints(250f);
            session.ClearRankUpNotice();
            Assert.AreEqual(2, session.GetKharunRank());
            session.GiveCargo(GoodId.MorRoot, 1f);
            Assert.AreEqual(-1, session.BasisAt(session.CargoCount - 1));
            session.SetKit(0f, 0f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.IsTrue(session.DeathPending);
            session.ContinueDeath();
            Assert.AreEqual(250f, session.KharunPoints, 0.001f);
            Assert.AreEqual(2, session.GetKharunRank());
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Draven));
            Assert.AreEqual(0, session.CargoCount);
            Assert.IsTrue(string.IsNullOrEmpty(session.RankUpNotice));
        }

        static float SellDear(CaravanSession session, PlaceId buyAt, PlaceId sellAt)
        {
            session.SetCoin(8000);
            session.ShotDockAt(buyAt);
            session.SetShelf(buyAt, GoodId.DreamLotusNectar, 30);
            Assert.IsTrue(session.TryBuy(GoodId.DreamLotusNectar), "buy at " + buyAt);
            int index = session.CargoCount - 1;
            int basis = session.BasisAt(index);
            int rankBefore = session.GetKharunRank();
            session.ShotDockAt(sellAt);
            session.SetShelf(sellAt, GoodId.DreamLotusNectar, 0);
            int net = session.SellValueAt(index);
            int profit = net - basis;
            Assert.Greater(profit, 0, buyAt + " to " + sellAt);
            float before = session.KharunPoints;
            bool seen = session.KharunFlipSeen(sellAt);
            HouseId? patronBefore = session.PatronOf(sellAt);
            Assert.IsTrue(session.TrySellStack(index));
            float divisor = EconomyRates.StandingCoinPerPointBase + EconomyRates.StandingCoinPerPointStep * rankBefore;
            float added = profit / divisor;
            float flip = !seen && patronBefore != HouseId.Kharun && session.KharunFlipSeen(sellAt)
                ? EconomyRates.StandingFlipBonus
                : 0f;
            Assert.AreEqual(before + added + flip, session.KharunPoints, 0.02f);
            return added;
        }

        static float Elapsed(CaravanSession session, int day, float hour)
        {
            return (session.Day - day) * EconomyRates.DayHours + (session.Hour - hour);
        }

        static void AssertResolved(CaravanSession session, string id)
        {
            ResolvedRow row = ResolvedRowOf(id);
            Assert.AreEqual(row.merchantMountTier, session.MerchantMountTier);
            Assert.AreEqual(row.dailyWater, session.DailyWater, 0.001f);
            Assert.AreEqual(row.dailyRations, session.DailyRations, 0.001f);
            Assert.AreEqual(row.kitWater, session.KitWaterCapacity, 0.001f);
            Assert.AreEqual(row.kitRations, session.KitRationCapacity, 0.001f);
            Assert.AreEqual(row.cargoSpace, session.CargoSpaceCap);
            Assert.AreEqual(row.cargoWeight, session.CargoWeightCap);
        }

        static ResolvedRow ResolvedRowOf(string id)
        {
            string path = Path.Combine(Application.dataPath, "..", "data", "config", "caravans_resolved.json");
            ResolvedSheet sheet = JsonUtility.FromJson<ResolvedSheet>(File.ReadAllText(path));
            Assert.IsNotNull(sheet);
            Assert.IsNotNull(sheet.rows);
            for (int i = 0; i < sheet.rows.Length; i++)
            {
                if (sheet.rows[i].id == id)
                    return sheet.rows[i];
            }

            Assert.Fail("Missing resolved row " + id);
            return null;
        }
    }

    [System.Serializable]
    public class ResolvedSheet
    {
        public ResolvedRow[] rows;
    }

    [System.Serializable]
    public class ResolvedRow
    {
        public string id;
        public int merchantMountTier;
        public int agents;
        public float dailyWater;
        public float dailyRations;
        public float kitWater;
        public float kitRations;
        public int cargoSpace;
        public int cargoWeight;
    }
}
