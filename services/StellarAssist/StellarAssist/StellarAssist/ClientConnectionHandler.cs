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
            _someTcpClient = someTcpClient;
            _clientStream = _someTcpClient.GetStream();
            Console.WriteLine("Client {0} connected ", _someTcpClient.Client.RemoteEndPoint);

            //waiting client messages
            try
            {
                string sessionKey = null;
                while (_clientStream.CanRead)
                {
                    //at first server should recognize client
                    if (_clientId == null)
                    {
                        _clientId = ReadData();
                        sessionKey = CryptoServiceKeysRepository.GetSessionKey(_clientId);
                        Console.WriteLine("Client id: {0}", _clientId);
                        SendData(CryptoServiceKeysRepository.PublicKeyString);
                    }
                    else
                    {
                        var commandTextCrypted = ReadData();
                        
                        var cryptoPerformer = new CryptoPerformer(sessionKey);
                        var commandText = cryptoPerformer.Perform(commandTextCrypted);
                        var client = new Client(_clientId);

                        Console.WriteLine("Client {0} command: {1}", _someTcpClient.Client.RemoteEndPoint, commandText);
                        
                        if (commandText.ToUpper().Contains("GetData".ToUpper()))
                        {
                            var data = client.ReadClientData();
                            SendData(cryptoPerformer.Perform(data));
                        }
                        if (commandText.ToUpper().Contains("GetResult".ToUpper()))
                        {
                            client.ExecuteClientCode();
                            var data = client.ReadClientData();
                            SendData(cryptoPerformer.Perform(data));
                        }
                        if (commandText.ToUpper().Contains("SetData".ToUpper()))
                        {
                            var dataTextCrypted = ReadData();
                            var dataTextEncrypted = new CryptoPerformer(sessionKey).Perform(dataTextCrypted);
                            var result = client.WriteClientData(dataTextEncrypted);
                            SendData(cryptoPerformer.Perform(result));
                        }
                        if (commandText.ToUpper().Contains("SetCode".ToUpper()))
                        {
                            var dataTextCrypted = ReadData();
                            var dataTextEncrypted = new CryptoPerformer(sessionKey).Perform(dataTextCrypted);
                            var result = client.WriteClientCode(dataTextEncrypted);
                            SendData(cryptoPerformer.Perform(result));
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
                Console.WriteLine("Some shit happens");
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
                    throw new Exception();
                }
                message += Encoding.UTF8.GetString(data, 0, bytes);
            } while (_clientStream.DataAvailable);
            return new Regex("\r\n").Replace(message, "");
        }
    }
}
