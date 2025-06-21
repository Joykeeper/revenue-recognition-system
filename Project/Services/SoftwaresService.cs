using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Exceptions;

namespace Project.Services;

public class SoftwaresService : ISoftwaresService
{
    private readonly DatabaseContext _context;
    private readonly ICurrencyConverter _exchangeRateService;
    public SoftwaresService(DatabaseContext context)
    {
        _context = context;
        _exchangeRateService = new CurrencyConverter();
    }
    
    public async Task<double> GetSoftwareIncome(int id, string currency)
    {
        var softwareExists = await _context.Softwares.AnyAsync(s => s.Id == id);
        if (!softwareExists)
        {
            throw new NotFoundException($"Software with ID {id} not found.");
        }

        var currentIncomePLN = await _context.Contracts
            .Where(c => c.SoftwareId == id)
            .Select(c => new
            {
                ContractPrice = c.Price,
                c.Signed,
                TotalPaid = c.Payments
                    .Where(p => !p.Returned)
                    .Sum(p => p.Amount)
            })
            .Where(x => x.TotalPaid >= x.ContractPrice)
            .Where(x => x.Signed == true)
            .SumAsync(x => x.ContractPrice);

        return await _exchangeRateService.ConvertFromPLN(currentIncomePLN, currency);
    }


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

        return await _exchangeRateService.ConvertFromPLN(expected, currency);
    }

}