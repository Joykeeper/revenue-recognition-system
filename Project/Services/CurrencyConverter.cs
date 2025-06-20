namespace Project.Services;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json; // You'll need to add the Newtonsoft.Json NuGet package

// Step 1: Define a class to represent the API response structure
public class ExchangeRateApiResponse
{
    public string Result { get; set; }
    [JsonProperty("base_code")]
    public string BaseCode { get; set; }
    [JsonProperty("conversion_rates")]
    public Dictionary<string, double> ConversionRates { get; set; }
    // Add other fields if needed, like 'documentation', 'terms_of_use', 'time_last_update_unix', etc.
}

// Step 2: Create a service to fetch exchange rates
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

    /// <summary>
    /// Fetches the exchange rate from the base currency to the target currency.
    /// </summary>
    /// <param name="baseCurrency">The currency to convert from (e.g., "PLN").</param>
    /// <param name="targetCurrency">The currency to convert to (e.g., "USD").</param>
    /// <returns>The exchange rate (e.g., how many targetCurrency units per 1 baseCurrency unit).</returns>
    /// <exception cref="HttpRequestException">Thrown if there's an issue with the HTTP request.</exception>
    /// <exception cref="JsonException">Thrown if there's an issue deserializing the JSON response.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the API response indicates an error or missing rate.</exception>
    public async Task<double> GetRate(string baseCurrency, string targetCurrency)
    {
        if (baseCurrency == targetCurrency)
        {
            return 1.0; // Rate is 1 if converting to the same currency
        }

        string requestUri = $"{ApiBaseUrl}{_apiKey}/latest/{baseCurrency}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode(); // Throws HttpRequestException for 4xx or 5xx responses

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
            throw; // Re-throw the exception to be handled by the caller
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

// Step 3: Your updated CurrencyConverter class
public class CurrencyConverter : ICurrencyConverter
{
    private readonly ExchangeRateService _exchangeRateService;

    // Constructor to inject the exchange rate service
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
            // Use the injected service to get the real exchange rate
            var exchangeRate = await _exchangeRateService.GetRate("PLN", targetCurrency);
            return (double)amount * exchangeRate;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting currency: {ex.Message}");
            // Depending on your application's needs, you might:
            // 1. Re-throw the exception.
            // 2. Return a default/error value (e.g., 0 or -1).
            // 3. Log the error and return 0.
            throw; // Re-throwing for now to indicate failure
        }
    }

    // Example of how to use it
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
