namespace DigitalServices.Application.Leads;

public sealed record AuditLeadRequestDto(
    string WebsiteUrl,
    string Contact
);

public sealed record AuditLeadResponseDto(
    bool Success,
    string Message
);
