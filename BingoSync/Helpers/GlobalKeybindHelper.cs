using UnityEngine;

namespace BingoSync.Helpers
{
    internal class GlobalKeybindHelper : MonoBehaviour
    {
        public void Update()
        {
            if (Controller.IsDebugMode && Controller.GlobalSettings.Keybinds.DumpDebugInfo.WasPressed)
            {
                Controller.DumpDebugInfo();
            }
        }
    }
}
