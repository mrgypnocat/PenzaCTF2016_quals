using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace StellarAssist
{
    public sealed class TcpCommandGetResult:TcpCommandAbstract
    {
        public override string CommandTextString 
        { 
            get { return "GetResult"; }
        }

        public override string ClientId { get; set; }

        public override void Execute(NetworkStream clientStream)
        {
            throw new NotImplementedException();
        }
    }
}
