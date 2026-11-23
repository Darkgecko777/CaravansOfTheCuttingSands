using System;
using System.Text;
using UnityEngine;

namespace Caravans.Play
{
    public sealed partial class CaravanSession
    {
        enum MapLegKind
        {
            None,
            ToRest,
            Return,
            Final
        }

        struct HeldPiece
        {
            public bool Owned;
            public int Day;
            public int ListPrice;
            public int Paid;
        }

        HeldPiece[] held = new HeldPiece[0];
        int mapCoin;
        int leadsUsed;
        int legsTravelled;
        int encountersResolved;
        int buyCoin;
        int saleCoin;
        int unitsSold;
        bool bestSaleSeen;
        int bestSaleCoin;
        GoodId bestSaleGood;
        PlaceId bestSalePlace;
        int banditUnits;
        int banditCoin;
        int embarkedDay = 1;
        int cardIndex;
        float mapSteadyHours;
        Vector2 mapFrom;
        Vector2 mapTo;
        string scriptTitle;
        string scriptBody;
        bool scriptCard;
        float previewSteady;
        float previewScale = 1f;
        bool atRest;
        bool endScreen;
        MapLegKind mapLeg = MapLegKind.None;
        readonly RunAchievements achievements = new RunAchievements();

        public int OwnedMapPieces
        {
            get
            {
                EnsureHeld();
                int count = 0;
                for (int i = 0; i < held.Length; i++)
                {
                    if (held[i].Owned)
                        count++;
                }

                return count;
            }
        }

        public int MapCoinPaid
        {
            get { return mapCoin; }
        }

        public int LeadsUsed
        {
            get { return leadsUsed; }
        }

        public bool MapJourneyUnlocked
        {
            get
            {
                int need = PiecesRequired();
                return need > 0 && OwnedMapPieces >= need;
            }
        }

        public bool MapJourneyAtGate
        {
            get { return MapJourneyUnlocked && Dock == GatePlace(); }
        }

        public bool RestPending
        {
            get { return atRest && !Traveling && !endScreen; }
        }

        public bool EndScreenUp
        {
            get { return endScreen; }
        }

        public float GateSteadyHours
        {
            get
            {
                MapPieceFile file = GameConfig.MapPieces;
                return file != null && file.gateSteadyHours > 0f ? file.gateSteadyHours : 12f;
            }
        }

        public float FinalSteadyHours
        {
            get
            {
                MapPieceFile file = GameConfig.MapPieces;
                return file != null && file.finalSteadyHours > 0f ? file.finalSteadyHours : 18f;
            }
        }

        public string RestTitle
        {
            get
            {
                MapPieceFile file = GameConfig.MapPieces;
                return file != null && !string.IsNullOrEmpty(file.arrivalTitle) ? file.arrivalTitle : "Sarn's Rest";
            }
        }

        public string RestScene
        {
            get
            {
                MapPieceFile file = GameConfig.MapPieces;
                return file != null ? file.arrivalScene ?? string.Empty : string.Empty;
            }
        }

        public string RestEmbarkLabel
        {
            get
            {
                MapPieceFile file = GameConfig.MapPieces;
                return file != null && !string.IsNullOrEmpty(file.embarkLabel) ? file.embarkLabel : "Embark";
            }
        }

        public string RestReturnLabel
        {
            get
            {
                MapPieceFile file = GameConfig.MapPieces;
                return file != null && !string.IsNullOrEmpty(file.returnLabel) ? file.returnLabel : "Return later";
            }
        }

        public string RestResult(int index)
        {
            MapPieceFile file = GameConfig.MapPieces;
            if (file == null)
                return string.Empty;
            return index == 0 ? file.embarkResult ?? string.Empty : file.returnResult ?? string.Empty;
        }

        public string EndTitle
        {
            get
            {
                EndScreenFile file = GameConfig.EndScreen;
                return file != null && !string.IsNullOrEmpty(file.title) ? file.title : "Embarked";
            }
        }

