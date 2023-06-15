using Modding.Converters;
using Newtonsoft.Json;

namespace Settings
{
    public class ModSettings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds Keybinds = new KeyBinds();
        public bool RevealCardOnGameStart = false;
        public string DefaultNickname = "";
        public string DefaultPassword = "";
        public string DefaultColor = "red";
    }
}
