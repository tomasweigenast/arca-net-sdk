using System.Security.Cryptography.X509Certificates;

namespace ArcaNet;

public record ArcaConnectionSettings
{
    public required X509Certificate2 Certificate { get; init; }
    
    public required string Cuit { get; init; }
}