        public string EndButtonLabel
        {
            get
            {
                EndScreenFile file = GameConfig.EndScreen;
                if (file == null || file.buttons == null || file.buttons.Length == 0 || string.IsNullOrEmpty(file.buttons[0].label))
                    return "Return to the title";
                return file.buttons[0].label;
            }
        }

        public bool MapPieceOwned(int oneBased)
        {
            int index = oneBased - 1;
            EnsureHeld();
            return index >= 0 && index < held.Length && held[index].Owned;
        }

        public bool MapPieceOnSale(int oneBased)
        {
            if (Traveling || atRest || endScreen || Dock != SellerPlace())
                return false;
            int next = NextUnowned();
            return next >= 0 && oneBased - 1 == next && RankMet(next);
        }

        public int MapPieceListPrice(int oneBased)
        {
            MapPieceFile.Piece row = PieceRow(oneBased - 1);
            return row == null ? 0 : row.price;
        }

        public int MapPieceDay(int oneBased)
        {
            int index = oneBased - 1;
            EnsureHeld();
            if (index < 0 || index >= held.Length || !held[index].Owned)
                return 0;
            return held[index].Day;
        }

        public string MapPieceName(int oneBased)
        {
            MapPieceFile.Piece row = PieceRow(oneBased - 1);
            return row != null && !string.IsNullOrEmpty(row.name) ? row.name : "Piece of Sarn's map";
        }

        public string MapPieceBody()
        {
            MapPieceFile file = GameConfig.MapPieces;
            return file != null && !string.IsNullOrEmpty(file.pieceText) ? file.pieceText : "A fragment of Sarn's map.";
        }

        public string MapPieceLock(int oneBased)
        {
            MapPieceFile.Piece row = PieceRow(oneBased - 1);
            if (row == null)
                return string.Empty;
            return "Needs Kharûn rank " + row.unlockRank;
        }

        public int NextMapPiecePrice()
        {
            if (!MapPieceOnSale(NextUnowned() + 1))
                return -1;
            MapPieceFile.Piece row = PieceRow(NextUnowned());
            if (row == null)
                return -1;
            return AdjustedPrice(row.price);
        }

        public bool TryBuyMapPiece(int oneBased)
        {
            if (!MapPieceOnSale(oneBased))
                return false;
            int index = oneBased - 1;
            MapPieceFile.Piece row = PieceRow(index);
            if (row == null || held[index].Owned)
                return false;
            int price = AdjustedPrice(row.price);
            if (Coin < price)
                return false;
            int spent = BankedLeads();
            Coin -= price;
            held[index].Owned = true;
            held[index].Day = Day;
            held[index].ListPrice = row.price;
            held[index].Paid = price;
            mapCoin += price;
            leadsUsed += spent;
            MapPieceLeads = 0;
            if (OwnedMapPieces == 1)
                achievements.Unlock(FirstAchievement());
            Touch();
            return true;
        }

        public bool AchievementUnlocked(string id)
        {
            return achievements.IsUnlocked(id);
        }

        public int AchievementUnlocks(string id)
        {
            return achievements.UnlockCount(id);
        }

        public string PlacesWonLine()
        {
            return PlacesWonCount() + " of 15";
        }

        public string EndStat(string key)
        {
            switch (key)
            {
                case "dateEmbarked":
                    return WorldClock.Date(embarkedDay);
                case "daysOnRoad":
                    return (Day - 1).ToString();
                case "coinTotal":
                    return Coin + " coin";
                case "tradeProfit":
                    return (saleCoin - buyCoin) + " coin";
                case "unitsSold":
                    return unitsSold.ToString();
                case "bestSale":
                    if (!bestSaleSeen)
                        return "\u2014";
                    return GoodName(bestSaleGood) + ", " + bestSaleCoin + " coin, " + PlaceName(bestSalePlace);
                case "legsTravelled":
                    return legsTravelled.ToString();
                case "encounters":
                    return encountersResolved.ToString();
                case "banditTolls":
                    return banditUnits + " units, " + banditCoin + " coin";
                case "mapCoin":
                    return mapCoin + " coin";
                case "mapDays":
                    return MapDayList();
                case "leadsUsed":
                    return leadsUsed.ToString();
                case "rescues":
                    return RescueCount.ToString();
                case "standing":
                    return "Rank " + GetKharunRank();
                case "placesWon":
                    return PlacesWonLine();
                default:
                    return string.Empty;
            }
        }

