using System;
using System.IO;
using Caravans.FrontDoor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Caravans.Play.Tests
{
    public class SaveTests
    {
        string previousOverride;
        Func<string, bool> previousValidator;
        string folder;

        [SetUp]
        public void SetUp()
        {
            previousOverride = SaveSlot.DirectoryOverride;
            previousValidator = SaveSlot.Validator;
            folder = Path.Combine(Application.temporaryCachePath, "caravans-save-tests", Guid.NewGuid().ToString("N"));
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
        public void ARunRoundTrips()
        {
            var session = new CaravanSession();
            session.SetCoin(80000);
            Assert.IsTrue(session.TryHireAgent("Guard"));
            session.SetShelf(PlaceId.Kharun, GoodId.DreamLotusNectar, 30);
            session.NoteShownBuy(GoodId.DreamLotusNectar);
            Assert.IsTrue(session.TryBuy(GoodId.DreamLotusNectar));
            session.GiveCargo(GoodId.Water, 1f);
            session.GiveCargo(GoodId.Rations, 1f);
            session.GiveCargo(GoodId.FrostIron, 1f);
            Refill(session);
            session.ChooseRoadPace(RoadPace.Rush);
            session.ForceRushCargoLossChance(1f);
            session.ForceRushCargoIndex(session.CargoCount - 1);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.OpenEncounter("CZ-03");
            session.ResolveOutcome(1, "b1");
            session.ResolveOutcome(1, "b1");
            session.ContinueEncounter();
            session.ClearRushCargoLossChance();
            Assert.Greater(session.RoadNoteCount, 0);
            Ride(session);
            Assert.AreEqual(PlaceId.Draven, session.Dock);
            Assert.Greater(session.CargoOf(GoodId.DreamLotusNectar), 0);
            session.SetShelf(PlaceId.Draven, GoodId.DreamLotusNectar, 0);
            session.NoteShownSale(0);
            Assert.IsTrue(session.TrySell(GoodId.DreamLotusNectar));
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Draven));
            Assert.AreEqual(HouseId.Kharun, session.PatronOf(PlaceId.Draven));
            session.GiveCargo(GoodId.Water, 1f);
            session.GiveCargo(GoodId.Water, 1f);
            session.ChooseRoadPace(RoadPace.Steady);
            Leave(session, PlaceId.Kharun);

            session.ShotDropDrinkPacks();
            session.SetCoin(80000);
            if (!session.CanTakeRoad())
                Assert.IsTrue(session.TryRest());
            session.SetKit(0.05f, 0.05f);
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.IsTrue(session.DeathPending);
            session.ContinueDeath();
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.GreaterOrEqual(session.RescueCount, 1);
            Assert.AreEqual(0, session.CargoCount);

            session.SetCoin(80000);
            Assert.IsTrue(session.TryHireAgent("Scout"));
            Assert.IsTrue(session.GuardHired);
            session.SetCaravan(2, session.AgentCount);
            session.SetPackBeasts(1, 2);
            session.SetKharunPoints(1000f);
            Assert.IsTrue(session.TryBuyMapPiece(1));
            session.SetMapPieceLeads(1);
            Assert.IsTrue(session.TryBuyMapPiece(2));
            Assert.AreEqual(0, session.MapPieceLeads);
            Assert.AreEqual(1, session.LeadsUsed);
            session.SetMapPieceLeads(1);
            session.SetKharunPoints(240f);
            session.ChooseRoadPace(RoadPace.Careful);
            session.TickMorning();
            session.TickMorning();
            Refill(session);
            float beforeWait = session.TravelHours;
            Assert.IsTrue(session.CanWait());
            session.WaitOneHour();
            Assert.IsFalse(session.DeathPending);
            Assert.AreEqual(beforeWait - 1f, session.TravelHours, 0.05f);
            session.AddPresence(PlaceId.Draven, HouseId.Kharun, 500);
            session.SetShelf(PlaceId.Kharun, GoodId.FrostIron, 7);
            session.SetShelf(PlaceId.Draven, GoodId.MorRoot, 3);
            session.SetShelf(PlaceId.Westmark, GoodId.BrineScrub, 9);
            session.SetShelf(PlaceId.Kharun, GoodId.MorRoot, 6);
            session.NoteShownBuy(GoodId.MorRoot);
            Assert.IsTrue(session.TryBuy(GoodId.MorRoot));
            session.GiveCargo(GoodId.MorRoot, 0.45f);
            session.GiveCargo(GoodId.BrineScrub, 0.8f);
            session.NoteShownSale(0);
            session.SetKit(12.5f, 7.25f);
            session.SetCoin(1234);
            session.SetFriendOfBandits(true);
            session.SetRunFlag("watched");

            Assert.IsTrue(session.AtSafePoint);
            Assert.IsFalse(session.Traveling);
            AssertPlanted(session);
            RunSave before = session.CaptureSave();
            Assert.IsTrue(RunSaveStore.TryWrite(session));
            CaravanSession loaded;
            Assert.IsTrue(RunSaveStore.TryLoad(out loaded));
            AssertPlanted(loaded);
            Same(before, loaded.CaptureSave());
            Assert.IsTrue(loaded.AtSafePoint);
            Assert.IsTrue(loaded.SaveDue);
            Assert.AreEqual(HouseId.Kharun, loaded.House);
            Assert.IsFalse(loaded.Traveling);
            Assert.AreEqual(2, loaded.AgentCount);
            Assert.AreEqual(session.MerchantSpread(), loaded.MerchantSpread(), 0.0001f);
            Assert.AreEqual(EconomyRates.MerchantSpread, loaded.MerchantSpread(), 0.0001f);
            Assert.IsTrue(loaded.HasSkill("Scout"));
            Assert.IsTrue(loaded.HasSkill("Guard"));
            Assert.IsFalse(loaded.HasSkill("Haggler"));
            Assert.AreEqual(session.DailyWater, loaded.DailyWater, 0.0001f);
            Assert.AreEqual(session.PriceOf(PlaceId.Kharun, GoodId.FrostIron), loaded.PriceOf(PlaceId.Kharun, GoodId.FrostIron));
            Assert.AreEqual(session.PriceOf(PlaceId.Draven, GoodId.MorRoot), loaded.PriceOf(PlaceId.Draven, GoodId.MorRoot));
            Assert.AreEqual(session.PriceOf(PlaceId.Westmark, GoodId.BrineScrub), loaded.PriceOf(PlaceId.Westmark, GoodId.BrineScrub));
            Assert.AreEqual(session.NpcCount, loaded.NpcCount);
            Assert.AreEqual(session.Masses.Count, loaded.Masses.Count);
        }

        [Test]
        public void WritesOnlyAtASafePoint()
        {
            var riding = new CaravanSession();
            Refill(riding);
            Assert.IsTrue(riding.DepartTo(PlaceId.Draven));
            Assert.IsTrue(riding.Traveling);
            PlayPresenter.SaveOnLeave(riding);
            Assert.IsTrue(riding.Traveling);
            Rejects(riding);

            SaveSlot.Clear();
            var card = new CaravanSession();
            card.OpenEncounter("CZ-03");
            Assert.IsTrue(card.EncounterPending);
            Rejects(card);

            SaveSlot.Clear();
            var camp = new CaravanSession();
            camp.ShotCamp();
            Assert.IsTrue(camp.CampPending);
            Rejects(camp);

            SaveSlot.Clear();
            var pace = new CaravanSession();
            pace.ShotResumePace();
            Assert.IsTrue(pace.AwaitingPace);
            Rejects(pace);

            SaveSlot.Clear();
            var rest = new CaravanSession();
            rest.SetKharunPoints(1000f);
            rest.SetCoin(40000);
            for (int piece = 1; piece <= 6; piece++)
                Assert.IsTrue(rest.TryBuyMapPiece(piece));
            rest.ShotDockAt(PlaceId.Westmark);
            Assert.IsTrue(rest.TryStartGateRoad());
            rest.CompleteMapLeg();
            Assert.IsTrue(rest.RestPending);
            Rejects(rest);

            SaveSlot.Clear();
            var ended = new CaravanSession();
            ended.ShotShowEnd();
            Assert.IsTrue(ended.EndScreenUp);
            Rejects(ended);

            SaveSlot.Clear();
            var home = new CaravanSession();
            Assert.IsTrue(home.AtSafePoint);
            Assert.AreEqual(PlaceId.Kharun, home.Dock);
            Assert.IsTrue(RunSaveStore.TryWrite(home));
            Assert.IsTrue(File.Exists(SaveSlot.MainPath));
            Assert.IsFalse(File.Exists(SaveSlot.TempPath));
            Assert.IsTrue(SaveSlot.HasValidSave());
        }

        [Test]
        public void ADawnOnTheRoadSavesAtTheNextTown()
        {
            var session = new CaravanSession();
            session.ClearSaveDue();
            Refill(session);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            Assert.IsTrue(session.Traveling);
            session.TickMorning();
            Assert.IsTrue(session.SaveDue);
            Assert.IsFalse(session.AtSafePoint);
            Assert.IsFalse(RunSaveStore.TryWrite(session));
            Assert.IsFalse(File.Exists(SaveSlot.MainPath));
            Ride(session);
            Assert.AreEqual(PlaceId.Draven, session.Dock);
            Assert.IsTrue(session.SaveDue);
            Assert.IsTrue(session.AtSafePoint);
            Assert.IsTrue(RunSaveStore.TryWrite(session));
            Assert.IsTrue(File.Exists(SaveSlot.MainPath));
            Assert.IsFalse(File.Exists(SaveSlot.TempPath));
        }

        [Test]
        public void DeathOnLeaveWakesAtKharun()
        {
            var session = new CaravanSession();
            session.SetCoin(500);
            session.SetKit(0.05f, 0.05f);
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(PlaceId.Draven));
            session.SpendRoadHours(1f);
            Assert.IsTrue(session.DeathPending);
            PlayPresenter.SaveOnLeave(session);
            Assert.IsFalse(session.DeathPending);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual(250, session.Coin);
            Assert.IsTrue(session.AtSafePoint);
            Assert.IsTrue(File.Exists(SaveSlot.MainPath));
            CaravanSession loaded;
            Assert.IsTrue(RunSaveStore.TryLoad(out loaded));
            Assert.AreEqual(250, loaded.Coin);
            Assert.AreEqual(PlaceId.Kharun, loaded.Dock);
            Assert.IsFalse(loaded.Traveling);
            Assert.IsTrue(loaded.AtSafePoint);
            Assert.GreaterOrEqual(loaded.RescueCount, 1);
        }

        [Test]
        public void ABadMainFallsBackToTheBackup()
        {
            WriteCoin(111);
            WriteCoin(222);
            RunSave main = JsonUtility.FromJson<RunSave>(File.ReadAllText(SaveSlot.MainPath));
            RunSave bak = JsonUtility.FromJson<RunSave>(File.ReadAllText(SaveSlot.BackupPath));
            Assert.AreEqual(222, main.coin);
            Assert.AreEqual(111, bak.coin);
            string good = File.ReadAllText(SaveSlot.BackupPath);

            File.WriteAllText(SaveSlot.MainPath, "this is not a save");
            ExpectSkip("caravans_save.json");
            LoadsBackup();

            File.WriteAllText(SaveSlot.MainPath, "");
            ExpectSkip("caravans_save.json");
            LoadsBackup();

            File.WriteAllText(SaveSlot.MainPath, good);
            File.Copy(SaveSlot.MainPath, SaveSlot.BackupPath, true);
            string latest = File.ReadAllText(SaveSlot.MainPath);
            int cut = latest.Length / 2;
            if (cut < 1)
                cut = 1;
            File.WriteAllText(SaveSlot.MainPath, latest.Substring(0, cut));
            File.WriteAllText(SaveSlot.BackupPath, good);
            ExpectSkip("caravans_save.json");
            LoadsBackup();

            File.WriteAllText(SaveSlot.MainPath, "%%%");
            File.WriteAllText(SaveSlot.BackupPath, "");
            ExpectSkip("caravans_save.json");
            ExpectSkip("caravans_save.json.bak");
            CaravanSession none;
            Assert.IsFalse(RunSaveStore.TryLoad(out none));
            ExpectSkip("caravans_save.json");
            ExpectSkip("caravans_save.json.bak");
            Assert.IsFalse(SaveSlot.HasValidSave());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AWrongSaveIsRejected()
        {
            var session = new CaravanSession();
            session.SetCoin(40);
            Assert.IsTrue(RunSaveStore.TryWrite(session));
            RunSave broken = session.CaptureSave();
            broken.shelfStock = new int[3];
            string shelf = JsonUtility.ToJson(broken);
            Assert.IsFalse(RunSaveStore.Validate(shelf));
            File.Delete(SaveSlot.BackupPath);
            File.WriteAllText(SaveSlot.MainPath, shelf);
            CaravanSession ignored;
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(RunSaveStore.TryLoad(out ignored));
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(SaveSlot.HasValidSave());

            broken = session.CaptureSave();
            broken.dock = 99;
            string dock = JsonUtility.ToJson(broken);
            Assert.IsFalse(RunSaveStore.Validate(dock));
            File.WriteAllText(SaveSlot.MainPath, dock);
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(RunSaveStore.TryLoad(out ignored));
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(SaveSlot.HasValidSave());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void VersionsFallBackOrLoad()
        {
            WriteCoin(111);
            string good = File.ReadAllText(SaveSlot.MainPath);
            File.Delete(SaveSlot.BackupPath);

            File.WriteAllText(SaveSlot.MainPath, "{}");
            Assert.IsFalse(RunSaveStore.Validate("{}"));
            CaravanSession ignored;
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(RunSaveStore.TryLoad(out ignored));
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(SaveSlot.HasValidSave());

            string zero = WithVersion(good, 0);
            File.WriteAllText(SaveSlot.MainPath, zero);
            Assert.IsFalse(RunSaveStore.Validate(zero));
            ExpectSkip("caravans_save.json");
            Assert.IsFalse(RunSaveStore.TryLoad(out ignored));

            File.WriteAllText(SaveSlot.BackupPath, good);
            string newer = WithVersion(good, RunSave.CurrentVersion + 1);
            File.WriteAllText(SaveSlot.MainPath, newer);
            Assert.IsFalse(RunSaveStore.Validate(newer));
            CaravanSession fromBak;
            ExpectSkip("caravans_save.json");
            Assert.IsTrue(RunSaveStore.TryLoad(out fromBak));
            Assert.AreEqual(111, fromBak.Coin);
            ExpectSkip("caravans_save.json");
            Assert.IsTrue(SaveSlot.HasValidSave());

            File.WriteAllText(SaveSlot.MainPath, good);
            File.Delete(SaveSlot.BackupPath);
            Assert.IsTrue(RunSaveStore.Validate(good));
            CaravanSession current;
            Assert.IsTrue(RunSaveStore.TryLoad(out current));
            Assert.AreEqual(111, current.Coin);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TwoWritesLeaveABackupAndNoScratch()
        {
            WriteCoin(111);
            WriteCoin(222);
            Assert.IsTrue(File.Exists(SaveSlot.MainPath));
            Assert.IsTrue(File.Exists(SaveSlot.BackupPath));
            Assert.IsFalse(File.Exists(SaveSlot.TempPath));
            RunSave main = JsonUtility.FromJson<RunSave>(File.ReadAllText(SaveSlot.MainPath));
            RunSave bak = JsonUtility.FromJson<RunSave>(File.ReadAllText(SaveSlot.BackupPath));
            Assert.AreEqual(222, main.coin);
            Assert.AreEqual(111, bak.coin);
            CaravanSession loaded;
            Assert.IsTrue(RunSaveStore.TryLoad(out loaded));
            Assert.AreEqual(222, loaded.Coin);
        }

        [Test]
        public void ClearRemovesTheSlotAndTheEndingClearsIt()
        {
            var session = new CaravanSession();
            session.SetCoin(77);
            Assert.IsTrue(RunSaveStore.TryWrite(session));
            File.WriteAllText(SaveSlot.TempPath, "scratch");
            RunSaveStore.Clear();
            Assert.IsFalse(File.Exists(SaveSlot.MainPath));
            Assert.IsFalse(File.Exists(SaveSlot.BackupPath));
            Assert.IsFalse(File.Exists(SaveSlot.TempPath));
            Assert.IsFalse(SaveSlot.HasValidSave());

            session.SetCoin(88);
            Assert.IsTrue(RunSaveStore.TryWrite(session));
            string written = File.ReadAllText(SaveSlot.MainPath);
            session.ShotShowEnd();
            Assert.IsTrue(session.EndScreenUp);
            PlayPresenter.SaveOnLeave(session);
            Assert.AreEqual(written, File.ReadAllText(SaveSlot.MainPath));
            PlayPresenter.ClearIfEnded(session);
            Assert.IsFalse(File.Exists(SaveSlot.MainPath));
            Assert.IsFalse(File.Exists(SaveSlot.BackupPath));
            Assert.IsFalse(File.Exists(SaveSlot.TempPath));
            Assert.IsFalse(SaveSlot.HasValidSave());
        }

        [Test]
        public void TheSameSaveRollsTheSameNextLeg()
        {
            var session = new CaravanSession();
            Leave(session, PlaceId.Draven);
            Assert.Greater(session.StretchChecks, 0);
            Assert.IsTrue(session.AtSafePoint);
            Assert.IsTrue(RunSaveStore.TryWrite(session));
            CaravanSession first;
            CaravanSession second;
            Assert.IsTrue(RunSaveStore.TryLoad(out first));
            Assert.IsTrue(RunSaveStore.TryLoad(out second));
            DepartSame(first, PlaceId.Kharun);
            DepartSame(second, PlaceId.Kharun);
            Assert.AreEqual(first.EncounterPending, second.EncounterPending);
            Assert.AreEqual(first.EncounterId, second.EncounterId);
            Assert.AreEqual(first.EncounterHour, second.EncounterHour, 0.0001f);
        }

        static void AssertPlanted(CaravanSession session)
        {
            RunSave save = session.CaptureSave();
            Assert.AreEqual(1234, session.Coin);
            Assert.AreEqual(1234, save.coin);
            Assert.AreEqual(12.5f, session.WaterLitres, 0.0001f);
            Assert.AreEqual(7.25f, session.RationKg, 0.0001f);
            Assert.AreEqual(12.5f, save.waterLitres, 0.0001f);
            Assert.AreEqual(7.25f, save.rationKg, 0.0001f);
            Assert.Greater(session.Day, 1);
            Assert.AreEqual(session.Day, save.day);
            Assert.AreEqual(session.Hour, save.hour, 0.0001f);
            Assert.AreEqual(PlaceId.Kharun, session.Dock);
            Assert.AreEqual((int)PlaceId.Kharun, save.dock);
            Assert.AreEqual(3, session.CargoCount);
            Assert.AreEqual(3, save.packs.Length);
            Assert.AreEqual((int)GoodId.MorRoot, save.packs[0].good);
            Assert.AreEqual(1f, save.packs[0].integrity, 0.0001f);
            Assert.AreEqual((int)GoodId.MorRoot, save.packs[1].good);
            Assert.AreEqual(0.45f, save.packs[1].integrity, 0.0001f);
            Assert.AreEqual((int)GoodId.BrineScrub, save.packs[2].good);
            Assert.AreEqual(0.8f, save.packs[2].integrity, 0.0001f);
            Assert.AreEqual(7, session.StockOf(PlaceId.Kharun, GoodId.FrostIron));
            Assert.AreEqual(3, session.StockOf(PlaceId.Draven, GoodId.MorRoot));
            Assert.AreEqual(9, session.StockOf(PlaceId.Westmark, GoodId.BrineScrub));
            Assert.AreEqual(5, session.StockOf(PlaceId.Kharun, GoodId.MorRoot));
            Assert.AreEqual(7, save.shelfStock[Slot(PlaceId.Kharun, GoodId.FrostIron)]);
            Assert.AreEqual(3, save.shelfStock[Slot(PlaceId.Draven, GoodId.MorRoot)]);
            Assert.AreEqual(9, save.shelfStock[Slot(PlaceId.Westmark, GoodId.BrineScrub)]);
            Assert.IsTrue(session.HasLoggedBuy(GoodId.DreamLotusNectar));
            Assert.IsTrue(session.HasLoggedSale(GoodId.DreamLotusNectar));
            Assert.IsTrue(save.loggedBuy[(int)GoodId.DreamLotusNectar].seen);
            Assert.IsTrue(save.loggedSale[(int)GoodId.DreamLotusNectar].seen);
            Assert.GreaterOrEqual(save.boughtHere[(int)GoodId.MorRoot], 1);
            Assert.Greater(session.InfluenceOf(PlaceId.Draven, HouseId.Kharun), 100f);
            Assert.AreEqual((int)HouseId.Kharun, save.patron[(int)PlaceId.Draven]);
            Assert.Greater(save.npcs.Length, 0);
            Assert.AreEqual(session.NpcCount, save.npcs.Length);
            Assert.Greater(save.weather.Length, 0);
            Assert.AreEqual(session.Masses.Count, save.weather.Length);
            Assert.AreEqual(240f, session.KharunPoints, 0.0001f);
            Assert.AreEqual(240f, save.kharunPoints, 0.0001f);
            Assert.AreEqual(2, session.GetKharunRank());
            Assert.IsTrue(session.KharunFlipSeen(PlaceId.Draven));
            Assert.IsTrue(save.kharunFlipSeen[(int)PlaceId.Draven]);
            Assert.IsTrue(session.MapPieceOwned(1));
            Assert.IsTrue(session.MapPieceOwned(2));
            Assert.IsTrue(save.held[0].owned);
            Assert.IsTrue(save.held[1].owned);
            Assert.AreEqual(1, session.MapPieceLeads);
            Assert.AreEqual(1, save.mapPieceLeads);
            Assert.AreEqual(1, session.LeadsUsed);
            Assert.AreEqual(1, save.leadsUsed);
            Assert.Greater(session.MapCoinPaid, 0);
            Assert.AreEqual(session.MapCoinPaid, save.mapCoin);
            Assert.Greater(save.buyCoin, 0);
            Assert.Greater(save.saleCoin, 0);
            Assert.Greater(save.unitsSold, 0);
            Assert.IsTrue(save.bestSaleSeen);
            Assert.IsTrue(session.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.IsTrue(HasAchievement(save, "ACH_FIRST_PIECE"));
            Assert.AreEqual(2, session.AgentCount);
            Assert.IsTrue(session.ScoutHired);
            Assert.IsFalse(session.HagglerHired);
            Assert.IsTrue(session.GuardHired);
            Assert.IsTrue(save.scoutHired);
            Assert.IsFalse(save.hagglerHired);
            Assert.IsTrue(save.guardHired);
            Assert.AreEqual(2, session.MerchantMountTier);
            Assert.AreEqual(2, save.merchantMountTier);
            Assert.AreEqual(2, save.packBeasts.Length);
            Assert.AreEqual(1, save.packBeasts[0]);
            Assert.AreEqual(2, save.packBeasts[1]);
            Assert.Greater(session.RoadNoteCount, 0);
            Assert.IsTrue(session.RoadNotesBlock().IndexOf("rush", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Greater(save.roadNotes.Length, 0);
            Assert.IsTrue(save.roadNotes[0].IndexOf("rush", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsTrue(session.HasRunFlag("watched"));
            Assert.IsTrue(session.HasRunFlag("prophet_ignored"));
            Assert.IsTrue(HasFlag(save, "watched"));
            Assert.IsTrue(HasFlag(save, "prophet_ignored"));
            Assert.IsTrue(session.HasTrait("FriendOfBandits"));
            Assert.IsTrue(save.friendOfBandits);
            Assert.GreaterOrEqual(session.RescueCount, 1);
            Assert.GreaterOrEqual(save.rescueCount, 1);
            Assert.IsFalse(string.IsNullOrEmpty(save.lastRescueText));
            Assert.Greater(session.StretchChecks, 0);
            Assert.AreEqual(session.StretchChecks, save.stretchChecks);
            Assert.AreEqual((int)RoadPace.Careful, save.chosenPace);
            Assert.Greater(save.legsTravelled, 0);
            Assert.AreEqual(RunSave.CurrentVersion, save.saveVersion);
        }

        static void Same(RunSave a, RunSave b)
        {
            Assert.AreEqual(a.saveVersion, b.saveVersion);
            Assert.IsFalse(string.IsNullOrEmpty(a.savedAtUtc));
            Assert.IsFalse(string.IsNullOrEmpty(b.savedAtUtc));
            Assert.AreEqual(a.buildVersion, b.buildVersion);
            Assert.AreEqual(a.coin, b.coin);
            Near(a.waterLitres, b.waterLitres);
            Near(a.rationKg, b.rationKg);
            Assert.AreEqual(a.day, b.day);
            Near(a.hour, b.hour);
            Near(a.travelHours, b.travelHours);
            Assert.AreEqual(a.dock, b.dock);
            Assert.AreEqual(a.travelFrom, b.travelFrom);
            Assert.AreEqual(a.travelTo, b.travelTo);
            Assert.AreEqual(a.chosenPace, b.chosenPace);
            Assert.AreEqual(a.stretchChecks, b.stretchChecks);
            Assert.AreEqual(a.packs.Length, b.packs.Length);
            for (int i = 0; i < a.packs.Length; i++)
            {
                Assert.AreEqual(a.packs[i].good, b.packs[i].good);
                Near(a.packs[i].integrity, b.packs[i].integrity);
                Assert.AreEqual(a.packs[i].decayedOnDay, b.packs[i].decayedOnDay);
                Assert.AreEqual(a.packs[i].basis, b.packs[i].basis);
            }

            Assert.AreEqual(a.merchantMountTier, b.merchantMountTier);
            CollectionAssert.AreEqual(a.packBeasts, b.packBeasts);
            Assert.AreEqual(a.scoutHired, b.scoutHired);
            Assert.AreEqual(a.hagglerHired, b.hagglerHired);
            Assert.AreEqual(a.guardHired, b.guardHired);
            CollectionAssert.AreEqual(a.roadNotes, b.roadNotes);
            CollectionAssert.AreEqual(a.shelfStock, b.shelfStock);
            Assert.AreEqual(a.eatAcc.Length, b.eatAcc.Length);
            for (int i = 0; i < a.eatAcc.Length; i++)
                Near(a.eatAcc[i], b.eatAcc[i]);
            Assert.AreEqual(a.makeAcc.Length, b.makeAcc.Length);
            for (int i = 0; i < a.makeAcc.Length; i++)
                Near(a.makeAcc[i], b.makeAcc[i]);
            CollectionAssert.AreEqual(a.boughtHere, b.boughtHere);
            SamePrices(a.loggedBuy, b.loggedBuy);
            SamePrices(a.loggedSale, b.loggedSale);
            Assert.AreEqual(a.influence.Length, b.influence.Length);
            for (int i = 0; i < a.influence.Length; i++)
                Near(a.influence[i], b.influence[i]);
            CollectionAssert.AreEqual(a.patron, b.patron);
            Assert.AreEqual(a.npcs.Length, b.npcs.Length);
            for (int i = 0; i < a.npcs.Length; i++)
                SameNpc(a.npcs[i], b.npcs[i]);
            Assert.AreEqual(a.weather.Length, b.weather.Length);
            for (int i = 0; i < a.weather.Length; i++)
                SameWeather(a.weather[i], b.weather[i]);
            Near(a.kharunPoints, b.kharunPoints);
            CollectionAssert.AreEqual(a.kharunFlipSeen, b.kharunFlipSeen);
            CollectionAssert.AreEqual(a.runFlags, b.runFlags);
            Assert.AreEqual(a.friendOfBandits, b.friendOfBandits);
            Assert.AreEqual(a.rescueCount, b.rescueCount);
            Assert.AreEqual(a.lastRescueText, b.lastRescueText);
            Assert.AreEqual(a.held.Length, b.held.Length);
            for (int i = 0; i < a.held.Length; i++)
            {
                Assert.AreEqual(a.held[i].owned, b.held[i].owned);
                Assert.AreEqual(a.held[i].day, b.held[i].day);
                Assert.AreEqual(a.held[i].listPrice, b.held[i].listPrice);
                Assert.AreEqual(a.held[i].paid, b.held[i].paid);
            }

            Assert.AreEqual(a.mapPieceLeads, b.mapPieceLeads);
            Assert.AreEqual(a.mapCoin, b.mapCoin);
            Assert.AreEqual(a.leadsUsed, b.leadsUsed);
            Assert.AreEqual(a.legsTravelled, b.legsTravelled);
            Assert.AreEqual(a.encountersResolved, b.encountersResolved);
            Assert.AreEqual(a.buyCoin, b.buyCoin);
            Assert.AreEqual(a.saleCoin, b.saleCoin);
            Assert.AreEqual(a.unitsSold, b.unitsSold);
            Assert.AreEqual(a.bestSaleSeen, b.bestSaleSeen);
            Assert.AreEqual(a.bestSaleCoin, b.bestSaleCoin);
            Assert.AreEqual(a.bestSaleGood, b.bestSaleGood);
            Assert.AreEqual(a.bestSalePlace, b.bestSalePlace);
            Assert.AreEqual(a.banditUnits, b.banditUnits);
            Assert.AreEqual(a.banditCoin, b.banditCoin);
            Assert.AreEqual(a.embarkedDay, b.embarkedDay);
            CollectionAssert.AreEqual(a.achievementIds, b.achievementIds);
            CollectionAssert.AreEqual(a.achievementFires, b.achievementFires);
        }

        static void SamePrices(RunSave.PriceRow[] a, RunSave.PriceRow[] b)
        {
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].seen, b[i].seen);
                Assert.AreEqual(a[i].coin, b[i].coin);
                Assert.AreEqual(a[i].place, b[i].place);
            }
        }

        static void SameNpc(RunSave.NpcRow a, RunSave.NpcRow b)
        {
            Assert.AreEqual(a.house, b.house);
            Assert.AreEqual(a.dock, b.dock);
            Assert.AreEqual(a.traveling, b.traveling);
            Assert.AreEqual(a.from, b.from);
            Assert.AreEqual(a.to, b.to);
            Near(a.distance, b.distance);
            Near(a.edgeLength, b.edgeLength);
            CollectionAssert.AreEqual(a.path, b.path);
            CollectionAssert.AreEqual(a.cargo, b.cargo);
        }

        static void SameWeather(RunSave.WeatherRow a, RunSave.WeatherRow b)
        {
            Assert.AreEqual(a.kind, b.kind);
            Near(a.centerX, b.centerX);
            Near(a.centerY, b.centerY);
            Near(a.radius, b.radius);
            Near(a.velocityX, b.velocityX);
            Near(a.velocityY, b.velocityY);
        }

        static void Near(float a, float b)
        {
            Assert.AreEqual(a, b, 0.0001f);
        }

        static int Slot(PlaceId place, GoodId good)
        {
            return (int)place * WorldIds.GoodCount + (int)good;
        }

        static bool HasFlag(RunSave save, string id)
        {
            for (int i = 0; i < save.runFlags.Length; i++)
            {
                if (save.runFlags[i] == id)
                    return true;
            }

            return false;
        }

        static bool HasAchievement(RunSave save, string id)
        {
            for (int i = 0; i < save.achievementIds.Length; i++)
            {
                if (save.achievementIds[i] == id)
                    return true;
            }

            return false;
        }

        static void Rejects(CaravanSession session)
        {
            Assert.IsFalse(session.AtSafePoint);
            Assert.IsFalse(RunSaveStore.TryWrite(session));
            Assert.IsFalse(File.Exists(SaveSlot.MainPath));
            Assert.IsFalse(SaveSlot.HasValidSave());
        }

        static void WriteCoin(int coin)
        {
            var session = new CaravanSession();
            session.SetCoin(coin);
            Assert.IsTrue(session.AtSafePoint);
            Assert.IsTrue(RunSaveStore.TryWrite(session));
        }

        static void ExpectSkip(string fileName)
        {
            LogAssert.Expect(LogType.Warning, "Caravans save skipped (" + fileName + ").");
        }

        static void LoadsBackup()
        {
            CaravanSession loaded;
            Assert.IsTrue(RunSaveStore.TryLoad(out loaded));
            Assert.AreEqual(111, loaded.Coin);
            ExpectSkip("caravans_save.json");
            Assert.IsTrue(SaveSlot.HasValidSave());
        }

        static string WithVersion(string json, int version)
        {
            const string key = "\"saveVersion\":";
            int at = json.IndexOf(key, StringComparison.Ordinal);
            Assert.GreaterOrEqual(at, 0);
            int start = at + key.Length;
            int end = start;
            while (end < json.Length && char.IsDigit(json[end]))
                end++;
            return json.Substring(0, start) + version.ToString() + json.Substring(end);
        }

        static void Refill(CaravanSession session)
        {
            session.SetKit(session.KitWaterCapacity, session.KitRationCapacity);
        }

        static void Leave(CaravanSession session, PlaceId dest)
        {
            if (!session.CanTakeRoad())
                Assert.IsTrue(session.TryRest());
            Refill(session);
            Assert.IsTrue(session.DepartTo(dest));
            Ride(session);
        }

        static void DepartSame(CaravanSession session, PlaceId dest)
        {
            if (!session.CanTakeRoad())
                Assert.IsTrue(session.TryRest());
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(dest));
        }

        static void Ride(CaravanSession session)
        {
            for (int i = 0; i < 4000 && session.Traveling; i++)
            {
                if (session.DeathPending)
                    break;
                if (session.EncounterPending)
                    session.SkipEncounter();
                if (session.CampPending)
                    session.MakeCamp();
                if (session.AwaitingPace)
                {
                    session.ChooseRoadPace(session.ChosenPace);
                    if (session.EncounterPending)
                        session.SkipEncounter();
                }

                session.Tick(0.25f);
            }

            Assert.IsFalse(session.DeathPending, "The ride ended in a rescue.");
            Assert.IsFalse(session.Traveling);
        }
    }
}
