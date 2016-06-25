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

        private string _cryptKey;
        public string CryptKey
        {
            get
            {
                _cryptKey=_cryptKey ?? CryptoKeys.PrivateKeyString;
                return _cryptKey;
            }
            set
            {
                _cryptKey = value ?? CryptoKeys.PrivateKeyString;
                using (var writer = new StreamWriter(Path.Combine(ClientFolder, KeySessionFileName), false, Encoding.UTF8))
                {
                    writer.WriteLine("key:{0}", _cryptKey);
                }
            }
        }

        public Client(string clientId)
        {
            ClientId = clientId.Length>11 ? clientId.Substring(1, 10) : clientId;
            
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            ClientFolder = ClientId != null ? Path.Combine(appDir, ClientId) : appDir;
            
            if (!Directory.Exists(ClientFolder))
            {
                Directory.CreateDirectory(ClientFolder);
                CryptKey = null;
            }
            else
            {
                var filePath = Path.Combine(ClientFolder, KeySessionFileName);
                using (var file = new StreamReader(filePath, Encoding.UTF8))
                {
                    var line = file.ReadLine();
                    _cryptKey = line != null ? line.Trim(' ').Replace("key:", "") : null;
                }
            }
        }

        public string WriteClientData(string data, bool crypted = true)
        {
            var filepath = Path.Combine(ClientFolder, DataFileName);
            using (var writer = new StreamWriter(path: filepath, append: false, encoding: Encoding.UTF8))
            {
                string performed = crypted ? new CryptoPerformer(CryptKey).Perform(data) : data;
                writer.Write(performed);
                writer.Flush();
            }
            return data;
        }

        public string ReadClientData(bool crypted=true)
        {
            var filepath = Path.Combine(ClientFolder, DataFileName);
            using (var reader = new StreamReader(filepath, Encoding.UTF8))
            {
                var result = reader.ReadToEnd();
                if (crypted)
                {
                    var performer = new CryptoPerformer(CryptKey);
                    result = performer.Perform(result);
                }
                return result;
            }
        }

        public string WriteClientCode(string data)
        {
            var filepath = Path.Combine(ClientFolder, SourceFileName);
            using (var writer = new StreamWriter(path: filepath, append: false, encoding: Encoding.UTF8))
            {
                writer.Write(data);
            }

            using (var proc = new Process
            {
                StartInfo =
                {
                    FileName = "mcs",
                    Arguments = filepath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            })
            {
                try
                {
                    proc.Start();
                    var result = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(1000);
                    File.Delete(filepath);
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
        }

        public string ExecuteClientCode(int maxExecutingTime=2500)
        {
            var filepath = Path.Combine(ClientFolder, ExecuteFileName);
            using (var proc = new Process
            {
                StartInfo =
                {
                    FileName = "mono",
                    Arguments = filepath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            })
            {
                try
                {
                    proc.Start();
                    var result = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(maxExecutingTime);
                    WriteClientData(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
        }
    }
}
