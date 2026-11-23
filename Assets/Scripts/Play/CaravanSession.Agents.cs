using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Caravans.Play
{
    public sealed partial class CaravanSession
    {
        bool scoutHired;
        bool hagglerHired;
        bool guardHired;
        bool guardSaveSpent;
        readonly List<string> roadNotes = new List<string>();
        string agentNotice;
        bool rushChanceForced;
        float rushChanceValue;
        int rushCargoIndex = -1;

        public bool ScoutHired
        {
            get { return scoutHired; }
        }

        public bool HagglerHired
        {
            get { return hagglerHired; }
        }

        public bool GuardHired
        {
            get { return guardHired; }
        }

        public int AgentCount
        {
            get { return agents; }
        }

        public string AgentNotice
        {
            get { return agentNotice ?? string.Empty; }
        }

        public int RoadNoteCount
        {
            get { return roadNotes.Count; }
        }

        public bool TryHireAgent(string role)
        {
            if (!TryRole(role, out int slot))
                return false;
            if (Dock != PlaceId.Kharun || Traveling || Hired(slot))
                return false;
            int cost = HireCost(slot);
            if (Coin < cost)
                return false;
            Coin -= cost;
            SetHired(slot, true);
            SyncHiredCount();
            Touch();
            return true;
        }

        public bool TryDismissAgent(string role)
        {
            if (!TryRole(role, out int slot))
                return false;
            if (Dock != PlaceId.Kharun || Traveling || !Hired(slot))
                return false;
            SetHired(slot, false);
            SyncHiredCount();
            Touch();
            return true;
        }

        public int AgentHireCost(string role)
        {
            return TryRole(role, out int slot) ? HireCost(slot) : 0;
        }

        public int AgentWage(string role)
        {
            return TryRole(role, out int slot) ? Wage(slot) : 0;
        }

        public bool AgentHired(string role)
        {
            return TryRole(role, out int slot) && Hired(slot);
        }

        public string ScoutLine(RoadPace pace, bool roadLit)
        {
            if (!scoutHired || !roadLit)
                return string.Empty;
            int percent = Mathf.RoundToInt(EncounterTable.Chance(pace) * 100f);
            return "Scout: " + percent + "% chance of an encounter at " + PaceLabel(pace) + ".";
        }

        public void ClearAgentNotice()
        {
            agentNotice = null;
        }

        public string RoadNote(int index)
        {
            if (index < 0 || index >= roadNotes.Count)
                return string.Empty;
            return roadNotes[index];
        }

        public string RoadNotesBlock()
        {
            if (roadNotes.Count == 0)
                return "No road notes yet.";
            var builder = new StringBuilder();
            builder.Append("Road notes");
            for (int i = 0; i < roadNotes.Count; i++)
            {
                builder.Append('\n');
                builder.Append(roadNotes[i]);
            }

            return builder.ToString();
        }

        internal void ForceRushCargoLossChance(float chance)
        {
            rushChanceForced = true;
            rushChanceValue = chance;
        }

        internal void ClearRushCargoLossChance()
        {
            rushChanceForced = false;
        }

        internal void ForceRushCargoIndex(int index)
        {
            rushCargoIndex = index;
        }

#if UNITY_EDITOR
        internal void ShotClearAgents()
        {
            ClearAgentRoles();
            agents = 0;
        }
#endif

        void PayWages()
        {
            var left = new List<string>();
            bool paid = false;
            if (scoutHired)
            {
                if (PayWage(EconomyRates.ScoutWage))
                    paid = true;
                else
                {
                    scoutHired = false;
                    left.Add("scout");
                }
            }

            if (hagglerHired)
            {
                if (PayWage(EconomyRates.HagglerWage))
                    paid = true;
                else
                {
                    hagglerHired = false;
                    left.Add("haggler");
                }
            }

            if (guardHired)
            {
                if (PayWage(EconomyRates.GuardWage))
                    paid = true;
                else
                {
                    guardHired = false;
                    left.Add("guard");
                }
            }

            if (left.Count == 0)
            {
                if (paid)
                    Touch();
                return;
            }

            // A dawn that pays everyone must leave a SetCaravan headcount alone.
            SyncHiredCount();
            agentNotice = LeaveLine(left);
            if (Traveling)
                lastNotice = agentNotice;
            Touch();
        }

        bool PayWage(int wage)
        {
            if (Coin < wage)
                return false;
            Coin -= wage;
            return true;
        }

        static string LeaveLine(List<string> names)
        {
            string who;
            if (names.Count <= 1)
                who = names[0];
            else if (names.Count == 2)
                who = names[0] + " and " + names[1];
            else
            {
                var builder = new StringBuilder();
                for (int i = 0; i < names.Count; i++)
                {
                    if (i > 0)
                        builder.Append(i == names.Count - 1 ? ", and " : ", ");
                    builder.Append(names[i]);
                }

                who = builder.ToString();
            }

            return "Your " + who + " left. You could not pay the day's wage.";
        }

        void CommitOutcome(EncounterOutcome outcome)
        {
            if (outcome != null && IsBad(outcome.tone) && guardHired && !guardSaveSpent)
            {
                guardSaveSpent = true;
                EncounterResult = "Your guard handled it.";
                EncounterResolved = true;
                Touch();
                return;
            }

            ApplyOutcome(outcome);
            ApplyRushCargo(outcome);
        }

        void ApplyRushCargo(EncounterOutcome outcome)
        {
            if (outcome == null || !IsBad(outcome.tone) || ChosenPace != RoadPace.Rush)
                return;
            // An empty hold must not spend the chance roll or a forced index.
            if (stacks.Count == 0)
                return;
            EnsureEncounterRandom();
            if (encounterRandom.NextDouble() >= RushChance())
                return;

            int index = rushCargoIndex;
            if (index < 0 || index >= stacks.Count)
                index = encounterRandom.Next(stacks.Count);
            GoodId good = stacks[index].Good;
            stacks.RemoveAt(index);
            rushCargoIndex = -1;
            string name = GoodName(good);
            string extra = " The rush cost you one " + name + ".";
            EncounterResult = string.IsNullOrEmpty(EncounterResult) ? extra.Substring(1) : EncounterResult + extra;
            roadNotes.Insert(0, WorldClock.Date(Day) + ": The rush cost you one " + name + " on the road to " + PlaceName(travelTo) + ".");
            Touch();
        }

        float RushChance()
        {
            return rushChanceForced ? rushChanceValue : EconomyRates.RushCargoLossChance;
        }

        void EnsureEncounterRandom()
        {
            if (encounterRandom == null)
                encounterRandom = new System.Random(GameConfig.Encounters.seed);
        }

        static bool IsBad(string tone)
        {
            return string.Equals(tone, "bad", System.StringComparison.OrdinalIgnoreCase);
        }

        void SyncHiredCount()
        {
            int count = 0;
            if (scoutHired)
                count++;
            if (hagglerHired)
                count++;
            if (guardHired)
                count++;
            agents = count;
        }

        void ClearAgentRoles()
        {
            scoutHired = false;
            hagglerHired = false;
            guardHired = false;
            guardSaveSpent = false;
            roadNotes.Clear();
            agentNotice = null;
            rushChanceForced = false;
            rushCargoIndex = -1;
        }

        void RestoreGuardSave()
        {
            guardSaveSpent = false;
        }

        bool Hired(int slot)
        {
            if (slot == 0)
                return scoutHired;
            if (slot == 1)
                return hagglerHired;
            return guardHired;
        }

        void SetHired(int slot, bool hired)
        {
            if (slot == 0)
                scoutHired = hired;
            else if (slot == 1)
                hagglerHired = hired;
            else
                guardHired = hired;
        }

        static int HireCost(int slot)
        {
            if (slot == 0)
                return EconomyRates.ScoutHire;
            if (slot == 1)
                return EconomyRates.HagglerHire;
            return EconomyRates.GuardHire;
        }

        static int Wage(int slot)
        {
            if (slot == 0)
                return EconomyRates.ScoutWage;
            if (slot == 1)
                return EconomyRates.HagglerWage;
            return EconomyRates.GuardWage;
        }

        static bool TryRole(string role, out int slot)
        {
            if (string.Equals(role, "Scout", System.StringComparison.OrdinalIgnoreCase))
            {
                slot = 0;
                return true;
            }

            if (string.Equals(role, "Haggler", System.StringComparison.OrdinalIgnoreCase))
            {
                slot = 1;
                return true;
            }

            if (string.Equals(role, "Guard", System.StringComparison.OrdinalIgnoreCase))
            {
                slot = 2;
                return true;
            }

            slot = -1;
            return false;
        }

        static string PaceLabel(RoadPace pace)
        {
            if (pace == RoadPace.Careful)
                return "Careful";
            if (pace == RoadPace.Rush)
                return "Rush";
            return "Steady";
        }
    }
}
