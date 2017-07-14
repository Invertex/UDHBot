using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Discord.Net.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBot
{
    public class PublisherService
    {
        public PublisherService()
        {
        }

        /*
        DailyObject => https://www.assetstore.unity3d.com/api/en-US/sale/results/10.json
        PackageOBject => https://www.assetstore.unity3d.com/api/en-US/content/overview/[PACKAGEID].json
        PriceObject (€) => https://www.assetstore.unity3d.com/api/en-US/content/price/[PACKAGEID].json
        PackageHead => https://www.assetstore.unity3d.com/api/en-US/head/package/[PACKAGEID].json
        blogfeed (xml) => https://blogs.unity3d.com/feed/  
        */

        public async Task<DailyObject> GetDaily()
        {
            using (var httpClient = new HttpClient())
            {
                string json = await httpClient.GetStringAsync(
                    $"https://www.assetstore.unity3d.com/api/en-US/sale/results/10.json");
                return JsonConvert.DeserializeObject<DailyObject>(json);
            }
        }

        public async Task<PackageObject> GetPackage(uint packageId)
        {
            using (var httpClient = new HttpClient())
            {
                string json = await httpClient.GetStringAsync(
                    $"https://www.assetstore.unity3d.com/api/en-US/content/overview/{packageId}.json");
                return JsonConvert.DeserializeObject<PackageObject>(json);
            }
        }

        public async Task<PackageHeadObject> GetPackageHead(uint packageId)
        {
            using (var httpClient = new HttpClient())
            {
                string json = await httpClient.GetStringAsync(
                    $"https://www.assetstore.unity3d.com/api/en-US/head/package/{packageId}.json");
                return JsonConvert.DeserializeObject<PackageHeadObject>(json);
            }
        }

        public async Task<PriceObject> GetPackagePrice(uint packageId)
        {
            using (var httpClient = new HttpClient())
            {
                string json = await httpClient.GetStringAsync(
                    $"https://www.assetstore.unity3d.com/api/en-US/content/price/{packageId}.json");
                return JsonConvert.DeserializeObject<PriceObject>(json);
            }
        }

        public string PublisherAdvertisting(string userMention, string publisherName, PackageObject package, PackageHeadObject packageHead,
            PriceObject packagePrice)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("**--- Publisher everyday Advertising ---**\n\n");
            sb.Append($"Today's daily advertisting goes to {userMention} (**{publisherName}**\n");
            sb.Append($"With their package : {packageHead.result.title}, priced at {packagePrice.price}\n");
            sb.Append("For any inquiry you can contact them here on **Unity Developer Hub** by mentioning them in the chat or PM.\n\n");
            sb.Append("*Rating* ");
            for (int i = 0; i < package.content.rating.average; i++)
                sb.Append("★");
            sb.Append($"(:bust_in_silhouette:{package.content.rating.count})\n");
            sb.Append($"Unity Asset Store Link - https://www.assetstore.unity3d.com/en/#!/content/{package.content.link.id}\n");
            sb.Append($"```{package.content.description.Substring(0, 250)}[...]```\n");
            sb.Append("To be part of this kind of advertising use `!pInfo` for more informations.");
            //TODO: add image

            return sb.ToString();
        }
    }
}