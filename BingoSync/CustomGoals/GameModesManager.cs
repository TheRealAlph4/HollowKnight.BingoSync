using System;
using System.Collections.Generic;
using System.Linq;
using BingoSync.Settings;
using static Mono.Security.X509.X520;

namespace BingoSync
{
    internal static class GameModesManager
    {
        private static Action<string> Log;
        private static readonly List<GameMode> gameModes = [];
        private static readonly Dictionary<string, BingoGoal> vanillaGoals = [];
        private static readonly Dictionary<string, List<BingoGoal>> goalGroupDefinitions = [];
        private static readonly List<BingoGoal> allCustomGoals = [];

        public static void Setup(Action<string> log)
        {
            Log = log;
            SetupVanillaGoals();
            gameModes.Add(new GameMode("Vanilla", vanillaGoals));
            RegisterGoalsForCustom("Vanilla", vanillaGoals);
        }

        public static void AddGameMode(GameMode gameMode)
        {
            gameModes.Add(gameMode);
        }

        public static GameMode FindGameModeByDisplayName(string name)
        {
            return gameModes.Find(gameMode => gameMode.GetDisplayName() == name);
        }

        public static void LoadCustomGameModes()
        {
            gameModes.RemoveAll(gameMode => gameMode.GetType() == typeof(CustomGameMode));
            foreach (GameMode gameMode in BingoSync.modSettings.CustomGameModes)
            {
                AddGameMode(gameMode);
            }
        }

        public static void RegisterGoalsForCustom(string groupName, Dictionary<string, BingoGoal> goals)
        {
            allCustomGoals.AddRange(goals.Values);
            goalGroupDefinitions[groupName] = goals.Values.ToList();
        }

        internal static List<BingoGoal> GetGoalsFromNames(List<string> names)
        {
            return allCustomGoals.Where(goal => names.Contains(goal.name)).ToList();
        }

        public static List<GoalGroup> CreateDefaultCustomSettings()
        {
            List<GoalGroup> defaultSettings = [];
            foreach(var group in goalGroupDefinitions)
            {
                defaultSettings.Add(new GoalGroup(group.Key, group.Value.Select(goal => goal.name).ToList()));
            }
            return defaultSettings;
        }

        public static List<string> GameModeNames()
        {
            List<string> names = [];
            foreach (GameMode gameMode in gameModes)
            {
                names.Add(gameMode.GetDisplayName());
            }
            return names;
        }

        public static void Generate()
        {
            int seed = Controller.GetCurrentSeed();
            string lockoutString = Controller.MenuIsLockout ? "lockout" : "non-lockout";
            BingoSyncClient.ChatMessage($"{Controller.RoomNickname} is generating {Anify(Controller.ActiveGameMode)} board in {lockoutString} mode with seed {seed}");
            string customJSON = GameMode.GetErrorBoard();
            if (Controller.ActiveGameMode != string.Empty)
            {
                customJSON = FindGameModeByDisplayName(Controller.ActiveGameMode).GenerateBoard(seed);
            }
            BingoSyncClient.NewCard(customJSON, Controller.MenuIsLockout);
        }

