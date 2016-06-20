using System.Text;

namespace StellarChecker
{
    public static class CryptoServiceKeysRepository
    {
        public static string PublicKeyString { get; set; }
        public static string PrivateKeyString { get; set; }

        static CryptoServiceKeysRepository()
        {
            PublicKeyString = "someTeamPublicKey";
            PrivateKeyString = "someTeamPrivateKey";
        }

        public static byte[] GetSessionKey(byte[] clientPublicKeyString)
        {
            //TODO:
            return clientPublicKeyString;
        }

        public static string GetSessionKey(string clientPublicKeyString)
        {
            //TODO:
            return clientPublicKeyString;
        }
    }

    public class CryptoPerformer
    {
        public readonly byte[] Key;
        protected ShuffleRegister<byte> Register;

        public CryptoPerformer(string key, int blockSize = 64)
        {
            Key = Encoding.UTF8.GetBytes(key);
            Register = new ShuffleRegister<byte>(blockSize);
            Register.SetFilling(Key);
        }

        public CryptoPerformer(byte[] key, int blockSize = 64)
        {
            Key = key;
            Register = new ShuffleRegister<byte>(blockSize);
            Register.SetFilling(Key);
        }

        public string Perform(string block)
        {
            var blockBytes = Encoding.UTF8.GetBytes(block);
            return Encoding.UTF8.GetString(Perform(blockBytes));
        }

        public byte[] Perform(byte[] block)
        {
            //TODO:
            return block;
        }
    }
}
