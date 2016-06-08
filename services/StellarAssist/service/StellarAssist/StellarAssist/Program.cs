using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StellarAssist
{
    class Program
    {
        static void Main(string[] args)
        {
            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
            var srv = new Server(IPAddress.Any, 8080);
        }
    }
}
