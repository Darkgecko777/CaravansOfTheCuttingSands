using System;
using System.Globalization;
using UnityEngine;

namespace Caravans.Play
{
    public sealed partial class CaravanSession
    {
        bool saveDue;

        internal bool SaveDue
        {
            get { return saveDue; }
        }

        public bool AtSafePoint
        {
            get
            {
                return !Traveling
                    && !EncounterPending
                    && !EncounterResolved
                    && !CampPending
                    && !AwaitingPace
                    && !CriticalPending
                    && !DeathPending
                    && !RestPending
                    && mapLeg == MapLegKind.None
                    && !EndScreenUp;
            }
        }

        internal void ClearSaveDue()
        {
            saveDue = false;
        }

        void MarkSaveDue()
        {
            saveDue = true;
        }

        internal void SetPackBeasts(params int[] tiers)
        {
            packBeasts = tiers == null || tiers.Length == 0 ? new int[0] : (int[])tiers.Clone();
        }

        internal RunSave CaptureSave()
        {
            var save = new RunSave();
            save.saveVersion = RunSave.CurrentVersion;
            save.savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            save.buildVersion = Application.version ?? "";
            save.coin = Coin;
            save.waterLitres = WaterLitres;
            save.rationKg = RationKg;
            save.day = Day;
            save.hour = Hour;
            save.travelHours = TravelHours;
            save.dock = (int)Dock;
            save.travelFrom = (int)travelFrom;
            save.travelTo = (int)travelTo;
            save.chosenPace = (int)ChosenPace;
            save.stretchChecks = StretchChecks;
            save.packs = new RunSave.PackRow[stacks.Count];
            for (int i = 0; i < stacks.Count; i++)
            {
                CargoUnit unit = stacks[i];
                save.packs[i] = new RunSave.PackRow
                {
                    good = (int)unit.Good,
                    integrity = unit.Integrity,
                    decayedOnDay = unit.DecayedOnDay,
                    basis = unit.Basis
                };
            }

            save.merchantMountTier = merchantMountTier;
            save.packBeasts = packBeasts == null ? new int[0] : (int[])packBeasts.Clone();
            save.scoutHired = scoutHired;
            save.hagglerHired = hagglerHired;
            save.guardHired = guardHired;
            save.roadNotes = roadNotes.ToArray();

            int places = WorldIds.PlaceCount;
            int goods = WorldIds.GoodCount;
            int houses = WorldIds.HouseCount;
            save.shelfStock = new int[places * goods];
            save.eatAcc = new float[places * goods];
            save.makeAcc = new float[places * goods];
            save.boughtHere = (int[])boughtHere.Clone();
            for (int place = 0; place < places; place++)
            {
                for (int good = 0; good < goods; good++)
                {
                    int index = place * goods + good;
                    save.shelfStock[index] = shelves[place, good].Stock;
                    save.eatAcc[index] = eatAcc[place, good];
                    save.makeAcc[index] = makeAcc[place, good];
                }
            }

            save.loggedBuy = Prices(loggedBuy);
            save.loggedSale = Prices(loggedSale);
            save.influence = new float[places * houses];
            save.patron = new int[places];
            save.kharunFlipSeen = (bool[])kharunFlipSeen.Clone();
            for (int place = 0; place < places; place++)
            {
                HouseId? holder = patron[place];
                save.patron[place] = holder.HasValue ? (int)holder.Value : -1;
                for (int house = 0; house < houses; house++)
                    save.influence[place * houses + house] = influence[place, house];
            }

            save.npcs = new RunSave.NpcRow[npcs.Count];
            for (int i = 0; i < npcs.Count; i++)
                save.npcs[i] = CaptureNpc(npcs[i]);
            save.weather = new RunSave.WeatherRow[masses.Count];
            for (int i = 0; i < masses.Count; i++)
            {
                WeatherMass mass = masses[i];
                save.weather[i] = new RunSave.WeatherRow
                {
                    kind = mass == null ? 0 : (int)mass.Kind,
                    centerX = mass == null ? 0f : mass.Center.x,
                    centerY = mass == null ? 0f : mass.Center.y,
                    radius = mass == null ? 0.1f : mass.Radius,
                    velocityX = mass == null ? 0f : mass.Velocity.x,
                    velocityY = mass == null ? 0f : mass.Velocity.y
                };
            }

            save.kharunPoints = kharunPoints;
            var flags = new System.Collections.Generic.List<string>(runFlags);
            flags.Sort(StringComparer.Ordinal);
            save.runFlags = flags.ToArray();
            save.friendOfBandits = friendOfBandits;
            save.rescueCount = rescueCount;
            save.lastRescueText = lastRescueText ?? "";

            EnsureHeld();
            save.held = new RunSave.HeldRow[held.Length];
            for (int i = 0; i < held.Length; i++)
            {
                save.held[i] = new RunSave.HeldRow
                {
                    owned = held[i].Owned,
                    day = held[i].Day,
                    listPrice = held[i].ListPrice,
                    paid = held[i].Paid
                };
            }

            save.mapPieceLeads = MapPieceLeads;
            save.mapCoin = mapCoin;
            save.leadsUsed = leadsUsed;
            save.legsTravelled = legsTravelled;
            save.encountersResolved = encountersResolved;
            save.buyCoin = buyCoin;
            save.saleCoin = saleCoin;
            save.unitsSold = unitsSold;
            save.bestSaleSeen = bestSaleSeen;
            save.bestSaleCoin = bestSaleCoin;
            save.bestSaleGood = (int)bestSaleGood;
            save.bestSalePlace = (int)bestSalePlace;
            save.banditUnits = banditUnits;
            save.banditCoin = banditCoin;
            save.embarkedDay = embarkedDay < 1 ? 1 : embarkedDay;
            achievements.Export(out save.achievementIds, out save.achievementFires);
            return save;
        }

