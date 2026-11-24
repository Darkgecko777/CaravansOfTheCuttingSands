using System;
using System.IO;
using Caravans.FrontDoor;
using NUnit.Framework;
using UnityEngine;

namespace Caravans.Play.Tests
{
    public class TrailerSetupTests
    {
        string previousOverride;
        string folder;

        [SetUp]
        public void SetUp()
        {
            previousOverride = SaveSlot.DirectoryOverride;
            folder = Path.Combine(Application.temporaryCachePath, "caravans-trailer-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            SaveSlot.DirectoryOverride = folder;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSlot.DirectoryOverride = previousOverride;
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch (IOException)
                {
                }
            }
        }

        [Test]
        public void SameSetupMatches()
        {
            string[] shots =
            {
                "04_road_day",
                "08_road_dusk",
                "14_sandstorm_in",
                "15_rush_cargo_loss",
                "17_village_flip",
                "18_rank_up"
            };
            for (int i = 0; i < shots.Length; i++)
            {
                CaravanSession left = Pinned();
                CaravanSession right = Pinned();
                Assert.IsTrue(left.TrailerApply(shots[i]), shots[i]);
                Assert.IsTrue(right.TrailerApply(shots[i]), shots[i]);
                Assert.AreEqual(left.TrailerFingerprint(), right.TrailerFingerprint(), shots[i]);
            }
        }

        [Test]
        public void PinStaysOnTheShot()
        {
            CaravanSession first = Pinned();
            CaravanSession second = Pinned();
            Assert.AreEqual(CaravanSession.EditorShotSeed, first.RunSeed);
            Assert.AreEqual(second.NpcCount, first.NpcCount);
            for (int i = 0; i < first.NpcCount; i++)
                Assert.AreEqual(second.NpcDock(i), first.NpcDock(i));

            string[] shots =
            {
                "04_road_day", "08_road_dusk", "14_sandstorm_in", "15_rush_cargo_loss",
                "17_village_flip", "18_rank_up", "13_guard_save", "03_depart_draven"
            };
            for (int i = 0; i < shots.Length; i++)
            {
                CaravanSession shot = Pinned();
                shot.TrailerApply(shots[i]);
            }

            var fresh = new CaravanSession();
            Assert.AreNotEqual(CaravanSession.EditorShotSeed, fresh.RunSeed);
        }

        [Test]
        public void RivalsSitOnTheDravenRoad()
        {
            CaravanSession session = Pinned();
            Assert.IsTrue(session.TrailerApply("04_road_day"));
            float length = RoadGraph.Length(PlaceId.Kharun, PlaceId.Draven);
            bool seen = false;
            float vision = session.VisionRadius();
            for (int i = 0; i < 3; i++)
            {
                NpcMark mark = session.NpcMarkAt(i);
                Assert.IsTrue(mark.Traveling);
                Vector2 expected = RoadGraph.PointAlong(PlaceId.Kharun, PlaceId.Draven, length * (0.22f + 0.22f * i));
                Assert.LessOrEqual(Vector2.Distance(mark.Point, expected), 0.0001f);
                if (RoadPath.MapDistance(session.CaravanPoint, mark.Point) <= vision + 0.0001f)
                    seen = true;
            }

            Assert.IsTrue(seen);
        }

        [Test]
        public void QuietRoadStaysOpen()
        {
            CaravanSession session = Pinned();
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.ChoosePace(2);
            session.TrailerQuietRoad();
            session.TrailerAdvanceLeg(0.3f);
            for (int i = 0; i < 390; i++)
                session.Tick(1f / 60f);
            Assert.IsFalse(session.CampPending);
            Assert.IsFalse(session.EncounterPending);
            Assert.IsFalse(session.CriticalPending);
            Assert.IsFalse(session.DeathPending);
            Assert.IsTrue(session.Traveling);
        }

        [Test]
        public void SandstormEntersInsideTheClip()
        {
            CaravanSession session = Pinned();
            Assert.IsTrue(session.TrailerApply("14_sandstorm_in"));
            Assert.AreEqual(WeatherKind.Clear, session.WeatherAt(session.CaravanPoint));
            bool entered = false;
            for (int i = 0; i < 300; i++)
            {
                session.Tick(1f / 60f);
                if (session.WeatherAt(session.CaravanPoint) != WeatherKind.Sandstorm)
                    continue;
                entered = true;
                break;
            }

            Assert.IsTrue(entered);
            Assert.AreEqual(TravelScale.VisionSandstorm, session.VisionRadius(), 0.0001f);
            Assert.AreEqual(WeatherKind.Sandstorm, WeatherLook.Target(true, session.WeatherAt(session.CaravanPoint)));
            float fade = GameConfig.WeatherLook.fadeSeconds;
            WeatherLook.Weights weights = default;
            for (int i = 0; i < 48; i++)
                weights = WeatherLook.Step(weights, WeatherKind.Sandstorm, 1f / 60f, fade);
            Assert.GreaterOrEqual(weights.Sandstorm, 0.999f);
        }

