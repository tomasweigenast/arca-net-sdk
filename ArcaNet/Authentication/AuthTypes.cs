namespace ArcaNet.Authentication;

/// <summary>
/// Authentication parameters for SOAP requests
/// </summary>
internal sealed record AuthParams(string Token, string Sign, long Cuit);

/// <summary>
/// WSAuthParam wrapper for SOAP requests
/// </summary>
internal sealed record WsAuthParam(AuthParams Auth);

/// <summary>
/// Login credentials from WSAA response
/// </summary>
internal sealed record LoginCredentials(
    LoginTicketHeader Header,
    LoginTicketCredentials Credentials
);

/// <summary>
/// Header from login ticket response
/// </summary>
internal sealed record LoginTicketHeader(
    string? Source,
    string? Destination,
    long UniqueId,
    DateTime GenerationTime,
    DateTime ExpirationTime
);

/// <summary>
/// Credentials from login ticket response
/// </summary>
internal sealed record LoginTicketCredentials(string Token, string Sign);