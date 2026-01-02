namespace ArcaNet.Authentication;

public interface ITicketStorage
{
    public Task SaveTicketAsync(AuthTicket ticket, CancellationToken cancellationToken = default);
    
    public Task<AuthTicket?> GetTicketAsync(CancellationToken cancellationToken = default);
    
    public Task RemoveTicketAsync(CancellationToken cancellationToken = default);
}