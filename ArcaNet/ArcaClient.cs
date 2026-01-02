using ArcaNet.Billing;
using ArcaNet.Services;
using Microsoft.Extensions.Logging;

namespace ArcaNet;

public class ArcaClient(ArcaOptions options, ArcaEnvironment environment, ILogger? logger = null)
{
    private readonly AuthService _authService = new();

    public BillingService Billing { get; } = new(options, environment, logger);

    /// <summary>
    /// Authenticates the client against ARCA and saves the ticket.
    /// This is a no-op if the ticket is already valid.
    /// </summary>
    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        logger?.LogDebug("Checking for existing valid ticket");
        
        var currentTicket = await options.TicketStorage.GetTicketAsync(cancellationToken);
        if (currentTicket is { IsValid: true })
        {
            logger?.LogDebug("Valid ticket found, expires at {Expiration}", currentTicket.ExpirationUtc);
            return;
        }
        
        logger?.LogInformation("No valid ticket found, authenticating against ARCA ({Environment})", 
            environment == ArcaEnvironment.Production ? "Production" : "Homologación");
        
        var ticket = await _authService.GetTicketAsync(
            options.ConnectionSettings.Certificate,
            environment == ArcaEnvironment.Production,
            ct: cancellationToken);
        
        logger?.LogInformation("Authentication successful, ticket expires at {Expiration}", ticket.ExpirationUtc);
        logger?.LogDebug("Saving ticket to storage");
        
        await options.TicketStorage.SaveTicketAsync(ticket, cancellationToken: cancellationToken);
        
        logger?.LogDebug("Ticket saved successfully");
    }
}