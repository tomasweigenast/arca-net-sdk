using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ArcaNet.Authentication;

public class FileTicketStorage(string filePath, ILogger? logger = null) : ITicketStorage
{
    private List<AuthTicket>? _tickets;

    public async Task SaveTicketAsync(AuthTicket ticket, CancellationToken cancellationToken = default)
    {
        await EnsureTicketsAsync();
        _tickets!.Clear();
        _tickets.Add(ticket);
        await SaveTicketsAsync();
    }

    public async Task<AuthTicket?> GetTicketAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTicketsAsync();
        return _tickets!.FirstOrDefault();
    }

    public async Task RemoveTicketAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTicketsAsync();
        _tickets!.Clear();
        await SaveTicketsAsync();
    }

    private async Task EnsureTicketsAsync()
    {
        if (_tickets != null) return;

        string? contents = null;
        try
        {
            contents = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException or DirectoryNotFoundException)
                _tickets = [];
        }

        if (contents is { Length: > 0 })
        {
            try
            {
                _tickets = JsonSerializer.Deserialize<List<AuthTicket>>(contents);
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Failed to deserialize tickets from file. Skipping token load.");
                _tickets = [];
            }
        }
        
    }

    private Task SaveTicketsAsync()
    {
        return File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(_tickets));
    }
    
}