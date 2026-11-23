using NUnit.Framework;
using UnityEngine;

namespace Caravans.Play.Tests
{
    public class AgentTests
    {
        [Test]
        public void AgentRatesComeFromTheConfig()
        {
            Assert.AreEqual(40, EconomyRates.ScoutHire);
            Assert.AreEqual(6, EconomyRates.ScoutWage);
            Assert.AreEqual(80, EconomyRates.HagglerHire);
            Assert.AreEqual(12, EconomyRates.HagglerWage);
            Assert.AreEqual(60, EconomyRates.GuardHire);
            Assert.AreEqual(8, EconomyRates.GuardWage);
            Assert.AreEqual(0.10f, EconomyRates.HagglerSpread, 0.0001f);
            Assert.AreEqual(1f, EconomyRates.RushCargoLossChance, 0.0001f);
            Assert.AreEqual(0.15f, EconomyRates.MerchantSpread, 0.0001f);
        }

        [Test]
        public void EachRoleHiresOnceAtKharunAndDismissDoesNotRefund()
        {
            var session = new CaravanSession();
            int start = session.Coin;
            HireOnce(session, "Scout", EconomyRates.ScoutHire);
            HireOnce(session, "Haggler", EconomyRates.HagglerHire);
            HireOnce(session, "Guard", EconomyRates.GuardHire);
            Assert.AreEqual(3, session.AgentCount);
            Assert.AreEqual(start - 40 - 80 - 60, session.Coin);
            int paid = session.Coin;
            Assert.IsTrue(session.TryDismissAgent("Scout"));
            Assert.IsTrue(session.TryDismissAgent("Haggler"));
            Assert.IsTrue(session.TryDismissAgent("Guard"));
            Assert.AreEqual(paid, session.Coin);
            Assert.AreEqual(0, session.AgentCount);
            Assert.IsFalse(session.TryDismissAgent("Scout"));
            Assert.AreEqual(paid, session.Coin);
            Assert.IsFalse(session.ScoutHired);
        }

        [Test]
        public void HireAndDismissFailAwayFromKharunAndWhileTraveling()
        {
            var away = new CaravanSession();
            away.ShotDockAt(PlaceId.Draven);
            int coin = away.Coin;
            Assert.IsFalse(away.TryHireAgent("Scout"));
            Assert.IsFalse(away.TryHireAgent("Haggler"));
            Assert.IsFalse(away.TryHireAgent("Guard"));
            Assert.IsFalse(away.TryDismissAgent("Scout"));
            Assert.AreEqual(coin, away.Coin);
            Assert.IsFalse(away.ScoutHired);

            var riding = new CaravanSession();
            Assert.IsTrue(riding.TryHireAgent("Scout"));
            int left = riding.Coin;
            Assert.IsTrue(riding.DepartTo(PlaceId.Draven));
            Assert.IsTrue(riding.Traveling);
            Assert.IsFalse(riding.TryHireAgent("Haggler"));
            Assert.IsFalse(riding.TryDismissAgent("Scout"));
            Assert.AreEqual(left, riding.Coin);
            Assert.IsTrue(riding.ScoutHired);
            Assert.IsFalse(riding.HagglerHired);
        }

        [Test]
        public void DawnPaysTheScoutAndAnUnpaidAgentLeaves()
        {
            var paid = new CaravanSession();
            paid.SetCoin(90);
            Assert.IsTrue(paid.TryHireAgent("Scout"));
            Assert.AreEqual(50, paid.Coin);
            paid.TickMorning();
            Assert.AreEqual(44, paid.Coin);
            Assert.IsTrue(paid.ScoutHired);
            Assert.AreEqual(string.Empty, paid.AgentNotice);

            var broke = new CaravanSession();
            Assert.IsTrue(broke.TryHireAgent("Scout"));
            broke.SetCoin(5);
            broke.TickMorning();
            Assert.IsFalse(broke.ScoutHired);
            Assert.AreEqual(5, broke.Coin);
            Assert.AreEqual("Your scout left. You could not pay the day's wage.", broke.AgentNotice);
        }

