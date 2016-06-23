using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace StellarAssist
{
    public sealed class ClientConnectionHandler
    {
        private readonly TcpClient _someTcpClient;
        private readonly NetworkStream _clientStream;
        private readonly string _clientId;

        public ClientConnectionHandler(TcpClient someTcpClient)
        {
            //waiting client messages
            try
            {
                string sessionKey = null;

                _someTcpClient = someTcpClient;
                _clientStream = _someTcpClient.GetStream();
                Console.WriteLine("Client {0} connected ", _someTcpClient.Client.RemoteEndPoint);

                while (_clientStream.CanRead)
                {
                    //at first server should recognize client and set a session key
                    if (_clientId == null)
                    {
                        var clientInfo = ReadData().Split(':');
                        if (clientInfo.Length>=3)
                        {
                            var clientPrime = clientInfo[0];
                            var clientBase = clientInfo[1];
                            var clientPublicKey = clientInfo[2];

                            var exchanger = new KeysExchanger(clientPrime, clientBase, CryptoKeys.PrivateKeyString);
                            SendData(exchanger.PublicKey);
                            sessionKey = exchanger.GetSessionKeyString(clientPublicKey);

                            _clientId = new Regex(@"W").Replace(clientPublicKey, "");
                            Console.WriteLine("Client id: {0}", _clientId);
                            Console.WriteLine("SessionKey: {0}", sessionKey);
                        }
                    }
                    
                    //next step is in interaction
                    else
                    {
                        var client = new Client(_clientId);
                        
                        var commandTextCrypted = ReadData();
                        var commandText = new CryptoPerformer(sessionKey).Perform(commandTextCrypted);

                        Console.WriteLine("Client {0} command: {1}", _someTcpClient.Client.RemoteEndPoint, commandText);
                        
                        if (commandText.ToUpper().Contains("GetData".ToUpper()))
                        {
                            var data = client.ReadClientData();
                            SendData(new CryptoPerformer(sessionKey).Perform(data));
                        }
                        if (commandText.ToUpper().Contains("GetResult".ToUpper()))
                        {
                            client.ExecuteClientCode();
                            var data = client.ReadClientData();
                            SendData(new CryptoPerformer(sessionKey).Perform(data));
                        }
                        if (commandText.ToUpper().Contains("SetData".ToUpper()))
                        {
                            var dataTextEncrypted = new CryptoPerformer(sessionKey).Perform(ReadData());
                            var result = client.WriteClientData(dataTextEncrypted);
                            SendData(new CryptoPerformer(sessionKey).Perform(result));
                        }
                        if (commandText.ToUpper().Contains("SetCode".ToUpper()))
                        {
                            var dataTextEncrypted = new CryptoPerformer(sessionKey).Perform(ReadData());
                            var result = client.WriteClientCode(dataTextEncrypted);
                            SendData(new CryptoPerformer(sessionKey).Perform(result));
                        }
                        if (commandText.ToUpper().Contains("Exit".ToUpper()))
                        {
                            Finish();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Finish();
            }
        }
        private void Finish()
        {
            Console.WriteLine("Client disconnected");
            _clientStream.Close();
            _someTcpClient.Close();
        }

        public void SendData(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data+"\r\n");
            _clientStream.Write(dataBytes, 0, dataBytes.Length);
        }

        public string ReadData(int buffSize = 24)
        {
            var message = "";
            do
            {
                var data = new byte[buffSize];
                var bytes = _clientStream.Read(data, 0, data.Length);
                if (bytes == 0)
                {
                    throw new Exception("end of transmission");
                }
                message += Encoding.UTF8.GetString(data, 0, bytes);
            } while (_clientStream.DataAvailable);
            return new Regex("\r\n").Replace(message, "");
        }
    }
}