        internal static bool TryRestore(RunSave save, out CaravanSession session)
        {
            session = null;
            if (!RunSaveStore.Accept(save))
                return false;
            var restored = new CaravanSession();
            restored.ApplySave(save);
            session = restored;
            return true;
        }

        void ApplySave(RunSave save)
        {
            Coin = save.coin;
            WaterLitres = save.waterLitres;
            RationKg = save.rationKg;
            Day = save.day;
            Hour = save.hour;
            TravelHours = save.travelHours;
            Dock = (PlaceId)save.dock;
            travelFrom = (PlaceId)save.travelFrom;
            travelTo = (PlaceId)save.travelTo;
            ChosenPace = (RoadPace)save.chosenPace;
            StretchChecks = save.stretchChecks;
            stacks.Clear();
            for (int i = 0; i < save.packs.Length; i++)
            {
                RunSave.PackRow pack = save.packs[i];
                stacks.Add(new CargoUnit
                {
                    Good = (GoodId)pack.good,
                    Integrity = pack.integrity,
                    DecayedOnDay = pack.decayedOnDay,
                    Basis = pack.basis
                });
            }

            merchantMountTier = save.merchantMountTier;
            packBeasts = (int[])save.packBeasts.Clone();
            scoutHired = save.scoutHired;
            hagglerHired = save.hagglerHired;
            guardHired = save.guardHired;
            roadNotes.Clear();
            for (int i = 0; i < save.roadNotes.Length; i++)
                roadNotes.Add(save.roadNotes[i]);

            int places = WorldIds.PlaceCount;
            int goods = WorldIds.GoodCount;
            int houses = WorldIds.HouseCount;
            for (int place = 0; place < places; place++)
            {
                patron[place] = save.patron[place] < 0 ? (HouseId?)null : (HouseId)save.patron[place];
                kharunFlipSeen[place] = save.kharunFlipSeen[place];
                for (int good = 0; good < goods; good++)
                {
                    int index = place * goods + good;
                    Shelf shelf = shelves[place, good];
                    shelf.Stock = save.shelfStock[index];
                    shelves[place, good] = shelf;
                    eatAcc[place, good] = save.eatAcc[index];
                    makeAcc[place, good] = save.makeAcc[index];
                }

                for (int house = 0; house < houses; house++)
                    influence[place, house] = save.influence[place * houses + house];
            }

            for (int i = 0; i < boughtHere.Length && i < save.boughtHere.Length; i++)
                boughtHere[i] = save.boughtHere[i];
            CopyPrices(loggedBuy, save.loggedBuy);
            CopyPrices(loggedSale, save.loggedSale);

            npcs.Clear();
            for (int i = 0; i < save.npcs.Length; i++)
                npcs.Add(RestoreNpc(save.npcs[i]));
            masses.Clear();
            for (int i = 0; i < save.weather.Length; i++)
            {
                RunSave.WeatherRow row = save.weather[i];
                masses.Add(new WeatherMass
                {
                    Kind = (WeatherKind)row.kind,
                    Center = new Vector2(row.centerX, row.centerY),
                    Radius = row.radius,
                    Velocity = new Vector2(row.velocityX, row.velocityY)
                });
            }

            kharunPoints = save.kharunPoints;
            runFlags.Clear();
            for (int i = 0; i < save.runFlags.Length; i++)
                runFlags.Add(save.runFlags[i]);
            friendOfBandits = save.friendOfBandits;
            rescueCount = save.rescueCount;
            lastRescueText = string.IsNullOrEmpty(save.lastRescueText) ? "" : save.lastRescueText;

            held = new HeldPiece[save.held.Length];
            for (int i = 0; i < save.held.Length; i++)
            {
                RunSave.HeldRow row = save.held[i];
                held[i] = new HeldPiece
                {
                    Owned = row.owned,
                    Day = row.day,
                    ListPrice = row.listPrice,
                    Paid = row.paid
                };
            }

            MapPieceLeads = save.mapPieceLeads;
            mapCoin = save.mapCoin;
            leadsUsed = save.leadsUsed;
            legsTravelled = save.legsTravelled;
            encountersResolved = save.encountersResolved;
            buyCoin = save.buyCoin;
            saleCoin = save.saleCoin;
            unitsSold = save.unitsSold;
            bestSaleSeen = save.bestSaleSeen;
            bestSaleCoin = save.bestSaleCoin;
            bestSaleGood = (GoodId)save.bestSaleGood;
            bestSalePlace = (PlaceId)save.bestSalePlace;
            banditUnits = save.banditUnits;
            banditCoin = save.banditCoin;
            embarkedDay = save.embarkedDay;
            achievements.Import(save.achievementIds, save.achievementFires);

            House = HouseId.Kharun;
            Traveling = false;
            TravelDistance = 0f;
            HoldTravel = false;
            Pace = 1;
            edgeLength = RoadGraph.Length(travelFrom, travelTo);
            ClearEncounter();
            ClearMapTravel();
            ClearSupplyFlags();
            RankUpNotice = null;
            lastNotice = string.Empty;
            RestoreGuardSave();
            SyncHiredCount();
            Reseed(save.day, save.stretchChecks, save.legsTravelled);
            NoteStandingAchievements();
            Touch();
        }

