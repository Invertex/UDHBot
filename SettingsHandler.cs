using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace DiscordBot
{
    public enum JsonFile
    {
        Settings,
        Commands,
        Roles,
        Achievements,
        PayWork,
        UserSettings
    }

    class SettingsHandler
    {
        public static Dictionary<string, string[]> SettingLine = new Dictionary<string, string[]>();

        private static string[] LoadValue(string path, JsonFile file)
        {
            string[] split = path.Split('/');
            int index = 0;
            List<string> keys = new List<string>();

            JObject data = JsonConvert.DeserializeObject<JObject>(GetJsonString(file.ToString()));

            while (index < split.Length)
            {
                string element = split[index];

                object value = data[element].Value<object>();

                if (value.GetType() == typeof(JValue))
                {
                    keys.Add(value.ToString());
                    break;
                }
                else if (value.GetType() == typeof(JObject))
                {
                    JObject v = value as JObject;
                    data = v;
                    index++;
                }
                else if (value is JArray)
                {
                    foreach (JValue val in (value as JArray))
                    {
                        keys.Add(val.ToString());
                    }

                    break;
                }
            }

            return keys.ToArray();
        }

        public static string[] LoadValueStringArray(string path, JsonFile file)
        {
            return LoadValue(path, file);
        }

        public static char LoadValueChar(string path, JsonFile file)
        {
            return char.Parse(LoadValue(path, file)[0]);
        }

        public static string LoadValueString(string path, JsonFile file)
        {
            return LoadValue(path, file)[0];
        }

        public static int LoadValueInt(string path, JsonFile file)
        {
            return int.Parse(LoadValue(path, file)[0]);
        }

        public static ulong LoadValueUlong(string path, JsonFile file)
        {
            return ulong.Parse(LoadValue(path, file)[0]);
        }

        public static float LoadValueFloat(string path, JsonFile file)
        {
            return float.Parse(LoadValue(path, file)[0]);
        }

        public static bool LoadValueBool(string path, JsonFile file)
        {
            return LoadValue(path, file)[0] != "0";
        }

       /* public static string[] StringToArray(string value)
        {
            List<string> values = new List<string>();
            string[] split = value.Split(new string[] {"\","}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split.Length; i++)
            {
                string newS = split[i];

                newS = newS.Replace("\"", "").Replace(",", "");
                //newS = Regex.Replace(newS, @"\s", "");
                newS = Regex.Replace(newS, @"\r", "");
                newS = Regex.Replace(newS, @"\n", "");
                newS = Regex.Replace(newS, @"\t", "");
                newS.Trim();
                if (string.IsNullOrWhiteSpace(newS)) continue;
                values.Add(i == 0 ? newS.Substring(1, newS.Length - 1).Trim() : i == split.Length - 1 ? newS.Substring(0, newS.Length - 1).Trim() : newS.Trim());
            }

            //Console.WriteLine("Length of: " + values.Count);
            return values.ToArray();
        }*/

        private static void AddKeyIfNotPresentToSettings(string key, string[] value)
        {
            if (SettingLine.ContainsKey(key)) return;

            SettingLine.Add(key, value);
        }

        public static string GetJsonString(string file)
        {
            return File.ReadAllText($"./Settings/{file}.json");
        }
    }
}