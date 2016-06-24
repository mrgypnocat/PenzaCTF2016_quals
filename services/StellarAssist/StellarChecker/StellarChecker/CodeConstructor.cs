using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace StellarChecker
{
    public static class CodeConstructor
    {
        public static string GetCode(string flag)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            var codePatternNamespace =
                new Regex(@"\d").Replace(
                    new string(Enumerable.Repeat(chars, 20).Select(s => s[random.Next(s.Length)]).ToArray()), "");

            int rndInt=random.Next(1,20);
            //1
            string pattern1 = "Console.WriteLine(new string(\"" + new string(flag.Reverse().ToArray()) + "\".Reverse().ToArray()));";
            //2
            string pattern2 = "Console.WriteLine(\"" + flag.Aggregate<char, string>(null, (current, t) => current + (char) ( t ^ (rndInt % 10 + 1) )) +
                "\".Aggregate<char, string>(null, (current, t) => current + (char) (t ^ " + ( rndInt % 10 + 1 ) + ")));";
            //3
            string pattern3 = "Console.WriteLine(\"" + flag.Aggregate<char, string>(null, (current, t) => current + (char) ( t + rndInt )) +
                "\".Aggregate<char, string>(null, (current, t) => current + (char) (t - " + rndInt + ")));";
            
            /*
            //4
            var rndStr = new string(Enumerable.Repeat(chars, flag.Length).Select(s => s[random.Next(s.Length)]).ToArray());
            string patternStr = null;
            for (var i = 0; i < flag.Length; i++)
            {
                patternStr += (char) ( flag[i] + rndStr[i] );
            }

            string pattern4 = "string someStr = null;" +
                              "for (var i = 0; i < \"" + patternStr + "\".Length; i++)" +
                              "{" +
                              "someStr += (char) ( \"" + patternStr + "\"[i] - \""+rndStr+"\"[i] );" +
                              "}" +
                              "Console.WriteLine(someStr);";
            */
            var patterns = new[] {pattern1, pattern2, pattern3};

            var codePatternFull = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;            
            namespace "+codePatternNamespace+
            "{" +
                "class Program { " +
                    "static void Main(string[] args){"
                        + patterns[random.Next(0, patterns.Length)] +
                    "}" +
                "}" +
            "}";

            return codePatternFull;
        }
    }
}
