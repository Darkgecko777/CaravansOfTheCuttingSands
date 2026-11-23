using System;
using System.Globalization;
using UnityEngine;

namespace Caravans.Play
{
    public struct SupplyForecast
    {
        public float WaterNeed;
        public float RationNeed;
        public float SupplyWater;
        public float SupplyRations;
        public int PackUnits;
        public int HaulWater;
        public int HaulRations;
        public bool Red;
        public bool Amber;
        public bool Sea;
        public float DaysAfter;
    }

    public sealed partial class CaravanSession
    {
        int merchantMountTier = 1;
        int agents;
        int[] packBeasts = new int[0];
        bool deathPending;
        bool criticalPending;
        bool criticalShown;
        bool lowNoted;
        bool suppressDeath;
        string lastNotice = string.Empty;
        string rescueTitle = string.Empty;
        string rescueLead = string.Empty;
        string rescueBody = string.Empty;
        string lastRescueText;
        int rescueCount;
        int pendingRescueDays;
        System.Random rescueRandom;

        public float DailyWater
        {
            get { return SumDaily(true); }
        }

        public float DailyRations
        {
            get { return SumDaily(false); }
        }

        public float KitWaterCapacity
        {
            get { return SumKit(true); }
        }

        public float KitRationCapacity
        {
            get { return SumKit(false); }
        }

        public int CargoSpaceCap
        {
            get
            {
                KitTier tier = MerchantTier();
                if (tier == null || tier.cargoSpace <= 0)
                    return EconomyRates.WagonSpace;
                return tier.cargoSpace;
            }
        }

        public int CargoWeightCap
        {
            get
            {
                KitTier tier = MerchantTier();
                if (tier == null || tier.cargoWeight <= 0)
                    return EconomyRates.WagonWeight;
                return tier.cargoWeight;
            }
        }

        public int MerchantMountTier
        {
            get { return merchantMountTier; }
        }

        public string LastNotice
        {
            get { return lastNotice ?? string.Empty; }
        }

        public bool DeathPending
        {
            get { return deathPending; }
        }

        public bool CriticalPending
        {
            get { return criticalPending; }
        }

        public string RescueTitle
        {
            get { return rescueTitle ?? string.Empty; }
        }

        public string RescueLead
        {
            get { return rescueLead ?? string.Empty; }
        }

        public string RescueBody
        {
            get { return rescueBody ?? string.Empty; }
        }

        public int RescueCount
        {
            get { return rescueCount; }
        }

        public float LowHours
        {
            get
            {
                KitWarning warning = GameConfig.Kit != null ? GameConfig.Kit.warning : null;
                if (warning != null && warning.lowHours > 0f)
                    return warning.lowHours;
                return 12f;
            }
        }

        public float CriticalHours
        {
            get
            {
                KitWarning warning = GameConfig.Kit != null ? GameConfig.Kit.warning : null;
                if (warning != null && warning.criticalHours > 0f)
                    return warning.criticalHours;
                return 6f;
            }
        }

        public float WaterSupplyHours
        {
            get { return WorstHours(SupplyLitres(GoodId.Water), DailyWater); }
        }

        public float RationSupplyHours
        {
            get { return WorstHours(SupplyKg(GoodId.Rations), DailyRations); }
        }

        public string HudWater
        {
            get { return StoreLine(WaterLitres, CargoOf(GoodId.Water), SupplyLitres(GoodId.Water), DailyWater, "L", !Traveling, "Buy water"); }
        }

        public string HudRations
        {
            get { return StoreLine(RationKg, CargoOf(GoodId.Rations), SupplyKg(GoodId.Rations), DailyRations, "kg", !Traveling, "Buy food"); }
        }

