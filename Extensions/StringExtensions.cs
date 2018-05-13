using System;
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

        public static List<string> MessageSplit(this string str, int maxLength = 1990)
        {
            List<string> list = str.Split('\n').ToList();
            List<string> ret = new List<string>();

            string currentString = "";
            foreach (var s in list)
            {
                if (currentString.Length + s.Length < maxLength)
                {
                    currentString += s + "\n";
                }
                else
                {
                    ret.Add(currentString);
                    currentString = s + "\n";
                }
            }
            
            if (!String.IsNullOrEmpty(currentString))
                ret.Add(currentString);

            return ret;
        }

        /// <summary>
        /// Adds a backslash behind each special character used by Discord to make a message appear plain-text.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string EscapeDiscordMarkup(this string content)
        {
            return System.Text.RegularExpressions.Regex.Replace(content, @"([\\~\\_\`\*\`])", "\\$1");
        }
    }
}