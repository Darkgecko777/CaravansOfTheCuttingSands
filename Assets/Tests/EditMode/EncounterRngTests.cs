using System.Collections.Generic;
using NUnit.Framework;

namespace Caravans.Play.Tests
{
    public class EncounterRngTests
    {
        [Test]
        public void DifferentSeedsOpenDifferentCardsOnTheFirstLeg()
        {
            var seen = new HashSet<string>();
            for (int seed = 1; seed <= 64; seed++)
            {
                var session = new CaravanSession(seed);
                bool pending;
                string id;
                float hour;
                OpenLeg(session, PlaceId.Draven, out pending, out id, out hour);
                if (pending && !string.IsNullOrEmpty(id))
                    seen.Add(id);
            }

            Assert.GreaterOrEqual(seen.Count, 4);

            var first = new CaravanSession();
            var second = new CaravanSession();
            Assert.AreNotEqual(0, first.RunSeed);
            Assert.AreNotEqual(0, second.RunSeed);
            Assert.AreNotEqual(first.RunSeed, second.RunSeed);
        }

        [Test]
        public void TheSameSeedRepeatsDocksAndTheFirstTwoLegs()
        {
            const int seed = 23;
            var left = new CaravanSession(seed);
            var right = new CaravanSession(seed);
            Assert.AreEqual(left.NpcCount, right.NpcCount);
            for (int i = 0; i < left.NpcCount; i++)
                Assert.AreEqual(left.NpcDock(i), right.NpcDock(i));

            MatchLeg(left, right, PlaceId.Draven);
            FinishLeg(left);
            FinishLeg(right);
            Assert.AreEqual(PlaceId.Draven, left.Dock);
            Assert.AreEqual(PlaceId.Draven, right.Dock);
            MatchLeg(left, right, PlaceId.Kharun);
        }

        static void MatchLeg(CaravanSession left, CaravanSession right, PlaceId dest)
        {
            bool leftPending;
            string leftId;
            float leftHour;
            bool rightPending;
            string rightId;
            float rightHour;
            OpenLeg(left, dest, out leftPending, out leftId, out leftHour);
            OpenLeg(right, dest, out rightPending, out rightId, out rightHour);
            Assert.AreEqual(leftPending, rightPending);
            Assert.AreEqual(leftId, rightId);
            Assert.AreEqual(leftHour, rightHour, 0.0001f);
        }

        static void OpenLeg(CaravanSession session, PlaceId dest, out bool pending, out string id, out float hour)
        {
            if (!session.CanTakeRoad())
                Assert.IsTrue(session.TryRest());
            session.SetKit(session.KitWaterCapacity, session.KitRationCapacity);
            session.ChooseRoadPace(RoadPace.Steady);
            Assert.IsTrue(session.DepartTo(dest));
            for (int i = 0; i < 4000 && session.Traveling && !session.EncounterPending && !session.DeathPending; i++)
            {
                if (session.CampPending)
                    session.MakeCamp();
                if (session.AwaitingPace)
                    session.ChooseRoadPace(RoadPace.Steady);
                if (session.EncounterPending)
                    break;
                session.Tick(0.25f);
            }

            pending = session.EncounterPending;
            id = session.EncounterId;
            hour = session.EncounterHour;
        }

        static void FinishLeg(CaravanSession session)
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
                    session.ChooseRoadPace(RoadPace.Steady);
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
