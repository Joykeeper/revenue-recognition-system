using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;

namespace Project.Services;

public class DbService : IDbService
{
    private readonly DatabaseContext _context;
    private readonly ICurrencyConverter _exchangeRateService;
    public DbService(DatabaseContext context)
    {
        _context = context;
        _exchangeRateService = new CurrencyConverter();
    }
    
    // TODO: fix all income calculations
    public async Task<double> GetSoftwareExpectedIncome(int id, string currency)
    {
        var softwareExists = await _context.Softwares.AnyAsync(s => s.Id == id);
        if (!softwareExists)
        {
            throw new NotFoundException($"Software with ID {id} not found.");
        }
        
        var expected = await _context.Contracts
            .Where(c => c.SoftwareId == id)
            .SumAsync(c => c.Price);

        return await ConvertFromPLN(expected, currency);
    }

    public async Task<double> GetSoftwareIncome(int id, string currency)
    {
        var softwareExists = await _context.Softwares.AnyAsync(s => s.Id == id);
        if (!softwareExists)
        {
            throw new NotFoundException($"Software with ID {id} not found.");
        }
        
        var currentIncomePLN = await _context.Contracts
            .Where(c => c.SoftwareId == id) // Filter for specific software
            .Select(c => new
            {
                ContractPrice = c.Price,
                TotalPaid = c.Payments // Access related payments
                    .Where(p => !p.Returned) // Only non-returned payments
                    .Sum(p => p.Amount) // Sum amounts for this contract
            })
            .Where(x => x.TotalPaid >= x.ContractPrice) // Only include contracts where total paid >= contract price
            .SumAsync(x => x.ContractPrice); // Sum the *contract prices* of fully paid contracts

        return await ConvertFromPLN(currentIncomePLN, currency);
    }

    public async Task<double> GetClientIncome(int clientId, string currency)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == clientId);
        if (!clientExists)
        {
            throw new NotFoundException($"Client with ID {clientId} not found.");
        }
        // Requirements for Current Income for a Client:
        // "Dopiero po pełnym uregulowaniu płatności możemy traktować wartość umowy jako przychód."

        var currentIncomePLN = await _context.Contracts
            .Where(c => c.BuyingClientId == clientId) // Filter for specific client
            .Select(c => new
            {
                ContractPrice = c.Price,
                TotalPaid = c.Payments // Access related payments
                    .Where(p => !p.Returned) // Only non-returned payments
                    .Sum(p => p.Amount) // Sum amounts for this contract
            })
            .Where(x => x.TotalPaid >= x.ContractPrice) // Only include contracts where total paid >= contract price
            .SumAsync(x => x.ContractPrice); // Sum the *contract prices* of fully paid contracts

        return await ConvertFromPLN(currentIncomePLN, currency);
    }

    public async Task<double> GetClientExpectedIncome(int id, string currency)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == id);
        if (!clientExists)
        {
            throw new NotFoundException($"Client with ID {id} not found.");
        }
        
        var contracts = await _context.Contracts
            .Where(c => c.SellingClientId == id)
            .ToListAsync();

        decimal expected = contracts.Sum(c => c.Price);

        return await ConvertFromPLN(expected, currency);
    }
    
    public async Task AddClient(NewClientDto data)
    {
        Client newClient;
        if (data.IsCompany)
        {
            newClient = new Company
            {
                Address = data.Address,
                Email = data.Email,
                Phone = data.Phone,
                Name = data.CompanyName!,
                KRS = data.KRS
            };
        }
        else
        {
            newClient = new PhysicalIndividual
            {
                Address = data.Address,
                Email = data.Email,
                Phone = data.Phone,
                Name = data.Name!,
                Surname = data.Surname!,
                PESEL = data.PESEL!.Value
            };
        }
        
        _context.Clients.Add(newClient);

        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);

        // 2. Check if the client exists
        if (client == null)
        {
            throw new NotFoundException("Client not found.");
        }
        
        // 3. Determine the actual type of the client
        if (client is Company)
        {
            // If it's a Company, throw the BadRequestException
            throw new BadRequestException("Cannot delete companies.");
        } 
        
        if (client is PhysicalIndividual individual)
        {
            // If it's a PhysicalIndividual, perform the soft delete
            individual.Name = "DELETED";
            individual.Surname = "DELETED";
            individual.PESEL = 0; // Or null if PESEL is nullable

            // Update base Client properties (they are on the same object 'individual' now, as it's cast from 'client')
            individual.Address = "DELETED";
            individual.Email = "deleted@example.com";
            individual.Phone = 0; // Or null if Phone is nullable
        } else
        {
            // Handle cases where the client might be of a base type directly
            // or an unexpected derived type, though less likely in your scenario.
            throw new InvalidOperationException($"Unexpected client type for ID {id}.");
        }

        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateClient(int id, UpdateClientDto data)
    {
        // Fetch the client once using the base DbSet
        var client = await _context.Clients.FindAsync(id);
    
        if (client == null)
        {
            // Use NotFoundException for better clarity if you have it
            throw new NotFoundException("Client not found."); 
            // Or keep: throw new InvalidOperationException("Client not found.");
        }

        // Update common properties (present on the base Client)
        client.Address = data.Address;
        client.Email = data.Email;
        client.Phone = data.Phone;

        // Check the actual runtime type and update type-specific properties
        if (client is Company company)
        {
            // This 'company' variable is a reference to the same 'client' object,
            // just typed as Company. Changes to 'company' are changes to 'client'.
            if (data.CompanyName != null)
            {
                company.Name = data.CompanyName;
            }
            // If there are other Company-specific properties in UpdateClientDto, update them here
            // e.g., company.KRS = data.KRS;
        }
        else if (client is PhysicalIndividual individual)
        {
            // This 'individual' variable is a reference to the same 'client' object,
            // just typed as PhysicalIndividual.
            if (data.Name != null) individual.Name = data.Name;
            if (data.Surname != null) individual.Surname = data.Surname;
            // If there are other PhysicalIndividual-specific properties in UpdateClientDto, update them here
            // e.g., individual.PESEL = data.PESEL;
        }
        // No 'else' needed here, as the client must be one of the derived types or the base itself.
        // If it's just a base Client and no derived properties were provided in DTO, it simply updates base fields.

        // Save all changes in one go
        await _context.SaveChangesAsync();
    }
    
    // public async Task UpdateClient(int id, UpdateClientDto data)
    // {
    //     var client = await _context.Clients.FindAsync(id);
    //     if (client == null) throw new InvalidOperationException("Client not found.");
    //
    //     client.Address = data.Address;
    //     client.Email = data.Email;
    //     client.Phone = data.Phone;
    //
    //     var company = await _context.Companies.FindAsync(id);
    //     if (company != null && data.CompanyName != null)
    //     {
    //         company.Name = data.CompanyName;
    //     }
    //
    //     var individual = await _context.PhysicalIndividuals.FindAsync(id);
    //     if (individual != null)
    //     {
    //         if (data.Name != null) individual.Name = data.Name;
    //         if (data.Surname != null) individual.Surname = data.Surname;
    //     }
    //
    //     await _context.SaveChangesAsync();
    // }

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

    public virtual async Task<double> ConvertFromPLN(decimal amount, string targetCurrency)
    {
        if (targetCurrency == "PLN") return (double)amount;

        try
        {
            var result = await _exchangeRateService.ConvertFromPLN(amount, targetCurrency);
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine("Provide correct currency name and positive amount decimal");
            throw e;
        }
    }
}