using ArcaNet.Authentication;

namespace ArcaNet;

public class ArcaOptions
{
    public required ArcaConnectionSettings ConnectionSettings { get; init; }
    
    public required ITicketStorage TicketStorage { get; init; }
}