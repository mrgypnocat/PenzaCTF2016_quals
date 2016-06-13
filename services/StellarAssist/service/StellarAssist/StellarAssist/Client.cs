using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace StellarAssist
{
    public sealed class Client
    {
        public readonly string ClientId;
        public string ClientFolder { get; private set; }

        public const string KeySessionFileName = "session.key";
        public const string ExecuteFileName = "data.exe";
        public const string DataFileName = "data.dat";

        private string _sessionKey;
        public string SessionKey
        {
            get
            {
                if (_sessionKey==null)
                {
                    var filePath = Path.Combine(ClientFolder, KeySessionFileName);
                    _sessionKey = File.Exists(filePath) ? File.ReadAllText(filePath, Encoding.UTF8) : CryptoServiceKeysRepository.GetSessionKey(ClientId);
                }
                return _sessionKey;
            }
            set
            {
                _sessionKey = value;
            }
        }


        public Client(string clientId)
        {
            _sessionKey = null;
            ClientId = clientId;
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            if (ClientId != null)
            {
                ClientFolder = Path.Combine(appDir, ClientId);
            }
            else
            {
                ClientFolder = appDir;
            }
            if (!Directory.Exists(ClientFolder))
            {
                Directory.CreateDirectory(ClientFolder);
            }
        }

        public string WriteClientData(string data, bool crypted = true)
        {
            var filepath = Path.Combine(ClientFolder, DataFileName);
            var writer = new StreamWriter(path: filepath, append: false, encoding: Encoding.UTF8);
            if (crypted)
            {
                var performer = new CryptoPerformer(ClientId);
                data = performer.Perform(data);
            }
            writer.Write(data);
            writer.Flush();
            writer.Close();
            return data;
        }

        public string ReadClientData(bool crypted=true)
        {
            var filepath = Path.Combine(ClientFolder, DataFileName);
            var reader = new StreamReader(filepath, Encoding.UTF8);
            var result = reader.ReadToEnd();

            if (crypted)
            {
                var performer = new CryptoPerformer(ClientId);
                result=performer.Perform(result);
            }
            return result;
        }

        public string WriteClientCode(string data)
        {
            var filepath = Path.Combine(ClientFolder, ExecuteFileName);
            var writer = new StreamWriter(path: filepath, append: false, encoding: Encoding.UTF8);
            writer.Write(data);

            writer.Flush();
            writer.Close();

            var proc = new Process
                {
                    StartInfo =
                        {
                            FileName = "/bin/bash",
                            Arguments = "-c gmcs \" " + filepath + " \"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        }
                };
            proc.Start();
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(1000);
            return result;
        }

        public string ExecuteClientCode(int maxExecutingTime=2500)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/bash",
                    Arguments = "-c mono \" " + ExecuteFileName + " \"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            proc.Start();
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(maxExecutingTime);
            WriteClientData(result);
            return result;
        }
    }
}
