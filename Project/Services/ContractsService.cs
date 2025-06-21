using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;

namespace Project.Services;

public class ContractsService : IContractsService
{
    private readonly DatabaseContext _context;
    public ContractsService(DatabaseContext context)
    {
        _context = context;
    }
    
    public async Task CreateContract(NewContractDto data)
    {
        // Validate input dates
        var duration = (data.EndDate - data.StartDate).Days;
        if (duration < 3 || duration > 30)
            throw new BadRequestException("Contract duration must be between 3 and 30 days.");

        var software = await _context.Softwares.FindAsync(data.SoftwareId);
        if (software == null)
            throw new NotFoundException("Software not found.");

        // Calculate base price
        decimal price = software.PriceForYear;

        // Add update support cost
        if (data.YearsOfUpdates < 1 || data.YearsOfUpdates > 3)
            throw new BadRequestException("Support years must be 1, 2, or 3.");
        price += 1000 * (data.YearsOfUpdates - 1);

        // Apply discount if present
        if (data.DiscountId != null)
        {
            var discount = await _context.Discounts.FindAsync(data.DiscountId);
            if (discount != null && data.StartDate >= discount.StartTime && data.StartDate <= discount.EndTime)
            {
                price -= price * discount.Percentage / 100m;
            }
        }

        // Loyal client discount (additional 5%)
        if (data.IsReturningClient)
            price *= 0.95m;

        var contract = new Contract
        {
            BuyingClientId = data.BuyingClientId,
            SellingClientId = data.SellingClientId,
            SoftwareId = data.SoftwareId,
            StartTime = data.StartDate,
            EndTime = data.EndDate,
            YearsOfUpdates = data.YearsOfUpdates,
            SoftwareVersion = data.SoftwareVersion,
            DiscountId = data.DiscountId,
            Price = price,
            Signed = false
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
    }


    public async Task PayContract(PaymentDto payment)
    {
        //TODO: not allow to overpay the contract
        var contract = await _context.Contracts.FindAsync(payment.ContractId);
        if (contract == null)
            throw new NotFoundException("Contract not found.");

        var now = payment.Date;
        var endTime = contract.EndTime;

        if (now > endTime)
            throw new BadRequestException("Cannot pay after contract end date.");

        // Check if already signed (fully paid)
        var isPaid = await IsContractFullyPaid(contract.Id);
        if (isPaid)
            throw new BadRequestException("Contract is already fully paid.");

        var entity = new Payment
        {
            Amount = payment.Amount,
            ClientId = payment.ClientId,
            ContractId = payment.ContractId,
            Date = payment.Date,
            Returned = false
        };

        _context.Payments.Add(entity);
        await _context.SaveChangesAsync();

        // Check after payment if fully paid
        var totalPaid = await _context.Payments
            .Where(p => p.ContractId == contract.Id && !p.Returned)
            .SumAsync(p => p.Amount);

        if (totalPaid >= contract.Price)
        {
            contract.Signed = true;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<bool> IsContractFullyPaid(int contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null) throw new NotFoundException("Contract not found");

        var totalPaid = await _context.Payments
            .Where(p => p.ContractId == contractId && !p.Returned)
            .SumAsync(p => p.Amount);

        return totalPaid >= contract.Price;
    }

}