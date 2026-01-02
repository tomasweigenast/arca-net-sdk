namespace ArcaNet.Billing.Models;

public class PointOfSale
{
    public required int Number { get; init; }
    public required string EmissionType { get; init; }
    public required bool Blocked { get; init; }
    public DateTime? DeactivationDate { get; init; }
}