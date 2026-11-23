using System;
using Caravans.FrontDoor;
using UnityEngine;

namespace Caravans.Play
{
    public static class RunSaveStore
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            SaveSlot.Validator = Validate;
        }

        public static bool TryWrite(CaravanSession session)
        {
            if (session == null || !session.AtSafePoint)
                return false;
            try
            {
                return SaveSlot.TryWrite(JsonUtility.ToJson(session.CaptureSave()));
            }
            catch (Exception error)
            {
                Debug.LogWarning("Caravans save was not written. " + error.Message);
                return false;
            }
        }

        public static bool TryLoad(out CaravanSession session)
        {
            session = null;
            string json;
            if (!SaveSlot.TryReadValid(out json))
                return false;
            try
            {
                RunSave save = JsonUtility.FromJson<RunSave>(json);
                return CaravanSession.TryRestore(save, out session);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Caravans save skipped (" + SaveSlot.FileName + "). " + error.Message);
                session = null;
                return false;
            }
        }

        public static bool Validate(string json)
        {
            if (!JsonObject(json))
                return false;
            try
            {
                RunSave save = JsonUtility.FromJson<RunSave>(json);
                if (save == null || save.saveVersion < 1 || save.saveVersion > RunSave.CurrentVersion)
                    return false;
                return Accept(save);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Clear()
        {
            SaveSlot.Clear();
        }

        internal static bool Accept(RunSave save)
        {
            if (save == null || save.saveVersion < 1 || save.saveVersion > RunSave.CurrentVersion)
                return false;
            int places = WorldIds.PlaceCount;
            int goods = WorldIds.GoodCount;
            int houses = WorldIds.HouseCount;
            if (!Grid(save.shelfStock, places * goods))
                return false;
            if (!Grid(save.eatAcc, places * goods) || !Finite(save.eatAcc))
                return false;
            if (!Grid(save.makeAcc, places * goods) || !Finite(save.makeAcc))
                return false;
            if (!Grid(save.boughtHere, goods) || !NonNegative(save.boughtHere))
                return false;
            if (!Prices(save.loggedBuy, goods) || !Prices(save.loggedSale, goods))
                return false;
            if (!Grid(save.influence, places * houses) || !Finite(save.influence))
                return false;
            if (save.patron == null || save.patron.Length != places)
                return false;
            for (int i = 0; i < save.patron.Length; i++)
            {
                if (save.patron[i] < -1 || save.patron[i] >= houses)
                    return false;
            }

            if (save.kharunFlipSeen == null || save.kharunFlipSeen.Length != places)
                return false;
            if (!Place(save.dock) || !Place(save.travelFrom) || !Place(save.travelTo))
                return false;
            if (save.chosenPace < 0 || save.chosenPace > (int)RoadPace.Rush)
                return false;
            if (save.day < 1 || save.coin < 0 || save.stretchChecks < 0)
                return false;
            float dayHours = EconomyRates.DayHours;
            if (dayHours <= 0f)
                dayHours = 24f;
            if (!Finite(save.hour) || save.hour < 0f || save.hour >= dayHours)
                return false;
            if (!Finite(save.waterLitres) || save.waterLitres < 0f)
                return false;
            if (!Finite(save.rationKg) || save.rationKg < 0f)
                return false;
            if (!Finite(save.travelHours) || save.travelHours < 0f)
                return false;
            if (!Finite(save.kharunPoints) || save.kharunPoints < 0f)
                return false;
            if (save.merchantMountTier < 1 || save.merchantMountTier > 3)
                return false;
            if (save.packBeasts == null)
                return false;
            for (int i = 0; i < save.packBeasts.Length; i++)
            {
                if (save.packBeasts[i] < 1 || save.packBeasts[i] > 3)
                    return false;
            }

            if (save.packs == null)
                return false;
            for (int i = 0; i < save.packs.Length; i++)
            {
                RunSave.PackRow pack = save.packs[i];
                if (pack == null || !Good(pack.good))
                    return false;
                if (!Finite(pack.integrity) || pack.integrity < 0f)
                    return false;
            }

            if (save.roadNotes == null || save.runFlags == null)
                return false;
            for (int i = 0; i < save.roadNotes.Length; i++)
            {
                if (save.roadNotes[i] == null)
                    return false;
            }

            for (int i = 0; i < save.runFlags.Length; i++)
            {
                if (string.IsNullOrEmpty(save.runFlags[i]))
                    return false;
            }

            if (save.npcs == null || save.weather == null)
                return false;
            for (int i = 0; i < save.npcs.Length; i++)
            {
                if (!Npc(save.npcs[i], goods, houses))
                    return false;
            }

            for (int i = 0; i < save.weather.Length; i++)
            {
                if (!Weather(save.weather[i]))
                    return false;
            }

            int pieces = PieceCount();
            if (save.held == null || save.held.Length != pieces)
                return false;
            for (int i = 0; i < save.held.Length; i++)
            {
                RunSave.HeldRow row = save.held[i];
                if (row == null || row.day < 0 || row.listPrice < 0 || row.paid < 0)
                    return false;
            }

            if (save.mapPieceLeads < 0 || save.mapCoin < 0 || save.leadsUsed < 0)
                return false;
            if (save.legsTravelled < 0 || save.encountersResolved < 0)
                return false;
            if (save.buyCoin < 0 || save.saleCoin < 0 || save.unitsSold < 0)
                return false;
            if (save.bestSaleCoin < 0 || !Good(save.bestSaleGood) || !Place(save.bestSalePlace))
                return false;
            if (save.banditUnits < 0 || save.banditCoin < 0 || save.embarkedDay < 1)
                return false;
            if (save.rescueCount < 0)
                return false;
            if (save.achievementIds == null || save.achievementFires == null)
                return false;
            if (save.achievementIds.Length != save.achievementFires.Length)
                return false;
            for (int i = 0; i < save.achievementIds.Length; i++)
            {
                if (string.IsNullOrEmpty(save.achievementIds[i]) || save.achievementFires[i] < 1)
                    return false;
            }

            return true;
        }

        static int PieceCount()
        {
            MapPieceFile file = GameConfig.MapPieces;
            if (file == null || file.pieces == null)
                return 0;
            return file.pieces.Length;
        }

        static bool Place(int id)
        {
            return id >= 0 && id < WorldIds.PlaceCount;
        }

        static bool Good(int id)
        {
            return id >= 0 && id < WorldIds.GoodCount;
        }

        static bool Grid(int[] values, int length)
        {
            return values != null && values.Length == length;
        }

        static bool Grid(float[] values, int length)
        {
            return values != null && values.Length == length;
        }

        static bool NonNegative(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < 0)
                    return false;
            }

            return true;
        }

        static bool Finite(float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!Finite(values[i]))
                    return false;
            }

            return true;
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool Prices(RunSave.PriceRow[] rows, int goods)
        {
            if (rows == null || rows.Length != goods)
                return false;
            for (int i = 0; i < rows.Length; i++)
            {
                RunSave.PriceRow row = rows[i];
                if (row == null || row.coin < 0 || !Place(row.place))
                    return false;
            }

            return true;
        }

        static bool Npc(RunSave.NpcRow row, int goods, int houses)
        {
            if (row == null || row.house < 0 || row.house >= houses)
                return false;
            if (!Place(row.dock) || !Place(row.from) || !Place(row.to))
                return false;
            if (!Finite(row.distance) || row.distance < 0f)
                return false;
            if (!Finite(row.edgeLength) || row.edgeLength < 0f)
                return false;
            if (row.path == null || row.cargo == null || row.cargo.Length != goods)
                return false;
            for (int i = 0; i < row.path.Length; i++)
            {
                if (!Place(row.path[i]))
                    return false;
            }

            return NonNegative(row.cargo);
        }

        static bool Weather(RunSave.WeatherRow row)
        {
            if (row == null || row.kind < 0 || row.kind > (int)WeatherKind.Sandstorm)
                return false;
            if (!Finite(row.centerX) || !Finite(row.centerY) || !Finite(row.radius))
                return false;
            if (row.radius <= 0f)
                return false;
            return Finite(row.velocityX) && Finite(row.velocityY);
        }

        static bool JsonObject(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;
            int start = 0;
            int end = json.Length - 1;
            while (start <= end && char.IsWhiteSpace(json[start]))
                start++;
            while (end >= start && char.IsWhiteSpace(json[end]))
                end--;
            if (start > end || json[start] != '{' || json[end] != '}')
                return false;
            int depth = 0;
            bool inString = false;
            bool escape = false;
            for (int i = start; i <= end; i++)
            {
                char ch = json[i];
                if (inString)
                {
                    if (escape)
                        escape = false;
                    else if (ch == '\\')
                        escape = true;
                    else if (ch == '"')
                        inString = false;
                    continue;
                }

                if (ch == '"')
                    inString = true;
                else if (ch == '{')
                    depth++;
                else if (ch == '}')
                    depth--;
                if (depth < 0)
                    return false;
            }

            return depth == 0 && !inString;
        }
    }
}
