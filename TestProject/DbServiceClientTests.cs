using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;
using Project.Services;

namespace TestProject;

public class DbServiceClientTests
{
    private DatabaseContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DatabaseContext(options);
    }

    [Fact]
    public async Task AddClient_ShouldAddPhysicalIndividual()
    {
        var context = GetInMemoryDbContext();
        var service = new ClientsService(context);

        var dto = new NewClientDto
        {
            Address = "Test St",
            Email = "user@example.com",
            Phone = 123456789,
            IsCompany = false,
            Name = "John",
            Surname = "Doe",
            PESEL = 123456789
        };

        await service.AddClient(dto);

        var client = await context.Clients.FirstOrDefaultAsync();
        var individual = await context.PhysicalIndividuals.FindAsync(client.Id);

        Assert.NotNull(client);
        Assert.NotNull(individual);
        Assert.Equal("John", individual.Name);
    }

    [Fact]
    public async Task AddClient_ShouldAddCompany()
    {
        var context = GetInMemoryDbContext();
        var service = new ClientsService(context);

        var dto = new NewClientDto
        {
            Address = "Biz Rd",
            Email = "biz@example.com",
            Phone = 987654321,
            IsCompany = true,
            CompanyName = "ACME Corp",
            KRS = "987654321"
        };

        await service.AddClient(dto);

        var client = await context.Clients.FirstOrDefaultAsync();
        var company = await context.Companies.FindAsync(client.Id);

        Assert.NotNull(client);
        Assert.NotNull(company);
        Assert.Equal("ACME Corp", company.Name);
    }

    [Fact]
    public async Task DeleteClient_ShouldSoftDeleteIndividual()
    {
        var context = GetInMemoryDbContext();
        var service = new ClientsService(context);
        
        var individualToAdd = new PhysicalIndividual
        {
            Address = "A",
            Email = "e@e.com",
            Phone = 111,
            Name = "Test",
            Surname = "User",
            PESEL = 123
        };
        
        context.Clients.Add(individualToAdd);
        await context.SaveChangesAsync();
        
        var clientId = individualToAdd.Id;
        
        await service.DeleteClient(clientId);
        
        var updatedClient = await context.Clients.FindAsync(clientId);

        Assert.NotNull(updatedClient);
        
        Assert.IsType<PhysicalIndividual>(updatedClient);

        var updatedIndividual = (PhysicalIndividual)updatedClient;

        Assert.Equal("DELETED", updatedIndividual.Name);
        Assert.Equal("DELETED", updatedIndividual.Surname);
        Assert.Equal(0, updatedIndividual.PESEL);
        Assert.Equal("deleted@example.com", updatedIndividual.Email);
        Assert.Equal("DELETED", updatedIndividual.Address);
        Assert.Equal(0, updatedIndividual.Phone);
    }

    [Fact]
    public async Task DeleteClient_ShouldThrowForCompany()
    {
        var context = GetInMemoryDbContext();
        var service = new ClientsService(context);
        
        var companyToAdd = new Company
        {
            Address = "B",
            Email = "biz@co.com",
            Phone = 222,
            Name = "BigCorp",
            KRS = "dasdasdasd"
        };

        context.Clients.Add(companyToAdd);
        await context.SaveChangesAsync();


        var companyId = companyToAdd.Id;
        
        await Assert.ThrowsAsync<BadRequestException>(() => service.DeleteClient(companyId));
        
        var fetchedCompany = await context.Clients.OfType<Company>().FirstOrDefaultAsync(c => c.Id == companyId);
        Assert.NotNull(fetchedCompany);
        Assert.Equal("BigCorp", fetchedCompany.Name);
        Assert.Equal("B", fetchedCompany.Address);
    }

    [Fact]
    public async Task UpdateClient_ShouldUpdateIndividualFields()
    {
        var context = GetInMemoryDbContext();
        var service = new ClientsService(context);
        
        var originalIndividual = new PhysicalIndividual
        {
            Address = "Addr",
            Email = "old@em.com",
            Phone = 1,
            Name = "Old",
            Surname = "Name",
            PESEL = 111
        };
        context.Clients.Add(originalIndividual);
        await context.SaveChangesAsync();

        var clientId = originalIndividual.Id;

        var dto = new UpdateClientDto
        {
            Address = "New Addr",
            Email = "new@em.com",
            Phone = 2,
            Name = "NewName",
            Surname = "NewSurname"
        };
        
        await service.UpdateClient(clientId, dto);
        
        var updatedClient = await context.Clients.FindAsync(clientId);

        Assert.NotNull(updatedClient);
        Assert.IsType<PhysicalIndividual>(updatedClient);

        var updatedIndividual = (PhysicalIndividual)updatedClient;
        
        Assert.Equal("New Addr", updatedClient.Address);
        Assert.Equal("new@em.com", updatedClient.Email);
        Assert.Equal(2, updatedClient.Phone);
        
        Assert.Equal("NewName", updatedIndividual.Name);
        Assert.Equal("NewSurname", updatedIndividual.Surname);
        Assert.Equal(111, updatedIndividual.PESEL);
    }

    [Fact]
    public async Task UpdateClient_ShouldUpdateCompanyName()
    {
        var context = GetInMemoryDbContext();
        var service = new ClientsService(context);
        
        var originalCompany = new Company
        {
            Address = "Start",
            Email = "email@em.com",
            Phone = 123,
            Name = "OldCorp",
            KRS = "dasdas"
        };
        context.Clients.Add(originalCompany);
        await context.SaveChangesAsync();

        var companyId = originalCompany.Id;

        var dto = new UpdateClientDto
        {
            Address = "HQ",
            Email = "new@em.com",
            Phone = 456,
            CompanyName = "NewCorp"
        };
        
        await service.UpdateClient(companyId, dto);
        
        var updatedClient = await context.Clients.FindAsync(companyId);

        Assert.NotNull(updatedClient);
        Assert.IsType<Company>(updatedClient);

        var updatedCompany = (Company)updatedClient;
        
        Assert.Equal("HQ", updatedClient.Address);
        Assert.Equal("new@em.com", updatedClient.Email);
        Assert.Equal(456, updatedClient.Phone);
        
        Assert.Equal("NewCorp", updatedCompany.Name);
        Assert.Equal("dasdas", updatedCompany.KRS);
    }
}