        public SupplyForecast ForecastRoad(PlaceId destination, RoadPace pace)
        {
            float supplyWater = SupplyLitres(GoodId.Water);
            float supplyFood = SupplyKg(GoodId.Rations);
            int packs = CargoOf(GoodId.Water) + CargoOf(GoodId.Rations);
            PlaceId from = Traveling ? travelFrom : Dock;
            bool sea = previewSteady <= 0.0001f && !OnMapLeg() && RoadGraph.IsSea(from, destination);
            float hours = ForecastHours(destination, pace);
            if (hours <= 0.0001f)
                return FinishForecast(0f, 0f, supplyWater, supplyFood, packs, sea || hours <= 0.0001f);

            float water = 0f;
            float food = 0f;
            float hour = Hour;
            float store = TravelHours;
            float paceKit = TravelScale.Kit(pace);
            float dayKit = TravelScale.DayKit;
            float left = hours;
            int guard = 0;
            while (left > 0.0001f && guard++ < 80)
            {
                float spendable = store - TravelScale.ReserveHours;
                if (spendable < 0f)
                    spendable = 0f;
                if (spendable <= 0.0001f)
                {
                    CampNeed(ref water, ref food, ref hour, ref store);
                    continue;
                }

                float road = left < spendable ? left : spendable;
                TravelScale.SplitDayNight(hour, road, out float dayHours, out float nightHours);
                water += RoadNeed(dayHours, nightHours, paceKit, dayKit, DailyWater);
                food += RoadNeed(dayHours, nightHours, paceKit, dayKit, DailyRations);
                hour = AdvanceForecastHour(hour, road);
                left -= road;
                store -= road;
                if (left > 0.0001f)
                    CampNeed(ref water, ref food, ref hour, ref store);
            }

            return FinishForecast(water, food, supplyWater, supplyFood, packs, false);
        }

        public void ContinueDeath()
        {
            if (!deathPending)
                return;
            ApplySoftReset();
        }

        public void ContinueCritical()
        {
            if (!criticalPending)
                return;
            criticalPending = false;
            if (!deathPending && !AwaitingPace && !CampPending && !EncounterPending)
                HoldTravel = false;
            Touch();
        }

        internal void SetCaravan(int tier, int hired)
        {
            if (tier < 1)
                tier = 1;
            if (tier > 3)
                tier = 3;
            if (hired < 0)
                hired = 0;
            merchantMountTier = tier;
            agents = hired;
            Touch();
        }

        internal void SetCoin(int coin)
        {
            Coin = coin < 0 ? 0 : coin;
        }

        internal void SpendRoadHours(float hours)
        {
            if (hours <= 0f)
                return;
            SpendWindow(hours, TravelScale.Kit(ChosenPace), true);
        }

        void ApplyDefaultCaravan()
        {
            ClearAgentRoles();
            KitFile file = GameConfig.Kit;
            KitCaravan shape = file != null ? file.defaultCaravan : null;
            if (shape == null)
            {
                merchantMountTier = 1;
                agents = 0;
                packBeasts = new int[0];
                return;
            }

            int tier = shape.merchantMountTier;
            if (tier < 1)
                tier = 1;
            if (tier > 3)
                tier = 3;
            merchantMountTier = tier;
            agents = shape.agents < 0 ? 0 : shape.agents;
            packBeasts = shape.packBeasts != null ? shape.packBeasts : new int[0];
        }

        void ClearSupplyFlags()
        {
            deathPending = false;
            criticalPending = false;
            criticalShown = false;
            lowNoted = false;
            lastNotice = string.Empty;
            rescueTitle = string.Empty;
            rescueLead = string.Empty;
            rescueBody = string.Empty;
            pendingRescueDays = 0;
        }

        void PourIfLow()
        {
            KitFile file = GameConfig.Kit;
            if (!Traveling || file == null || file.autoPour == null || !file.autoPour.enabled)
                return;
            PourWater();
            PourRations();
        }

        void PourWater()
        {
            float unit = WaterUnit();
            float perHour = WorstPerHour(DailyWater);
            int guard = 0;
            while (guard++ < 8 && unit > 0f && perHour > 0f)
            {
                float room = KitWaterCapacity - WaterLitres;
                if (WaterLitres / perHour > LowHours + 0.0001f)
                    return;
                if (room + 0.0001f < unit)
                    return;
                int index = LowestIndex(GoodId.Water);
                if (index < 0)
                    return;
                stacks.RemoveAt(index);
                WaterLitres = Mathf.Min(KitWaterCapacity, WaterLitres + unit);
                int left = CargoOf(GoodId.Water);
                lastNotice = "Poured a skin of water from the packs (" + left + " left).";
            }
        }

