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
            Assert.AreEqual(8, session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars));
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
