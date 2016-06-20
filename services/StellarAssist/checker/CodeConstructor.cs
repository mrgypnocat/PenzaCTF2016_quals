using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarChecker
{
    public class CodeConstructor
    {
        public string GetCode(string flag)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();


            var codePatternNamespace = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
            string codePatternMain = null;

            var codePatternFull = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;
            namespace"+codePatternNamespace+
            "{" +
                "class Program { " +
                    "static void Main(string[] args){"
                        +codePatternMain+
                    "}" +
                "}" +
            "}";
          
            return codePatternFull;
        }
    }
}
