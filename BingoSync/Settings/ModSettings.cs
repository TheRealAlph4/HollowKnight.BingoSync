using Modding.Converters;
using Newtonsoft.Json;

namespace Settings
{
    public class ModSettings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds Keybinds = new();
        public bool RevealCardOnGameStart = false;
        public bool UnmarkGoals = false;
        public string DefaultNickname = "";
        public string DefaultPassword = "";
        public string DefaultColor = "red";
    }
}
