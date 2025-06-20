using Microsoft.EntityFrameworkCore;
using Project.Models;

namespace Project.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options)
    {
    }
    
    public DbSet<Client> Clients { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<PhysicalIndividual> PhysicalIndividuals { get; set; }
    public DbSet<Software> Softwares { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>().ToTable("Client");
        modelBuilder.Entity<Company>().ToTable("Company");
        modelBuilder.Entity<PhysicalIndividual>().ToTable("PhysicalIndividual");
        modelBuilder.Entity<Contract>().ToTable("Contract");
        
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.SellingClient)
            .WithMany(cl => cl.ContractsAsSeller)
            .HasForeignKey(c => c.SellingClientId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.BuyingClient)
            .WithMany(cl => cl.ContractsAsBuyer)
            .HasForeignKey(c => c.BuyingClientId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Software)
            .WithMany(o => o.Contracts)
            .HasForeignKey(c => c.SoftwareId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Discount)
            .WithMany(d => d.Contracts)
            .HasForeignKey(c => c.DiscountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Client)
            .WithMany(cl => cl.Payments)
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Contract)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "User" }
        );
        
    }
}