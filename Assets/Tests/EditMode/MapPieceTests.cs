using NUnit.Framework;

namespace Caravans.Play.Tests
{
    public class MapPieceTests
    {
        [Test]
        public void PiecesSellInOrderAndRankDoesNotChangeThePrice()
        {
            var session = new CaravanSession();
            Assert.IsFalse(session.MapPieceOnSale(1));
            Assert.AreEqual(-1, session.NextMapPiecePrice());

            session.SetKharunPoints(100f);
            session.SetCoin(20000);
            Assert.AreEqual(1, session.GetKharunRank());
            Assert.IsTrue(session.MapPieceOnSale(1));
            Assert.IsFalse(session.MapPieceOnSale(2));
            Assert.AreEqual(600, session.NextMapPiecePrice());
            Assert.AreEqual("Needs Kharûn rank 3", session.MapPieceLock(2));

            session.SetKharunPoints(540f);
            Assert.AreEqual(5, session.GetKharunRank());
            Assert.IsTrue(session.MapPieceOnSale(1));
            Assert.IsFalse(session.MapPieceOnSale(2));
            Assert.AreEqual(600, session.NextMapPiecePrice());

            session.SetKharunPoints(1000f);
            int coin = session.Coin;
            int cargo = session.CargoCount;
            Assert.IsTrue(session.TryBuyMapPiece(1));
            Assert.AreEqual(600, coin - session.Coin);
            Assert.AreEqual(cargo, session.CargoCount);
            Assert.IsTrue(session.MapPieceOwned(1));
            Assert.IsTrue(session.MapPieceOnSale(2));
            Assert.AreEqual(1900, session.NextMapPiecePrice());
            Assert.AreEqual("Needs Kharûn rank 3", session.MapPieceLock(2));
        }

        [Test]
        public void BankedLeadsDiscountTheNextPieceThenClear()
        {
            var session = Ready();
            Assert.AreEqual(600, Price(session, 0));
            session.SetMapPieceLeads(1);
            Assert.AreEqual(540, Price(session, 1));
            session.SetMapPieceLeads(2);
            Assert.AreEqual(480, Price(session, 2));
            session.SetMapPieceLeads(3);
            Assert.AreEqual(2, session.MapPieceLeads);
            Assert.AreEqual(480, session.NextMapPiecePrice());

            Assert.IsTrue(session.TryBuyMapPiece(1));
            Assert.AreEqual(0, session.MapPieceLeads);
            Assert.AreEqual(480, session.MapCoinPaid);
            Assert.AreEqual(2, session.LeadsUsed);

            session.SetKharunPoints(300f);
            Assert.AreEqual(1900, session.NextMapPiecePrice());
            session.SetMapPieceLeads(1);
            Assert.AreEqual(1710, session.NextMapPiecePrice());
            session.SetMapPieceLeads(2);
            Assert.AreEqual(1520, session.NextMapPiecePrice());
        }

        [Test]
        public void AShortPurseOrTheWrongDockCannotBuy()
        {
            var session = new CaravanSession();
            session.SetKharunPoints(100f);
            session.SetCoin(599);
            Assert.IsTrue(session.MapPieceOnSale(1));
            Assert.IsFalse(session.TryBuyMapPiece(1));
            Assert.AreEqual(599, session.Coin);
            Assert.IsFalse(session.MapPieceOwned(1));
            Assert.AreEqual(0, session.CargoCount);

            session.SetCoin(600);
            session.ShotDockAt(PlaceId.Draven);
            Assert.IsFalse(session.TryBuyMapPiece(1));
            Assert.AreEqual(600, session.Coin);
            Assert.IsFalse(session.MapPieceOwned(1));

            session.ShotDockAt(PlaceId.Kharun);
            Assert.IsTrue(session.TryBuyMapPiece(1));
            Assert.AreEqual(0, session.Coin);
            Assert.AreEqual(0, session.CargoCount);
            Assert.IsTrue(session.AchievementUnlocked("ACH_FIRST_PIECE"));
        }