        public string EndLedger()
        {
            EndScreenFile file = GameConfig.EndScreen;
            var text = new StringBuilder();
            if (file != null && file.lines != null)
            {
                for (int i = 0; i < file.lines.Length; i++)
                {
                    text.Append(file.lines[i]);
                    text.Append('\n');
                }
            }

            if (file != null && !string.IsNullOrEmpty(file.footer))
            {
                text.Append(file.footer);
                text.Append('\n');
            }

            if (file != null && !string.IsNullOrEmpty(file.statsHeading))
            {
                text.Append(file.statsHeading);
                text.Append('\n');
            }

            if (file != null && file.stats != null)
            {
                for (int i = 0; i < file.stats.Length; i++)
                {
                    EndScreenFile.Stat stat = file.stats[i];
                    if (stat == null || string.IsNullOrEmpty(stat.key))
                        continue;
                    text.Append(stat.label);
                    text.Append("  ");
                    text.Append(EndStat(stat.key));
                    text.Append('\n');
                }
            }

            return text.ToString();
        }

        public string MapLogbookBlock()
        {
            EnsureHeld();
            var text = new StringBuilder();
            text.Append("Sarn's map");
            if (MapPieceLeads > 0)
            {
                text.Append('\n');
                text.Append(MapPieceLeads == 1 ? "1 lead banked" : MapPieceLeads + " leads banked");
            }

            int owned = 0;
            for (int i = 0; i < held.Length; i++)
            {
                if (!held[i].Owned)
                    continue;
                owned++;
                text.Append('\n');
                text.Append("Piece ");
                text.Append(i + 1);
                text.Append(" of ");
                text.Append(PiecesRequired());
                text.Append(" · ");
                text.Append(WorldClock.Date(held[i].Day));
            }

            if (owned == 0)
            {
                text.Append('\n');
                text.Append("No pieces of Sarn's map yet.");
            }

            return text.ToString();
        }

        public bool TryStartGateRoad()
        {
            if (Traveling || atRest || endScreen || !MapJourneyUnlocked || !CanTakeRoad())
                return false;
            if (Dock != GatePlace())
                return false;
            BeginMapLeg(MapLegKind.ToRest);
            return true;
        }

        public bool TryStartReturn()
        {
            if (!RestPending || !CanTakeRoad())
                return false;
            lastNotice = RestResult(1);
            BeginMapLeg(MapLegKind.Return);
            return true;
        }

        public bool TryStartFinalRun()
        {
            if (!RestPending || !CanTakeRoad())
                return false;
            lastNotice = RestResult(0);
            BeginMapLeg(MapLegKind.Final);
            return true;
        }

        public SupplyForecast ForecastGate(RoadPace pace)
        {
            return ForecastLandmark(pace, GateSteadyHours, RoadGraph.DockPoint(PlaceId.Westmark), RestPoint());
        }

        public SupplyForecast ForecastFinal(RoadPace pace)
        {
            return ForecastLandmark(pace, FinalSteadyHours, RestPoint(), BeyondPoint());
        }

        public SupplyForecast ForecastReturn(RoadPace pace)
        {
            return ForecastLandmark(pace, GateSteadyHours, RestPoint(), RoadGraph.DockPoint(PlaceId.Westmark));
        }

        public void SplitLandmarkHours(float steadyHours, RoadPace pace, out float dayHours, out float nightHours)
        {
            float scale = TravelScale.TimeScale(WeatherOnSegment(RoadGraph.DockPoint(PlaceId.Westmark), RestPoint()));
            float speed = Mathf.Max(0.01f, TravelScale.Speed(pace));
            float hours = steadyHours * Mathf.Max(0.01f, scale) / speed;
            float spendable = TravelHours - TravelScale.ReserveHours;
            if (spendable < 0f)
                spendable = 0f;
            if (hours > spendable)
                hours = spendable;
            TravelScale.SplitDayNight(Hour, hours, out dayHours, out nightHours);
        }

