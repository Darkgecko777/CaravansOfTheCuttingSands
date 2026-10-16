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
            Assert.IsTrue(session.TravelsByDay);
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
        public void AClearDayOnTheRoadSpendsOneDayOfKit()
        {
            var session = new CaravanSession();
            int day = session.Day;
            float hour = session.Hour;
            float water = session.WaterLitres;
            float food = session.RationKg;
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            Ride(session);
            float elapsed = (session.Day - day) * EconomyRates.DayHours + (session.Hour - hour);
            Assert.Greater(elapsed, 20f);
            Assert.AreEqual(water - elapsed / EconomyRates.DayHours * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
            Assert.AreEqual(food - elapsed / EconomyRates.DayHours * EconomyRates.RationsKgPerDay, session.RationKg, 0.05f);
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

        static void Ride(CaravanSession session)
        {
            for (int i = 0; i < 4000 && session.Traveling; i++)
            {
                if (session.CampPending)
                    session.MakeCamp();
                if (session.AwaitingPace)
                    session.ChooseRoadPace(session.ChosenPace, session.TravelsByDay);
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
            session.ChooseRoadPace(RoadPace.Steady, false);
            Assert.IsFalse(session.TravelsByDay);
            float water = session.WaterLitres;
            int day = session.Day;
            float hour = session.Hour;
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
                session.Tick(0.25f);

            Assert.IsTrue(session.CampPending);
            Assert.AreEqual(TravelScale.ReserveHours, session.TravelHours, 0.05f);
            float elapsed = Elapsed(session, day, hour);
            Assert.AreEqual(TravelScale.WindowHours - TravelScale.ReserveHours, elapsed, 0.05f);
            float travelDays = elapsed / EconomyRates.DayHours;
            Assert.AreEqual(water - travelDays * EconomyRates.WaterLitresPerDay, session.WaterLitres, 0.05f);
            float covered = session.TravelDistance / RoadGraph.Length(PlaceId.Kharun, PlaceId.Draven);
            Assert.AreEqual(travelDays, covered, 0.02f);

            session.MakeCamp();
            Assert.AreEqual(TravelScale.WindowHours, session.TravelHours, 0.001f);
            Assert.IsTrue(session.AwaitingPace);
            float afterCamp = Elapsed(session, day, hour);
            Assert.AreEqual(elapsed + TravelScale.CampHours, afterCamp, 0.05f);
            session.ChooseRoadPace(RoadPace.Steady, true);
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
            Assert.IsTrue(session.DepartTo(PlaceId.Neth));
            Assert.IsFalse(session.Traveling);
            Assert.AreEqual(PlaceId.Neth, session.Dock);
            Assert.AreEqual(hours, session.TravelHours, 0.001f);
        }

        static float DistanceUntilCamp(RoadPace pace)
        {
            var session = new CaravanSession();
            session.ChooseRoadPace(pace, true);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
                session.Tick(0.25f);
            Assert.IsTrue(session.CampPending);
            return session.TravelDistance;
        }

        static float KitUntilCamp(RoadPace pace)
        {
            var session = new CaravanSession();
            float water = session.WaterLitres;
            session.ChooseRoadPace(pace, true);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            for (int i = 0; i < 4000 && !session.CampPending; i++)
                session.Tick(0.25f);
            Assert.IsTrue(session.CampPending);
            return water - session.WaterLitres;
        }

        static float Elapsed(CaravanSession session, int day, float hour)
        {
            return (session.Day - day) * EconomyRates.DayHours + (session.Hour - hour);
        }
    }
}