        private static void SetupVanillaGoals()
        {
            List<string> goals = ["Abyss Shriek", "All Grubs: Greenpath (4) + Fungal (2)", "All Grubs: Xroads (5) + Fog Canyon (1)", "Break the 420 geo rock in Kingdom's Edge", "Broken Vessel", "Buy 6 map pins from Iselda (All but two)", "Buy 6 maps", "Buy 8 map pins from Iselda (All)", "Collect 1 Arcane Egg", "Collect 3 King's Idols", "Collect 500 essence", "Collector", "Colosseum 1", "Complete 4 full dream trees", "Crystal Guardian 1", "Crystal Guardian 2", "Crystal Heart", "Cyclone Slash", "Dash Slash", "Deep Focus + Quick Focus", "Defeat Colosseum Zote", "Descending Dark", "Desolate Dive", "Dream Gate", "Dream Nail", "Dream Wielder", "Dung Defender", "Elder Hu", "Failed Champion", "False Knight + Brooding Mawlek", "Flukemarm", "Flukenest", "Fragile Heart, Greed, and Strength", "Galien", "Give Flower to Elderbug", "Glowing Womb + Grimmchild", "Goam and Garpede Journal Entries", "Gorb", "Great Slash", "Grubsong", "Have 1500 geo in the bank", "Have 2 Pale Ore", "Have 4 Rancid Eggs", "Have 5 Hallownest Seals", "Have 5 Wanderer's Journals", "Heavy Blow + Steady Body", "Herrah", "Hive Knight", "Hiveblood", "Hornet 2", "Howling Wraiths", "Interact with 5 Cornifer locations", "Isma's Tear", "Kill 2 Soul Warriors", "Kill 4 Mimics", "Kill 6 different Stalking Devouts", "Kill Myla", "Kill your shade in Jiji's Hut", "Lifeblood Heart + Joni's Blessing", "Longnail + MoP", "Lost Kin", "Lumafly Lantern", "Lurien", "Mantis Lords", "Markoth", "Marmu", "Mask Shard  in the Hive", "Monarch Wings", "Monomon", "Nail 2", "Nail 3", "No Eyes", "Nosk", "Obtain 1 extra mask", "Obtain 1 extra soul vessel", "Obtain 2 extra masks", "Obtain 3 extra notches", "Obtain fountain vessel fragment", "Pale Lurker", "Parry Revek 3 times without dying (Glade of Hope Guard)", "Pay for 6 tolls", "Pick up the Love Key", "Quick Slash", "Rescue Bretta + Sly", "Rescue Zote in Deepnest", "Save 15 grubs", "Save 20 grubs", "Save the 3 grubs in Queen's Garden", "Save the 3 grubs in Waterways", "Save the 5 grubs in CoT", "Save the 5 grubs in Deepnest", "Save the 7 grubs in Crystal Peak", "Shade Cloak", "Shade Soul", "Shape of Unn", "Sharp Shadow", "Soul Master", "Soul Tyrant", "Spell Twister + Shaman Stone", "Spend 3000 geo", "Spend 4000 geo", "Spend 5000 geo", "Sprintmaster + Dashmaster", "Stag Nest vessel fragment", "Take a bath in all 4 Hot Springs", "Talk to Bardoon", "Talk to Emilitia (shortcut out of sewers)", "Talk to Hornet at CoT Statue + Herrah", "Talk to Lemm with Crest Equipped", "Talk to Mask Maker", "Talk to Midwife", "Talk to the Fluke Hermit", "Thorns of agony + Baldur Shell + Spore Shroom", "Traitor Lord", "Tram Pass + Visit all 5 Tram Stations", "Unlock Deepnest Stag", "Unlock Hidden Stag Station", "Unlock Queen's Garden Stag", "Unlock Queen's Stag + King's Stag Stations", "Upgrade Grimmchild once", "Use 2 Simple Keys", "Use City Crest + Ride both CoT large elevators", "Uumuu", "Vengefly King + Massive Moss Charger", "Void Tendrils Journal Entry", "Watch Cloth Die", "Watcher Knights", "Weaversong", "Xero"];
            foreach (string goal in goals)
            {
                vanillaGoals.Add(goal, new(goal));
            }
            vanillaGoals["Break the 420 geo rock in Kingdom's Edge"].exclusions = ["Quick Slash"];
            vanillaGoals["Broken Vessel"].exclusions = ["Monarch Wings"];
            vanillaGoals["Buy 6 map pins from Iselda (All but two)"].exclusions = ["Buy 8 map pins from Iselda (All)"];
            vanillaGoals["Buy 8 map pins from Iselda (All)"].exclusions = ["Buy 6 map pins from Iselda (All but two)"];
            vanillaGoals["Collect 1 Arcane Egg"].exclusions = ["Shade Cloak", "Void Tendrils Journal Entry"];
            vanillaGoals["Collect 500 essence"].exclusions = ["Dream Wielder"];
            vanillaGoals["Colosseum 1"].exclusions = ["Defeat Colosseum Zote"];
            vanillaGoals["Defeat Colosseum Zote"].exclusions = ["Colosseum 1", "Rescue Zote in Deepnest"];
            vanillaGoals["Desolate Dive"].exclusions = ["Soul Master"];
            vanillaGoals["Dream Nail"].exclusions = ["Xero"];
            vanillaGoals["Dream Wielder"].exclusions = ["Collect 500 essence"];
            vanillaGoals["Dung Defender"].exclusions = ["Talk to Lemm with Crest Equipped"];
            vanillaGoals["Flukemarm"].exclusions = ["Flukenest"];
            vanillaGoals["Flukenest"].exclusions = ["Flukemarm"];
            vanillaGoals["Have 2 Pale Ore"].exclusions = ["Nail 3"];
            vanillaGoals["Herrah"].exclusions = ["Talk to Hornet at CoT Statue + Herrah"];
            vanillaGoals["Hive Knight"].exclusions = ["Hiveblood", "Mask Shard  in the Hive", "Tram Pass + Visit all 5 Tram Stations"];
            vanillaGoals["Hiveblood"].exclusions = ["Hive Knight", "Mask Shard  in the Hive", "Tram Pass + Visit all 5 Tram Stations"];
            vanillaGoals["Isma's Tear"].exclusions = ["Talk to Emilitia (shortcut out of sewers)"];
            vanillaGoals["Kill 4 Mimics"].exclusions = ["Save the 5 grubs in Deepnest", "Save the 7 grubs in Crystal Peak"];
            vanillaGoals["Longnail + MoP"].exclusions = ["Mantis Lords"];
            vanillaGoals["Mantis Lords"].exclusions = ["Longnail + MoP"];
            vanillaGoals["Mask Shard  in the Hive"].exclusions = ["Hive Knight", "Hiveblood", "Tram Pass + Visit all 5 Tram Stations"];
            vanillaGoals["Monarch Wings"].exclusions = ["Broken Vessel"];
            vanillaGoals["Nail 3"].exclusions = ["Have 2 Pale Ore"];
            vanillaGoals["Obtain fountain vessel fragment"].exclusions = ["Spend 3000 geo", "Spend 4000 geo"];
            vanillaGoals["Quick Slash"].exclusions = ["Break the 420 geo rock in Kingdom's Edge"];
            vanillaGoals["Rescue Zote in Deepnest"].exclusions = ["Defeat Colosseum Zote"];
            vanillaGoals["Save the 5 grubs in Deepnest"].exclusions = ["Kill 4 Mimics"];
            vanillaGoals["Save the 7 grubs in Crystal Peak"].exclusions = ["Kill 4 Mimics"];
            vanillaGoals["Shade Cloak"].exclusions = ["Collect 1 Arcane Egg", "Void Tendrils Journal Entry"];
            vanillaGoals["Soul Master"].exclusions = ["Desolate Dive"];
            vanillaGoals["Spend 3000 geo"].exclusions = ["Obtain fountain vessel fragment", "Spend 4000 geo"];
            vanillaGoals["Spend 4000 geo"].exclusions = ["Obtain fountain vessel fragment", "Spend 3000 geo", "Spend 5000 geo"];
            vanillaGoals["Spend 5000 geo"].exclusions = ["Obtain fountain vessel fragment", "Spend 4000 geo"];
            vanillaGoals["Talk to Emilitia (shortcut out of sewers)"].exclusions = ["Isma's Tear"];
            vanillaGoals["Talk to Hornet at CoT Statue + Herrah"].exclusions = ["Herrah"];
            vanillaGoals["Talk to Lemm with Crest Equipped"].exclusions = ["Dung Defender"];
            vanillaGoals["Talk to Mask Maker"].exclusions = ["Talk to Midwife"];
            vanillaGoals["Talk to Midwife"].exclusions = ["Talk to Mask Maker"];
            vanillaGoals["Traitor Lord"].exclusions = ["Watch Cloth Die"];
            vanillaGoals["Tram Pass + Visit all 5 Tram Stations"].exclusions = ["Hive Knight", "Hiveblood", "Mask Shard  in the Hive"];
            vanillaGoals["Void Tendrils Journal Entry"].exclusions = ["Collect 1 Arcane Egg", "Shade Cloak"];
            vanillaGoals["Watch Cloth Die"].exclusions = ["Traitor Lord"];
            vanillaGoals["Xero"].exclusions = ["Dream Nail"];
        }

        public static Dictionary<string, BingoGoal> GetVanillaGoals()
        {
            return vanillaGoals;
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
