using Rocket.Core.Logging;
using Rocket.Core.Utils;
using SDG.Unturned;
using System;
using System.Threading;

namespace Anomoly.ChatTags.Helpers
{
    public class ThreadHelper
    {
        public static void RunAsynchronously(System.Action action, string exceptionMessage = null)
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    RunSynchronously(() => Logger.LogException(e, exceptionMessage));
                }
            });
        }

        public static void RunSynchronously(System.Action action, float delaySeconds = 0)
        {
            if (ThreadUtil.IsGameThread(Thread.CurrentThread) && delaySeconds == 0)
            {
                action.Invoke();
            }
            else
            {
                QueueOnMainThread(action, delaySeconds);
            }
        }

        public static void QueueOnMainThread(System.Action action, float delaySeconds = 0)
        {
            TaskDispatcher.QueueOnMainThread(action, delaySeconds);
        }
    }
}