        void PourRations()
        {
            float unit = RationUnit();
            float perHour = WorstPerHour(DailyRations);
            int guard = 0;
            while (guard++ < 8 && unit > 0f && perHour > 0f)
            {
                float room = KitRationCapacity - RationKg;
                if (RationKg / perHour > LowHours + 0.0001f)
                    return;
                if (room + 0.0001f < unit)
                    return;
                int index = LowestIndex(GoodId.Rations);
                if (index < 0)
                    return;
                stacks.RemoveAt(index);
                RationKg = Mathf.Min(KitRationCapacity, RationKg + unit);
                int left = CargoOf(GoodId.Rations);
                lastNotice = "Opened a ration bundle from the packs (" + left + " left).";
            }
        }

        void NoteSupply()
        {
            bool waterLow = WaterSupplyHours <= LowHours + 0.0001f;
            bool foodLow = RationSupplyHours <= LowHours + 0.0001f;
            if (!waterLow && !foodLow)
                lowNoted = false;
            else if (!lowNoted)
            {
                lowNoted = true;
                lastNotice = "The skins run low.";
            }

            if (!Traveling || deathPending || criticalShown)
                return;
            bool waterCritical = WaterSupplyHours <= CriticalHours + 0.0001f;
            bool foodCritical = RationSupplyHours <= CriticalHours + 0.0001f;
            if (!waterCritical && !foodCritical)
                return;
            criticalShown = true;
            criticalPending = true;
            HoldTravel = true;
        }

        void CheckDeath()
        {
            if (suppressDeath || deathPending || !Traveling || edgeLength <= 0.000001f)
                return;
            bool dryWater = WaterLitres <= 0.0001f && CargoOf(GoodId.Water) == 0;
            bool dryFood = RationKg <= 0.0001f && CargoOf(GoodId.Rations) == 0;
            if (!dryWater && !dryFood)
                return;
            deathPending = true;
            HoldTravel = true;
            criticalPending = false;
            PickRescue();
        }

        void PickRescue()
        {
            RescueFile file = GameConfig.Rescue;
            rescueTitle = file != null && !string.IsNullOrEmpty(file.title) ? file.title : "Dry";
            rescueLead = file != null && !string.IsNullOrEmpty(file.lead) ? file.lead : "The skins are empty. The sand is not.";
            RescueRegion region = MatchRegion(file);
            pendingRescueDays = region != null && region.rescueDays > 0 ? region.rescueDays : 2;
            rescueBody = PickText(region);
            lastRescueText = rescueBody;
        }

        RescueRegion MatchRegion(RescueFile file)
        {
            if (file == null || file.regions == null || file.regions.Length == 0)
                return null;
            string forward = travelFrom + "-" + travelTo;
            string back = travelTo + "-" + travelFrom;
            for (int i = 0; i < file.regions.Length; i++)
            {
                RescueRegion region = file.regions[i];
                if (region == null || region.roads == null)
                    continue;
                for (int r = 0; r < region.roads.Length; r++)
                {
                    string road = region.roads[r];
                    if (road == forward || road == back)
                        return region;
                }
            }

            return file.regions[0];
        }

        string PickText(RescueRegion region)
        {
            if (region == null || region.texts == null || region.texts.Length == 0)
                return "The road brought you back to Kharûn.";
            if (region.texts.Length == 1 || rescueRandom == null)
                return region.texts[0];
            string chosen = region.texts[0];
            for (int n = 0; n < 8; n++)
            {
                chosen = region.texts[rescueRandom.Next(region.texts.Length)];
                if (chosen != lastRescueText)
                    return chosen;
            }

            for (int i = 0; i < region.texts.Length; i++)
            {
                if (region.texts[i] != lastRescueText)
                    return region.texts[i];
            }

            return chosen;
        }