        public Vector2 RestMapPoint()
        {
            return RestPoint();
        }

        public Vector2 GateMapPoint()
        {
            return RoadGraph.DockPoint(PlaceId.Westmark);
        }

        public bool OnMapRoute(out Vector2 from, out Vector2 to)
        {
            from = mapFrom;
            to = mapTo;
            return mapLeg != MapLegKind.None && Traveling;
        }

        public string LandmarkAheadName()
        {
            if (mapLeg == MapLegKind.Return)
                return PlaceName(PlaceId.Westmark);
            if (mapLeg == MapLegKind.ToRest || mapLeg == MapLegKind.Final || atRest)
                return "Sarn's Rest";
            return string.Empty;
        }

        void NoteBuyCoin(int price)
        {
            if (price > 0)
                buyCoin += price;
        }

        void NoteSale(GoodId good, int net, PlaceId place)
        {
            saleCoin += net;
            unitsSold += 1;
            if (!bestSaleSeen || net > bestSaleCoin)
            {
                bestSaleSeen = true;
                bestSaleCoin = net;
                bestSaleGood = good;
                bestSalePlace = place;
            }
        }

        void NoteBanditUnits(int removed)
        {
            if (removed > 0)
                banditUnits += removed;
        }

        void NoteBanditCoin(int coin)
        {
            if (coin > 0)
                banditCoin += coin;
        }

        void NoteLeg()
        {
            legsTravelled++;
        }

        void NoteResolvedEncounter()
        {
            if (!string.IsNullOrEmpty(EncounterId))
                encountersResolved++;
        }

        void AddMapLeads(int count)
        {
            if (count <= 0)
                return;
            int next = MapPieceLeads + count;
            int max = LeadBankMax();
            if (next > max)
                next = max;
            if (next < 0)
                next = 0;
            MapPieceLeads = next;
        }

        void ClearMapProgress()
        {
            MapPieceFile file = GameConfig.MapPieces;
            int count = file != null && file.pieces != null ? file.pieces.Length : 0;
            held = new HeldPiece[count];
            MapPieceLeads = 0;
            mapCoin = 0;
            leadsUsed = 0;
            legsTravelled = 0;
            encountersResolved = 0;
            buyCoin = 0;
            saleCoin = 0;
            unitsSold = 0;
            bestSaleSeen = false;
            bestSaleCoin = 0;
            banditUnits = 0;
            banditCoin = 0;
            embarkedDay = 1;
            achievements.Clear();
            ClearMapTravel();
        }

        void ClearMapTravel()
        {
            mapLeg = MapLegKind.None;
            atRest = false;
            endScreen = false;
            cardIndex = 0;
            mapSteadyHours = 0f;
            previewSteady = 0f;
            previewScale = 1f;
            ClearScriptCard();
        }

        void ClearScriptCard()
        {
            scriptCard = false;
            scriptTitle = null;
            scriptBody = null;
        }

        bool TryLandmarkPoint(out Vector2 point)
        {
            if (mapLeg != MapLegKind.None && Traveling && edgeLength > 0.000001f)
            {
                float along = TravelDistance / edgeLength;
                if (along < 0f)
                    along = 0f;
                if (along > 1f)
                    along = 1f;
                point = Vector2.Lerp(mapFrom, mapTo, along);
                return true;
            }

            if (atRest)
            {
                point = RestPoint();
                return true;
            }

            point = default;
            return false;
        }

        string LandmarkLabel()
        {
            if (atRest && !Traveling)
                return "Sarn's Rest";
            if (!Traveling || mapLeg == MapLegKind.None)
                return null;
            if (mapLeg == MapLegKind.Return)
                return "Road to " + PlaceName(PlaceId.Westmark);
            return "Road to Sarn's Rest";
        }

