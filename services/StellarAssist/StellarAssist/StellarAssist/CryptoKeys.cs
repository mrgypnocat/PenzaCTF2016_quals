using System;
using System.IO;

namespace StellarAssist
{
    public static class CryptoKeys
    {
        public const string KeyFileName = "key.key";
        public static readonly string FilePath;

        private static string _privateKeyString;
        public static string PrivateKeyString
        {
            get
            {
                if (_privateKeyString==null)
                {
                    if (File.Exists(FilePath))
                    {
                        using (var file = new StreamReader(FilePath, System.Text.Encoding.UTF8))
                        {
                            var readLine = file.ReadLine();
                            _privateKeyString = readLine != null ? readLine.Trim(' ').Replace("private:", "") : "someKey";
                        }
                    }
                    else
                    {
                        _privateKeyString = "someKey";
                    }
                }
                return _privateKeyString;
            }
            set
            {
                _privateKeyString = value;
                using (var writer = new StreamWriter(FilePath,false,System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("private:{0}",value);
                }
            }
        }
        static CryptoKeys()
        {
            FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);
        }
    }
}