        void ApplySoftReset()
        {
            SoftResetFile file = GameConfig.SoftReset;
            float fraction = file != null && file.carriedCoinFraction > 0f ? file.carriedCoinFraction : 0.5f;
            int kept = (int)Math.Floor(Coin * (double)fraction);
            if (file != null && file.paupersPurse && kept < file.pauperFloorCoin)
                kept = file.pauperFloorCoin;
            Coin = kept;
            stacks.Clear();
            int days = pendingRescueDays > 0 ? pendingRescueDays : 2;
            Day += days;
            float waking = file != null && file.kitOnWakingDays > 0f ? file.kitOnWakingDays : 1f;
            WaterLitres = Mathf.Min(KitWaterCapacity, DailyWater * waking);
            RationKg = Mathf.Min(KitRationCapacity, DailyRations * waking);
            PlaceId home = PlaceId.Kharun;
            if (file != null && Enum.TryParse(file.returnTo, false, out PlaceId parsed))
                home = parsed;
            Dock = home;
            Traveling = false;
            HoldTravel = false;
            CampPending = false;
            AwaitingPace = false;
            Pace = 1;
            TravelDistance = 0f;
            TravelHours = TravelScale.WindowHours;
            LegTravelHours = 0f;
            travelFrom = home;
            ClearEncounter();
            deathPending = false;
            criticalPending = false;
            criticalShown = false;
            lowNoted = false;
            lastNotice = string.Empty;
            ClearAgentNotice();
            rescueCount++;
            ClearMapTravel();
            // TODO: ACH_DRAGGED_HOME increments with RescueCount. A later bank's BankedCoin stays through this reset.
            MarkSaveDue();
            Touch();
        }

        float AdjustSupply(float kit, float delta, float max, float reserve, float packs)
        {
            if (delta >= 0f)
                return ClampStore(kit + delta, max);
            if (kit + packs <= reserve + 0.0001f)
                return kit;
            float next = kit + delta;
            if (next < 0f)
                next = 0f;
            if (next + packs < reserve)
                next = Mathf.Max(0f, reserve - packs);
            return ClampStore(next, max);
        }

        float HoldSupply(float now, float beforeKit, float packsBefore, float packsNow, float max, float reserve)
        {
            if (now > max)
                now = max;
            float beforeSupply = beforeKit + packsBefore;
            if (beforeSupply <= reserve + 0.0001f && now < beforeKit)
                return beforeKit;
            if (now + packsNow < reserve)
                return ClampStore(Mathf.Max(0f, reserve - packsNow), max);
            return now;
        }

        static float ClampStore(float value, float max)
        {
            if (value < 0f)
                return 0f;
            if (value > max)
                return max;
            return value;
        }

        float SupplyLitres(GoodId good)
        {
            if (good != GoodId.Water)
                return 0f;
            return WaterLitres + CargoOf(GoodId.Water) * WaterUnit();
        }

        float SupplyKg(GoodId good)
        {
            if (good != GoodId.Rations)
                return 0f;
            return RationKg + CargoOf(GoodId.Rations) * RationUnit();
        }

        float PourableLitres(GoodId good)
        {
            if (good != GoodId.Water)
                return 0f;
            return CargoOf(GoodId.Water) * WaterUnit();
        }

        float PourableKg(GoodId good)
        {
            if (good != GoodId.Rations)
                return 0f;
            return CargoOf(GoodId.Rations) * RationUnit();
        }

