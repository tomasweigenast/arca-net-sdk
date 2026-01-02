namespace ArcaNet.Authentication;

/// <summary>
/// Domain entity representing an authentication ticket from ARCA
/// </summary>
public sealed class AuthTicket
{
    private readonly LoginTicketHeader _header;
    private readonly LoginTicketCredentials _credentials;

    private AuthTicket(LoginTicketHeader header, LoginTicketCredentials credentials)
    {
        _header = header;
        _credentials = credentials;
        Validate();
    }

    /// <summary>
    /// Factory method to create an AccessTicket
    /// </summary>
    internal static AuthTicket Create(LoginCredentials data) =>
        new(data.Header, data.Credentials);

    /// <summary>
    /// Factory method to create from raw values
    /// </summary>
    public static AuthTicket Create(
        string token,
        string sign,
        DateTime expirationTime,
        DateTime? generationTime = null,
        string? source = null,
        string? destination = null,
        long uniqueId = 0)
    {
        var header = new LoginTicketHeader(
            source,
            destination,
            uniqueId,
            generationTime ?? DateTime.UtcNow,
            expirationTime
        );
        var credentials = new LoginTicketCredentials(token, sign);
        return new AuthTicket(header, credentials);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(_credentials.Token))
            throw new InvalidOperationException("Token is required");

        if (string.IsNullOrWhiteSpace(_credentials.Sign))
            throw new InvalidOperationException("Sign is required");

        if (_header.ExpirationTime == default)
            throw new InvalidOperationException("Expiration time is required");
    }

    /// <summary>
    /// Gets the sign from credentials
    /// </summary>
    public string Sign => _credentials.Sign;

    /// <summary>
    /// Gets the token from credentials
    /// </summary>
    public string Token => _credentials.Token;

    /// <summary>
    /// Gets the expiration date (UTC)
    /// </summary>
    public DateTime ExpirationUtc => _header.ExpirationTime.ToUniversalTime();

    /// <summary>
    /// Gets the header
    /// </summary>
    internal LoginTicketHeader Header => _header;

    /// <summary>
    /// Gets the credentials
    /// </summary>
    internal LoginTicketCredentials Credentials => _credentials;

    /// <summary>
    /// Converts to LoginCredentials for storage/reuse
    /// </summary>
    internal LoginCredentials ToLoginCredentials() => new(_header, _credentials);

    /// <summary>
    /// Formats the ticket for SOAP authentication
    /// </summary>
    internal WsAuthParam GetWsAuthFormat(long cuit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cuit);
        return new WsAuthParam(new AuthParams(Token, Sign, cuit));
    }

    /// <summary>
    /// Checks if the ticket is expired (with 5 min buffer)
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpirationUtc.AddMinutes(-5);

    /// <summary>
    /// Checks if the ticket is valid (not expired)
    /// </summary>
    public bool IsValid => !IsExpired;

    /// <summary>
    /// Gets time remaining until expiration
    /// </summary>
    public TimeSpan TimeUntilExpiration =>
        IsExpired ? TimeSpan.Zero : ExpirationUtc - DateTime.UtcNow;
}