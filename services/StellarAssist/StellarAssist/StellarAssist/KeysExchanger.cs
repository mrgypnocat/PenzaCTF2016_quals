using System;
using ForeignLibs;

namespace StellarAssist
{
    public class KeysExchanger
    {
        public const int Radix = 36;
        protected const int Confidence = 30;
        protected const int DefaultKeySize = 64;
        
        protected BigInteger _p;
        protected BigInteger _g;
        protected BigInteger _secret;
        protected BigInteger _public;

        public string Prime
        {
            get { return _p.ToString(Radix); }
            protected set { _p = new BigInteger(value, Radix); }
        }

        public string Base
        {
            get { return _g.ToString(Radix); }
            protected set { _g = new BigInteger(value, Radix); }
        }

        public string SecretKey
        {
            get { return _secret.ToString(Radix); }
            protected set { _secret = new BigInteger(value, Radix); }
        }

        public string PublicKey
        {
            get { return _public.ToString(Radix); }
            protected set { _public = new BigInteger(value, Radix); }
        }

        protected BigInteger CalculatePublicKey()
        {
            return _g.ModPow(_secret, _p);
        }

        public KeysExchanger(BigInteger p, BigInteger g, BigInteger secret)
        {
            _p = p;
            _g = g;
            _secret = secret;
            _public = _g.ModPow(_secret, _p);
        }

        public KeysExchanger(BigInteger p, BigInteger g)
            : this(p, g, BigInteger.GenPseudoPrime(DefaultKeySize, Confidence, new StrongNumberProvider()))
        {
        }

        public KeysExchanger(BigInteger secret)
        {
            _p = BigInteger.GenPseudoPrime(DefaultKeySize, Confidence, new StrongNumberProvider());
            _g = BigInteger.GenPseudoPrime(DefaultKeySize, Confidence, new StrongNumberProvider());
            _secret = secret;
            _public = _g.ModPow(_secret, _p);
        }

        public KeysExchanger()
            : this(BigInteger.GenPseudoPrime(DefaultKeySize, Confidence, new StrongNumberProvider()))
        {
        }

        public KeysExchanger(string p, string g, string secret) : this(
            new BigInteger(p, Radix), new BigInteger(g, Radix), new BigInteger(secret, Radix)
            )
        {
        }

        public KeysExchanger(string p, string g)
            : this(new BigInteger(p, Radix), new BigInteger(g, Radix))
        {
        }

        public KeysExchanger(string secret) : this(
            new BigInteger(secret, Radix))
        {
        }

        public BigInteger GetSessionKey(BigInteger clientKey)
        {
            return clientKey.ModPow(_secret, _p);
        }

        public BigInteger GetSessionKey(string clientKey)
        {
            return new BigInteger(clientKey, Radix).ModPow(_secret, _p);
        }

        public string GetSessionKeyString(string clientKey)
        {
            return GetSessionKey(clientKey).ToString(Radix);
        }

        public static string PackParamsSecret(KeysExchanger keyExchangerParams)
        {
            return String.Format("{0}:{1}:{2}", keyExchangerParams.Prime, keyExchangerParams.Base,
                                 keyExchangerParams.SecretKey);
        }

        public static KeysExchanger UnpackParams(string keyExchangerParams)
        {
            var strs = keyExchangerParams.Split(':');
            var result = new KeysExchanger(strs[0], strs[1], strs[2]);
            return result;
        }
    }
}