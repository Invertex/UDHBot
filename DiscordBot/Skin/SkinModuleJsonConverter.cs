using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Skin
{
    public class SkinModuleJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ISkinModule));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            Type type;
            try
            {
                string t = $"DiscordBot.Skin.{jo["Type"].Value<string>()}SkinModule";
                type = Type.GetType(t);
                return jo.ToObject(type);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}