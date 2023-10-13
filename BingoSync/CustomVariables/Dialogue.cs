namespace BingoSync.CustomVariables
{
    internal static class Dialogue
    {
        public static void StartConversation(On.DialogueBox.orig_StartConversation orig, DialogueBox self, string convName, string sheetName)
        {
            orig(self, convName, sheetName);
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
            // Mr Mushroom
            if (convName.StartsWith("MR_MUSHROOM")) {
                BingoTracker.UpdateBoolean("metMrMushroom", true);
            }
            // Hornet at Beast's Den
            if (convName.StartsWith("HORNET_SPIDER_TOWN")) {
                BingoTracker.UpdateBoolean("metHornetBeastsDen", true);
            }
        }
    }
}
