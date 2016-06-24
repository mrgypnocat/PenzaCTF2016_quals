using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace StellarAssist
{
	public sealed class Server
	{
        public readonly IPAddress LocalIpAddress;
        public readonly int LocalPort;
	    
        public TcpClient TcpClientParams;
	    private TcpListener TcpListener;

	    public Server(IPAddress ipAddr, int localPort)
		{
		    LocalIpAddress = ipAddr;
		    LocalPort = localPort;

            TcpListener = new TcpListener(LocalIpAddress, localPort);
            TcpClientParams=new TcpClient();
            //TODO
	        if (SomeShitCodeGuy())
            //if (true)
	        {
                Console.WriteLine("Rewrite me, baby!");
                RunWorkingCycle();
	        }
	        else
	        {
	            Console.WriteLine("only root can run it!");
	        }
        }

        ~Server()
        {
            if (TcpListener != null)
            {
                TcpListener.Stop();
            }
        }

        private bool SomeShitCodeGuy()
        {
            var proc = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/bash",
                    Arguments = "-c whoami",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            proc.Start();
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(1000);
            return result.Contains("root");
        }

	    private void RunWorkingCycle()
        {
            TcpListener.Start();
            while (true)
            {
                var someClient = TcpListener.AcceptTcpClient();

                someClient.SendBufferSize = TcpClientParams.SendBufferSize;
                someClient.ReceiveBufferSize = TcpClientParams.ReceiveBufferSize;

                someClient.SendTimeout = TcpClientParams.SendTimeout;
                someClient.ReceiveTimeout = TcpClientParams.ReceiveTimeout;

                /*                
                var thread = new Thread(new ParameterizedThreadStart(ClientThread));
                thread.Start(client);
                 */
// ReSharper disable RedundantDelegateCreation
                ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), someClient);
// ReSharper restore RedundantDelegateCreation
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

	    private static void ClientThread(Object someClient)
        {
// ReSharper disable ObjectCreationAsStatement
            new ClientConnectionHandler((TcpClient)someClient);
// ReSharper restore ObjectCreationAsStatement
        }
	}
}