        bool OnMapLeg()
        {
            return mapLeg != MapLegKind.None && Traveling;
        }

        float MapHoursRemaining(RoadPace pace)
        {
            if (edgeLength <= 0.000001f)
                return 0f;
            float remaining = (edgeLength - TravelDistance) / edgeLength;
            if (remaining < 0f)
                remaining = 0f;
            float speed = Mathf.Max(0.01f, TravelScale.Speed(pace));
            return mapSteadyHours * remaining * Mathf.Max(0.01f, LegScale) / speed;
        }

        bool FinishMapLegIfAny()
        {
            if (mapLeg == MapLegKind.None)
                return false;
            if (edgeLength > 0.000001f && TravelDistance < edgeLength - 0.000001f)
                return false;
            MapLegKind kind = mapLeg;
            if (kind == MapLegKind.Return)
            {
                mapLeg = MapLegKind.None;
                atRest = false;
                Arrive(GatePlace());
                return true;
            }

            if (kind == MapLegKind.Final)
            {
                NoteLeg();
                ShowEnd();
                return true;
            }

            NoteLeg();
            mapLeg = MapLegKind.None;
            atRest = true;
            Traveling = false;
            HoldTravel = true;
            CampPending = false;
            AwaitingPace = false;
            Pace = 1;
            TravelDistance = 0f;
            LegTravelHours = 0f;
            ClearEncounter();
            criticalShown = false;
            criticalPending = false;
            Touch();
            return true;
        }

        void ShowEnd()
        {
            mapLeg = MapLegKind.None;
            atRest = false;
            Traveling = false;
            HoldTravel = true;
            CampPending = false;
            AwaitingPace = false;
            Pace = 1;
            endScreen = true;
            embarkedDay = Day;
            ClearEncounter();
            MapPieceFile file = GameConfig.MapPieces;
            if (file != null)
                achievements.Unlock(file.embarkAchievement);
            Touch();
        }

        void BeginMapLeg(MapLegKind kind)
        {
            float steady = kind == MapLegKind.Final ? FinalSteadyHours : GateSteadyHours;
            if (kind == MapLegKind.Return)
            {
                mapFrom = RestPoint();
                mapTo = RoadGraph.DockPoint(PlaceId.Westmark);
            }
            else if (kind == MapLegKind.Final)
            {
                mapFrom = RestPoint();
                mapTo = BeyondPoint();
            }
            else
            {
                mapFrom = RoadGraph.DockPoint(PlaceId.Westmark);
                mapTo = RestPoint();
            }

            atRest = false;
            endScreen = false;
            mapLeg = kind;
            cardIndex = 0;
            previewSteady = 0f;
            mapSteadyHours = steady;
            LegWeather = WeatherOnSegment(mapFrom, mapTo);
            LegScale = TravelScale.TimeScale(LegWeather);
            float day = TravelScale.BaseLegHours;
            if (day <= 0f)
                day = 24f;
            edgeLength = steady / day * RoadPath.Length;
            TravelDistance = 0f;
            Traveling = true;
            travelFrom = PlaceId.Westmark;
            travelTo = PlaceId.Westmark;
            CampPending = false;
            AwaitingPace = false;
            Pace = 1;
            HoldTravel = false;
            LegTravelHours = 0f;
            criticalShown = false;
            criticalPending = false;
            ClearEncounter();
            RestoreGuardSave();
            if (kind == MapLegKind.Final)
                SyncFinalCardHour();
            else
                CheckLeg();
            Touch();
        }

