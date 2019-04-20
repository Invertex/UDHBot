using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Services
{
    public class CurrencyService
    {
        private Dictionary<string, Rate> _rates;

        public CurrencyService()
        {
            _rates = new Dictionary<string, Rate>();
        }

        public async Task<double> GetRate(string currency)
        {
            if (!HasValidRate(currency))
            {
                var rate = await GetNewRate(currency);
                if (rate == null)
                {
                    return -1;
                }
                _rates[currency] = rate;
            }
            return _rates[currency].value;
        }

        private static async Task<Rate> GetNewRate(string currency)
        {
            // Url to exchange rate from USD
            string apiKey = "5d733a03e0274dff3e7f";
            string rateUrl = $"https://free.currencyconverterapi.com/api/v6/convert?q=USD_{currency}&compact=ultra&apiKey={apiKey}";

            string rateJson;
            using (var wc = new WebClient())
            {
                // Download json string from url
                rateJson = await wc.DownloadStringTaskAsync(rateUrl);
            }

            if (string.IsNullOrEmpty(rateJson) || rateJson.Equals("{}"))
            {
                return null;
            }

            Rate rate = new Rate
            {
                value = (double)JObject.Parse(rateJson)[$"USD_{currency}"],
                lastUpdated = DateTime.Today
            };

            return rate;
        }


        private bool HasValidRate(string currency)
        {
            return _rates.ContainsKey(currency) && !IsExpired(_rates[currency]);
        }

        private bool IsExpired(Rate rate)
        {
            return rate.lastUpdated.Date != DateTime.Today;
        }
    }

    public class Rate
    {
        public double value = -1;
        public DateTime lastUpdated;

        public Rate()
        {
            value = -1;
            lastUpdated = DateTime.MinValue;
        }
    }
}
