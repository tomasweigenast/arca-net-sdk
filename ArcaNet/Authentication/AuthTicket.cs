using System.Text.Json.Serialization;

namespace ArcaNet.Authentication;

/// <summary>
/// Domain entity representing an authentication ticket from ARCA
/// </summary>
public sealed class AuthTicket
{
    /// <summary>
    /// Factory method to create from raw values
    /// </summary>
    internal static AuthTicket Create(
        string token,
        string sign,
        DateTime expirationTime,
        DateTime? generationTime = null,
        string? source = null,
        string? destination = null,
        long uniqueId = 0)
    {
        return new AuthTicket
        {
            Sign = sign,
            Token = token,
            ExpirationUtc = expirationTime
        };
    }

    /// <summary>
    /// Gets the sign from credentials
    /// </summary>
    public required string Sign { get; init; }

    /// <summary>
    /// Gets the token from credentials
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Gets the expiration date (UTC)
    /// </summary>
    public required DateTime ExpirationUtc { get; init; }
    
    /// <summary>
    /// Checks if the ticket is expired (with 5 min buffer)
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= ExpirationUtc.AddMinutes(-5);

    /// <summary>
    /// Checks if the ticket is valid (not expired)
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !IsExpired;
}