using BingoSync;
using Modding.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BingoSync.Settings
{
    public class ModSettings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds Keybinds = new();
        public int BoardID = 0;
        public bool RevealCardOnGameStart = false;
        public bool RevealCardWhenOthersReveal = false;
        public bool UnmarkGoals = false;
        public string DefaultNickname = "";
        public string DefaultPassword = "";
        public string DefaultColor = "red";
        public int CustomGameModeCount = 3;
        public List<CustomGameMode> CustomGameModes = [];
    }
}
