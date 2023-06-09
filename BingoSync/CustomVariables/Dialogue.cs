namespace BingoSync.CustomVariables
{
    internal static class Dialogue
    {
        public static void StartConversation(On.DialogueBox.orig_StartConversation orig, DialogueBox self, string convName, string sheetName)
        {
            // Lemm with Crest
            if (convName == "RELICDEALER_DUNG") {
                BingoTracker.UpdateBoolean("metLemmWithCrest", true);
            }
            // Fluke Hermit
            if (convName.StartsWith("FLUKE_HERMIT")) {
                BingoTracker.UpdateBoolean("metFlukeHermit", true);
            }
            // Cornifer
            if (sheetName == "Cornifer") {
                var scene = GameManager.instance.GetSceneNameString();
                var variableName = $"cornifer_{scene}";
                BingoTracker.UpdateBoolean(variableName, true);
            }
            orig(self, convName, sheetName);
        }
    }
}
