using Project.Data;
using Project.Services;

namespace TestProject;

public class TestDbService : DbService
{
    public TestDbService(DatabaseContext context) : base(context) { }

    public override async Task<double> ConvertFromPLN(decimal amount, string currency)
    {
        // Stub: return 2x if "USD", else identity
        return await Task.FromResult(currency == "USD" ? (double)(amount * 2) : (double)amount);
    }
}
