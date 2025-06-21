namespace Project.Services;

public interface ISoftwaresService
{
    Task<double> GetSoftwareIncome(int id, string currency);
    Task<double> GetSoftwareExpectedIncome(int id, string currency);
}