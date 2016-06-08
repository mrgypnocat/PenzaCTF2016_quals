using System;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace StellarAssist
{
    public abstract class TcpCommandAbstract
    {
        public virtual string ClientId { get; set; }
        public abstract string CommandTextString { get; }
        public abstract void Execute(NetworkStream clientStream);

        public static void SendData(string data, NetworkStream someNetworkStream)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            someNetworkStream.Write(dataBytes, 0, dataBytes.Length);
        }

        public static string ReadData(NetworkStream someNetworkStream, int buffSize = 24)
        {
            var message = "";
            var data = new byte[buffSize];
            do
            {
                var bytes = someNetworkStream.Read(data, 0, data.Length);
                if (bytes == 0)
                {
                    throw new Exception();
                }
                message += Encoding.UTF8.GetString(data, 0, bytes);
            } while (someNetworkStream.DataAvailable);
            return message;
        }
    }

    public sealed class ClientConnectionHandler
    {
        private readonly TcpClient _someClient;
        private readonly NetworkStream _clientStream;
        private readonly string _clientId;
        private readonly string _sessionKey;

        private static readonly TcpCommandAbstract[] TcpCommandsAbstract;
        private static readonly int MaxCommandLength;
        
        static ClientConnectionHandler()
        {
            TcpCommandsAbstract = new TcpCommandAbstract[]
                {
                    new TcpCommandGetData(), 
                    new TcpCommandGetResult(), 
                    new TcpCommandSetData(), 
                    new TcpCommandSetResult(), 
                };
            MaxCommandLength = TcpCommandsAbstract.Select(c => c.CommandTextString.Length).Concat(new[] { 0 }).Max();
        }

        public ClientConnectionHandler(TcpClient someClient)
        {
            _someClient = someClient;
            _clientStream = _someClient.GetStream();
            Console.WriteLine("Client {0} connected ", _someClient.Client.RemoteEndPoint);

            //waiting client messages
            try
            {
                while (_clientStream.CanRead)
                {
                    //at first server should recognize client
                    if (_clientId == null)
                    {
                        _clientId = TcpCommandAbstract.ReadData(_clientStream);
                        _sessionKey = CryptoServiceKeysRepository.GetSessionKey(_clientId);
                        Console.WriteLine("Client id: {0}", _clientId);
                        TcpCommandAbstract.SendData(CryptoServiceKeysRepository.PublicKeyString, _clientStream);
                    }
                    else
                    {
                        var commandTextCrypted = TcpCommandAbstract.ReadData(_clientStream);
                        var cryptoPerformer = new CryptoPerformer(_sessionKey);
                        var commandText = cryptoPerformer.Perform(commandTextCrypted);

                        Console.WriteLine("Client {0} command: {1}", _someClient.Client.RemoteEndPoint, commandText);
                        foreach (var tcpCommand in TcpCommandsAbstract.Where(tcpCommand => commandText.ToUpper().Contains(tcpCommand.CommandTextString.ToUpper())))
                        {
                            tcpCommand.ClientId = _clientId;
                            //TODO:
                            try
                            {
                                tcpCommand.Execute(_clientStream);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("{0} executed", tcpCommand.CommandTextString);
                            }
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                Console.WriteLine("Client disconnected");
                _clientStream.Close();
                someClient.Close();
            }
        }
    }
}