        int LowestIndex(GoodId good)
        {
            int best = -1;
            float bestIntegrity = float.MaxValue;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].Good != good)
                    continue;
                float integrity = stacks[i].Integrity;
                if (best < 0 || integrity < bestIntegrity - 0.0001f)
                {
                    best = i;
                    bestIntegrity = integrity;
                }
            }

            return best;
        }

        float WorstPerHour(float daily)
        {
            float day = EconomyRates.DayHours;
            if (day <= 0f)
                return 0f;
            return daily / day * TravelScale.Kit(ChosenPace) * TravelScale.DayKit;
        }

        float WorstHours(float supply, float daily)
        {
            float perHour = WorstPerHour(daily);
            if (perHour <= 0.000001f)
                return supply > 0f ? 999f : 0f;
            return supply / perHour;
        }

        static string StoreLine(float kit, int packs, float supply, float daily, string unit, bool docked, string empty)
        {
            if (docked && supply <= 0.05f)
                return empty;
            string amount = kit.ToString("0.#", CultureInfo.InvariantCulture) + " " + unit;
            if (packs > 0)
                amount += " +" + packs + " in packs";
            return amount + " · " + TimeLeft(supply, daily);
        }

        static string TimeLeft(float supply, float daily)
        {
            if (daily <= 0f)
                return "0 h";
            float hours = supply / daily * 24f;
            if (hours < 0.05f)
                return "0 h";
            if (hours < 24f)
                return hours.ToString("0.#", CultureInfo.InvariantCulture) + " h";
            return (hours / 24f).ToString("0.#", CultureInfo.InvariantCulture) + " d";
        }

        float SumDaily(bool water)
        {
            KitFile file = GameConfig.Kit;
            if (file == null || file.merchant == null || file.tiers == null)
                return water ? EconomyRates.WaterLitresPerDay : EconomyRates.RationsKgPerDay;
            KitTier mount = MerchantTier();
            KitTier thrask = TierOf(1);
            float people = water ? file.merchant.waterPerDay : file.merchant.rationsPerDay;
            float agentShare = 0f;
            if (file.agent != null)
                agentShare = water ? file.agent.waterPerDay : file.agent.rationsPerDay;
            float thraskShare = 0f;
            if (thrask != null)
                thraskShare = water ? thrask.waterPerDay : thrask.rationsPerDay;
            float sum = people + agents * (agentShare + thraskShare);
            if (mount != null)
                sum += water ? mount.waterPerDay : mount.rationsPerDay;
            if (packBeasts != null)
            {
                for (int i = 0; i < packBeasts.Length; i++)
                {
                    KitTier beast = TierOf(packBeasts[i]);
                    if (beast != null)
                        sum += water ? beast.waterPerDay : beast.rationsPerDay;
                }
            }

            if (sum <= 0f)
                return water ? EconomyRates.WaterLitresPerDay : EconomyRates.RationsKgPerDay;
            return sum;
        }

        float SumKit(bool water)
        {
            KitFile file = GameConfig.Kit;
            if (file == null || file.merchant == null || file.tiers == null)
                return water ? EconomyRates.CanteenLitres : EconomyRates.RationBagKg;
            KitTier mount = MerchantTier();
            KitTier thrask = TierOf(1);
            float sum = water ? file.merchant.kitWater : file.merchant.kitRations;
            if (thrask != null)
                sum += agents * (water ? thrask.kitWater : thrask.kitRations);
            if (mount != null)
                sum += water ? mount.kitWater : mount.kitRations;
            if (packBeasts != null)
            {
                for (int i = 0; i < packBeasts.Length; i++)
                {
                    KitTier beast = TierOf(packBeasts[i]);
                    if (beast != null)
                        sum += water ? beast.kitWater : beast.kitRations;
                }
            }

            if (sum <= 0f)
                return water ? EconomyRates.CanteenLitres : EconomyRates.RationBagKg;
            return sum;
        }

        KitTier MerchantTier()
        {
            return TierOf(merchantMountTier);
        }

        static KitTier TierOf(int tier)
        {
            KitFile file = GameConfig.Kit;
            if (file == null || file.tiers == null)
                return null;
            for (int i = 0; i < file.tiers.Length; i++)
            {
                KitTier row = file.tiers[i];
                if (row != null && row.tier == tier)
                    return row;
            }

            return null;
        }

        float WaterUnit()
        {
            KitUnits units = GameConfig.Kit != null ? GameConfig.Kit.units : null;
            if (units != null && units.waterLitresPerUnit > 0f)
                return units.waterLitresPerUnit;
            return EconomyRates.WaterLitresPerUnit;
        }

        float RationUnit()
        {
            KitUnits units = GameConfig.Kit != null ? GameConfig.Kit.units : null;
            if (units != null && units.rationsKgPerUnit > 0f)
                return units.rationsKgPerUnit;
            return EconomyRates.RationsKgPerUnit;
        }

        float ForecastHours(PlaceId destination, RoadPace pace)
        {
            float speed = Mathf.Max(0.01f, TravelScale.Speed(pace));
            if (previewSteady > 0.0001f)
                return previewSteady * Mathf.Max(0.01f, previewScale) / speed;
            if (OnMapLeg())
                return MapHoursRemaining(pace);
            if (Traveling && destination == travelTo && edgeLength > 0.000001f)
            {
                float remaining = (edgeLength - TravelDistance) / edgeLength;
                if (remaining < 0f)
                    remaining = 0f;
                return RoadGraph.Hours(travelFrom, travelTo) * remaining * Mathf.Max(0.01f, LegScale) / speed;
            }

            if (!RoadGraph.Connects(Dock, destination))
                return 0f;
            if (RoadGraph.IsSea(Dock, destination) || RoadGraph.Length(Dock, destination) <= 0.000001f)
                return 0f;
            float scale = TravelScale.TimeScale(WeatherOn(Dock, destination));
            return RoadGraph.Hours(Dock, destination) * Mathf.Max(0.01f, scale) / speed;
        }

        void CampNeed(ref float water, ref float food, ref float hour, ref float store)
        {
            float camp = TravelScale.CampHours;
            float day = EconomyRates.DayHours;
            if (day <= 0f)
                day = 24f;
            water += camp / day * DailyWater;
            food += camp / day * DailyRations;
            hour = AdvanceForecastHour(hour, camp);
            store = TravelScale.WindowHours;
        }

        static float RoadNeed(float dayHours, float nightHours, float paceKit, float dayKit, float daily)
        {
            float day = EconomyRates.DayHours;
            if (day <= 0f)
                return 0f;
            float scaled = dayHours * paceKit * dayKit + nightHours * paceKit;
            return scaled / day * daily;
        }

        static float AdvanceForecastHour(float hour, float span)
        {
            float day = EconomyRates.DayHours;
            if (day <= 0f)
                return hour;
            hour += span;
            while (hour >= day)
                hour -= day;
            if (hour < 0f)
                hour = 0f;
            return hour;
        }

        SupplyForecast FinishForecast(float waterNeed, float rationNeed, float supplyWater, float supplyFood, int packs, bool sea)
        {
            float amber = 0.85f;
            KitDeparture check = GameConfig.Kit != null ? GameConfig.Kit.departureCheck : null;
            if (check != null && check.amber > 0f)
                amber = check.amber;
            bool red = !sea && (waterNeed > supplyWater + 0.0001f || rationNeed > supplyFood + 0.0001f);
            bool amberOn = !sea && !red && (
                (supplyWater > 0f && waterNeed > supplyWater * amber) ||
                (supplyFood > 0f && rationNeed > supplyFood * amber));
            float waterDays = DailyWater > 0f ? (supplyWater - waterNeed) / DailyWater : 0f;
            float foodDays = DailyRations > 0f ? (supplyFood - rationNeed) / DailyRations : 0f;
            return new SupplyForecast
            {
                WaterNeed = waterNeed,
                RationNeed = rationNeed,
                SupplyWater = supplyWater,
                SupplyRations = supplyFood,
                PackUnits = packs,
                HaulWater = HaulUnits(waterNeed - supplyWater, WaterUnit()),
                HaulRations = HaulUnits(rationNeed - supplyFood, RationUnit()),
                Red = red,
                Amber = amberOn,
                Sea = sea,
                DaysAfter = waterDays < foodDays ? waterDays : foodDays
            };
        }

        static int HaulUnits(float shortfall, float unit)
        {
            if (shortfall <= 0.001f || unit <= 0f)
                return 0;
            return (int)Math.Ceiling(shortfall / unit - 0.000001d);
        }

