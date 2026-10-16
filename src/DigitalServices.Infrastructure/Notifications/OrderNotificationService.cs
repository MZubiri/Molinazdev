using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DigitalServices.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Notifications;

public sealed class OrderNotificationService : IOrderNotificationService
{
    private readonly NotificationOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(
        IOptions<NotificationOptions> options,
        HttpClient httpClient,
        ILogger<OrderNotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifyOrderPaidAsync(
        OrderPaidNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation(
            "Triggering paid order notifications for Order {OrderNumber} ({OrderId}) to {ClientEmail}.",
            notification.OrderNumber,
            notification.OrderId,
            notification.ClientEmail);

        // 1. Send Customer Confirmation Email (if enabled)
        if (_options.Email.Enabled && !string.IsNullOrWhiteSpace(_options.Email.SmtpHost))
        {
            try
            {
                await SendCustomerConfirmationEmailAsync(notification, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to send confirmation email to customer for Order {OrderNumber}.",
                    notification.OrderNumber);
            }
        }
        else
        {
            _logger.LogInformation(
                "Customer confirmation email skipped for Order {OrderNumber} (Email notifications disabled or unconfigured).",
                notification.OrderNumber);
        }

        // 2. Send Admin Sales Alert via Discord / Telegram Webhook (if enabled)
        if (_options.AdminAlert.Enabled)
        {
            try
            {
                await SendAdminAlertAsync(notification, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to send sales alert to admin for Order {OrderNumber}.",
                    notification.OrderNumber);
            }
        }
    }

    private async Task SendCustomerConfirmationEmailAsync(
        OrderPaidNotification notification,
        CancellationToken cancellationToken)
    {
        var emailOpts = _options.Email;
        using var smtpClient = new SmtpClient(emailOpts.SmtpHost, emailOpts.SmtpPort)
        {
            EnableSsl = emailOpts.EnableSsl,
            Credentials = new NetworkCredential(emailOpts.SmtpUsername, emailOpts.SmtpPassword),
            Timeout = 10000
        };

        var fromAddress = new MailAddress(emailOpts.FromAddress, emailOpts.FromName);
        var toAddress = new MailAddress(notification.ClientEmail, notification.ClientName);

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = $"¡Pago Confirmado! Tu orden #{notification.OrderNumber} en MolinazDev",
            Body = BuildCustomerEmailHtml(notification),
            IsBodyHtml = true
        };

        await smtpClient.SendMailAsync(message, cancellationToken);

        _logger.LogInformation(
            "Confirmation email sent successfully for Order {OrderNumber} to {ClientEmail}.",
            notification.OrderNumber,
            notification.ClientEmail);
    }

    private async Task SendAdminAlertAsync(
        OrderPaidNotification notification,
        CancellationToken cancellationToken)
    {
        var alertOpts = _options.AdminAlert;

        // Discord Webhook
        if (!string.IsNullOrWhiteSpace(alertOpts.DiscordWebhookUrl))
        {
            var payload = new
            {
                username = "MolinazDev Bot",
                avatar_url = "https://molinazdev.lat/favicon.svg",
                embeds = new[]
                {
                    new
                    {
                        title = "🎉 ¡Nueva Venta Confirmada en MolinazDev!",
                        color = 5025616, // Indigo/Emerald tone
                        description = $"Se ha confirmado el pago para la orden **#{notification.OrderNumber}**.",
                        fields = new[]
                        {
                            new { name = "📦 Paquete", value = notification.PackageName, inline = true },
                            new { name = "💰 Monto", value = $"{notification.Currency} {notification.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)}", inline = true },
                            new { name = "👤 Cliente", value = notification.ClientName, inline = true },
                            new { name = "📧 Email", value = notification.ClientEmail, inline = true },
                            new { name = "📱 Teléfono", value = notification.ClientPhone ?? "No especificado", inline = true },
                            new { name = "🏢 Empresa", value = notification.CompanyName ?? "Particular", inline = true }
                        },
                        footer = new { text = $"Fecha: {notification.PaidAt:yyyy-MM-dd HH:mm:ss} UTC" }
                    }
                }
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                alertOpts.DiscordWebhookUrl,
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Admin Discord sales alert sent for Order {OrderNumber}.",
                    notification.OrderNumber);
            }
            else
            {
                _logger.LogWarning(
                    "Discord webhook returned status {StatusCode} for Order {OrderNumber}.",
                    response.StatusCode,
                    notification.OrderNumber);
            }
        }

