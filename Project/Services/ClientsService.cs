using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;

namespace Project.Services;

public class ClientsService : IClientsService
{
    private readonly DatabaseContext _context;
    private readonly ICurrencyConverter _exchangeRateService;
    public ClientsService(DatabaseContext context)
    {
        _context = context;
        _exchangeRateService = new CurrencyConverter();
    }
    
    public async Task<double> GetClientIncome(int clientId, string currency)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == clientId);
        if (!clientExists)
        {
            throw new NotFoundException($"Client with ID {clientId} not found.");
        }


        var currentIncomePLN = await _context.Contracts
            .Where(c => c.BuyingClientId == clientId)
            .Select(c => new
            {
                ContractPrice = c.Price,
                TotalPaid = c.Payments
                    .Where(p => !p.Returned)
                    .Sum(p => p.Amount)
            })
            .Where(x => x.TotalPaid >= x.ContractPrice)
            .SumAsync(x => x.ContractPrice);

        return await _exchangeRateService.ConvertFromPLN(currentIncomePLN, currency);
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

        return await _exchangeRateService.ConvertFromPLN(expected, currency);
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
            throw new BadRequestException("Cannot delete companies.");
        } 
        
        if (client is PhysicalIndividual individual)
        {
            // If it's a PhysicalIndividual, perform the soft delete
            individual.Name = "DELETED";
            individual.Surname = "DELETED";
            individual.PESEL = 0;
            
            individual.Address = "DELETED";
            individual.Email = "deleted@example.com";
            individual.Phone = 0;
        } else
        {
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
            throw new NotFoundException("Client not found."); 
        }

        // Update common properties (present on the base Client)
        client.Address = data.Address;
        client.Email = data.Email;
        client.Phone = data.Phone;

        // Check the actual runtime type and update type-specific properties
        if (client is Company company)
        {
            if (data.CompanyName != null)
            {
                company.Name = data.CompanyName;
            }
        }
        else if (client is PhysicalIndividual individual)
        {
            if (data.Name != null) individual.Name = data.Name;
            if (data.Surname != null) individual.Surname = data.Surname;
        }
        
        await _context.SaveChangesAsync();
    }

}