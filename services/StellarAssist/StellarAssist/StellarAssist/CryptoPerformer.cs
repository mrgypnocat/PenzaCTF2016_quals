using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace StellarAssist
{
    public class CryptoPerformer
    {
        private DelayLine<byte> _delayLine;
        protected byte[] Filling;

        public CryptoPerformer(string key)
            : this(Encoding.UTF8.GetBytes(key))
        {
        }

        public CryptoPerformer(byte[] key)
        {
            /*
            Filling = new int[key.Length];
            for (int i = 0; i < key.Length; i++)
            {
                Filling[i] = Convert.ToInt32(key[i]);
            }
             */
            _delayLine = new DelayLine<byte>(
                removalPointIndexes: new[] { 21, 13, 5, 2, 1, 0 },
                filling: key,
                action: ( (op1, op2) => (byte) ( op1 ^ op2 ) )
                );
        }

        public string Perform(string block)
        {
            var blockBytes = Encoding.UTF8.GetBytes(block);
            return Encoding.UTF8.GetString(Perform(blockBytes));
        }

        public byte[] Perform(byte[] block)
        {
            //TODO:
            var length = block.Length;
            var result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = (byte)(block[i] ^ _delayLine.GetCount());
            }
            return result;
        }

        public static int[] GeneratePrimesNaive(int n)
        {
            var primes = new List<int> { 2 };
            int nextPrime = 3;
            while (primes.Count < n)
            {
                var sqrt = (int) Math.Sqrt(nextPrime);
                var isPrime = true;
                for (int i = 0; primes[i] <= sqrt; i++)
                {
                    if (nextPrime % primes[i] == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }
                if (isPrime)
                {
                    primes.Add(nextPrime);
                }
                nextPrime += 2;
            }
            return primes.ToArray();
        }

        protected static int ApproximateNthPrime(int nn)
        {
            var n = (double) nn;
            double p;
            if (nn >= 7022)
            {
                p = n * Math.Log(n) + n * ( Math.Log(Math.Log(n)) - 0.9385 );
            }
            else if (nn >= 6)
            {
                p = n * Math.Log(n) + n * Math.Log(Math.Log(n));
            }
            else if (nn > 0)
            {
                p = new[] { 2, 3, 5, 7, 11 }[nn - 1];
            }
            else
            {
                p = 0;
            }
            return (int) p;
        }

        // Find all primes up to and including the limit

        protected static BitArray SieveOfEratosthenes(int limit)
        {
            var bits = new BitArray(limit + 1, true);
            bits[0] = false;
            bits[1] = false;
            for (int i = 0; i * i <= limit; i++)
            {
                if (bits[i])
                {
                    for (int j = i * i; j <= limit; j += i)
                    {
                        bits[j] = false;
                    }
                }
            }
            return bits;
        }

        public static int[] GeneratePrimesSieveOfEratosthenes(int n)
        {
            int limit = ApproximateNthPrime(n);
            BitArray bits = SieveOfEratosthenes(limit);
            var primes = new List<int>();
            for (int i = 0, found = 0; i < limit && found < n; i++)
            {
                if (bits[i])
                {
                    primes.Add(i);
                    found++;
                }
            }
            return primes.ToArray();
        }
    }
}