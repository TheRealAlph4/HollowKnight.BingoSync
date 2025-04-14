using BingoSync.CustomGoals;
using Modding.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BingoSync.Settings
{
    public class ModSettings
    {
        public enum HighlightType
        {
            Border,
            Star,
        }

        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds Keybinds = new();
        public bool RevealCardOnGameStart = false;
        public bool RevealCardWhenOthersReveal = false;
        public bool UnmarkGoals = false;
        public string DefaultNickname = "";
        public string DefaultPassword = "";
        public string DefaultColor = "red";
        public List<CustomGameMode> CustomGameModes = [];
        public bool DebugMode = false;
        public int BoardAlphaIndex = 0;
        public List<float> BoardAlphas = [0.135f, 0.5f, 1f];
        public float BoardAlpha {
            get
            {
                return BoardAlphas[BoardAlphaIndex];
            }
            set {}
        }
        public HighlightType SelectedHighlightSprite = HighlightType.Border;
    }
}
