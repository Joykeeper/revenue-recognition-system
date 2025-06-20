using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq; // For HttpClient mocking
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq.Protected;
using Newtonsoft.Json;
using Project.Data;
using Project.Exceptions;
using Project.Models;
using TestProject; // For JSON serialization/deserialization in HttpClient mock

public class IncomeCalculationTests // Renamed for consistency with previous output
{
    private const double AvgPlnToEur = 0.234;
    private const double AvgPlnToUsd = 0.269;
    private const double TolerancePercentage = 0.02; // +/- 2% tolerance
    
    // Helper to get a unique in-memory DbContext for each test
    private DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Ensures isolated DB for each test
            .Options;
        return new DatabaseContext(options);
    }

    // Helper for mocking HttpClient's SendAsync to return a consistent exchange rate
    private HttpClient CreateMockHttpClient(decimal exchangeRate = 0.25m) // Default EUR rate
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected() // Needed to mock protected SendAsync method
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(
                    new { conversion_rates = new Dictionary<string, decimal> { { "EUR", exchangeRate }, { "USD", 0.22m }, { "PLN", 1m } } }
                ))
            });
        return new HttpClient(mockHandler.Object);
    }

    // --- Helper methods for seeding data, consistent with previous extensive tests ---
    private async Task<Software> SeedSoftware(DatabaseContext context, string name)
    {
        var software = new Software { Name = name };
        context.Softwares.Add(software);
        await context.SaveChangesAsync();
        return software;
    }

    private async Task<Client> SeedClient(DatabaseContext context, string name)
    {
        var client = new PhysicalIndividual { Name = name, Surname = "Test", PESEL = 123456789 };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        return client;
    }

    private async Task<Contract> SeedContract(DatabaseContext context, int softwareId, int buyingClientId, int sellingClientId, decimal price, bool signed = true)
    {
        var contract = new Contract
        {
            SoftwareId = softwareId,
            BuyingClientId = buyingClientId,
            SellingClientId = sellingClientId,
            Price = price,
            Signed = signed
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        return contract;
    }

    private async Task SeedPayment(DatabaseContext context, int contractId, decimal amount, bool returned = false)
    {
        context.Payments.Add(new Payment { ContractId = contractId, Amount = amount, Returned = returned });
        await context.SaveChangesAsync();
    }


    // --- Updated Tests ---

    [Fact]
    public async Task GetSoftwareExpectedIncome_ShouldReturnCorrectSumForExistingSoftware()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        // TestDbService constructor only takes context now
        var service = new TestDbService(context);

        var software1 = await SeedSoftware(context, "Software A");
        var software2 = await SeedSoftware(context, "Software B");

        await SeedContract(context, software1.Id, 1, 2, 1000m);
        await SeedContract(context, software1.Id, 1, 2, 2000m);
        await SeedContract(context, software2.Id, 3, 4, 5000m);

        // Act
        var income = await service.GetSoftwareExpectedIncome(software1.Id, "PLN");

        // Assert
        Assert.Equal(3000, income); // 1000 + 2000, PLN converts to 1x
    }
    
    
    [Fact]
    public async Task GetSoftwareExpectedIncome_ShouldThrowNotFoundExceptionForNonExistingSoftware()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var httpClient = CreateMockHttpClient();
        var service = new TestDbService(context);

        // No software seeded

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetSoftwareExpectedIncome(999, "PLN"));
    }

    [Fact]
    public async Task GetSoftwareIncome_ShouldReturnSumOfFullyPaidContracts()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new TestDbService(context);

        var software1 = await SeedSoftware(context, "Software A");
        var client1 = await SeedClient(context, "Client A");
        var client2 = await SeedClient(context, "Client B");

        // Contract 1: Fully paid
        var contract1 = await SeedContract(context, software1.Id, client1.Id, client2.Id, 1000m, signed: true);
        await SeedPayment(context, contract1.Id, 1000m, returned: false);

        // Contract 2: Partially paid
        var contract2 = await SeedContract(context, software1.Id, client1.Id, client2.Id, 2000m, signed: true);
        await SeedPayment(context, contract2.Id, 1000m, returned: false);

        // Contract 3: Fully paid but with a returned payment (net effect is not fully paid)
        var contract3 = await SeedContract(context, software1.Id, client1.Id, client2.Id, 1500m, signed: true);
        await SeedPayment(context, contract3.Id, 1500m, returned: false); // Total paid 1500
        await SeedPayment(context, contract3.Id, 500m, returned: true);   // 500 returned, effective paid 1000

        // Contract 4: Unsigned (should not contribute to current income at all, regardless of payments)
        var contract4 = await SeedContract(context, software1.Id, client1.Id, client2.Id, 3000m, signed: false);
        await SeedPayment(context, contract4.Id, 3000m, returned: false); // Even if paid, not signed

        // Act
        var result = await service.GetSoftwareIncome(software1.Id, "PLN");

        // Assert
        // Only contract1 is fully paid (1000) and signed.
        // contract2 is partially paid (1000 < 2000).
        // contract3 has effective 1000 paid (1000 < 1500).
        // contract4 is not signed.
        Assert.Equal(1000, result);
    }

    [Fact]
    public async Task GetSoftwareIncome_ShouldThrowNotFoundExceptionForNonExistingSoftware()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var httpClient = CreateMockHttpClient();
        var service = new TestDbService(context);

        // No software seeded

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetSoftwareIncome(999, "PLN"));
    }

    [Fact]
    public async Task GetClientIncome_ShouldReturnSumOfFullyPaidContractsForBuyingClient()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var httpClient = CreateMockHttpClient();
        var service = new TestDbService(context);

        var client1 = await SeedClient(context, "Client 1");
        var client2 = await SeedClient(context, "Client 2");
        var software = await SeedSoftware(context, "Software X");

        // Contract 1 (BuyingClientId = client1): Fully paid
        var contract1 = await SeedContract(context, software.Id, client1.Id, client2.Id, 1000m, signed: true);
        await SeedPayment(context, contract1.Id, 1000m, returned: false);

        // Contract 2 (BuyingClientId = client1): Partially paid
        var contract2 = await SeedContract(context, software.Id, client1.Id, client2.Id, 2000m, signed: true);
        await SeedPayment(context, contract2.Id, 1500m, returned: false);

        // Contract 3 (BuyingClientId = client2): Fully paid (should not count for client1)
        var contract3 = await SeedContract(context, software.Id, client2.Id, client1.Id, 5000m, signed: true);
        await SeedPayment(context, contract3.Id, 5000m, returned: false);

        // Act
        var result = await service.GetClientIncome(client1.Id, "PLN");

        // Assert
        // Only contract1 contributes for client1's buying income (1000)
        Assert.Equal(1000, result);
    }

    [Fact]
    public async Task GetClientIncome_ShouldThrowNotFoundExceptionForNonExistingClient()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var httpClient = CreateMockHttpClient();
        var service = new TestDbService(context);

        // No client seeded

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetClientIncome(999, "PLN"));
    }

    [Fact]
    public async Task GetClientExpectedIncome_ShouldReturnSumOfContractPricesForSellingClient()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new TestDbService(context);

        var client1 = await SeedClient(context, "Client 1");
        var client2 = await SeedClient(context, "Client 2");
        var software = await SeedSoftware(context, "Software X");

        // Contracts where Client1 is the SellingClientId
        await SeedContract(context, software.Id, client2.Id, client1.Id, 500m, signed: true);
        await SeedContract(context, software.Id, client2.Id, client1.Id, 1000m, signed: false); // Unsigned still counts for expected
        await SeedContract(context, software.Id, client1.Id, client2.Id, 700m, signed: true); // Client1 is Buying, not Selling

        // Act
        var result = await service.GetClientExpectedIncome(client1.Id, "PLN");

        // Assert
        // 500 (Sold by Client1) + 1000 (Sold by Client1) = 1500
        Assert.Equal(1500, result);
    }

    [Fact]
    public async Task GetClientExpectedIncome_ShouldThrowNotFoundExceptionForNonExistingClient()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var httpClient = CreateMockHttpClient();
        var service = new TestDbService(context);

        // No client seeded

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetClientExpectedIncome(999, "PLN"));
    }

    [Fact]
    public async Task GetSoftwareExpectedIncome_ShouldConvertCurrencyWithinRange()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new TestDbService(context); // Using real HttpClient

        var software1 = await SeedSoftware(context, "Software A");
        await SeedContract(context, software1.Id, 1, 2, 1000m);
        await SeedContract(context, software1.Id, 1, 2, 2000m); // Total 3000 PLN

        double expectedPln = 3000;
        double expectedEurAverage = expectedPln * AvgPlnToEur;
        double lowerBound = expectedEurAverage * (1 - TolerancePercentage);
        double upperBound = expectedEurAverage * (1 + TolerancePercentage);

        // Act
        var income = await service.GetSoftwareExpectedIncome(software1.Id, "EUR");

        // Assert
        Assert.True(income >= lowerBound && income <= upperBound,
            $"Expected income for EUR to be between {lowerBound:F2} and {upperBound:F2}, but was {income:F2}");
    }

    [Fact]
    public async Task GetSoftwareIncome_ShouldConvertCurrencyWithinRange()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new TestDbService(context); // Using real HttpClient

        var software1 = await SeedSoftware(context, "Software A");
        var client1 = await SeedClient(context, "Client A");
        var client2 = await SeedClient(context, "Client B");

        var contract1 = await SeedContract(context, software1.Id, client1.Id, client2.Id, 1000m, signed: true);
        await SeedPayment(context, contract1.Id, 1000m, returned: false); // Fully paid: 1000 PLN

        var contract2 = await SeedContract(context, software1.Id, client1.Id, client2.Id, 2000m, signed: true);
        await SeedPayment(context, contract2.Id, 1500m, returned: false); // Partially paid

        double expectedPln = 1000; // Only contract1 is fully paid
        double expectedEurAverage = expectedPln * AvgPlnToEur;
        double lowerBound = expectedEurAverage * (1 - TolerancePercentage);
        double upperBound = expectedEurAverage * (1 + TolerancePercentage);

        // Act
        var income = await service.GetSoftwareIncome(software1.Id, "EUR");

        // Assert
        Assert.True(income >= lowerBound && income <= upperBound,
            $"Expected income for EUR to be between {lowerBound:F2} and {upperBound:F2}, but was {income:F2}");
    }


    [Fact]
    public async Task GetClientIncome_ShouldConvertCurrencyWithinRange()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new TestDbService(context); // Using real HttpClient

        var client1 = await SeedClient(context, "Client 1");
        var client2 = await SeedClient(context, "Client 2");
        var software = await SeedSoftware(context, "Software X");

        var contract1 = await SeedContract(context, software.Id, client1.Id, client2.Id, 1000m, signed: true);
        await SeedPayment(context, contract1.Id, 1000m, returned: false); // Fully paid: 1000 PLN

        var contract2 = await SeedContract(context, software.Id, client1.Id, client2.Id, 2000m, signed: true);
        await SeedPayment(context, contract2.Id, 1500m, returned: false); // Partially paid

        double expectedPln = 1000; // Only contract1 is fully paid for client1
        double expectedEurAverage = expectedPln * AvgPlnToEur;
        double lowerBound = expectedEurAverage * (1 - TolerancePercentage);
        double upperBound = expectedEurAverage * (1 + TolerancePercentage);

        // Act
        var income = await service.GetClientIncome(client1.Id, "EUR");

        // Assert
        Assert.True(income >= lowerBound && income <= upperBound,
            $"Expected income for EUR to be between {lowerBound:F2} and {upperBound:F2}, but was {income:F2}");
    }
    
    [Fact]
    public async Task GetClientExpectedIncome_ShouldConvertCurrencyWithinRange()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new TestDbService(context); // Using real HttpClient

        var client1 = await SeedClient(context, "Client 1");
        var client2 = await SeedClient(context, "Client 2");
        var software = await SeedSoftware(context, "Software X");

        await SeedContract(context, software.Id, client2.Id, client1.Id, 500m, signed: true);
        await SeedContract(context, software.Id, client2.Id, client1.Id, 1000m, signed: false); // Unsigned still counts for expected

        double expectedPln = 1500; // Total expected for client1 as selling client
        double expectedEurAverage = expectedPln * AvgPlnToEur;
        double lowerBound = expectedEurAverage * (1 - TolerancePercentage);
        double upperBound = expectedEurAverage * (1 + TolerancePercentage);

        // Act
        var income = await service.GetClientExpectedIncome(client1.Id, "EUR");

        // Assert
        Assert.True(income >= lowerBound && income <= upperBound,
            $"Expected income for EUR to be between {lowerBound:F2} and {upperBound:F2}, but was {income:F2}");
    }
}