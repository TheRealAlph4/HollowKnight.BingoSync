using BingoSync;
using System;
using System.Threading;
using System.Threading.Tasks;

internal static class RetryHelper
{
    private static readonly int delayMilliseconds = 100;
    private static readonly int maxDelayMilliseconds = 2000;

    public static void RetryWithExponentialBackoff(Func<Task> action, int maxRetries, string requestName, Action failCallback = null, int retries = 0)
    {
        if (retries >= maxRetries) {
            Log.Error($"All retries used but could not complete request {requestName}");
            failCallback?.Invoke();
            return;
        }

        Timer timer = null;
        Task currentTask = action.Invoke();
        _ = currentTask.ContinueWith(task =>
        {
            if (task.Exception == null)
            {
                if (retries > 0)
                {
                    Log.Info($"{requestName} request was successful on try {retries}");
                }
                return;
            }
            int delay = Math.Min((2 << retries) * delayMilliseconds, maxDelayMilliseconds);
            timer = new Timer(_ => {
                timer.Dispose();
                RetryWithExponentialBackoff(action, maxRetries, requestName, failCallback, retries + 1);
            }, null, delay, 0);
        });
    }
}