        // Telegram Bot Alert
        if (!string.IsNullOrWhiteSpace(alertOpts.TelegramBotToken) &&
            !string.IsNullOrWhiteSpace(alertOpts.TelegramChatId))
        {
            var phone = notification.ClientPhone ?? "No especificado";
            var company = notification.CompanyName ?? "Particular";
            var formattedAmount = notification.TotalAmount.ToString("N2", CultureInfo.InvariantCulture);

            var text = $"🎉 *¡Nueva Venta Confirmada en MolinazDev!*\n\n" +
                       $"*Orden:* `#{notification.OrderNumber}`\n" +
                       $"*Paquete:* {notification.PackageName}\n" +
                       $"*Monto:* {notification.Currency} {formattedAmount}\n" +
                       $"*Cliente:* {notification.ClientName}\n" +
                       $"*Email:* {notification.ClientEmail}\n" +
                       $"*Teléfono:* {phone}\n" +
                       $"*Empresa:* {company}";

            var telegramUrl = $"https://api.telegram.org/bot{alertOpts.TelegramBotToken}/sendMessage";
            var telegramPayload = new
            {
                chat_id = alertOpts.TelegramChatId,
                text,
                parse_mode = "Markdown"
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(telegramPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(telegramUrl, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Admin Telegram sales alert sent for Order {OrderNumber}.",
                    notification.OrderNumber);
            }
        }
    }

    private static string BuildCustomerEmailHtml(OrderPaidNotification notification)
    {
        var currencyLabel = notification.Currency == "PEN" ? "S/." : notification.Currency;
        var formattedTotal = $"{currencyLabel} {notification.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)}";

        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #fafaf9; margin: 0; padding: 24px; color: #18181b; }}
    .container {{ max-width: 580px; margin: 0 auto; background: #ffffff; border: 2px solid #18181b; border-radius: 16px; box-shadow: 4px 4px 0px #18181b; overflow: hidden; }}
    .header {{ background: #18181b; color: #ffffff; padding: 24px 32px; }}
    .header h1 {{ margin: 0; font-size: 20px; font-weight: 800; }}
    .content {{ padding: 32px; }}
    .badge {{ display: inline-block; background: #dcfce7; color: #166534; font-weight: 800; font-size: 11px; text-transform: uppercase; padding: 4px 10px; border-radius: 999px; margin-bottom: 16px; }}
    .order-box {{ background: #f4f4f5; border: 1px solid #e4e4e7; border-radius: 12px; padding: 20px; margin: 20px 0; }}
    .row {{ display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 13px; }}
    .row.total {{ border-top: 1px solid #d4d4d8; padding-top: 10px; margin-top: 10px; font-weight: 800; font-size: 16px; color: #4f46e5; }}
    .steps {{ background: #eef2ff; border-left: 4px solid #4f46e5; padding: 16px 20px; border-radius: 8px; margin: 24px 0; }}
    .steps h3 {{ margin: 0 0 10px 0; font-size: 14px; font-weight: 700; color: #3730a3; }}
    .steps ul {{ margin: 0; padding-left: 20px; font-size: 13px; color: #312e81; }}
    .steps li {{ margin-bottom: 6px; }}
    .footer {{ text-align: center; font-size: 11px; color: #71717a; padding: 20px; border-top: 1px solid #f4f4f5; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>Molinaz<span style='color: #818cf8;'>Dev</span> &middot; Confirmación de Servicio</h1>
    </div>
    <div class='content'>
      <span class='badge'>✓ Pago Procesado con Éxito</span>
      <h2 style='margin-top: 0;'>¡Gracias por confiar en nosotros, {WebUtility.HtmlEncode(notification.ClientName)}!</h2>
      <p style='font-size: 14px; line-height: 1.6; color: #52525b;'>
        Mercado Pago nos ha confirmado tu transacción. Tu orden se encuentra registrada bajo el número de pedido <strong>#{notification.OrderNumber}</strong>.
      </p>

      <div class='order-box'>
        <div class='row'><span>Paquete contratado:</span><strong>{WebUtility.HtmlEncode(notification.PackageName)}</strong></div>
        <div class='row'><span>Fecha de confirmación:</span><span>{notification.PaidAt:dd/MM/yyyy HH:mm} UTC</span></div>
        <div class='row total'><span>Total abonado (con IGV):</span><span>{formattedTotal}</span></div>
      </div>

      <div class='steps'>
        <h3>📌 Próximos pasos inmediatos:</h3>
        <ul>
          <li>Un asesor técnico de MolinazDev te contactará por WhatsApp o correo en menos de <strong>24 horas hábiles</strong>.</li>
          <li>Coordinaremos la reunión de arranque para definir contenidos, paleta de marca y accesos de dominio y hosting.</li>
          <li>Tu proyecto incluye soporte técnico garantizado por 60 días tras la entrega.</li>
        </ul>
      </div>

      <p style='font-size: 13px; color: #71717a;'>
        Si tienes alguna pregunta urgente, puedes escribirnos respondiendo a este correo o vía WhatsApp al <a href='https://wa.me/51983529230' style='color: #4f46e5; font-weight: 700;'>+51 983 529 230</a>.
      </p>
    </div>
    <div class='footer'>
      &copy; {DateTime.UtcNow.Year} MolinazDev &middot; RUC: 15613205389 &middot; Desarrollo Web, Chatbots IA y Software a Medida en Lima y LATAM.<br>
      contacto@molinazdev.lat &middot; https://molinazdev.lat
    </div>
  </div>
</body>
</html>";
    }
}
