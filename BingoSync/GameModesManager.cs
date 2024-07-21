using MagicUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BingoSync
{
    internal static class GameModesManager
    {
        private static Action<string> Log;
        private static readonly List<GameMode> _gameModes = [];
        private static readonly Dictionary<string, BingoGoal> _vanillaGoals = [];
        private static string activeGameMode = string.Empty;
        private static bool lockout = true;

        public static void Setup(Action<string> log)
        {
            Log = log;
            SetupVanillaGoals();
            _gameModes.Add(new GameMode("Vanilla", _vanillaGoals));
        }

        public static void AddGameMode(GameMode gameMode)
        {
            _gameModes.Add(gameMode);
        }

        public static void RegisterGoalsForCustom(string groupName, Dictionary<string, BingoGoal> goals)
        {
            // implement custom gamemode at some point
        }

        public static List<string> GameModeNames()
        {
            List<string> names = [];
            foreach (GameMode gameMode in _gameModes)
            {
                names.Add(gameMode.GetName());
            }
            return names;
        }

        public static void SetActiveGameMode(string gameMode)
        {
            activeGameMode = gameMode;
        }

        public static bool GetLockout()
        {
            return lockout;
        }

        public static void SetLockout(bool input)
        {
            lockout = input;
        }

        public static void Generate(Button sender)
        {
            Log("Generate button clicked");
            string lockoutString = lockout ? "lockout" : "non-lockout";
            BingoSyncClient.ChatMessage($"{BingoSyncClient.nickname} is generating {Anify(activeGameMode)} board in {lockoutString} mode");
            string customJSON = GameMode.GetErrorBoard();
            if (activeGameMode != string.Empty)
            {
                customJSON = _gameModes.Find(gameMode => gameMode.GetName() == activeGameMode).GenerateBoard();
            }
            BingoSyncClient.NewCard(customJSON, lockout);
        }

        private static void SetupVanillaGoals()
        {
            List<string> goals = ["Abyss Shriek", "All Grubs: Greenpath (4) + Fungal (2)", "All Grubs: Xroads (5) + Fog Canyon (1)", "Break the 420 geo rock in Kingdom's Edge", "Broken Vessel", "Buy 6 map pins from Iselda (All but two)", "Buy 6 maps", "Buy 8 map pins from Iselda (All)", "Collect 1 Arcane Egg", "Collect 3 King's Idols", "Collect 500 essence", "Collector", "Colosseum 1", "Complete 4 full dream trees", "Crystal Guardian 1", "Crystal Guardian 2", "Crystal Heart", "Cyclone Slash", "Dash Slash", "Deep Focus + Quick Focus", "Defeat Colosseum Zote", "Descending Dark", "Desolate Dive", "Dream Gate", "Dream Nail", "Dream Wielder", "Dung Defender", "Elder Hu", "Failed Champion", "False Knight + Brooding Mawlek", "Flukemarm", "Flukenest", "Fragile Heart, Greed, and Strength", "Galien", "Give Flower to Elderbug", "Glowing Womb + Grimmchild", "Goam and Garpede Journal Entries", "Gorb", "Great Slash", "Grubsong", "Have 1500 geo in the bank", "Have 2 Pale Ore", "Have 4 Rancid Eggs", "Have 5 Hallownest Seals", "Have 5 Wanderer's Journals", "Heavy Blow + Steady Body", "Herrah", "Hive Knight", "Hiveblood", "Hornet 2", "Howling Wraiths", "Interact with 5 Cornifer locations", "Isma's Tear", "Kill 2 Soul Warriors", "Kill 4 Mimics", "Kill 6 different Stalking Devouts", "Kill Myla", "Kill your shade in Jiji's Hut", "Lifeblood Heart + Joni's Blessing", "Longnail + MoP", "Lost Kin", "Lumafly Lantern", "Lurien", "Mantis Lords", "Markoth", "Marmu", "Mask Shard  in the Hive", "Monarch Wings", "Monomon", "Nail 2", "Nail 3", "No Eyes", "Nosk", "Obtain 1 extra mask", "Obtain 1 extra soul vessel", "Obtain 2 extra masks", "Obtain 3 extra notches", "Obtain fountain vessel fragment", "Pale Lurker", "Parry Revek 3 times without dying (Glade of Hope Guard)", "Pay for 6 tolls", "Pick up the Love Key", "Quick Slash", "Rescue Bretta + Sly", "Rescue Zote in Deepnest", "Save 15 grubs", "Save 20 grubs", "Save the 3 grubs in Queen's Garden", "Save the 3 grubs in Waterways", "Save the 5 grubs in CoT", "Save the 5 grubs in Deepnest", "Save the 7 grubs in Crystal Peak", "Shade Cloak", "Shade Soul", "Shape of Unn", "Sharp Shadow", "Soul Master", "Soul Tyrant", "Spell Twister + Shaman Stone", "Spend 3000 geo", "Spend 4000 geo", "Spend 5000 geo", "Sprintmaster + Dashmaster", "Stag Nest vessel fragment", "Take a bath in all 4 Hot Springs", "Talk to Bardoon", "Talk to Emilitia (shortcut out of sewers)", "Talk to Hornet at CoT Statue + Herrah", "Talk to Lemm with Crest Equipped", "Talk to Mask Maker", "Talk to Midwife", "Talk to the Fluke Hermit", "Thorns of agony + Baldur Shell + Spore Shroom", "Traitor Lord", "Tram Pass + Visit all 5 Tram Stations", "Unlock Deepnest Stag", "Unlock Hidden Stag Station", "Unlock Queen's Garden Stag", "Unlock Queen's Stag + King's Stag Stations", "Upgrade Grimmchild once", "Use 2 Simple Keys", "Use City Crest + Ride both CoT large elevators", "Uumuu", "Vengefly King + Massive Moss Charger", "Void Tendrils Journal Entry", "Watch Cloth Die", "Watcher Knights", "Weaversong", "Xero"];
            foreach (string goal in goals)
            {
                _vanillaGoals.Add(goal, new(goal));
            }
            _vanillaGoals["Break the 420 geo rock in Kingdom's Edge"].exclusions = ["Quick Slash"];
            _vanillaGoals["Broken Vessel"].exclusions = ["Monarch Wings"];
            _vanillaGoals["Buy 6 map pins from Iselda (All but two)"].exclusions = ["Buy 8 map pins from Iselda (All)"];
            _vanillaGoals["Buy 8 map pins from Iselda (All)"].exclusions = ["Buy 6 map pins from Iselda (All but two)"];
            _vanillaGoals["Collect 1 Arcane Egg"].exclusions = ["Shade Cloak", "Void Tendrils Journal Entry"];
            _vanillaGoals["Collect 500 essence"].exclusions = ["Dream Wielder"];
            _vanillaGoals["Colosseum 1"].exclusions = ["Defeat Colosseum Zote"];
            _vanillaGoals["Defeat Colosseum Zote"].exclusions = ["Colosseum 1", "Rescue Zote in Deepnest"];
            _vanillaGoals["Desolate Dive"].exclusions = ["Soul Master"];
            _vanillaGoals["Dream Nail"].exclusions = ["Xero"];
            _vanillaGoals["Dream Wielder"].exclusions = ["Collect 500 essence"];
            _vanillaGoals["Dung Defender"].exclusions = ["Talk to Lemm with Crest Equipped"];
            _vanillaGoals["Flukemarm"].exclusions = ["Flukenest"];
            _vanillaGoals["Flukenest"].exclusions = ["Flukemarm"];
            _vanillaGoals["Have 2 Pale Ore"].exclusions = ["Nail 3"];
            _vanillaGoals["Herrah"].exclusions = ["Talk to Hornet at CoT Statue + Herrah"];
            _vanillaGoals["Hive Knight"].exclusions = ["Hiveblood", "Mask Shard  in the Hive", "Tram Pass + Visit all 5 Tram Stations"];
            _vanillaGoals["Hiveblood"].exclusions = ["Hive Knight", "Mask Shard  in the Hive", "Tram Pass + Visit all 5 Tram Stations"];
            _vanillaGoals["Isma's Tear"].exclusions = ["Talk to Emilitia (shortcut out of sewers)"];
            _vanillaGoals["Kill 4 Mimics"].exclusions = ["Save the 5 grubs in Deepnest", "Save the 7 grubs in Crystal Peak"];
            _vanillaGoals["Longnail + MoP"].exclusions = ["Mantis Lords"];
            _vanillaGoals["Mantis Lords"].exclusions = ["Longnail + MoP"];
            _vanillaGoals["Mask Shard  in the Hive"].exclusions = ["Hive Knight", "Hiveblood", "Tram Pass + Visit all 5 Tram Stations"];
            _vanillaGoals["Monarch Wings"].exclusions = ["Broken Vessel"];
            _vanillaGoals["Nail 3"].exclusions = ["Have 2 Pale Ore"];
            _vanillaGoals["Obtain fountain vessel fragment"].exclusions = ["Spend 3000 geo", "Spend 4000 geo"];
            _vanillaGoals["Quick Slash"].exclusions = ["Break the 420 geo rock in Kingdom's Edge"];
            _vanillaGoals["Rescue Zote in Deepnest"].exclusions = ["Defeat Colosseum Zote"];
            _vanillaGoals["Save the 5 grubs in Deepnest"].exclusions = ["Kill 4 Mimics"];
            _vanillaGoals["Save the 7 grubs in Crystal Peak"].exclusions = ["Kill 4 Mimics"];
            _vanillaGoals["Shade Cloak"].exclusions = ["Collect 1 Arcane Egg", "Void Tendrils Journal Entry"];
            _vanillaGoals["Soul Master"].exclusions = ["Desolate Dive"];
            _vanillaGoals["Spend 3000 geo"].exclusions = ["Obtain fountain vessel fragment", "Spend 4000 geo"];
            _vanillaGoals["Spend 4000 geo"].exclusions = ["Obtain fountain vessel fragment", "Spend 3000 geo", "Spend 5000 geo"];
            _vanillaGoals["Spend 5000 geo"].exclusions = ["Obtain fountain vessel fragment", "Spend 4000 geo"];
            _vanillaGoals["Talk to Emilitia (shortcut out of sewers)"].exclusions = ["Isma's Tear"];
            _vanillaGoals["Talk to Hornet at CoT Statue + Herrah"].exclusions = ["Herrah"];
            _vanillaGoals["Talk to Lemm with Crest Equipped"].exclusions = ["Dung Defender"];
            _vanillaGoals["Talk to Mask Maker"].exclusions = ["Talk to Midwife"];
            _vanillaGoals["Talk to Midwife"].exclusions = ["Talk to Mask Maker"];
            _vanillaGoals["Traitor Lord"].exclusions = ["Watch Cloth Die"];
            _vanillaGoals["Tram Pass + Visit all 5 Tram Stations"].exclusions = ["Hive Knight", "Hiveblood", "Mask Shard  in the Hive"];
            _vanillaGoals["Void Tendrils Journal Entry"].exclusions = ["Collect 1 Arcane Egg", "Shade Cloak"];
            _vanillaGoals["Watch Cloth Die"].exclusions = ["Traitor Lord"];
            _vanillaGoals["Xero"].exclusions = ["Dream Nail"];
        }

        public static Dictionary<string, BingoGoal> GetVanillaGoals()
        {
            return _vanillaGoals;
        }

        private static string Anify(string word)
        {
            if (new List<string> { "a", "e", "i", "o", "u" }.Contains(word.Substring(0, 1).ToLower()))
            {
                return "an " + word;
            }
            return "a " + word;
        }
    }
}