        [Test]
        public void UnpaidAgentsLeaveInHireOrder()
        {
            var pair = new CaravanSession();
            Assert.IsTrue(pair.TryHireAgent("Scout"));
            Assert.IsTrue(pair.TryHireAgent("Guard"));
            pair.SetCoin(5);
            pair.TickMorning();
            Assert.IsFalse(pair.ScoutHired);
            Assert.IsFalse(pair.GuardHired);
            Assert.AreEqual(5, pair.Coin);
            Assert.AreEqual("Your scout and guard left. You could not pay the day's wage.", pair.AgentNotice);

            var all = new CaravanSession();
            Assert.IsTrue(all.TryHireAgent("Scout"));
            Assert.IsTrue(all.TryHireAgent("Haggler"));
            Assert.IsTrue(all.TryHireAgent("Guard"));
            all.SetCoin(5);
            all.TickMorning();
            Assert.AreEqual(0, all.AgentCount);
            Assert.AreEqual(5, all.Coin);
            Assert.AreEqual("Your scout, haggler, and guard left. You could not pay the day's wage.", all.AgentNotice);

            var ordered = new CaravanSession();
            Assert.IsTrue(ordered.TryHireAgent("Scout"));
            Assert.IsTrue(ordered.TryHireAgent("Haggler"));
            Assert.IsTrue(ordered.TryHireAgent("Guard"));
            ordered.SetCoin(6);
            ordered.TickMorning();
            Assert.IsTrue(ordered.ScoutHired);
            Assert.IsFalse(ordered.HagglerHired);
            Assert.IsFalse(ordered.GuardHired);
            Assert.AreEqual(0, ordered.Coin);
            Assert.AreEqual("Your haggler and guard left. You could not pay the day's wage.", ordered.AgentNotice);
        }

        [Test]
        public void AScoutReadsThePaceChance()
        {
            var session = new CaravanSession();
            Assert.AreEqual(string.Empty, session.ScoutLine(RoadPace.Rush, true));
            Assert.IsTrue(session.TryHireAgent("Scout"));
            Assert.AreEqual(string.Empty, session.ScoutLine(RoadPace.Rush, false));
            AssertLine(session, RoadPace.Careful, 35, "Careful");
            AssertLine(session, RoadPace.Steady, 50, "Steady");
            AssertLine(session, RoadPace.Rush, 65, "Rush");
        }

        [Test]
        public void AHagglerCutsBothSidesOfTheSpread()
        {
            var plain = new CaravanSession();
            Assert.AreEqual(0.15f, plain.MerchantSpread(), 0.0001f);
            Assert.AreEqual(EconomyRates.MerchantSpread, plain.MerchantSpread(), 0.0001f);

            var session = new CaravanSession();
            Assert.IsTrue(session.TryHireAgent("Haggler"));
            Assert.AreEqual(0.10f, session.MerchantSpread(), 0.0001f);
            int stock = session.StockOf(PlaceId.Kharun, GoodId.ShimmersteelJars);
            int buyMid = Shelf(GoodId.ShimmersteelJars, stock - 1);
            int ask = Mathf.RoundToInt(buyMid * 1.10f);
            if (ask < 1)
                ask = 1;
            Assert.AreEqual(ask, session.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars));
            session.GiveCargo(GoodId.ShimmersteelJars, 1f);
            int sellMid = Shelf(GoodId.ShimmersteelJars, stock + 1);
            int net = Mathf.Max(0, Mathf.RoundToInt(sellMid * 0.90f));
            Assert.AreEqual(net, session.SellValue(PlaceId.Kharun, GoodId.ShimmersteelJars));