#if UNITY_EDITOR
        public void ShotOpenCritical()
        {
            if (!Traveling)
                ShotDepartPaused(PlaceId.Draven);
            criticalPending = true;
            criticalShown = true;
            HoldTravel = true;
            Touch();
        }

        public void ShotOpenDeath()
        {
            if (!Traveling)
                ShotDepartPaused(PlaceId.Draven);
            rescueTitle = "Dry";
            rescueLead = "The skins are empty. The sand is not.";
            rescueBody = HighlandsText();
            pendingRescueDays = 2;
            deathPending = true;
            criticalPending = false;
            HoldTravel = true;
            Touch();
        }

        public void ShotClearSupply()
        {
            if (!deathPending && !criticalPending)
                return;
            deathPending = false;
            criticalPending = false;
            if (!CampPending && !AwaitingPace && !EncounterPending)
                HoldTravel = false;
        }

        public void ShotCargoWaterSkin()
        {
            stacks.Clear();
            GiveCargo(GoodId.Water, 1f);
        }

        public void ShotDropDrinkPacks()
        {
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                GoodId good = stacks[i].Good;
                if (good == GoodId.Water || good == GoodId.Rations || good == GoodId.SeaWater)
                    stacks.RemoveAt(i);
            }
        }

        string HighlandsText()
        {
            RescueFile file = GameConfig.Rescue;
            if (file == null || file.regions == null)
                return "The road brought you back to Kharûn.";
            for (int i = 0; i < file.regions.Length; i++)
            {
                RescueRegion region = file.regions[i];
                if (region == null || region.id != "highlands" || region.texts == null || region.texts.Length == 0)
                    continue;
                return region.texts[0];
            }

            return "The road brought you back to Kharûn.";
        }
#endif
    }
}
