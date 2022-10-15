using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Services;

public class CurrencyService
{
    #region Configuration

    private const int ApiVersion = 1;
    private const string ValidCurrenciesEndpoint = "currencies.min.json";
    private const string ExchangeRatesEndpoint = "currencies";
    
    private class Currency
    {
        public string Name { get; set; }
        public string Short { get; set; }
    }

    #endregion // Configuration
    
    private readonly Dictionary<string, Currency> _currencies = new Dictionary<string, Currency>();

    private static readonly string ApiUrl = $"https://cdn.jsdelivr.net/gh/fawazahmed0/currency-api@{ApiVersion}/latest/";

    public async Task<float> GetConversion(string toCurrency, string fromCurrency = "usd")
    {
        toCurrency = toCurrency.ToLower();
        fromCurrency = fromCurrency.ToLower();
        
        var url = $"{ApiUrl}{ExchangeRatesEndpoint}/{fromCurrency.ToLower()}/{toCurrency.ToLower()}.min.json";
        var response = await GetResponse(url);
        if (string.IsNullOrEmpty(response))
            return -1;
        
        var json = JObject.Parse(response);
        return json[$"{toCurrency}"].Value<float>();
    }
    
    #region Public Methods

    public async Task<string> GetCurrencyName(string currency)
    {
        currency = currency.ToLower();
        if (!await IsCurrency(currency))
            return string.Empty;
        return _currencies[currency].Name;
    }

    // Checks if a provided currency is valid, it also checks is we have a list of currencies to check against and rebuilds it if not. (If the API was down when bot started)
    public async Task<bool> IsCurrency(string currency)
    {
        if (_currencies.Count  <= 1)
            await BuildCurrencyList();
        return _currencies.ContainsKey(currency);
    }

    #endregion // Public Methods

    #region Private Methods

    private async Task BuildCurrencyList()
    {
        var url = ApiUrl + ValidCurrenciesEndpoint;
        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        var currencies = JObject.Parse(json);
        
        // Json is weird format of `Code: Name` each in dependant ie; {"1inch":"1inch Network","aave":"Aave"}
        foreach (var currency in currencies)
        {
            _currencies.Add(currency.Key, new Currency
            {
                Name = currency.Value!.ToString(),
                Short = currency.Key
            });
        }
    }

    private async Task<string> GetResponse(string url)
    {
        string jsonString = string.Empty;

        using var client = new HttpClient();
        
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            jsonString = await response.Content.ReadAsStringAsync();
        }

        return jsonString;
    }

    #endregion // Private Methods

}