namespace ArcaNet.Billing.Enums;

public record Currency(string ArcaCode)
{
    public static readonly Currency Ars = new("PES");
    public static readonly Currency Usd = new("DOL");
    public static readonly Currency Eur = new("060");
}