using System;
using System.Net;
using System.Threading;
using ForeignLibs;

namespace StellarAssist
{
    class Program
    {
        static void Main()
        {
            int maxThreadsCount = Environment.ProcessorCount * 8;
            ThreadPool.SetMaxThreads(maxThreadsCount, maxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
            new Server(IPAddress.Any, 8080);
        }
    }
}
