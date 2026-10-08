using NUnit.Framework;
using UnityEngine;

namespace Caravans.JarsRun.Tests
{
    public class JarsRunTests
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
        public void BuyingJarsKeepsTheCardCoin()
        {
            var session = new CaravanSession();
            session.NoteMarketOpened();
            int bought = 0;
            while (session.TryBuy(GoodId.ShimmersteelJars))
                bought += 1;

            Assert.Greater(bought, 0);
            Assert.GreaterOrEqual(session.Coin, ThrowawayEconomy.CardCoin);
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
        public void JarsRunChangesCoinAndEndsAtHouseChoice()
        {
            var session = new CaravanSession();
            session.NoteMarketOpened();
            Assert.IsTrue(session.TryBuy(GoodId.ShimmersteelJars));
            int paid = ThrowawayEconomy.StartingCoin - session.Coin;

            session.NotePlazaOpened();
            Assert.AreEqual(TutorialStep.LeaveForDraven, session.Step);
            Assert.IsTrue(session.Depart());
            session.ChoosePace(4);

            Pump(session, TutorialStep.RoadCard);
            Assert.AreEqual(TutorialStep.RoadCard, session.Step);
            int beforeCard = session.Coin;
            session.ResolveCard(false);
            Assert.AreEqual(beforeCard - ThrowawayEconomy.CardCoin, session.Coin);
            Assert.AreEqual(1, session.CardsResolved);
            session.ContinueFromCard();

            Pump(session, TutorialStep.ArriveDraven);
            Assert.AreEqual(PlaceId.Draven, session.Dock);
            Assert.IsFalse(session.Traveling);
            Assert.Greater(session.PriceOf(PlaceId.Draven, GoodId.ShimmersteelJars), paid);

            session.NoteSellOpened();
            Assert.IsTrue(session.TrySell(GoodId.ShimmersteelJars));
            Assert.AreEqual(TutorialStep.BuyFungus, session.Step);
            int fungusPaid = session.PriceOf(PlaceId.Draven, GoodId.FingerFungus);
            Assert.IsTrue(session.TryBuy(GoodId.FingerFungus));
            session.NotePlazaOpened();
            Assert.AreEqual(TutorialStep.LeaveForKharun, session.Step);
            Assert.IsTrue(session.Depart());
            Pump(session, TutorialStep.SellFungus);

            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(1, session.CardsResolved);
            Assert.AreEqual(TutorialStep.SellFungus, session.Step);
            int beforeSale = session.Coin;
            Assert.IsTrue(session.TrySell(GoodId.FingerFungus));
            Assert.Greater(session.Coin, beforeSale);
            Assert.Greater(session.Coin - beforeSale, fungusPaid);
            Assert.Greater(session.Coin, ThrowawayEconomy.StartingCoin);
            Assert.AreEqual(TutorialStep.HouseChoice, session.Step);
        }

        [Test]
        public void ReturnLegDoesNotOpenACard()
        {
            var session = RideToDraven();
            session.NoteSellOpened();
            session.TrySell(GoodId.ShimmersteelJars);
            session.TryBuy(GoodId.FingerFungus);
            session.NotePlazaOpened();
            session.Depart();

            for (int i = 0; i < 400 && session.Traveling; i++)
            {
                session.Tick(0.25f);
                Assert.IsFalse(session.CardPending);
            }

            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(1, session.CardsResolved);
        }

        [Test]
        public void SkipDropsTheStartingCaravanAtHouseChoice()
        {
            var session = new CaravanSession();
            session.NoteMarketOpened();
            session.TryBuy(GoodId.ShimmersteelJars);
            session.SkipTutorial();

            Assert.AreEqual(TutorialStep.HouseChoice, session.Step);
            Assert.AreEqual(ThrowawayEconomy.StartingCoin, session.Coin);
            Assert.AreEqual(0, session.CargoOf(GoodId.ShimmersteelJars));
            Assert.AreEqual(EconomyCatalog.StartingStock(PlaceId.Kharun, GoodId.ShimmersteelJars), session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars));
            Assert.IsFalse(session.Traveling);
            Assert.IsTrue(session.Skipped);
            Assert.IsFalse(session.ChooseHouse(HouseId.Zamath));
            Assert.IsTrue(session.ChooseHouse(HouseId.Kharun));
            Assert.AreEqual(TutorialStep.Free, session.Step);
            Assert.AreEqual(ThrowawayEconomy.StartingCoin, session.Coin);
        }

        [Test]
        public void CardCannotDriveCoinBelowZero()
        {
            var session = new CaravanSession();
            session.SkipTutorial();
            session.ChooseHouse(HouseId.Kharun);
            while (session.TryBuy(GoodId.Water)) { }
            while (session.TryBuy(GoodId.Rations)) { }
            while (session.TryBuy(GoodId.ShimmersteelJars)) { }
            while (session.TryBuy(GoodId.FingerFungus)) { }

            Assert.IsTrue(session.Depart());
            for (int i = 0; i < 400 && !session.CardPending && session.Traveling; i++)
                session.Tick(0.25f);

            if (session.CardPending)
                session.ResolveCard(true);

            Assert.GreaterOrEqual(session.Coin, 0);
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
            session.SkipTutorial();
            session.ChooseHouse(HouseId.Kharun);
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
            session.SkipTutorial();
            session.ChooseHouse(HouseId.Kharun);
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

        static int Cut(int price, int percent)
        {
            return (price * percent + 50) / 100;
        }

        static CaravanSession RideToDraven()
        {
            var session = new CaravanSession();
            session.NoteMarketOpened();
            session.TryBuy(GoodId.ShimmersteelJars);
            session.NotePlazaOpened();
            session.Depart();
            session.ChoosePace(4);
            Pump(session, TutorialStep.RoadCard);
            session.ResolveCard(true);
            session.ContinueFromCard();
            Pump(session, TutorialStep.ArriveDraven);
            return session;
        }

        static void Pump(CaravanSession session, TutorialStep until)
        {
            for (int i = 0; i < 400 && session.Step != until; i++)
                session.Tick(0.25f);
            Assert.AreEqual(until, session.Step);
        }
    }
}