        [Test]
        public void TheFirstPieceAchievementFiresOnce()
        {
            var session = Ready();
            Assert.IsFalse(session.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.AreEqual(0, session.AchievementUnlocks("ACH_FIRST_PIECE"));
            Assert.IsTrue(session.TryBuyMapPiece(1));
            Assert.IsTrue(session.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_PIECE"));
            session.SetKharunPoints(300f);
            Assert.IsTrue(session.TryBuyMapPiece(2));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_FIRST_PIECE"));
            Assert.IsFalse(session.AchievementUnlocked("ACH_EMBARK"));
        }

        [Test]
        public void AllSixPiecesOpenTheWestmarkRoadAndTheLedger()
        {
            var session = Ready();
            Assert.IsFalse(session.MapJourneyUnlocked);
            for (int i = 1; i <= 5; i++)
            {
                Assert.IsTrue(session.TryBuyMapPiece(i));
                session.AdvanceHours(24f);
            }

            Assert.IsFalse(session.MapJourneyUnlocked);
            Assert.IsFalse(session.TryStartGateRoad());
            Assert.IsTrue(session.TryBuyMapPiece(6));
            Assert.IsTrue(session.MapJourneyUnlocked);
            Assert.AreEqual(30500, session.MapCoinPaid);
            Assert.IsFalse(session.TryStartGateRoad());

            session.ShotDockAt(PlaceId.Westmark);
            Assert.IsTrue(session.TryStartGateRoad());
            Assert.AreEqual("Road to Sarn's Rest", session.BoundLabel());
            session.CompleteMapLeg();
            Assert.IsTrue(session.RestPending);
            Assert.AreEqual("Sarn's Rest", session.RestTitle);
            Assert.AreEqual("Sarn's Rest", session.BoundLabel());
            Assert.IsTrue(session.RestScene.Contains("Plain lilies at the water"));
            Assert.AreEqual("Embark", session.RestEmbarkLabel);
            Assert.AreEqual("Return later", session.RestReturnLabel);
            Assert.IsTrue(session.RestResult(0).Contains("ride out along the line"));
            Assert.IsTrue(session.RestResult(1).Contains("turn back for Westmark"));

            Assert.IsTrue(session.TryStartReturn());
            Assert.AreEqual("Road to Westmark", session.BoundLabel());
            session.CompleteMapLeg();
            Assert.AreEqual(PlaceId.Westmark, session.Dock);
            Assert.IsFalse(session.Traveling);
            Assert.IsFalse(session.RestPending);
            Assert.IsTrue(session.MapJourneyUnlocked);

            Assert.IsTrue(session.TryStartGateRoad());
            session.CompleteMapLeg();
            Assert.IsTrue(session.TryStartFinalRun());
            Assert.AreEqual("Road to Sarn's Rest", session.BoundLabel());
            session.CompleteMapLeg();
            Assert.IsTrue(session.EndScreenUp);
            Assert.IsTrue(session.AchievementUnlocked("ACH_EMBARK"));
            Assert.AreEqual(1, session.AchievementUnlocks("ACH_EMBARK"));
            Assert.AreEqual("Rank 10", session.EndStat("standing"));
            Assert.AreEqual("0 of 15", session.EndStat("placesWon"));
            Assert.AreEqual("30500 coin", session.EndStat("mapCoin"));
            Assert.IsTrue(session.EndStat("mapDays").Contains("1 Vekmar, Year 112"));
            Assert.IsTrue(session.EndStat("mapDays").Contains(WorldClock.Date(session.MapPieceDay(6))));
            Assert.AreEqual("Embarked", session.EndTitle);
            Assert.AreEqual("Return to the title", session.EndButtonLabel);
            Assert.IsTrue(session.EndLedger().Contains("The ledger"));
            Assert.IsTrue(session.EndLedger().Contains("Every road has a price."));
            Assert.AreEqual(0, session.CargoCount);
        }

        [Test]
        public void ADeathOnTheLandmarkKeepsThePieces()
        {
            var gate = Bought();
            int day = gate.MapPieceDay(1);
            gate.SetMapPieceLeads(2);
            gate.GiveCargo(GoodId.MorRoot, 1f);
            gate.ShotDockAt(PlaceId.Westmark);
            Assert.IsTrue(gate.TryStartGateRoad());
            gate.SetKit(0.05f, 0.05f);
            gate.SpendRoadHours(1f);
            Assert.IsTrue(gate.DeathPending);
            int coin = gate.Coin;
            gate.ContinueDeath();
            Assert.IsFalse(gate.DeathPending);
            Assert.AreEqual(PlaceId.Kharun, gate.Dock);
            Assert.AreEqual(0, gate.CargoCount);
            Assert.AreEqual(coin / 2, gate.Coin);
            Assert.AreEqual(6, gate.OwnedMapPieces);
            Assert.AreEqual(2, gate.MapPieceLeads);
            Assert.AreEqual(day, gate.MapPieceDay(1));
            Assert.IsTrue(gate.MapJourneyUnlocked);
            Assert.IsTrue(gate.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.IsFalse(gate.EndScreenUp);

            var finalRun = Bought();
            finalRun.ShotDockAt(PlaceId.Westmark);
            Assert.IsTrue(finalRun.TryStartGateRoad());
            finalRun.CompleteMapLeg();
            Assert.IsTrue(finalRun.TryStartFinalRun());
            finalRun.SetKit(0.05f, 0.05f);
            finalRun.SpendRoadHours(1f);
            Assert.IsTrue(finalRun.DeathPending);
            finalRun.ContinueDeath();
            Assert.AreEqual(PlaceId.Kharun, finalRun.Dock);
            Assert.AreEqual(6, finalRun.OwnedMapPieces);
            Assert.IsTrue(finalRun.MapJourneyUnlocked);
            Assert.IsFalse(finalRun.EndScreenUp);
            Assert.IsFalse(finalRun.AchievementUnlocked("ACH_EMBARK"));
        }

        [Test]
        public void ANewGameHasNoPiecesAndNoJourney()
        {
            var played = Bought();
            Assert.IsTrue(played.MapJourneyUnlocked);
            var fresh = new CaravanSession();
            Assert.AreEqual(0, fresh.OwnedMapPieces);
            Assert.AreEqual(0, fresh.MapPieceLeads);
            Assert.AreEqual(0, fresh.MapCoinPaid);
            Assert.IsFalse(fresh.MapJourneyUnlocked);
            Assert.IsFalse(fresh.MapPieceOwned(1));
            Assert.IsFalse(fresh.AchievementUnlocked("ACH_FIRST_PIECE"));
            Assert.IsFalse(fresh.AchievementUnlocked("ACH_EMBARK"));
            Assert.IsTrue(fresh.MapLogbookBlock().Contains("No pieces of Sarn's map yet."));
        }

        static CaravanSession Ready()
        {
            var session = new CaravanSession();
            session.SetKharunPoints(1000f);
            session.SetCoin(40000);
            return session;
        }

        static CaravanSession Bought()
        {
            var session = Ready();
            for (int i = 1; i <= 6; i++)
                Assert.IsTrue(session.TryBuyMapPiece(i));
            return session;
        }

        static int Price(CaravanSession session, int leads)
        {
            session.SetMapPieceLeads(leads);
            return session.NextMapPiecePrice();
        }
    }
}
