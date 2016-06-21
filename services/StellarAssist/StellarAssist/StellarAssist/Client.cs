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
        public const string SourceFileName = "code.cs";
        public const string ExecuteFileName = "code.exe";
        public const string DataFileName = "data.dat";

        private string _sessionKey;
        public string SessionKey
        {
            get
            {
                if (_sessionKey==null)
                {
                    var filePath = Path.Combine(ClientFolder, KeySessionFileName);
                    if (File.Exists(filePath))
                    {
                        _sessionKey = File.ReadAllText(filePath, Encoding.UTF8);
                    }
                    else
                    {
                        _sessionKey = CryptoServiceKeysRepository.GetSessionKey(ClientId);
                        var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
                        writer.Write(_sessionKey);
                        writer.Flush();
                        writer.Close();
                    }
                     
                }
                return _sessionKey;
            }
            set
            {
                _sessionKey = value;
                var writer = new StreamWriter(Path.Combine(ClientFolder, KeySessionFileName), append: false, encoding: Encoding.UTF8);
                writer.Write(_sessionKey);
                writer.Flush();
                writer.Close();
            }
        }

        public Client(string clientId)
        {
            _sessionKey = null;
            ClientId = clientId;
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            ClientFolder = ClientId != null ? Path.Combine(appDir, ClientId) : appDir;
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
            reader.Close();
            return result;
        }

        public string WriteClientCode(string data)
        {
            var filepath = Path.Combine(ClientFolder, SourceFileName);
            
            var writer = new StreamWriter(path: filepath, append: false, encoding: Encoding.UTF8);
            writer.Write(data);

            writer.Flush();
            writer.Close();

            var proc = new Process
                {
                    StartInfo =
                        {
                            FileName = "gmcs",
                            Arguments = filepath,
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
            var filepath = Path.Combine(ClientFolder, ExecuteFileName);
            var proc = new Process
            {
                StartInfo =
                {
                    FileName = "mono",
                    Arguments = filepath,
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
