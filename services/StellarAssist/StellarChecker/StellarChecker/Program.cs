using System;

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
                    //"get",
                    "put",
                    "10.1.0.3",
                    "8080",
                    "flag="
                    //"3T8XUBG5IHHUV:2UZOLHOGOOENH:25LC09EQY2XM7"
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
                            Console.Error.WriteLine("unknown command {0}", command);
                            ReturnCheckerError();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    ReturnCheckerError();
                }
                
                switch (checkState)
                {
                    case CheckStates.Up:
                        PrintState(response);
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

        public static void PrintState(string response)
        {
            if (response!=null)
            {
                Console.WriteLine(response);
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

