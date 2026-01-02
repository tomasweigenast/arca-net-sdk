using ArcaNet.Billing.Enums;

namespace ArcaNet.Billing.Models;

public class InvoiceResult
{
    public required bool Approved { get; init; }
    public required int PointOfSale { get; init; }
    public required InvoiceType Type { get; init; }
    public required long InvoiceNumber { get; init; }
    public string? Cae { get; init; }
    public DateTime? CaeExpiration { get; init; }
    public DateTime? ProcessDate { get; init; }
    public bool Reprocessed { get; init; }
    public List<InvoiceError> Errors { get; init; } = [];
    public List<InvoiceObservation> Observations { get; init; } = [];
    public List<InvoiceEvent> Events { get; init; } = [];
}

public class InvoiceError
{
    public required int Code { get; init; }
    public required string Message { get; init; }
}

public class InvoiceObservation
{
    public required int Code { get; init; }
    public required string Message { get; init; }
}

public class InvoiceEvent
{
    public required int Code { get; init; }
    public required string Message { get; init; }
}