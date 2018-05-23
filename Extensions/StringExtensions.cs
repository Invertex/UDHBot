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

        public static int CalculateLevenshteinDistance(this string source1, string source2) //O(n*m)
        {
            var source1Length = source1.Length;
            var source2Length = source2.Length;

            var matrix = new int[source1Length + 1, source2Length + 1];

            // First calculation, if one entry is empty return full length
            if (source1Length == 0)
                return source2Length;

            if (source2Length == 0)
                return source1Length;

            // Initialization of matrix with row size source1Length and columns size source2Length
            for (var i = 0; i <= source1Length; matrix[i, 0] = i++)
            {
            }

            for (var j = 0; j <= source2Length; matrix[0, j] = j++)
            {
            }

            // Calculate rows and collumns distances
            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            // return result
            return matrix[source1Length, source2Length];
        }
    }
}