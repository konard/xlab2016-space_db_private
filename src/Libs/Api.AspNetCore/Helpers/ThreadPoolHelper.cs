using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Api.AspNetCore.Helpers
{
    public static class ThreadPoolHelper
    {
        public static void SetMaxThreads(out int workerThreads, out int ioCompletionThreads,
            out int minWorkerThread, out int minPortCompletionThreads)
        {
            ThreadPool.GetMaxThreads(out workerThreads, out ioCompletionThreads);
            ThreadPool.SetMaxThreads(Math.Max(100000, workerThreads), Math.Max(100000, ioCompletionThreads));

            ThreadPool.GetMinThreads(out minWorkerThread, out minPortCompletionThreads);
            ThreadPool.SetMinThreads(100000, 100000);
        }
    }
}
