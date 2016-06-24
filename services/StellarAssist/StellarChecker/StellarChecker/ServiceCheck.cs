using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StellarChecker
{
    public enum CheckStates { Up, Down, Mumble, Corrupted }

    public class ServiceCheck
    {
        protected const int WaitingResponseSecondsTimeout = 2;
        
        protected readonly TcpClient Client;
        protected readonly NetworkStream ClientNetworkStream;

        private string CommonKey { get; set; }
        private string ClientId { get; set; }

        public bool IsConnected { get; protected set; }
        
        public ServiceCheck(string clientIpAddr, int clientPort)
        {
            Client = new TcpClient {SendTimeout = 1};
            try
            {
                var connection = Client.BeginConnect(clientIpAddr, clientPort, null, null);
                if (connection.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(WaitingResponseSecondsTimeout)))
                {
                    ClientNetworkStream = Client.GetStream();
                    IsConnected = true;
                }
            }
            catch (Exception)
            {
                IsConnected = false;
            }
        }

        ~ServiceCheck()
        {
            if (ClientNetworkStream != null)
            {
                ClientNetworkStream.Close();
            }

            if (Client != null)
            {
                Client.Close();
            }
        }        

        //some SHITcode here and below cause the logic was fit in a totally hurry. I'm sorry.
        private CheckStates SetNewSessionKey(KeysExchanger keyExchangerParams)
        {
            if (IsConnected)
            {
                SendData(String.Format("{0}:{1}:{2}", keyExchangerParams.Prime, keyExchangerParams.Base,
                                 keyExchangerParams.PublicKey));
                
                var getPublicKeyTask = Task.Factory.StartNew( () => ReadData() );
                if (getPublicKeyTask.Wait(TimeSpan.FromSeconds(WaitingResponseSecondsTimeout)))
                {
                    ClientId = getPublicKeyTask.Result;
                    CommonKey = keyExchangerParams.GetSessionKeyString(ClientId);
                    return CheckStates.Up;
                }
                return CheckStates.Mumble;
            }
            return CheckStates.Down;
        }

        public virtual CheckStates PutFlag(string flag, out string result)
        {
            result = null;
            if (!IsConnected)
            {
                return CheckStates.Down;
            }

            var exchanger = new KeysExchanger();
            var keyState = SetNewSessionKey(exchanger);
            if (keyState!=CheckStates.Up)
            {
                return keyState;
            }
            result = KeysExchanger.PackParamsSecret(exchanger);

            var commandStringPerformed = new CryptoPerformer(CommonKey).Perform("SetCode");
            SendData(commandStringPerformed);

            var codeString = CodeConstructor.GetCode(flag);
            var codePerformed = new CryptoPerformer(CommonKey).Perform(codeString);
            SendData(codePerformed);

            var getCompilationResultTask = Task.Factory.StartNew(() => ReadData());
            if (!getCompilationResultTask.Wait(TimeSpan.FromSeconds(WaitingResponseSecondsTimeout))){
                return CheckStates.Mumble;
            }

            commandStringPerformed = new CryptoPerformer(CommonKey).Perform("GetResult");
            SendData(commandStringPerformed);

            var getCodeResultTask = Task.Factory.StartNew(() => ReadData());
            if (getCodeResultTask.Wait(TimeSpan.FromSeconds(WaitingResponseSecondsTimeout)))
            {
                if (new CryptoPerformer(CommonKey).Perform(getCodeResultTask.Result).Contains(flag))
                {
                    return CheckStates.Up;
                }
            }
            return CheckStates.Mumble;
        }
        
        public virtual CheckStates GetFlag(string state, out string result)
        {
            result = null;
            if (!IsConnected)
            {
                return CheckStates.Down;
            }

            KeysExchanger exchanger;
            try
            {
                exchanger = KeysExchanger.UnpackParams(state);
            }
            catch (Exception)
            {
                throw new Exception(String.Format("can not parse state {0}", state));
            }

            var keyState = SetNewSessionKey(exchanger);
            if (keyState != CheckStates.Up)
            {
                return keyState;
            }

            var commandStringPerformed = new CryptoPerformer(CommonKey).Perform("GetData");
            SendData(commandStringPerformed);

            var getDataTask = Task.Factory.StartNew(() => ReadData());
            if (getDataTask.Wait(TimeSpan.FromSeconds(WaitingResponseSecondsTimeout)))
            {
                result = new CryptoPerformer(CommonKey).Perform(getDataTask.Result);
                return CheckStates.Up;
            }
            return CheckStates.Mumble;
        }

        public virtual CheckStates Check()
        {
            if (!IsConnected)
            {
                return CheckStates.Down;
            }
            
            var keyState = SetNewSessionKey(new KeysExchanger());
            if (keyState != CheckStates.Up)
            {
                return keyState;
            }

            var commandStringPerformed = new CryptoPerformer(CommonKey).Perform("SetData");
            SendData(commandStringPerformed);

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var rndStr = new string(Enumerable.Repeat(chars, random.Next(5, 51)).Select(s => s[random.Next(s.Length)]).ToArray());
            
            var performedRndStr = new CryptoPerformer(CommonKey).Perform(rndStr);
            SendData(performedRndStr);

            var getDataTask = Task.Factory.StartNew(() => ReadData());
            if (getDataTask.Wait(TimeSpan.FromSeconds(WaitingResponseSecondsTimeout)))
            {
                var res = new CryptoPerformer(CommonKey).Perform(getDataTask.Result);
                if (res.Contains(rndStr))
                {
                    commandStringPerformed = new CryptoPerformer(CommonKey).Perform("GetResult");
                    SendData(commandStringPerformed);
                    return CheckStates.Up;
                }
            }
            return CheckStates.Mumble;
        }

        private string ReadData(int buffSize = 24)
        {
            var message = "";
            do
            {
                var data = new byte[buffSize];
                var bytes = ClientNetworkStream.Read(data, 0, data.Length);
                if (bytes == 0)
                {
                    throw new Exception("end of receiving");
                }
                message += Encoding.UTF8.GetString(data, 0, bytes);
            } while (ClientNetworkStream.DataAvailable);
            return new Regex("\r\n").Replace(message, "");
        }

        private void SendData(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data + "\r\n");
            ClientNetworkStream.Write(dataBytes, 0, dataBytes.Length);
        }
    }
}