        void SyncFinalCardHour()
        {
            if (mapLeg != MapLegKind.Final || EncounterPending || CampPending)
                return;
            MapPieceFile file = GameConfig.MapPieces;
            if (file == null || file.finalCards == null || cardIndex < 0 || cardIndex >= file.finalCards.Length)
            {
                encounterArmed = false;
                return;
            }

            MapPieceFile.Card card = file.finalCards[cardIndex];
            float fraction = card != null ? card.fraction : 1f;
            if (fraction < 0f)
                fraction = 0f;
            if (fraction > 1f)
                fraction = 1f;
            float remaining = edgeLength * fraction - TravelDistance;
            if (remaining < 0.00001f)
                remaining = 0.00001f;
            EncounterHour = LegTravelHours + HoursForDistance(remaining);
            encounterArmed = true;
            EncounterPending = false;
            EncounterResolved = false;
            EncounterResult = null;
            EncounterId = null;
            HoldTravel = false;
        }

        bool TryOpenFinalCard()
        {
            MapPieceFile file = GameConfig.MapPieces;
            if (file == null || file.finalCards == null || cardIndex < 0 || cardIndex >= file.finalCards.Length)
                return false;
            MapPieceFile.Card card = file.finalCards[cardIndex];
            cardIndex++;
            if (card == null || string.IsNullOrEmpty(card.id))
                return false;
            scriptTitle = card.title;
            scriptBody = card.line;
            scriptCard = true;
            OpenEncounter(card.id);
            return true;
        }

        bool TryOpenMapPoolCard()
        {
            if (encounterRandom == null)
                encounterRandom = new System.Random(GameConfig.Encounters.seed);
            string id = EncounterCards.Pick(
                MapPool(LegWeather),
                PlaceId.Westmark,
                PlaceId.Westmark,
                Hour,
                HasRunFlag,
                encounterRandom);
            if (string.IsNullOrEmpty(id))
                return false;
            OpenEncounter(id);
            return true;
        }

        static System.Collections.Generic.List<EncounterCategory> MapPool(WeatherKind weather)
        {
            var pool = new System.Collections.Generic.List<EncounterCategory>();
            pool.Add(EncounterCategory.WildlifeDesert);
            pool.Add(EncounterCategory.Bandits);
            pool.Add(EncounterCategory.Catchall);
            pool.Add(EncounterTable.WeatherBucket(weather));
            return pool;
        }

        SupplyForecast ForecastLandmark(RoadPace pace, float steady, Vector2 from, Vector2 to)
        {
            previewScale = TravelScale.TimeScale(WeatherOnSegment(from, to));
            previewSteady = steady;
            SupplyForecast forecast = ForecastRoad(PlaceId.Westmark, pace);
            previewSteady = 0f;
            return forecast;
        }

        WeatherKind WeatherOnSegment(Vector2 from, Vector2 to)
        {
            WeatherKind worst = WeatherKind.Clear;
            for (int i = 0; i < 8; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / 7f);
                WeatherKind kind = WeatherAt(point);
                if (TravelScale.Severity(kind) > TravelScale.Severity(worst))
                    worst = kind;
            }

            return worst;
        }

        static Vector2 RestPoint()
        {
            Vector2 dock = RoadGraph.DockPoint(PlaceId.Westmark);
            dock.x -= 160f / RoadPath.MapWidth;
            if (dock.x < 0.004f)
                dock.x = 0.004f;
            return dock;
        }

        static Vector2 BeyondPoint()
        {
            Vector2 point = RestPoint();
            point.x -= 140f / RoadPath.MapWidth;
            if (point.x < 0.004f)
                point.x = 0.004f;
            return point;
        }

        void EnsureHeld()
        {
            MapPieceFile file = GameConfig.MapPieces;
            int count = file != null && file.pieces != null ? file.pieces.Length : 0;
            if (held != null && held.Length == count)
                return;
            var next = new HeldPiece[count];
            if (held != null)
            {
                int copy = held.Length < count ? held.Length : count;
                for (int i = 0; i < copy; i++)
                    next[i] = held[i];
            }

            held = next;
        }

        int NextUnowned()
        {
            EnsureHeld();
            for (int i = 0; i < held.Length; i++)
            {
                if (!held[i].Owned)
                    return i;
            }

            return -1;
        }

        bool RankMet(int index)
        {
            MapPieceFile.Piece row = PieceRow(index);
            return row != null && GetKharunRank() >= row.unlockRank;
        }