        [Test]
        public void OneSaleFlipsDraven()
        {
            CaravanSession session = Pinned();
            Assert.IsTrue(session.TrailerApply("17_village_flip"));
            HouseId? rival = session.PatronOf(PlaceId.Draven);
            Assert.IsTrue(rival.HasValue);
            Assert.AreNotEqual(HouseId.Kharun, rival.Value);
            float before = session.KharunPoints;
            Assert.IsTrue(session.TrySellStack(0));
            Assert.AreEqual(HouseId.Kharun, session.PatronOf(PlaceId.Draven));
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Draven));
            Assert.GreaterOrEqual(session.KharunPoints, before + EconomyRates.StandingFlipBonus);
        }

        [Test]
        public void OneSaleReachesRankFive()
        {
            CaravanSession session = Pinned();
            Assert.IsTrue(session.TrailerApply("18_rank_up"));
            Assert.AreEqual(4, session.GetKharunRank());
            Assert.IsTrue(session.TrySellStack(0));
            Assert.AreEqual(5, session.GetKharunRank());
            Assert.IsTrue(session.RankUpNotice != null && session.RankUpNotice.Contains("rank 5"));
        }

        [Test]
        public void RushLosesTheForcedUnitAndTheGuardDoesNot()
        {
            CaravanSession rush = Pinned();
            Assert.IsTrue(rush.TrailerApply("15_rush_cargo_loss"));
            int index = rush.TrailerRushIndex;
            GoodId forced = rush.CargoGoodAt(index);
            int notes = rush.RoadNoteCount;
            int packs = rush.CargoCount;
            string lost = rush.TrailerResolveTone(0, false);
            Assert.IsTrue(lost != null && lost.Contains("The rush cost you one"));
            Assert.AreEqual(packs - 1, rush.CargoCount);
            Assert.AreEqual(notes + 1, rush.RoadNoteCount);
            for (int i = 0; i < rush.CargoCount; i++)
                Assert.AreNotEqual(forced, rush.CargoGoodAt(i));

            CaravanSession guard = Pinned();
            Assert.IsTrue(guard.TrailerApply("13_guard_save"));
            int held = guard.CargoCount;
            string saved = guard.TrailerResolveTone(0, false);
            Assert.AreEqual("Your guard handled it.", saved);
            Assert.AreEqual(held, guard.CargoCount);
        }

        [Test]
        public void HelpersLeaveTheRatesAlone()
        {
            float rush = EconomyRates.RushCargoLossChance;
            int scout = EconomyRates.ScoutWage;
            int haggler = EconomyRates.HagglerWage;
            int guard = EconomyRates.GuardWage;
            CaravanSession before = Pinned();
            int price = before.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars);
            string[] shots =
            {
                "02_kharun_market", "03_depart_draven", "04_road_day", "05_market_buy",
                "06_market_sell", "07_inn_fill", "08_road_dusk", "09_tavern_hire",
                "10_gates_scout_rush", "11_encounter_good", "12_skins_low", "13_guard_save",
                "14_sandstorm_in", "15_rush_cargo_loss", "16_logbook_road_note", "17_village_flip",
                "18_rank_up", "19_tavern_map_piece", "20_westmark_gate", "21_sarn_road",
                "22_embarked", "23_camp_card", "24_departure_short", "25_rescue_dry",
                "26_sarns_rest", "27_ridgewatch_market", "28_road_night_4x"
            };
            for (int i = 0; i < shots.Length; i++)
            {
                CaravanSession shot = Pinned();
                shot.TrailerApply(shots[i]);
            }

            Assert.AreEqual(rush, EconomyRates.RushCargoLossChance);
            Assert.AreEqual(scout, EconomyRates.ScoutWage);
            Assert.AreEqual(haggler, EconomyRates.HagglerWage);
            Assert.AreEqual(guard, EconomyRates.GuardWage);
            CaravanSession after = Pinned();
            Assert.AreEqual(price, after.BuyCost(PlaceId.Kharun, GoodId.ShimmersteelJars));
        }

        static CaravanSession Pinned()
        {
            var session = new CaravanSession();
            session.ShotPinRun();
            return session;
        }
    }
}
