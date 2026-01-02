using ArcaNet.Billing.Enums;

namespace ArcaNet.Billing.Models;

public class InvoiceDetail
{
    public required int PointOfSale { get; init; }
    public required InvoiceType Type { get; init; }
    public required ConceptType Concept { get; init; }
    public required DocumentType CustomerDocumentType { get; init; }
    public required long CustomerDocumentNumber { get; init; }
    public required long NumberFrom { get; init; }
    public required long NumberTo { get; init; }
    public required DateTime Date { get; init; }
    public required decimal TotalAmount { get; init; }
    public required decimal NetAmount { get; init; }
    public required decimal ExemptAmount { get; init; }
    public required decimal TaxAmount { get; init; }
    public required decimal VatAmount { get; init; }
    public required Currency Currency { get; init; }
    public required decimal ExchangeRate { get; init; }
    public string? Cae { get; init; }
    public DateTime? CaeExpiration { get; init; }
    public string? Result { get; init; }
    public DateTime? ServiceFrom { get; init; }
    public DateTime? ServiceTo { get; init; }
    public DateTime? PaymentDue { get; init; }
    public DateTime? ProcessDate { get; init; }
    public List<VatItem> Vat { get; init; } = [];
    public List<InvoiceObservation> Observations { get; init; } = [];
}