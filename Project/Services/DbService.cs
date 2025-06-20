using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;

namespace Project.Services;

public class DbService : IDbService
{
    private readonly DatabaseContext _context;
    public DbService(DatabaseContext context)
    {
        _context = context;
    }
    
    public async Task<double> GetSoftwareExpectedIncome(int id, string currency)
    {
        var expected = await _context.Contracts
            .Where(c => c.SoftwareId == id)
            .SumAsync(c => (decimal)c.Price);

        return await ConvertFromPLN(expected, currency);
    }

    public async Task<double> GetSoftwareIncome(int id, string currency)
    {
        var total = await _context.Payments
            .Where(p => p.Returned == false && p.Contract.SoftwareId == id && p.Contract.Signed)
            .SumAsync(p => p.Amount);

        return await ConvertFromPLN(total, currency);
    }

    public async Task<double> GetClientIncome(int id, string currency)
    {
        var total = await _context.Payments
            .Where(p => p.ClientId == id && p.Returned == false)
            .SumAsync(p => p.Amount);

        return await ConvertFromPLN(total, currency);
    }

    public async Task<double> GetClientExpectedIncome(int id, string currency)
    {
        var contracts = await _context.Contracts
            .Where(c => c.BuyingClientId == id)
            .ToListAsync();

        decimal expected = contracts.Sum(c => c.Price);

        return await ConvertFromPLN(expected, currency);
    }

    public async Task AddClient(NewClientDto data)
    {
        var client = new Client
        {
            Address = data.Address,
            Email = data.Email,
            Phone = data.Phone
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        if (data.IsCompany)
        {
            var company = new Company
            {
                Id = client.Id,
                Name = data.CompanyName!,
                KRS = data.KRS!.Value
            };
            _context.Companies.Add(company);
        }
        else
        {
            var individual = new PhysicalIndividual
            {
                Id = client.Id,
                Name = data.Name!,
                Surname = data.Surname!,
                PESEL = data.PESEL!.Value
            };
            _context.PhysicalIndividuals.Add(individual);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteClient(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company != null)
            throw new BadRequestException("Cannot delete companies.");

        var individual = await _context.PhysicalIndividuals.FindAsync(id);
        if (individual == null)
            throw new NotFoundException("Client not found.");

        individual.Name = "DELETED";
        individual.Surname = "DELETED";
        individual.PESEL = 0;

        var client = await _context.Clients.FindAsync(id);
        client.Address = "DELETED";
        client.Email = "deleted@example.com";
        client.Phone = 0;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateClient(int id, UpdateClientDto data)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null) throw new InvalidOperationException("Client not found.");

        client.Address = data.Address;
        client.Email = data.Email;
        client.Phone = data.Phone;

        var company = await _context.Companies.FindAsync(id);
        if (company != null && data.CompanyName != null)
        {
            company.Name = data.CompanyName;
        }

        var individual = await _context.PhysicalIndividuals.FindAsync(id);
        if (individual != null)
        {
            if (data.Name != null) individual.Name = data.Name;
            if (data.Surname != null) individual.Surname = data.Surname;
        }

        await _context.SaveChangesAsync();
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

    public async Task<double> ConvertFromPLN(decimal amount, string targetCurrency)
    {
        if (targetCurrency == "PLN") return (double)amount;
    
        // TODO: Placeholder: Replace with real API
        var exchangeRate = 2; //await _exchangeRateService.GetRate("PLN", targetCurrency);
        return (double)amount * exchangeRate;
    }
}