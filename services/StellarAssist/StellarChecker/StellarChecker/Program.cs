using System;
using System.Text.RegularExpressions;

namespace StellarChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if ((args == null)||(args.Length==0))
            {
                args = new[]
                {
                    "chk",
                    "172.16.40.152",
                    "8080",
                    "fucker="
                };
            }
            
            if (args.Length>=3)
            {
                string command = args[0];
                string ipAddr = args[1];
                int port;
                string state = null;
                if (!Int32.TryParse(args[2], out port))
                {
                    ReturnCheckerError();
                }

                //if some state in args
                if (args.Length >= 4)
                {
                    state = args[3];
                }

                var checkState = CheckStates.Mumble;
                string response = null;
                
                //trying to connect to service
                var checker = new ServiceCheck(ipAddr, port);
                switch (command.ToLower())
                {
                    case "put":
                        checkState = checker.PutFlag(state, out response);
                        break;
                    case "get":
                        checkState = checker.GetFlag(out response);
                        break;
                    case "chk":
                        checkState = checker.Check();
                        break;
                    default:
                        ReturnCheckerError();
                        break;
                }
                switch (checkState)
                {
                    case CheckStates.Up:
                        if (response != null) PrintState(response);
                        ReturnServiceIsUp();
                        break;
                    case CheckStates.Down:
                        ReturnServiceIsDown();
                        break;
                    case CheckStates.Mumble:
                        ReturnServiceIsMumble();
                        break;
                    case CheckStates.Corrupted:
                        ReturnServiceIsCorrupt();
                        break;
                }

            }
            else
            {
                ReturnCheckerError();
            }
        }

        public static void PrintState(string response)
        {
            if (response!=null)
            {
                Console.WriteLine(response);
            }
            else
            {
                ReturnServiceIsMumble();
            }
        }

        public static void ReturnServiceIsUp()
        {
            Environment.Exit(0);
        }

        public static void ReturnCheckerError()
        {
            Environment.Exit(1);
        }

        public static void ReturnServiceIsMumble()
        {
            Environment.Exit(2);
        }

        public static void ReturnServiceIsCorrupt()
        {
            Environment.Exit(3);
        }

        public static void ReturnServiceIsDown()
        {
            Environment.Exit(4);
        }
    }
}

