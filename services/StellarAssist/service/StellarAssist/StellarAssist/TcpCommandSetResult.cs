using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace StellarAssist
{
    class TcpCommandSetResult:TcpCommandAbstract
    {
        public override string CommandTextString
        {
            get { return "SetResult"; }
        }

        public override string ClientId { get; set; }
        public override void Execute(NetworkStream clientStream)
        {
            throw new NotImplementedException();
        }
    }
}
