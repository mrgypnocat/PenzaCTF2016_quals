using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace StellarAssist
{
    public sealed class TcpCommandGetData:TcpCommandAbstract
    {
        public override string CommandTextString
        {
            get { return "GetData"; }
        }

        public override string ClientId { get; set; }

        public override void Execute(NetworkStream clientStream)
        {
            Console.WriteLine("data: {0}",TcpCommandAbstract.ReadData(clientStream));
        }
    }
}
