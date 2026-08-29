using System.Text.Json;
using DigitalServices.Application.Catalog;
using DigitalServices.Application.Checkout;
using DigitalServices.Application.Payments;
using DigitalServices.Infrastructure;
using DigitalServices.Infrastructure.Payments;
using DigitalServices.Web.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    // Coolify assigns proxy addresses dynamically inside its private Docker network.
    // The application port must not be exposed directly to untrusted networks.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("mysql");
builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddOptions<MercadoPagoOptions>()
    .Bind(builder.Configuration.GetSection(MercadoPagoOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        static options =>
            IsAbsoluteHttpUrl(options.SuccessUrl) &&
            IsAbsoluteHttpUrl(options.FailureUrl) &&
            IsAbsoluteHttpUrl(options.PendingUrl) &&
            IsAbsoluteHttpUrl(options.NotificationUrl),
        "Mercado Pago return and notification URLs must be absolute HTTP or HTTPS URLs.")
    .Validate(
        options =>
            !builder.Environment.IsProduction() ||
            (IsAbsoluteHttpsUrl(options.SuccessUrl) &&
             IsAbsoluteHttpsUrl(options.FailureUrl) &&
             IsAbsoluteHttpsUrl(options.PendingUrl) &&
             IsAbsoluteHttpsUrl(options.NotificationUrl)),
        "Mercado Pago URLs must use HTTPS in production.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection must be configured outside source control.");
}

var configuredServerVersion = builder.Configuration["Database:ServerVersion"] ?? "8.0.36";
if (!Version.TryParse(configuredServerVersion, out var mysqlServerVersion))
{
    throw new InvalidOperationException("Database:ServerVersion must be a valid MySQL version.");
}

builder.Services.AddInfrastructure(
    connectionString,
    mysqlServerVersion,
    enableDetailedErrors: builder.Environment.IsDevelopment());

builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IPaymentWebhookProcessor, PaymentWebhookProcessor>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler("/error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; " +
        "img-src 'self' https: data:; object-src 'none'; script-src 'self'; style-src 'self'";
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = static async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new { status = report.Status.ToString().ToLowerInvariant() },
            cancellationToken: context.RequestAborted);
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool IsAbsoluteHttpUrl(string? value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

static bool IsAbsoluteHttpsUrl(string? value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps;
}

public partial class Program;
