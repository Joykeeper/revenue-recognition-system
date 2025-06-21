using Project.Dtos;

namespace Project.Services;

public interface IContractsService
{
    Task CreateContract(NewContractDto data);
    Task PayContract(PaymentDto paymentData);
}