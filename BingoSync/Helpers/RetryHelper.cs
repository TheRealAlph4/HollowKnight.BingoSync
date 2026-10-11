using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BingoSync.Helpers
{
    internal static class RetryHelper
    {
        private static readonly int defaultDelayMilliseconds = 100;
        private static readonly int maxDelayMilliseconds = 2000;

        public static void RetryWithExponentialBackoff(Func<Task> action, int maxRetries, string requestName, Action? failCallback = null, int retries = 0)
        {
            if (retries >= maxRetries)
            {
                Log.Error($"All retries used but could not complete request {requestName}");
                failCallback?.Invoke();
                return;
            }

            Timer? timer = null;
            Task currentTask = action.Invoke();
            _ = currentTask.ContinueWith(task =>
            {
                int delayMilliseconds = Math.Min((2 << retries) * defaultDelayMilliseconds, maxDelayMilliseconds);
                if (task.Exception == null)
                {
                    if (retries > 0)
                    {
                        Log.Info($"{requestName} request was successful on try {retries}");
                    }
                    return;
                }
                else if (task.Exception.InnerException is HttpRequestException ex)
                {
                    Log.Warn($"Exception during {requestName} request: '{ex.Message}'");
                    if (ex.Message.Contains("400"))
                    {
                        Log.Error($"{requestName} request failed: '{ex.Message}'");
                        failCallback?.Invoke();
                        return;
                    }
                    if (ex.Message.Contains("429"))
                    {
                        Log.Info($"Retrying {requestName} request with maximum delay due to response '{ex.Message}'");
                        delayMilliseconds = maxDelayMilliseconds;
                    }
                }
                timer = new Timer(_ => {
                    timer?.Dispose();
                    RetryWithExponentialBackoff(action, maxRetries, requestName, failCallback, retries + 1);
                }, null, delayMilliseconds, 0);
            });
        }
    }
}
