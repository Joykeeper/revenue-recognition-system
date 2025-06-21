namespace Project.Services;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


public class ExchangeRateApiResponse
{
    public string Result { get; set; }
    [JsonProperty("base_code")]
    public string BaseCode { get; set; }
    [JsonProperty("conversion_rates")]
    public Dictionary<string, double> ConversionRates { get; set; }
}

public class ExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiBaseUrl = "https://v6.exchangerate-api.com/v6/";

    public ExchangeRateService(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }
    
    public async Task<double> GetRate(string baseCurrency, string targetCurrency)
    {
        if (baseCurrency == targetCurrency)
        {
            return 1.0;
        }

        string requestUri = $"{ApiBaseUrl}{_apiKey}/latest/{baseCurrency}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            ExchangeRateApiResponse apiResponse = JsonConvert.DeserializeObject<ExchangeRateApiResponse>(jsonResponse);

            if (apiResponse == null || apiResponse.Result != "success")
            {
                throw new InvalidOperationException($"ExchangeRate-API error: {apiResponse?.Result}");
            }

            if (apiResponse.ConversionRates != null && apiResponse.ConversionRates.TryGetValue(targetCurrency, out double rate))
            {
                return rate;
            }
            else
            {
                throw new InvalidOperationException($"Could not find exchange rate for {targetCurrency} from {baseCurrency}.");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request Error: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Deserialization Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            throw;
        }
    }
}

public class CurrencyConverter : ICurrencyConverter
{
    private readonly ExchangeRateService _exchangeRateService;
    
    public CurrencyConverter()
    {
        string apiKey = "471bc65f5dab827e822266eb";

        _exchangeRateService = new ExchangeRateService(apiKey);
    }

    public async Task<double> ConvertFromPLN(decimal amount, string targetCurrency)
    {
        if (targetCurrency == "PLN")
        {
            return (double)amount;
        }

        try
        {
            var exchangeRate = await _exchangeRateService.GetRate("PLN", targetCurrency);
            return (double)amount * exchangeRate;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting currency: {ex.Message}");
            throw;
        }
    }
    
    // public static async Task Main(string[] args)
    // {
    //     var converter = new CurrencyConverter();
    //
    //     try
    //     {
    //         decimal plnAmount = 100m;
    //         string targetUsd = "USD";
    //         double convertedUsd = await converter.ConvertFromPLN(plnAmount, targetUsd);
    //         Console.WriteLine($"{plnAmount} PLN is {convertedUsd:F2} {targetUsd}"); // Output: 100 PLN is 25.00 USD (example rate)
    //
    //         string targetEur = "EUR";
    //         double convertedEur = await converter.ConvertFromPLN(plnAmount, targetEur);
    //         Console.WriteLine($"{plnAmount} PLN is {convertedEur:F2} {targetEur}");
    //
    //         string targetPln = "PLN";
    //         double convertedPln = await converter.ConvertFromPLN(plnAmount, targetPln);
    //         Console.WriteLine($"{plnAmount} PLN is {convertedPln:F2} {targetPln}");
    //
    //         // Example of an invalid currency (this should throw an error)
    //         string invalidCurrency = "XYZ";
    //         Console.WriteLine($"Attempting to convert to {invalidCurrency}...");
    //         await converter.ConvertFromPLN(plnAmount, invalidCurrency);
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"An error occurred in Main: {ex.Message}");
    //     }
    //
    //     Console.ReadLine(); // Keep console open
    // }
}
