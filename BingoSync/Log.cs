namespace BingoSync
{
    internal static class Log
    {
        public static void Info(object msg)
        {
            if (BingoSync.Instance != null)
            {
                BingoSync.Instance.Log(msg);
            }
            else
            {
                Modding.Logger.Log("[BingoSync] - " + msg.ToString());
            }
        }

        public static void Warn(object msg)
        {
            if (BingoSync.Instance != null)
            {
                BingoSync.Instance.LogWarn(msg);
            }
            else
            {
                Modding.Logger.LogWarn("[BingoSync] - " + msg.ToString());
            }
        }

        public static void Error(object msg)
        {
            if (BingoSync.Instance != null)
            {
                BingoSync.Instance.LogError(msg);
            }
            else
            {
                Modding.Logger.LogError("[BingoSync] - " + msg.ToString());
            }
        }
    }
}
