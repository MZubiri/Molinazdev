using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DigitalServices.Application.Leads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Notifications;

public sealed class AuditLeadNotificationService(
    IOptions<NotificationOptions> options,
    HttpClient httpClient,
    ILogger<AuditLeadNotificationService> logger) : IAuditLeadNotificationService
{
    private readonly NotificationOptions _options = options.Value;

    public async Task NotifyAuditLeadAsync(
        AuditLeadRequestDto lead,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);

        var recipientEmail = string.IsNullOrWhiteSpace(_options.AdminAlert.AdminEmail)
            ? "contacto@molinazdev.lat"
            : _options.AdminAlert.AdminEmail;

        logger.LogInformation(
            "New Web Audit Lead received: Website={WebsiteUrl}, Contact={Contact}, IP={ClientIp}",
            lead.WebsiteUrl,
            lead.Contact,
            clientIp ?? "Unknown");

        // 1. Send Email Notification to Admin (contacto@molinazdev.lat)
        if (_options.Email.Enabled && !string.IsNullOrWhiteSpace(_options.Email.SmtpHost))
        {
            try
            {
                await SendAdminAuditEmailAsync(lead, recipientEmail, clientIp, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send audit lead email notification to {RecipientEmail}.", recipientEmail);
            }
        }
        else
        {
            logger.LogInformation(
                "Audit lead email to {RecipientEmail} skipped because SMTP is not enabled or unconfigured.",
                recipientEmail);
        }

        // 2. Send Discord / Telegram alerts if configured
        if (_options.AdminAlert.Enabled)
        {
            try
            {
                await SendAdminInstantAlertsAsync(lead, clientIp, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send instant admin alert for audit lead.");
            }
        }
    }

    private async Task SendAdminAuditEmailAsync(
        AuditLeadRequestDto lead,
        string recipientEmail,
        string? clientIp,
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
        var toAddress = new MailAddress(recipientEmail, "MolinazDev Admin");

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = $"⚡ Nueva Solicitud de Auditoría Web Gratuita ({lead.WebsiteUrl})",
            Body = BuildAuditEmailHtml(lead, clientIp),
            IsBodyHtml = true
        };

        await smtpClient.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Audit lead email notification successfully sent to {RecipientEmail}.", recipientEmail);
    }

    private async Task SendAdminInstantAlertsAsync(
        AuditLeadRequestDto lead,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var alertOpts = _options.AdminAlert;

        // Discord Webhook
        if (!string.IsNullOrWhiteSpace(alertOpts.DiscordWebhookUrl))
        {
            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "⚡ ¡Nueva Solicitud de Auditoría Web Gratuita!",
                        description = "Un cliente potencial ha solicitado la auditoría de rendimiento y SEO en 24h.",
                        color = 16763904, // Amber color
                        fields = new[]
                        {
                            new { name = "🌐 Sitio Web", value = lead.WebsiteUrl, inline = false },
                            new { name = "📬 Contacto (Email/WhatsApp)", value = lead.Contact, inline = false },
                            new { name = "🖥️ IP Cliente", value = clientIp ?? "No disponible", inline = true },
                            new { name = "⏰ Fecha (UTC)", value = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), inline = true }
                        }
                    }
                }
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            await httpClient.PostAsync(alertOpts.DiscordWebhookUrl, content, cancellationToken);
        }

        // Telegram Bot Alert
        if (!string.IsNullOrWhiteSpace(alertOpts.TelegramBotToken) &&
            !string.IsNullOrWhiteSpace(alertOpts.TelegramChatId))
        {
            var text = $"⚡ *¡Nueva Solicitud de Auditoría Web Gratuita!*\n\n" +
                       $"🌐 *Sitio Web:* {lead.WebsiteUrl}\n" +
                       $"📬 *Contacto:* {lead.Contact}\n" +
                       $"🖥️ *IP:* `{clientIp ?? "No disponible"}`\n" +
                       $"⏰ *Fecha:* {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

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

            await httpClient.PostAsync(telegramUrl, content, cancellationToken);
        }
    }

    private static string BuildAuditEmailHtml(AuditLeadRequestDto lead, string? clientIp)
    {
        var encodedUrl = Uri.EscapeDataString(lead.WebsiteUrl);
        var pageSpeedUrl = $"https://pagespeed.web.dev/analysis?url={encodedUrl}";

        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f4f5; margin: 0; padding: 24px; color: #18181b; }}
    .card {{ max-width: 600px; margin: 0 auto; background: #ffffff; border: 2px solid #18181b; border-radius: 16px; padding: 32px; box-shadow: 4px 4px 0px #18181b; }}
    .badge {{ display: inline-block; background: #fef08a; color: #854d0e; font-size: 11px; font-weight: 800; padding: 4px 12px; border-radius: 9999px; border: 1.5px solid #18181b; text-transform: uppercase; margin-bottom: 12px; }}
    h1 {{ font-size: 22px; font-weight: 800; margin: 0 0 16px 0; color: #18181b; }}
    .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
    .info-table td {{ padding: 12px 14px; border-bottom: 1px solid #e4e4e7; font-size: 14px; }}
    .info-table td.label {{ font-weight: 700; color: #52525b; width: 35%; }}
    .info-table td.value {{ font-weight: 600; color: #09090b; }}
    .btn {{ display: inline-block; background: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 12px 24px; border-radius: 9999px; font-weight: 800; font-size: 13px; text-transform: uppercase; border: 2px solid #18181b; margin-top: 16px; }}
    .footer {{ margin-top: 24px; font-size: 12px; color: #71717a; border-top: 1px solid #e4e4e7; padding-top: 16px; text-align: center; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='badge'>⚡ Lead de Auditoría Técnica</div>
    <h1>Nueva Solicitud de Auditoría en 24h</h1>
    <p style='font-size: 14px; color: #52525b; margin-bottom: 20px;'>
      Un cliente potencial ha completado el formulario de auditoría gratuita en la web.
    </p>

    <table class='info-table'>
      <tr>
        <td class='label'>🌐 Sitio Web:</td>
        <td class='value'><a href='{lead.WebsiteUrl}' target='_blank' style='color: #4f46e5; text-decoration: underline;'>{lead.WebsiteUrl}</a></td>
      </tr>
      <tr>
        <td class='label'>📬 Contacto:</td>
        <td class='value' style='font-size: 15px; color: #15803d;'><strong>{lead.Contact}</strong></td>
      </tr>
      <tr>
        <td class='label'>⏰ Fecha y Hora:</td>
        <td class='value'>{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td>
      </tr>
      <tr>
        <td class='label'>🖥️ IP Visitante:</td>
        <td class='value'>{clientIp ?? "No disponible"}</td>
      </tr>
    </table>

    <div style='text-align: center;'>
      <a href='{pageSpeedUrl}' target='_blank' class='btn'>
        🔍 Analizar en Google PageSpeed Insights &rarr;
      </a>
    </div>

    <div class='footer'>
      MolinazDev &middot; Sistema Automatizado de Captación &middot; contacto@molinazdev.lat
    </div>
  </div>
</body>
</html>";
    }
}
