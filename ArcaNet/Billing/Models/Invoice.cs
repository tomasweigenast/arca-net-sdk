using ArcaNet.Billing.Enums;

namespace ArcaNet.Billing.Models;

public class Invoice
{
    public required int PointOfSale { get; init; }
    public required InvoiceType Type { get; init; }
    public required ConceptType Concept { get; init; }
    public required Customer Customer { get; init; }
    public required decimal NetAmount { get; init; }
    public decimal ExemptAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public Currency Currency { get; init; } = Currency.Ars;
    public decimal ExchangeRate { get; init; } = 1;
    public DateTime? Date { get; init; }
    public DateTime? ServiceFrom { get; init; }
    public DateTime? ServiceTo { get; init; }
    public DateTime? PaymentDue { get; init; }
    public List<VatItem> Vat { get; init; } = [];
    public List<AssociatedInvoice>? AssociatedInvoices { get; init; }
}

public class Customer
{
    public required DocumentType DocumentType { get; init; }
    public required long DocumentNumber { get; init; }
    public IvaCondition? IvaCondition { get; init; }
}

public class VatItem
{
    public required VatRate Rate { get; init; }
    public required decimal BaseAmount { get; init; }
    public required decimal Amount { get; init; }
}

public class AssociatedInvoice
{
    public required InvoiceType Type { get; init; }
    public required int PointOfSale { get; init; }
    public required long Number { get; init; }
    public string? Cuit { get; init; }
    public DateTime? Date { get; init; }
}