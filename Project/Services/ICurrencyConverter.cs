namespace Project.Services;

public interface ICurrencyConverter
{
    Task<double> ConvertFromPLN(decimal amount, string targetCurrency);
}