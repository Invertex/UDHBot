using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static List<string> MessageSplit(this string str, int maxLength)
        {
            List<string> list = str.Split('\n').ToList();
            List<string> ret = new List<string>();

            string currentString = "";
            foreach (var s in list)
            {
                if (currentString.Length + s.Length < 1990)
                    currentString += s + "\n";
                else
                {
                    ret.Add(currentString);
                    currentString = s + "\n";
                }
            }

            return ret;
        }
    }
}