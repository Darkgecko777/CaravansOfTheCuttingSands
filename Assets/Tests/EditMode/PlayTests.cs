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
        public void RivalCutFallsFromTwelveToFour()
        {
            var session = new CaravanSession();
            session.AddPresence(PlaceId.Draven, HouseId.Zamath, 80);
            session.AddPresence(PlaceId.Draven, HouseId.Thalor, 80);
            Assert.AreEqual(HouseId.Zamath, session.PatronOf(PlaceId.Draven));
            Assert.AreEqual(EconomyCatalog.CityHouse(PlaceId.Kharun), session.PatronOf(PlaceId.Kharun));

            int shelf = session.PriceOf(PlaceId.Draven, GoodId.ShimmersteelJars);
            session.SetStanding(HouseId.Zamath, 0);
            Assert.AreEqual(12, session.TariffPercent(PlaceId.Draven));
            Assert.AreEqual(shelf + Cut(shelf, 12), session.BuyCost(PlaceId.Draven, GoodId.ShimmersteelJars));
            session.SetStanding(HouseId.Zamath, 8);
            Assert.AreEqual(4, session.TariffPercent(PlaceId.Draven));
            Assert.AreEqual(shelf + Cut(shelf, 4), session.BuyCost(PlaceId.Draven, GoodId.ShimmersteelJars));
            Assert.AreEqual(0, session.TariffPercent(PlaceId.Kharun));
            Assert.AreEqual(session.PriceOf(PlaceId.Kharun, GoodId.ShimmersteelJars), session.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars));

            float before = session.InfluenceOf(PlaceId.Draven, HouseId.Zamath);
            session.TickMorning();
            Assert.AreEqual(before * 0.75f, session.InfluenceOf(PlaceId.Draven, HouseId.Zamath), 0.01f);
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
            var session = CaravanSession.AtHouse(HouseId.Kharun);
            Assert.IsTrue(session.House.HasValue);
            Assert.AreEqual(HouseId.Kharun, session.House.Value);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(TravelScale.StartingCoin, session.Coin);
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));
            Assert.IsFalse(session.Traveling);
        }

        [Test]
        public void EachHouseStartsInItsOwnCity()
        {
            Assert.AreEqual(PlaceId.Kharun, CaravanSession.AtHouse(HouseId.Kharun).Dock);
            Assert.AreEqual(PlaceId.Zamath, CaravanSession.AtHouse(HouseId.Zamath).Dock);
            Assert.AreEqual(PlaceId.Thalor, CaravanSession.AtHouse(HouseId.Thalor).Dock);
            Assert.AreEqual(PlaceId.Veythar, CaravanSession.AtHouse(HouseId.Veythar).Dock);
            Assert.AreEqual(PlaceId.Ghorath, CaravanSession.AtHouse(HouseId.Ghorath).Dock);
            Assert.AreEqual(HouseId.Zamath, CaravanSession.AtHouse(HouseId.Zamath).House.Value);
        }

        [Test]
        public void TheKitStartsFullAndFollowsTheClock()
        {
            var session = new CaravanSession();
            Assert.AreEqual(EconomyRates.CanteenLitres, session.WaterLitres, 0.001f);
            Assert.AreEqual(EconomyRates.RationBagKg, session.RationKg, 0.001f);
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Rations));

            session.AdvanceHours(EconomyRates.DayHours);
            Assert.AreEqual(EconomyRates.CanteenLitres - EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.02f);
            Assert.AreEqual(EconomyRates.RationBagKg - EconomyRates.RationsKgPerDay, session.RationKg, 0.02f);

            float water = session.WaterLitres;
            float food = session.RationKg;
            session.WaitOneHour();
            float hourShare = 1f / EconomyRates.DayHours;
            Assert.AreEqual(water - EconomyRates.WaterLitresPerDay * hourShare, session.WaterLitres, 0.02f);
            Assert.AreEqual(food - EconomyRates.RationsKgPerDay * hourShare, session.RationKg, 0.02f);
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
            Assert.AreEqual(water - kitHours / EconomyRates.DayHours * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - kitHours / EconomyRates.DayHours * EconomyRates.RationsKgPerDay, session.RationKg, 0.05f);
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
            Assert.AreEqual(EconomyRates.CanteenLitres, session.WaterLitres, 0.001f);
            Assert.AreEqual(stock - 1, session.StockOf(PlaceId.Kharun, GoodId.Water));
            Assert.AreEqual(0, session.CargoOf(GoodId.Water));
            Assert.Less(session.Coin, coin);
            Assert.IsFalse(session.CanFillWater());
            Assert.IsFalse(session.TryFillWater());

            Assert.IsTrue(session.TryFillFood());
            Assert.AreEqual(EconomyRates.RationBagKg, session.RationKg, 0.001f);
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
            session.NoteMarketShown(false);
            Assert.IsFalse(session.HasLoggedBuy(GoodId.FrostIron));

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
            Assert.AreEqual(homeSale, session.LoggedSale(GoodId.ShimmersteelJars));

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

            var zamath = CaravanSession.AtHouse(HouseId.Zamath);
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
            Assert.IsTrue(session.DepartTo(dest));
            Ride(session);
        }

        static void Release(CaravanSession session)
        {
            if (session.EncounterPending)
                session.ContinueEncounter();
        }

        static void Ride(CaravanSession session)
        {
            for (int i = 0; i < 4000 && session.Traveling; i++)
            {
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
            Assert.AreEqual(water - TravelScale.TownRestHours / EconomyRates.DayHours * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.02f);
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
            Assert.AreEqual(water - travelDays * TravelScale.DayKit * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
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
            var session = CaravanSession.AtHouse(HouseId.Zamath);
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
            Assert.AreEqual(water - kitHours * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - kitHours * EconomyRates.RationsKgPerDay, session.RationKg, 0.05f);
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
            Assert.AreEqual(water - waited * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - waited * EconomyRates.RationsKgPerDay, session.RationKg, 0.05f);
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
            Assert.AreEqual(water - waited * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - waited * EconomyRates.RationsKgPerDay, session.RationKg, 0.05f);
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
            Assert.AreEqual(water - waited * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
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
        public void TheEncounterChartHasEightyIdsAndStoresDayAndNight()
        {
            IReadOnlyList<EncounterRow> rows = EncounterTable.All;
            Assert.AreEqual(80, rows.Count);
            var seen = new HashSet<string>();
            int day = 0;
            int night = 0;
            int either = 0;
            var categories = (EncounterCategory[])System.Enum.GetValues(typeof(EncounterCategory));
            for (int c = 0; c < categories.Length; c++)
            {
                IReadOnlyList<EncounterRow> group = EncounterTable.Rows(categories[c]);
                Assert.AreEqual(5, group.Count);
                Assert.AreEqual(DayNight.Day, group[0].When);
                Assert.AreEqual(DayNight.Night, group[1].When);
                Assert.AreEqual(DayNight.Either, group[2].When);
                Assert.AreEqual(DayNight.Day, group[3].When);
                Assert.AreEqual(DayNight.Night, group[4].When);
            }

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

            Assert.AreEqual(32, day);
            Assert.AreEqual(32, night);
            Assert.AreEqual(16, either);
            Assert.AreEqual("SS-01", EncounterTable.Rows(EncounterCategory.WeatherSandstorm)[0].Id);
            Assert.AreEqual("WS-01", EncounterTable.Rows(EncounterCategory.WildlifeSea)[0].Id);
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
                Assert.GreaterOrEqual(session.EncounterHour, 1f);
                Assert.LessOrEqual(session.EncounterHour, 6f);
                for (int i = 0; i < 4000 && !session.EncounterPending; i++)
                    session.Tick(0.25f);
                Assert.IsTrue(session.EncounterPending);
                Assert.IsTrue(session.HoldTravel);
                Assert.AreEqual(session.EncounterHour, session.LegTravelHours, 0.05f);
                Assert.Greater(session.TravelDistance, 0f);
            }

            if (!session.EncounterPending)
                session.OpenEncounter("CA-03");
            string id = session.EncounterId;
            int coin = session.Coin;
            float travel = session.TravelHours;
            float water = session.WaterLitres;
            float food = session.RationKg;
            float hour = session.Hour;
            int day = session.Day;
            float distance = session.TravelDistance;
            Assert.IsTrue(session.HoldTravel);
            session.ContinueEncounter();
            Assert.IsFalse(session.EncounterPending);
            Assert.IsFalse(session.HoldTravel);
            Assert.AreEqual(coin, session.Coin);
            Assert.AreEqual(travel, session.TravelHours, 0.001f);
            Assert.AreEqual(water, session.WaterLitres, 0.001f);
            Assert.AreEqual(food, session.RationKg, 0.001f);
            Assert.AreEqual(hour, session.Hour, 0.001f);
            Assert.AreEqual(day, session.Day);
            Assert.AreEqual(distance, session.TravelDistance, 0.001f);
            Assert.IsFalse(string.IsNullOrEmpty(id));

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

            var sea = CaravanSession.AtHouse(HouseId.Zamath);
            Assert.IsTrue(sea.DepartTo(PlaceId.Neth));
            Assert.AreEqual(0, sea.StretchChecks);
            Assert.IsFalse(sea.EncounterPending);
            Assert.AreEqual(-1f, sea.EncounterHour, 0.001f);
            Assert.AreEqual(PlaceId.Neth, sea.Dock);
        }

        [Test]
        public void StarterWagonHoldsEightSlotsAndWeightTwenty()
        {
            Assert.AreEqual(8, EconomyRates.WagonSpace);
            Assert.AreEqual(20, EconomyRates.WagonWeight);
            Assert.AreEqual(1, EconomyCatalog.Space(GoodId.LivingLumber));
            Assert.AreEqual(EconomyCatalog.Space(GoodId.Water), EconomyCatalog.Space(GoodId.SeaWater));

            var heavy = new CaravanSession();
            heavy.SetShelf(PlaceId.Kharun, GoodId.TitansHeartRock, 12);
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(heavy.TryBuy(GoodId.TitansHeartRock));
            Assert.AreEqual(4, heavy.CargoOf(GoodId.TitansHeartRock));
            Assert.AreEqual(4, heavy.CargoSpace());
            Assert.AreEqual(20, heavy.CargoWeight());
            Assert.Less(heavy.CargoSpace(), EconomyRates.WagonSpace);
            Assert.GreaterOrEqual(heavy.Coin, heavy.BuyCost(PlaceId.Kharun, GoodId.TitansHeartRock));
            Assert.Greater(heavy.StockOf(PlaceId.Kharun, GoodId.TitansHeartRock), 0);
            Assert.IsFalse(heavy.CanBuy(GoodId.TitansHeartRock));
            Assert.IsFalse(heavy.TryBuy(GoodId.TitansHeartRock));
            Assert.AreEqual(4, heavy.CargoOf(GoodId.TitansHeartRock));

            var light = new CaravanSession();
            light.SetShelf(PlaceId.Kharun, GoodId.MorRoot, 20);
            for (int i = 0; i < 8; i++)
                Assert.IsTrue(light.TryBuy(GoodId.MorRoot));
            Assert.AreEqual(8, light.CargoSpace());
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

            var dry = CaravanSession.AtHouse(HouseId.Ghorath);
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

        static float Elapsed(CaravanSession session, int day, float hour)
        {
            return (session.Day - day) * EconomyRates.DayHours + (session.Hour - hour);
        }
    }
}
