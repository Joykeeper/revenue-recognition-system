using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;
using Project.Services;

namespace TestProject;

public class DbServiceContractTests
{
    private DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DatabaseContext(options);
    }

    [Fact]
    public async Task CreateContract_ShouldApplyDiscountsCorrectly()
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);

        context.Softwares.Add(new Software { Id = 1, PriceForYear = 1000 });
        context.Discounts.Add(new Discount
        {
            Id = 1,
            Name = "Promo",
            Percentage = 10,
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddDays(5)
        });
        await context.SaveChangesAsync();

        var dto = new NewContractDto
        {
            BuyingClientId = 1,
            SellingClientId = 2,
            SoftwareId = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(5),
            SoftwareVersion = "v1.0",
            YearsOfUpdates = 2,
            DiscountId = 1,
            IsReturningClient = true
        };

        await service.CreateContract(dto);

        var contract = await context.Contracts.FirstOrDefaultAsync();

        // Base: 1000 + 1000 (1 extra year) = 2000
        // 10% discount = 1800
        // Returning client discount (5%) = 1710
        Assert.NotNull(contract);
        Assert.Equal(1710, contract.Price, 1);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(31, 1)]
    public async Task CreateContract_ShouldThrowOnInvalidDuration(int days, int years)
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);
        context.Softwares.Add(new Software { Id = 1, PriceForYear = 1000 });
        await context.SaveChangesAsync();

        var dto = new NewContractDto
        {
            BuyingClientId = 1,
            SellingClientId = 2,
            SoftwareId = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(days),
            SoftwareVersion = "v1.0",
            YearsOfUpdates = years
        };

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateContract(dto));
    }

    [Fact]
    public async Task CreateContract_ShouldThrowOnInvalidSupportYears()
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);
        context.Softwares.Add(new Software { Id = 1, PriceForYear = 1000 });
        await context.SaveChangesAsync();

        var dto = new NewContractDto
        {
            BuyingClientId = 1,
            SellingClientId = 2,
            SoftwareId = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(5),
            SoftwareVersion = "v1.0",
            YearsOfUpdates = 5
        };

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateContract(dto));
    }

    [Fact]
    public async Task PayContract_ShouldMarkAsSigned_WhenFullyPaid()
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);

        var contract = new Contract
        {
            Id = 1,
            SoftwareId = 1,
            Price = 500,
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddDays(5),
            Signed = false
        };

        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var dto = new PaymentDto
        {
            ClientId = 1,
            ContractId = 1,
            Amount = 500,
            Date = DateTime.Today
        };

        await service.PayContract(dto);

        var updated = await context.Contracts.FindAsync(1);
        Assert.True(updated.Signed);
    }

    [Fact]
    public async Task PayContract_ShouldThrow_WhenPastContractEnd()
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);

        var contract = new Contract
        {
            Id = 1,
            SoftwareId = 1,
            Price = 100,
            StartTime = DateTime.Today.AddDays(-10),
            EndTime = DateTime.Today.AddDays(-1),
            Signed = false
        };

        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var dto = new PaymentDto
        {
            ClientId = 1,
            ContractId = 1,
            Amount = 100,
            Date = DateTime.Today
        };

        await Assert.ThrowsAsync<BadRequestException>(() => service.PayContract(dto));
    }

    [Fact]
    public async Task IsContractFullyPaid_ShouldReturnTrue_WhenEnoughPaid()
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);

        var contract = new Contract { Id = 1, Price = 200 };
        context.Contracts.Add(contract);
        context.Payments.AddRange(
            new Payment { ContractId = 1, Amount = 150, Returned = false },
            new Payment { ContractId = 1, Amount = 50, Returned = false }
        );
        await context.SaveChangesAsync();

        var result = await service.IsContractFullyPaid(1);
        Assert.True(result);
    }

    [Fact]
    public async Task IsContractFullyPaid_ShouldReturnFalse_WhenInsufficientPayment()
    {
        var context = GetInMemoryDbContext();
        var service = new ContractsService(context);

        var contract = new Contract { Id = 1, Price = 300 };
        context.Contracts.Add(contract);
        context.Payments.Add(new Payment { ContractId = 1, Amount = 100, Returned = false });
        await context.SaveChangesAsync();

        var result = await service.IsContractFullyPaid(1);
        Assert.False(result);
    }
}
