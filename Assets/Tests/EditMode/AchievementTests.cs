using System;
using System.IO;
using Caravans.FrontDoor;
using NUnit.Framework;
using UnityEngine;

namespace Caravans.Play.Tests
{
    public class AchievementTests
    {
        string previousOverride;
        Func<string, bool> previousValidator;
        string folder;

        [SetUp]
        public void SetUp()
        {
            previousOverride = SaveSlot.DirectoryOverride;
            previousValidator = SaveSlot.Validator;
            folder = Path.Combine(Application.temporaryCachePath, "caravans-achievement-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            SaveSlot.DirectoryOverride = folder;
            SaveSlot.Validator = RunSaveStore.Validate;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSlot.DirectoryOverride = previousOverride;
            SaveSlot.Validator = previousValidator;
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
        public void RankFiveUnlocksAtRankFive()
        {
            var below = new CaravanSession();
            below.SetKharunPoints(439f);
            Assert.AreEqual(4, below.GetKharunRank());
            Assert.IsFalse(below.AchievementUnlocked("ACH_RANK_5"));

            below.SetKharunPoints(540f);
            Assert.AreEqual(5, below.GetKharunRank());
            Assert.IsTrue(below.AchievementUnlocked("ACH_RANK_5"));
            Assert.AreEqual(1, below.AchievementUnlocks("ACH_RANK_5"));
            below.AddKharunPoints(80f);
            Assert.AreEqual(1, below.AchievementUnlocks("ACH_RANK_5"));

            var crossed = new CaravanSession();
            crossed.SetKharunPoints(400f);
            Assert.IsFalse(crossed.AchievementUnlocked("ACH_RANK_5"));
            crossed.AddKharunPoints(140f);
            Assert.AreEqual(5, crossed.GetKharunRank());
            Assert.IsTrue(crossed.AchievementUnlocked("ACH_RANK_5"));
            Assert.AreEqual(1, crossed.AchievementUnlocks("ACH_RANK_5"));
        }

        [Test]
        public void TheFirstFlipUnlocksOnce()
        {
            var session = new CaravanSession();
            Assert.IsFalse(session.AchievementUnlocked("ACH_FIRST_FLIP"));
            FlipAt(session, PlaceId.Draven);
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Draven));
            Assert.IsTrue(session.AchievementUnlocked("ACH_FIRST_FLIP"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_FLIP"));
            FlipAt(session, PlaceId.Westmark);
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Westmark));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_FLIP"));

            var npc = new CaravanSession();
            npc.AddPresence(PlaceId.Draven, HouseId.Kharun, 40);
            Assert.AreEqual(HouseId.Kharun, npc.PatronOf(PlaceId.Draven));
            Assert.IsFalse(npc.KharunFlipSeen(PlaceId.Draven));
            Assert.IsFalse(npc.AchievementUnlocked("ACH_FIRST_FLIP"));
        }

        [Test]
        public void TheFirstPieceStillUnlocksOnceAndEmbarkStaysLocked()
        {
            var session = new CaravanSession();
            session.SetKharunPoints(1000f);
            session.SetCoin(40000);
            Assert.IsFalse(session.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.IsFalse(session.AchievementUnlocked("ACH_EMBARK"));
            Assert.IsTrue(session.TryBuyMapPiece(1));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_PIECE"));
            Assert.IsTrue(session.TryBuyMapPiece(2));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_PIECE"));
            Assert.IsFalse(session.AchievementUnlocked("ACH_EMBARK"));
        }

        [Test]
        public void UnlockWithoutSteamStillSetsTheLocalFlag()
        {
            var session = new CaravanSession();
            Assert.DoesNotThrow(() => session.SetKharunPoints(540f));
            Assert.IsTrue(session.AchievementUnlocked("ACH_RANK_5"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_RANK_5"));
        }

        [Test]
        public void ContinueKeepsUnlocksAndAFreshRunClearsThem()
        {
            var session = new CaravanSession();
            session.SetKharunPoints(540f);
            FlipAt(session, PlaceId.Draven);
            session.ShotDockAt(PlaceId.Kharun);
            session.SetCoin(40000);
            Assert.IsTrue(session.TryBuyMapPiece(1));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_RANK_5"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_FLIP"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_PIECE"));

            Assert.IsTrue(RunSaveStore.TryWrite(session));
            CaravanSession loaded;
            Assert.IsTrue(RunSaveStore.TryLoad(out loaded));
            Assert.IsTrue(loaded.AchievementUnlocked("ACH_RANK_5"));
            Assert.IsTrue(loaded.AchievementUnlocked("ACH_FIRST_FLIP"));
            Assert.IsTrue(loaded.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.AreEqual(1, loaded.AchievementUnlocks("ACH_RANK_5"));
            Assert.AreEqual(1, loaded.AchievementUnlocks("ACH_FIRST_FLIP"));
            Assert.AreEqual(1, loaded.AchievementUnlocks("ACH_FIRST_PIECE"));

            var fresh = new CaravanSession();
            Assert.IsFalse(fresh.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.IsFalse(fresh.AchievementUnlocked("ACH_EMBARK"));
            Assert.IsFalse(fresh.AchievementUnlocked("ACH_RANK_5"));
            Assert.IsFalse(fresh.AchievementUnlocked("ACH_FIRST_FLIP"));
        }

        [Test]
        public void ASavedRankOrFlipUnlocksOnceWhenTheFlagWasMissing()
        {
            var session = new CaravanSession();
            session.SetKharunPoints(540f);
            FlipAt(session, PlaceId.Westmark);
            RunSave save = session.CaptureSave();
            save.achievementIds = new string[0];
            save.achievementFires = new int[0];

            CaravanSession restored;
            Assert.IsTrue(CaravanSession.TryRestore(save, out restored));
            Assert.IsTrue(restored.AchievementUnlocked("ACH_RANK_5"));
            Assert.AreEqual(1, restored.AchievementUnlocks("ACH_RANK_5"));
            Assert.IsTrue(restored.AchievementUnlocked("ACH_FIRST_FLIP"));
            Assert.AreEqual(1, restored.AchievementUnlocks("ACH_FIRST_FLIP"));

            CaravanSession second;
            Assert.IsTrue(CaravanSession.TryRestore(restored.CaptureSave(), out second));
            Assert.AreEqual(1, second.AchievementUnlocks("ACH_RANK_5"));
            Assert.AreEqual(1, second.AchievementUnlocks("ACH_FIRST_FLIP"));
        }

        [Test]
        public void ASoftResetKeepsTheFlagsAndDoesNotRefireThem()
        {
            var session = new CaravanSession();
            session.SetKharunPoints(540f);
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_RANK_5"));
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SetKit(0.05f, 0.05f);
            session.SpendRoadHours(1f);
            Assert.IsTrue(session.DeathPending);
            session.ContinueDeath();
            Assert.IsFalse(session.DeathPending);
            Assert.IsTrue(session.AchievementUnlocked("ACH_RANK_5"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_RANK_5"));
        }

        static void FlipAt(CaravanSession session, PlaceId place)
        {
            session.SetCoin(8000);
            session.ShotDockAt(PlaceId.Kharun);
            session.SetShelf(PlaceId.Kharun, GoodId.DreamLotusNectar, 30);
            Assert.IsTrue(session.TryBuy(GoodId.DreamLotusNectar));
            session.ShotDockAt(place);
            session.SetShelf(place, GoodId.DreamLotusNectar, 0);
            Assert.IsTrue(session.TrySell(GoodId.DreamLotusNectar));
        }
    }
}
