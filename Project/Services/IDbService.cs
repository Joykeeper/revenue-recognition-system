using Project.Dtos;

namespace Project.Services;

public interface IDbService
{
    Task<double> GetSoftwareExpectedIncome(int id, string currency);
    Task<double> GetSoftwareIncome(int id, string currency);
    Task<double> GetClientIncome(int id, string currency);
    Task<double> GetClientExpectedIncome(int id, string currency);
    Task AddClient(NewClientDto data);
    Task DeleteClient(int id);
    Task UpdateClient(int id, UpdateClientDto data);
    Task CreateContract(NewContractDto data);
    Task PayContract(PaymentDto paymentData);

    Task<bool> IsContractFullyPaid(int contractId);
}