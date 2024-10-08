﻿using BingoSync.CustomGoals;
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
        public List<CustomGameMode> CustomGameModes = [];
        public bool DebugMode = false;
    }
}
