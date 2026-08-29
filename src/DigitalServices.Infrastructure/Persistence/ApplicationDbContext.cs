using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalServices.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Service> Services => Set<Service>();

    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Payment> Payments => Set<Payment>();

    internal DbSet<PaymentNotificationReceipt> PaymentNotificationReceipts =>
        Set<PaymentNotificationReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
