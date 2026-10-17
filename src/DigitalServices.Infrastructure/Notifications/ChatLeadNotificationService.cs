using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DigitalServices.Application.Chatbot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Notifications;

public sealed class ChatLeadNotificationService(
    IOptions<NotificationOptions> options,
    HttpClient httpClient,
    ILogger<ChatLeadNotificationService> logger) : IChatLeadNotificationService
{
    private readonly NotificationOptions _options = options.Value;

    public async Task NotifyChatLeadAsync(
        ChatLeadNotificationDto lead,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);

        var recipientEmail = string.IsNullOrWhiteSpace(_options.AdminAlert.AdminEmail)
            ? "molinazdevpe@gmail.com"
            : _options.AdminAlert.AdminEmail;

        logger.LogInformation(
            "⚡ [CHAT LEAD CAPTURED] Contact={Contact}, IP={ClientIp}, Message={LastMessage}",
            lead.Contact,
            lead.ClientIp ?? "Unknown",
            lead.LastUserMessage);

        var emailOpts = _options.Email;
        var smtpHost = !string.IsNullOrWhiteSpace(emailOpts.SmtpHost)
            ? emailOpts.SmtpHost
            : Environment.GetEnvironmentVariable("SMTP_HOST");
        var smtpUsername = !string.IsNullOrWhiteSpace(emailOpts.SmtpUsername)
            ? emailOpts.SmtpUsername
            : Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? Environment.GetEnvironmentVariable("SMTP_USER");
        var smtpPassword = !string.IsNullOrWhiteSpace(emailOpts.SmtpPassword)
            ? emailOpts.SmtpPassword
            : Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? Environment.GetEnvironmentVariable("SMTP_PASS");

        var isEmailConfigured = emailOpts.Enabled || (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpPassword));

        // 1. Send Email Notification to Admin (molinazdevpe@gmail.com)
        if (isEmailConfigured && !string.IsNullOrWhiteSpace(smtpHost))
        {
            try
            {
                await SendAdminChatLeadEmailAsync(lead, recipientEmail, smtpHost, smtpUsername ?? "", smtpPassword ?? "", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send chat lead email notification to {RecipientEmail}.", recipientEmail);
            }
        }
        else
        {
            logger.LogInformation(
                "Chat lead email to {RecipientEmail} skipped because SMTP is disabled or password is empty. Contact captured: {Contact}",
                recipientEmail,
                lead.Contact);
        }

        // 2. Send Discord / Telegram alerts if configured
        if (_options.AdminAlert.Enabled)
        {
            try
            {
                await SendAdminInstantAlertsAsync(lead, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send instant admin alert (Discord/Telegram) for chat lead.");
            }
        }
    }

    private async Task SendAdminChatLeadEmailAsync(
        ChatLeadNotificationDto lead,
        string recipientEmail,
        string smtpHost,
        string smtpUsername,
        string smtpPassword,
        CancellationToken cancellationToken)
    {
        var emailOpts = _options.Email;
        var port = emailOpts.SmtpPort > 0 ? emailOpts.SmtpPort : 587;

        using var smtpClient = new SmtpClient(smtpHost, port)
        {
            EnableSsl = emailOpts.EnableSsl,
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            Timeout = 10000
        };

        var fromAddress = new MailAddress(
            string.IsNullOrWhiteSpace(emailOpts.FromAddress) ? "molinazdevpe@gmail.com" : emailOpts.FromAddress,
            string.IsNullOrWhiteSpace(emailOpts.FromName) ? "MolinazDev AI" : emailOpts.FromName);

        var toAddress = new MailAddress(recipientEmail, "MolinazDev Admin");

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = $"🚀 [Lead Chatbot] Nuevo Contacto: {lead.Contact}",
            Body = BuildChatLeadEmailHtml(lead, recipientEmail),
            IsBodyHtml = true
        };

        await smtpClient.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Chat lead email successfully sent to {RecipientEmail} for contact {Contact}.", recipientEmail, lead.Contact);
    }

    private async Task SendAdminInstantAlertsAsync(
        ChatLeadNotificationDto lead,
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
                        title = "🚀 ¡Nuevo Prospecto Capturado en el Chatbot!",
                        description = "Un cliente ha dejado su dato de contacto en el simulador de chat de la web.",
                        color = 49151, // Indigo/Emerald color
                        fields = new[]
                        {
                            new { name = "📞 Contacto", value = lead.Contact, inline = false },
                            new { name = "💬 Último Mensaje", value = lead.LastUserMessage, inline = false },
                            new { name = "📲 WhatsApp Directo", value = lead.WhatsAppUrl ?? "N/A", inline = false },
                            new { name = "🖥️ IP Cliente", value = lead.ClientIp ?? "No disponible", inline = true },
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
            var text = $"🚀 *¡Nuevo Prospecto Capturado en el Chatbot!*\n\n" +
                       $"📞 *Contacto:* {lead.Contact}\n" +
                       $"💬 *Mensaje:* {lead.LastUserMessage}\n" +
                       (lead.WhatsAppUrl != null ? $"📲 [Abrir Chat de WhatsApp]({lead.WhatsAppUrl})\n" : "") +
                       $"🖥️ *IP:* `{lead.ClientIp ?? "No disponible"}`\n" +
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

    private static string BuildChatLeadEmailHtml(ChatLeadNotificationDto lead, string recipientEmail)
    {
        var conversationRows = new StringBuilder();
        if (lead.History is { Count: > 0 })
        {
            foreach (var msg in lead.History)
            {
                var isUser = msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase);
                var roleLabel = isUser ? "Visitante" : "MolinazDev AI";
                var bg = isUser ? "#18181b" : "#f4f4f5";
                var textColor = isUser ? "#ffffff" : "#18181b";
                var align = isUser ? "right" : "left";

                conversationRows.Append($@"
                <div style='margin-bottom: 8px; text-align: {align};'>
                  <div style='display: inline-block; max-width: 85%; background: {bg}; color: {textColor}; padding: 10px 14px; border-radius: 12px; font-size: 13px; text-align: left; line-height: 1.4; border: 1px solid #18181b;'>
                    <strong style='font-size: 11px; opacity: 0.8; display: block; margin-bottom: 4px;'>{roleLabel}:</strong>
                    {WebUtility.HtmlEncode(msg.Content)}
                  </div>
                </div>");
            }
        }

        // Add the last user message to the transcript
        conversationRows.Append($@"
        <div style='margin-bottom: 8px; text-align: right;'>
          <div style='display: inline-block; max-width: 85%; background: #15803d; color: #ffffff; padding: 10px 14px; border-radius: 12px; font-size: 13px; text-align: left; line-height: 1.4; border: 1px solid #18181b;'>
            <strong style='font-size: 11px; opacity: 0.9; display: block; margin-bottom: 4px;'>Visitante (Dato Detectado):</strong>
            {WebUtility.HtmlEncode(lead.LastUserMessage)}
          </div>
        </div>");

        var ctaButton = !string.IsNullOrWhiteSpace(lead.WhatsAppUrl)
            ? $@"<a href='{lead.WhatsAppUrl}' target='_blank' class='btn-wa' style='display: inline-block; background: #25D366; color: #ffffff !important; text-decoration: none; padding: 14px 28px; border-radius: 9999px; font-weight: 800; font-size: 14px; text-transform: uppercase; border: 2px solid #18181b; box-shadow: 3px 3px 0px #18181b;'>
                  💬 Escribir al Cliente por WhatsApp &rarr;
                </a>"
            : $@"<a href='mailto:{lead.Contact}' class='btn-wa' style='display: inline-block; background: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 14px 28px; border-radius: 9999px; font-weight: 800; font-size: 14px; text-transform: uppercase; border: 2px solid #18181b; box-shadow: 3px 3px 0px #18181b;'>
                  ✉️ Responder por Correo al Cliente &rarr;
                </a>";

        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f4f5; margin: 0; padding: 24px; color: #18181b; }}
    .card {{ max-width: 620px; margin: 0 auto; background: #ffffff; border: 2px solid #18181b; border-radius: 16px; padding: 32px; box-shadow: 4px 4px 0px #18181b; }}
    .badge {{ display: inline-block; background: #dcfce7; color: #166534; font-size: 11px; font-weight: 800; padding: 4px 12px; border-radius: 9999px; border: 1.5px solid #18181b; text-transform: uppercase; margin-bottom: 12px; }}
    h1 {{ font-size: 22px; font-weight: 800; margin: 0 0 8px 0; color: #18181b; }}
    .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
    .info-table td {{ padding: 10px 12px; border-bottom: 1px solid #e4e4e7; font-size: 14px; }}
    .info-table td.label {{ font-weight: 700; color: #52525b; width: 35%; }}
    .info-table td.value {{ font-weight: 600; color: #09090b; }}
    .chat-box {{ background: #fafafa; border: 2px solid #e4e4e7; border-radius: 12px; padding: 16px; margin: 20px 0; }}
    .footer {{ margin-top: 24px; font-size: 12px; color: #71717a; border-top: 1px solid #e4e4e7; padding-top: 16px; text-align: center; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='badge'>🚀 Lead Capturado por Chatbot IA</div>
    <h1>¡Nuevo Prospecto Interesado!</h1>
    <p style='font-size: 14px; color: #52525b; margin-bottom: 16px;'>
      Un visitante ha dejado sus datos de contacto en el chat de MolinazDev para coordinar una asesoría o cotización.
    </p>

    <table class='info-table'>
      <tr>
        <td class='label'>📞 Contacto:</td>
        <td class='value' style='font-size: 16px; color: #15803d;'><strong>{lead.Contact}</strong></td>
      </tr>
      <tr>
        <td class='label'>💬 Mensaje:</td>
        <td class='value'><em>""{WebUtility.HtmlEncode(lead.LastUserMessage)}""</em></td>
      </tr>
      <tr>
        <td class='label'>⏰ Fecha y Hora:</td>
        <td class='value'>{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td>
      </tr>
      <tr>
        <td class='label'>🖥️ IP Visitante:</td>
        <td class='value'>{lead.ClientIp ?? "No disponible"}</td>
      </tr>
    </table>

    <div style='text-align: center; margin: 24px 0;'>
      {ctaButton}
    </div>

    <div class='chat-box'>
      <div style='font-size: 12px; font-weight: 800; text-transform: uppercase; color: #52525b; margin-bottom: 12px;'>
        📜 Historial de la Conversación:
      </div>
      {conversationRows}
    </div>

    <div class='footer'>
      MolinazDev &middot; Notificaciones Automáticas de Chat &middot; {recipientEmail}
    </div>
  </div>
</body>
</html>";
    }
}
