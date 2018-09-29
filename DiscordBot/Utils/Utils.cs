using System.Collections;
using System.Collections.Generic;

public static class Utils
{
    public static string FormatTime(uint seconds)
    {
        System.TimeSpan span = System.TimeSpan.FromSeconds(seconds);
        if (span.TotalSeconds == 0) { return "0 seconds"; }

        List<string> parts = new List<string>();
        if (span.Days > 0) { parts.Add($"{span.Days} day{(span.Days > 1 ? "s" : "")}"); }
        if (span.Hours > 0) { parts.Add($"{span.Hours} hour{(span.Hours > 1 ? "s" : "")}"); }
        if (span.Minutes > 0) { parts.Add($"{span.Minutes} minute{(span.Minutes > 1 ? "s" : "")}"); }
        if (span.Seconds > 0) { parts.Add($"{span.Seconds} second{(span.Seconds > 1 ? "s" : "")}"); }

        string finishedTime = "";
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                if (i == parts.Count - 1) { finishedTime += " and "; }
                else { finishedTime += ", "; }
            }
            finishedTime += parts[i];
        }

        return finishedTime;
    }
}