using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class Utils
{
    public static string FormatTime(uint seconds)
    {
        System.TimeSpan span = System.TimeSpan.FromSeconds(seconds);
        if (span.TotalSeconds == 0)
        {
            return "0 seconds";
        }

        List<string> parts = new List<string>();
        if (span.Days > 0)
        {
            parts.Add($"{span.Days} day{(span.Days > 1 ? "s" : "")}");
        }

        if (span.Hours > 0)
        {
            parts.Add($"{span.Hours} hour{(span.Hours > 1 ? "s" : "")}");
        }

        if (span.Minutes > 0)
        {
            parts.Add($"{span.Minutes} minute{(span.Minutes > 1 ? "s" : "")}");
        }

        if (span.Seconds > 0)
        {
            parts.Add($"{span.Seconds} second{(span.Seconds > 1 ? "s" : "")}");
        }

        string finishedTime = "";
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                if (i == parts.Count - 1)
                {
                    finishedTime += " and ";
                }
                else
                {
                    finishedTime += ", ";
                }
            }

            finishedTime += parts[i];
        }

        return finishedTime;
    }

    /// <summary>
    /// Sanitize XML, from https://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string SanitizeXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        StringBuilder buffer = new StringBuilder(xml.Length);

        foreach (char c in xml)
        {
            if (IsLegalXmlChar(c))
            {
                buffer.Append(c);
            }
        }

        return buffer.ToString();
    }

    /// <summary>
    /// Whether a given character is allowed by XML 1.0.
    /// </summary>
    public static bool IsLegalXmlChar(int character)
    {
        return
        (
            character == 0x9 /* == '\t' == 9   */ ||
            character == 0xA /* == '\n' == 10  */ ||
            character == 0xD /* == '\r' == 13  */ ||
            (character >= 0x20 && character <= 0xD7FF) ||
            (character >= 0xE000 && character <= 0xFFFD) ||
            (character >= 0x10000 && character <= 0x10FFFF)
        );
    }

    /// <summary>
    /// Levenshtein string distance algorithm. 
    /// Returns integer of how many edits required for the two strings to match.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="t"></param>
    public static int Difference(this string s, string t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) { return m; }
        if (m == 0) { return n; }

        int[,] d = new int[n + 1, m + 1];

        //Initialize arrays with values
        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}