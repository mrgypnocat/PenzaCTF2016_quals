using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StellarChecker
{
    public enum CheckStates { Up, Down, Mumble, Corrupted }

    public class ServiceCheck
    {
        private const int WaitingResponseSeconds = 2;
        private readonly TcpClient _client;
        private readonly NetworkStream _networkStream;
        private readonly string _sessionKey;
        private readonly string ClientId;
        public bool IsConnected { get; protected set; }

        //waiting for WaitingResponseSeconds
        public ServiceCheck(string clientIpAddr, int clientPort)
        {
            try
            {
                _client = new TcpClient(clientIpAddr, clientPort);
                _networkStream = _client.GetStream();
                IsConnected = true;
            }
            catch (Exception)
            {
                IsConnected = false;
            }

            if (IsConnected)
            {
                SendData(CryptoServiceKeysRepository.PublicKeyString);
                var task = Task.Run(() => ReadData());

                if (task.Wait(TimeSpan.FromSeconds(WaitingResponseSeconds)))
                {
                    ClientId = task.Result;
                    _sessionKey = CryptoServiceKeysRepository.GetSessionKey(ClientId);
                }
            }
        }

        ~ServiceCheck()
        {
            try
            {
                if (_networkStream != null)
                {
                    _networkStream.Close();
                }

                if (_client != null)
                {
                    _client.Close();
                }
            }
            catch
            {
            }
        }


        //totally waiting for (2 x WaitingResponseSeconds) + constructor
        public virtual CheckStates PutFlag(string flag, out string result)
        {
            result = null;

            if (!IsConnected)
            {
                return CheckStates.Down;
            }

            var commandStringPerformed = new CryptoPerformer(_sessionKey).Perform("SetCode");
            SendData(commandStringPerformed);

            var codeString = new CodeConstructor().GetCode(flag);
            var codePerformed = new CryptoPerformer(_sessionKey).Perform(codeString);
            SendData(codePerformed);

            var getCompilationResultTask = Task.Run(() => ReadData());
            if (!getCompilationResultTask.Wait(TimeSpan.FromSeconds(WaitingResponseSeconds))){
                return CheckStates.Mumble;
            }

            commandStringPerformed = new CryptoPerformer(_sessionKey).Perform("GetResult");
            SendData(commandStringPerformed);

            var getCodeResultTask = Task.Run(() => ReadData());
            if (getCodeResultTask.Wait(TimeSpan.FromSeconds(WaitingResponseSeconds)))
            {
                if (new CryptoPerformer(_sessionKey).Perform(getCodeResultTask.Result).Contains(new CryptoPerformer(_sessionKey).Perform(flag)))
                {
                    result = ClientId;
                    return CheckStates.Up;
                }
            }
            return CheckStates.Mumble;
        }

        //totally waiting for WaitingResponseSeconds + constructor
        public virtual CheckStates GetFlag(out string result)
        {
            result = null;
            if (!IsConnected)
            {
                return CheckStates.Down;
            }
            var commandStringPerformed = new CryptoPerformer(_sessionKey).Perform("GetData");
            SendData(commandStringPerformed);

            var getDataTask = Task.Run(() => ReadData());
            if (getDataTask.Wait(TimeSpan.FromSeconds(WaitingResponseSeconds)))
            {
                result = new CryptoPerformer(_sessionKey).Perform(getDataTask.Result);
                return CheckStates.Up;
            }
            return CheckStates.Mumble;
        }

        //totally waiting for (2 x WaitingResponseSeconds) + constructor
        public virtual CheckStates Check()
        {
            if (!IsConnected)
            {
                return CheckStates.Down;
            }
            //TODO
            /*
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var rndStr = new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
            */
            const string rndStr = "I am Fucking Checker!";

            var commandStringPerformed = new CryptoPerformer(_sessionKey).Perform("SetData");
            SendData(commandStringPerformed);

            var performedRndStr = new CryptoPerformer(_sessionKey).Perform(rndStr);
            SendData(performedRndStr);


            var getDataTask = Task.Run(() => ReadData());
            if (getDataTask.Wait(TimeSpan.FromSeconds(WaitingResponseSeconds)))
            {
                if (getDataTask.Result==performedRndStr)
                {
                    commandStringPerformed = new CryptoPerformer(_sessionKey).Perform("GetResult");
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
                var bytes = _networkStream.Read(data, 0, data.Length);
                if (bytes == 0)
                {
                    throw new Exception();
                }
                message += Encoding.UTF8.GetString(data, 0, bytes);
            } while (_networkStream.DataAvailable);
            return message;
        }

        private void SendData(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data + "\r\n");
            _networkStream.Write(dataBytes, 0, dataBytes.Length);
        }
    }
}
