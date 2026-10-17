using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Application.Chatbot;
using DigitalServices.Application.Leads;
using DigitalServices.Application.Notifications;
using DigitalServices.Application.Payments;
using DigitalServices.Infrastructure.Chatbot;
using DigitalServices.Infrastructure.Notifications;
using DigitalServices.Infrastructure.Payments;
using DigitalServices.Infrastructure.Persistence;
using DigitalServices.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalServices.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Version mysqlServerVersion,
        bool enableDetailedErrors = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(mysqlServerVersion);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(mysqlServerVersion),
                mysql =>
                {
                    mysql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });

            options.EnableDetailedErrors(enableDetailedErrors);
        });

        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPaymentWebhookStore, PaymentWebhookStore>();

        services.AddScoped<IPaymentGateway, MercadoPagoPaymentGateway>();
        services.AddSingleton<IPaymentWebhookSignatureValidator, MercadoPagoWebhookSignatureValidator>();

        services.AddHttpClient<IOrderNotificationService, OrderNotificationService>();
        services.AddHttpClient<IAuditLeadNotificationService, AuditLeadNotificationService>();
        services.AddHttpClient<IChatLeadNotificationService, ChatLeadNotificationService>();
        services.AddHttpClient<IChatbotService, GeminiChatbotService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHostedService<PaymentReconciliationWorker>();

        return services;
    }
}
