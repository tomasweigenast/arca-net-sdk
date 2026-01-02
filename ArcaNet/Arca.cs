using ArcaNet.Billing;
using ArcaNet.Services;

namespace ArcaNet;

public class ArcaClient(ArcaOptions options, ArcaEnvironment environment)
{
    private readonly AuthService _authService = new();

    public BillingService Billing { get; } = new(options, environment);

    /// <summary>
    /// Authenticates the client against ARCA and saves the ticket.
    /// This is a no-op if the ticket is already valid.
    /// </summary>
    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var currentTicket = await options.TicketStorage.GetTicketAsync(cancellationToken);
        if (currentTicket is { IsValid: true })
            return;
        
        var ticket = await _authService.GetTicketAsync(
            options.ConnectionSettings.Certificate,
            environment == ArcaEnvironment.Production,
            ct: cancellationToken);
        
        await options.TicketStorage.SaveTicketAsync(ticket,  cancellationToken: cancellationToken);
    }
}