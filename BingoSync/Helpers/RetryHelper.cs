using System;
using System.Threading;
using System.Threading.Tasks;

public static class RetryHelper
{
    private static readonly int delayMilliseconds = 100;
    private static Action<string> Log;

    public static void Setup(Action<string> log) {
        Log = log;
    }

    public static void RetryWithExponentialBackoff(Func<Task> action, int maxRetries, string requestName, Action failCallback = null, int retries = 0)
    {
        if (retries == maxRetries) {
            Log($"All retries used but could not complete request {requestName}");
            failCallback?.Invoke();
            return;
        }

        Timer timer = null;

        Task currentTask = action.Invoke();
        _ = currentTask.ContinueWith(task =>
        {
            if (task.Exception == null)
            {
                return;
            }
            int delay = (int)Math.Pow(2, retries) * delayMilliseconds;
            timer = new Timer(_ => {
                timer.Dispose();
                RetryWithExponentialBackoff(action, maxRetries, requestName, failCallback, retries + 1);
            }, null, delay, 0);
        });
    }
}