        int AdjustedPrice(int listPrice)
        {
            int keep = 100 - LeadPercent() * BankedLeads();
            if (keep < 0)
                keep = 0;
            return listPrice * keep / 100;
        }

        int BankedLeads()
        {
            int leads = MapPieceLeads;
            int max = LeadBankMax();
            if (leads > max)
                leads = max;
            if (leads < 0)
                leads = 0;
            return leads;
        }

        int LeadPercent()
        {
            MapPieceFile file = GameConfig.MapPieces;
            return file != null ? file.leadPercent : 0;
        }

        int LeadBankMax()
        {
            MapPieceFile file = GameConfig.MapPieces;
            return file != null ? file.leadBankMax : 0;
        }

        int PiecesRequired()
        {
            MapPieceFile file = GameConfig.MapPieces;
            if (file != null && file.piecesRequired > 0)
                return file.piecesRequired;
            EnsureHeld();
            return held.Length;
        }

        PlaceId SellerPlace()
        {
            MapPieceFile file = GameConfig.MapPieces;
            PlaceId place;
            if (file != null && Enum.TryParse(file.sellerPlace, false, out place))
                return place;
            return PlaceId.Kharun;
        }

        PlaceId GatePlace()
        {
            MapPieceFile file = GameConfig.MapPieces;
            PlaceId place;
            if (file != null && Enum.TryParse(file.gatePlace, false, out place))
                return place;
            return PlaceId.Westmark;
        }

        string FirstAchievement()
        {
            MapPieceFile file = GameConfig.MapPieces;
            return file != null ? file.firstAchievement : null;
        }

        MapPieceFile.Piece PieceRow(int index)
        {
            MapPieceFile file = GameConfig.MapPieces;
            if (file == null || file.pieces == null || index < 0 || index >= file.pieces.Length)
                return null;
            return file.pieces[index];
        }

        int PlacesWonCount()
        {
            int count = 0;
            for (int i = 0; i < WorldIds.PlaceCount; i++)
            {
                if (EconomyCatalog.IsCity((PlaceId)i))
                    continue;
                if (kharunFlipSeen[i])
                    count++;
            }

            return count;
        }

        string MapDayList()
        {
            EnsureHeld();
            var text = new StringBuilder();
            for (int i = 0; i < held.Length; i++)
            {
                if (!held[i].Owned)
                    continue;
                if (text.Length > 0)
                    text.Append(" · ");
                text.Append(WorldClock.Date(held[i].Day));
            }

            return text.Length == 0 ? "\u2014" : text.ToString();
        }

#if UNITY_EDITOR
        internal void SetMapPieceLeads(int count)
        {
            MapPieceLeads = 0;
            AddMapLeads(count);
            Touch();
        }

        internal void CompleteMapLeg()
        {
            if (mapLeg == MapLegKind.None || !Traveling)
                return;
            TravelDistance = edgeLength;
            FinishMapLegIfAny();
        }

        internal void ShotOwnPieces(int count, bool stampDays)
        {
            EnsureHeld();
            mapCoin = 0;
            for (int i = 0; i < held.Length; i++)
            {
                if (i >= count)
                {
                    held[i] = default;
                    continue;
                }

                MapPieceFile.Piece row = PieceRow(i);
                int list = row != null ? row.price : 0;
                held[i].Owned = true;
                held[i].Day = stampDays ? 1 + i : Day;
                held[i].ListPrice = list;
                held[i].Paid = list;
                mapCoin += list;
            }

            Touch();
        }

        internal void ShotClearMapState()
        {
            ClearMapProgress();
            SetKharunPoints(0f);
            SetCoin(TravelScale.StartingCoin);
            Traveling = false;
            HoldTravel = false;
            CampPending = false;
            AwaitingPace = false;
            Pace = 1;
            TravelDistance = 0f;
            TravelHours = TravelScale.WindowHours;
        }

        internal void ShotShowEnd()
        {
            ShowEnd();
        }
#endif
    }
}
