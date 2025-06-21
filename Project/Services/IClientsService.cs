using Project.Dtos;

namespace Project.Services;

public interface IClientsService
{
    Task<double> GetClientIncome(int id, string currency);
    Task<double> GetClientExpectedIncome(int id, string currency);
    Task AddClient(NewClientDto data);
    Task DeleteClient(int id);
    Task UpdateClient(int id, UpdateClientDto data);
}