            Assert.IsTrue(session.TryDismissAgent("Haggler"));
            Assert.AreEqual(0.15f, session.MerchantSpread(), 0.0001f);
            var fresh = new CaravanSession();
            Assert.AreEqual(fresh.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars), session.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars));
        }

        [Test]
        public void AGuardCleansTheFirstBadOutcomeAndANewLegRestoresIt()
        {
            var session = new CaravanSession();
            Assert.IsTrue(session.TryHireAgent("Guard"));
            session.ChooseRoadPace(RoadPace.Rush);
            session.GiveCargo(GoodId.MorRoot, 1f);
            session.GiveCargo(GoodId.MorRoot, 1f);
            float water = session.WaterLitres;
            float food = session.RationKg;
            int coin = session.Coin;
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Your guard handled it.", session.EncounterResult);
            Assert.AreEqual(2, session.CargoOf(GoodId.MorRoot));
            Assert.IsFalse(session.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(water, session.WaterLitres, 0.001f);
            Assert.AreEqual(food, session.RationKg, 0.001f);
            Assert.AreEqual(coin, session.Coin);
            Assert.AreEqual(0, session.RoadNoteCount);

            session.ResolveOutcome(1, "b1");
            Assert.IsTrue(session.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(1, session.CargoOf(GoodId.MorRoot));
            Assert.IsTrue(session.EncounterResult.EndsWith(" The rush cost you one Mor Root."));
            Assert.AreEqual(water, session.WaterLitres, 0.001f);
            Assert.AreEqual(food, session.RationKg, 0.001f);
            Assert.AreEqual(coin, session.Coin);

            session.ShotDockAt(PlaceId.Kharun);
            session.ChooseRoadPace(RoadPace.Steady);
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Her words follow you a while, then the road swallows them.", session.EncounterResult);

            session.ShotDockAt(PlaceId.Kharun);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Your guard handled it.", session.EncounterResult);
        }

        [Test]
        public void WithoutAGuardTheBadOutcomeApplies()
        {
            var session = new CaravanSession();
            session.ChooseRoadPace(RoadPace.Steady);
            session.GiveCargo(GoodId.MorRoot, 1f);
            session.GiveCargo(GoodId.MorRoot, 1f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Her words follow you a while, then the road swallows them.", session.EncounterResult);
            Assert.IsTrue(session.HasRunFlag("prophet_ignored"));
            Assert.AreEqual(2, session.CargoOf(GoodId.MorRoot));
            Assert.AreEqual(0, session.RoadNoteCount);
        }

        [Test]
        public void TheWestmarkGateRestoresTheGuardSave()
        {
            var session = new CaravanSession();
            Assert.IsTrue(session.TryHireAgent("Guard"));
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Your guard handled it.", session.EncounterResult);
            session.ShotOwnPieces(6, true);
            session.ShotDockAt(PlaceId.Westmark);
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Her words follow you a while, then the road swallows them.", session.EncounterResult);
            Assert.IsTrue(session.TryStartGateRoad());
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual("Your guard handled it.", session.EncounterResult);
        }

        [Test]
        public void HiredRolesSatisfyOnlyTheirOwnSkill()
        {
            var session = new CaravanSession();
            Assert.IsFalse(session.HasSkill("Scout"));
            Assert.IsFalse(session.HasSkill("Guard"));
            Assert.IsFalse(session.HasSkill("Haggler"));
            Assert.IsFalse(session.HasSkill("Handler"));
            Assert.IsFalse(session.HasSkill("Gossip"));
            Assert.IsTrue(session.TryHireAgent("Scout"));
            Assert.IsTrue(session.TryHireAgent("Guard"));
            Assert.IsTrue(session.TryHireAgent("Haggler"));
            Assert.IsTrue(session.HasSkill("Scout"));
            Assert.IsTrue(session.HasSkill("Guard"));
            Assert.IsTrue(session.HasSkill("Haggler"));
            Assert.IsFalse(session.HasSkill("Handler"));
            Assert.IsFalse(session.HasSkill("Gossip"));

            var hands = new CaravanSession();
            float before = hands.DailyWater;
            hands.SetCaravan(2, 2);
            Assert.AreNotEqual(before, hands.DailyWater);
            Assert.IsFalse(hands.HasSkill("Scout"));
            Assert.IsFalse(hands.HasSkill("Guard"));
            Assert.IsFalse(hands.HasSkill("Haggler"));
        }

        [Test]
        public void TwoHiresMatchThatKitAndDismissReturnsToNone()
        {
            var hired = new CaravanSession();
            Assert.IsTrue(hired.TryHireAgent("Scout"));
            Assert.IsTrue(hired.TryHireAgent("Guard"));
            var match = new CaravanSession();
            match.SetCaravan(hired.MerchantMountTier, 2);
            Assert.AreEqual(match.DailyWater, hired.DailyWater, 0.001f);
            Assert.AreEqual(match.DailyRations, hired.DailyRations, 0.001f);
            Assert.AreEqual(match.KitWaterCapacity, hired.KitWaterCapacity, 0.001f);
            Assert.AreEqual(match.KitRationCapacity, hired.KitRationCapacity, 0.001f);
            Assert.IsTrue(hired.TryDismissAgent("Scout"));
            Assert.IsTrue(hired.TryDismissAgent("Guard"));
            var fresh = new CaravanSession();
            Assert.AreEqual(fresh.DailyWater, hired.DailyWater, 0.001f);
            Assert.AreEqual(fresh.DailyRations, hired.DailyRations, 0.001f);
            Assert.AreEqual(fresh.KitWaterCapacity, hired.KitWaterCapacity, 0.001f);
            Assert.AreEqual(fresh.KitRationCapacity, hired.KitRationCapacity, 0.001f);
            Assert.AreEqual(0, hired.AgentCount);
        }

        [Test]
        public void RushDropsOnePackUnitAndQuieterPacesDoNot()
        {
            var rush = Ride(RoadPace.Rush, GoodId.MorRoot, GoodId.MorRoot);
            rush.ForceRushCargoIndex(0);
            ResolveBad(rush);
            Assert.AreEqual(1, rush.CargoCount);
            Assert.AreEqual(1, rush.CargoOf(GoodId.MorRoot));
            Assert.IsTrue(rush.EncounterResult.EndsWith(" The rush cost you one Mor Root."));
            Assert.AreEqual(1, rush.RoadNoteCount);
            Assert.AreEqual(
                WorldClock.Date(rush.Day) + ": The rush cost you one Mor Root on the road to Draven.",
                rush.RoadNote(0));
            Assert.AreEqual(new CaravanSession().WaterLitres, rush.WaterLitres, 0.001f);
            Assert.AreEqual(new CaravanSession().RationKg, rush.RationKg, 0.001f);

            var careful = Ride(RoadPace.Careful, GoodId.MorRoot, GoodId.MorRoot);
            ResolveBad(careful);
            Assert.AreEqual(2, careful.CargoOf(GoodId.MorRoot));
            Assert.IsFalse(careful.EncounterResult.Contains("rush cost"));
            Assert.AreEqual(0, careful.RoadNoteCount);

            var steady = Ride(RoadPace.Steady, GoodId.MorRoot, GoodId.MorRoot);
            ResolveBad(steady);
            Assert.AreEqual(2, steady.CargoOf(GoodId.MorRoot));
            Assert.AreEqual(0, steady.RoadNoteCount);
        }

        [Test]
        public void RushLeavesAnEmptyHoldAndAMissAndAGoodOutcomeAlone()
        {
            var empty = new CaravanSession();
            int coin = empty.Coin;
            float water = empty.WaterLitres;
            float food = empty.RationKg;
            empty.ChooseRoadPace(RoadPace.Rush);
            Assert.IsTrue(empty.DepartTo(PlaceId.Draven));
            empty.OpenEncounter("CZ-03");
            empty.ResolveOutcome(1, "b1");
            Assert.AreEqual(0, empty.CargoCount);
            Assert.AreEqual(coin, empty.Coin);
            Assert.AreEqual(water, empty.WaterLitres, 0.001f);
            Assert.AreEqual(food, empty.RationKg, 0.001f);
            Assert.AreEqual("Her words follow you a while, then the road swallows them.", empty.EncounterResult);
            Assert.AreEqual(0, empty.RoadNoteCount);

            var missed = Ride(RoadPace.Rush, GoodId.MorRoot, GoodId.MorRoot);
            missed.ForceRushCargoLossChance(0f);
            missed.ForceRushCargoIndex(0);
            ResolveBad(missed);
            Assert.AreEqual(2, missed.CargoOf(GoodId.MorRoot));
            Assert.AreEqual(0, missed.RoadNoteCount);
            Assert.IsFalse(missed.EncounterResult.Contains("rush cost"));

            var waterPack = Ride(RoadPace.Rush, GoodId.Water);
            float litres = waterPack.WaterLitres;
            waterPack.ForceRushCargoIndex(0);
            ResolveBad(waterPack);
            Assert.AreEqual(0, waterPack.CargoOf(GoodId.Water));
            Assert.AreEqual(litres, waterPack.WaterLitres, 0.001f);
            Assert.IsTrue(waterPack.EncounterResult.EndsWith(" The rush cost you one Water."));

            var good = Ride(RoadPace.Rush, GoodId.MorRoot, GoodId.MorRoot);
            int before = good.Coin;
            good.OpenEncounter("CA-01");
            good.ResolveOutcome(0, "a1");
            Assert.AreEqual(2, good.CargoOf(GoodId.MorRoot));
            Assert.AreEqual(before + 15, good.Coin);
            Assert.AreEqual(0, good.RoadNoteCount);
            Assert.IsFalse(good.EncounterResult.Contains("rush cost"));
        }

        [Test]
        public void ADesertDeathKeepsTheRolesAndANewGameClearsThem()
        {
            var session = new CaravanSession();
            Assert.IsTrue(session.TryHireAgent("Scout"));
            Assert.IsTrue(session.TryHireAgent("Guard"));
            session.ChooseRoadPace(RoadPace.Rush);
            session.GiveCargo(GoodId.MorRoot, 1f);
            session.GiveCargo(GoodId.MorRoot, 1f);
            session.ForceRushCargoIndex(0);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            session.ResolveOutcome(1, "b1");
            Assert.AreEqual(1, session.RoadNoteCount);
            string note = session.RoadNote(0);
            session.SetKit(0f, 0f);
            session.SpendRoadHours(1f);
            Assert.IsTrue(session.DeathPending);
            session.ContinueDeath();
            Assert.IsTrue(session.ScoutHired);
            Assert.IsTrue(session.GuardHired);
            Assert.AreEqual(0, session.CargoCount);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(1, session.RoadNoteCount);
            Assert.AreEqual(note, session.RoadNote(0));
            Assert.AreEqual(string.Empty, session.AgentNotice);
            var match = new CaravanSession();
            match.SetCaravan(session.MerchantMountTier, 2);
            Assert.AreEqual(match.DailyWater, session.DailyWater, 0.001f);

            var born = new CaravanSession();
            Assert.IsFalse(born.ScoutHired);
            Assert.IsFalse(born.HagglerHired);
            Assert.IsFalse(born.GuardHired);
            Assert.AreEqual(0, born.AgentCount);
            Assert.AreEqual(0, born.RoadNoteCount);
            var none = new CaravanSession();
            none.SetCaravan(born.MerchantMountTier, 0);
            Assert.AreEqual(none.DailyWater, born.DailyWater, 0.001f);
            Assert.AreEqual(none.KitRationCapacity, born.KitRationCapacity, 0.001f);
        }

        static void HireOnce(CaravanSession session, string role, int cost)
        {
            int before = session.Coin;
            Assert.IsTrue(session.TryHireAgent(role));
            Assert.AreEqual(before - cost, session.Coin);
            Assert.IsTrue(session.AgentHired(role));
            Assert.IsFalse(session.TryHireAgent(role));
            Assert.AreEqual(before - cost, session.Coin);
        }

        static void AssertLine(CaravanSession session, RoadPace pace, int percent, string name)
        {
            int live = Mathf.RoundToInt(EncounterTable.Chance(pace) * 100f);
            Assert.AreEqual(percent, live);
            Assert.AreEqual("Scout: " + percent + "% chance of an encounter at " + name + ".", session.ScoutLine(pace, true));
        }

        static int Shelf(GoodId good, int stock)
        {
            if (stock < 0)
                stock = 0;
            return EconomyCatalog.ShelfPrice(EconomyCatalog.BasePrice(good), stock, EconomyCatalog.DayUnit(PlaceId.Kharun, good));
        }

        static CaravanSession Ride(RoadPace pace, params GoodId[] goods)
        {
            var session = new CaravanSession();
            session.ChooseRoadPace(pace);
            for (int i = 0; i < goods.Length; i++)
                session.GiveCargo(goods[i], 1f);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            return session;
        }

        static void ResolveBad(CaravanSession session)
        {
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
        }
    }
}
