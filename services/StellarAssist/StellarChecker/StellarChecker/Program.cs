using System;

namespace StellarChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            var client = new KeysExchanger();
            var server = new KeysExchanger(client.Prime, client.Base);

            var cSession = client.GetSessionKey(server.PublicKey);
            var sSession = server.GetSessionKey(client.PublicKey);
            return;
            */

            if ((args == null)||(args.Length==0))
            {
                args = new[]
                {
                    "chk",
                    //"192.168.1.37",
                    "127.0.0.1",
                    "8080",
                    "fucker="
                };
            }
while (true)
{
                
            
            if (args.Length>=3)
            {
                string command = args[0];
                string ipAddr = args[1];
                int port;
                string state = null;
                if (!Int32.TryParse(args[2], out port))
                {
                    Console.Error.WriteLine("can not parse port value");
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
                try
                {
                    var checker = new ServiceCheck(ipAddr, port);
                    switch (command.ToLower())
                    {
                        case "put":
                            checkState = checker.PutFlag(state, out response);
                            break;
                        case "get":
                            checkState = checker.GetFlag(state, out response);
                            break;
                        case "chk":
                            checkState = checker.Check();
                            break;
                        default:
                            ReturnCheckerError();
                            Console.Error.WriteLine("unknown command {0}", command);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    ReturnCheckerError();
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
                Console.Error.WriteLine("few arguments");
                ReturnCheckerError();
            }
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