        void Reseed(int day, int checks, int legs)
        {
            int mix;
            unchecked
            {
                mix = GameConfig.Encounters.seed;
                mix = mix * 31 + day;
                mix = mix * 31 + checks;
                mix = mix * 31 + legs;
            }

            if (mix == int.MinValue)
                mix = 1;
            encounterRandom = new System.Random(mix);
            rescueRandom = new System.Random(mix);
        }

        static RunSave.PriceRow[] Prices(LoggedPrice[] source)
        {
            var rows = new RunSave.PriceRow[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                rows[i] = new RunSave.PriceRow
                {
                    seen = source[i].Seen,
                    coin = source[i].Coin,
                    place = (int)source[i].Place
                };
            }

            return rows;
        }

        static void CopyPrices(LoggedPrice[] destination, RunSave.PriceRow[] source)
        {
            int count = destination.Length < source.Length ? destination.Length : source.Length;
            for (int i = 0; i < count; i++)
            {
                destination[i] = new LoggedPrice
                {
                    Seen = source[i].seen,
                    Coin = source[i].coin,
                    Place = (PlaceId)source[i].place
                };
            }
        }

        static RunSave.NpcRow CaptureNpc(NpcCaravan npc)
        {
            var row = new RunSave.NpcRow();
            row.house = (int)npc.House;
            row.dock = (int)npc.Dock;
            row.traveling = npc.Traveling;
            row.from = (int)npc.From;
            row.to = (int)npc.To;
            row.distance = npc.Distance;
            row.edgeLength = npc.EdgeLength;
            row.path = new int[npc.Path.Count];
            for (int i = 0; i < npc.Path.Count; i++)
                row.path[i] = (int)npc.Path[i];
            row.cargo = (int[])npc.Cargo.Clone();
            return row;
        }

        static NpcCaravan RestoreNpc(RunSave.NpcRow row)
        {
            var npc = new NpcCaravan();
            npc.House = (HouseId)row.house;
            npc.Dock = (PlaceId)row.dock;
            npc.Traveling = row.traveling;
            npc.From = (PlaceId)row.from;
            npc.To = (PlaceId)row.to;
            npc.Distance = row.distance;
            npc.EdgeLength = row.edgeLength;
            for (int i = 0; i < row.path.Length; i++)
                npc.Path.Add((PlaceId)row.path[i]);
            int count = row.cargo.Length < npc.Cargo.Length ? row.cargo.Length : npc.Cargo.Length;
            for (int i = 0; i < count; i++)
                npc.Cargo[i] = row.cargo[i];
            return npc;
        }
    }
}
