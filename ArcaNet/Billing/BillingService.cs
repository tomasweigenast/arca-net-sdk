using ArcaNet.Authentication;
using ArcaNet.Billing.Enums;
using ArcaNet.Billing.Models;
using ArcaNet.Services;
using ArcaNet.Wsfe;
using Microsoft.Extensions.Logging;

namespace ArcaNet.Billing;

public class BillingService
{
    private readonly ITicketStorage _ticketStorage;
    private readonly ArcaOptions _options;
    private readonly ArcaEnvironment _environment;
    private readonly ILogger? _logger;

    internal BillingService(ArcaOptions options, ArcaEnvironment environment, ILogger? logger = null)
    {
        _options = options;
        _environment = environment;
        _ticketStorage = options.TicketStorage;
        _logger = logger;
    }

    public async Task<InvoiceResult> CreateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        _logger?.LogDebug("Creating invoice: POS={PointOfSale}, Type={Type}, Customer={DocType}:{DocNumber}", 
            invoice.PointOfSale, invoice.Type, invoice.Customer.DocumentType, invoice.Customer.DocumentNumber);
        
        var ticket = await GetValidTicketAsync(ct);
        
        var lastNumber = await GetLastInvoiceNumberAsync(invoice.PointOfSale, invoice.Type, ct);
        var number = lastNumber + 1;
        
        _logger?.LogDebug("Next invoice number: {Number}", number);

        var request = MapToRequest(invoice, number);
        var service = CreateInternalService();
        
        _logger?.LogInformation("Requesting CAE for invoice {Number} (POS={PointOfSale}, Type={Type}, Total={Total})", 
            number, invoice.PointOfSale, invoice.Type, invoice.TotalAmount);
        
        var response = await service.RequestCaeAsync(ticket, request, ct);
        var result = MapToResult(response, number);

        if (result.Approved)
        {
            _logger?.LogInformation("Invoice {Number} approved. CAE={Cae}, Expires={CaeExpiration}", 
                number, result.Cae, result.CaeExpiration);
        }
        else
        {
            _logger?.LogWarning("Invoice {Number} rejected. Errors: {Errors}", 
                number, string.Join("; ", result.Errors.Select(e => $"[{e.Code}] {e.Message}")));
        }

        return result;
    }

    public async Task<int> GetLastInvoiceNumberAsync(int pointOfSale, InvoiceType type, CancellationToken ct = default)
    {
        _logger?.LogDebug("Getting last invoice number for POS={PointOfSale}, Type={Type}", pointOfSale, type);
        
        var ticket = await GetValidTicketAsync(ct);
        var service = CreateInternalService();
        var number = await service.GetLastInvoiceNumberAsync(ticket, pointOfSale, (int)type, ct);
        
        _logger?.LogDebug("Last invoice number: {Number}", number);
        return number;
    }

    public async Task<InvoiceDetail?> GetInvoiceAsync(int pointOfSale, InvoiceType type, long number, CancellationToken ct = default)
    {
        _logger?.LogDebug("Getting invoice: POS={PointOfSale}, Type={Type}, Number={Number}", pointOfSale, type, number);
        
        var ticket = await GetValidTicketAsync(ct);
        var service = CreateInternalService();
        var response = await service.GetInvoiceAsync(ticket, pointOfSale, (int)type, number, ct);

        if (response is null)
        {
            _logger?.LogDebug("Invoice not found");
            return null;
        }

        _logger?.LogDebug("Invoice found: CAE={Cae}, Total={Total}", response.CodAutorizacion, response.ImpTotal);
        return MapToInvoiceDetail(response);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug("Performing WSFE health check");
        
        var service = CreateInternalService();
        var response = await service.HealthCheckAsync(ct);
        var healthy = response is { AppServer: "OK", DbServer: "OK", AuthServer: "OK" };

        if (healthy)
        {
            _logger?.LogDebug("WSFE health check passed");
        }
        else
        {
            _logger?.LogWarning("WSFE health check failed: App={AppServer}, Db={DbServer}, Auth={AuthServer}",
                response.AppServer, response.DbServer, response.AuthServer);
        }

        return healthy;
    }
    
    public async Task<List<PointOfSale>> GetPointsOfSaleAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug("Getting points of sale");
        
        var ticket = await GetValidTicketAsync(ct);
        var service = CreateInternalService();
        var response = await service.GetPointsOfSaleAsync(ticket, ct);

        var result = response?
            .Select(p => new PointOfSale
            {
                Number = p.Nro,
                EmissionType = p.EmisionTipo ?? "",
                Blocked = p.Bloqueado == "S",
                DeactivationDate = string.IsNullOrEmpty(p.FchBaja) ? null : DateTime.ParseExact(p.FchBaja, "yyyyMMdd", null)
            })
            .ToList() ?? [];

        _logger?.LogDebug("Found {Count} points of sale", result.Count);
        return result;
    }

    private async Task<AuthTicket> GetValidTicketAsync(CancellationToken ct)
    {
        var ticket = await _ticketStorage.GetTicketAsync(ct);
        if (ticket is not { IsValid: true })
        {
            _logger?.LogError("No valid authentication ticket available");
            throw new UnauthorizedAccessException("There is no valid ticket to authenticate the request.");
        }
        return ticket;
    }

    private InvoiceService CreateInternalService() 
        => new(_options.ConnectionSettings.Cuit, _environment == ArcaEnvironment.Production);

    private static FECAERequest MapToRequest(Invoice invoice, long number)
    {
        var date = invoice.Date ?? DateTime.Today;

        var detail = new FECAEDetRequest
        {
            Concepto = (int)invoice.Concept,
            DocTipo = (int)invoice.Customer.DocumentType,
            DocNro = invoice.Customer.DocumentNumber,
            CbteDesde = number,
            CbteHasta = number,
            CbteFch = date.ToString("yyyyMMdd"),
            ImpTotal = (double)invoice.TotalAmount,
            ImpNeto = (double)invoice.NetAmount,
            ImpOpEx = (double)invoice.ExemptAmount,
            ImpTrib = (double)invoice.TaxAmount,
            ImpIVA = (double)invoice.Vat.Sum(v => v.Amount),
            ImpTotConc = 0,
            MonId = invoice.Currency.ArcaCode,
            MonCotiz = (double)invoice.ExchangeRate,
            CondicionIVAReceptorId = (int)(invoice.Customer.IvaCondition ?? IvaCondition.ConsumidorFinal)
        };

        if (invoice.Concept != ConceptType.Products)
        {
            detail.FchServDesde = (invoice.ServiceFrom ?? date).ToString("yyyyMMdd");
            detail.FchServHasta = (invoice.ServiceTo ?? date).ToString("yyyyMMdd");
            detail.FchVtoPago = (invoice.PaymentDue ?? date).ToString("yyyyMMdd");
        }

        if (invoice.Vat.Count > 0)
        {
            detail.Iva = invoice.Vat.Select(v => new AlicIva
            {
                Id = (int)v.Rate,
                BaseImp = (double)v.BaseAmount,
                Importe = (double)v.Amount
            }).ToArray();
        }

        if (invoice.AssociatedInvoices is { Count: > 0 })
        {
            detail.CbtesAsoc = invoice.AssociatedInvoices.Select(a => new CbteAsoc
            {
                Tipo = (int)a.Type,
                PtoVta = a.PointOfSale,
                Nro = a.Number,
                Cuit = a.Cuit,
                CbteFch = a.Date?.ToString("yyyyMMdd")
            }).ToArray();
        }

        return new FECAERequest
        {
            FeCabReq = new FECAECabRequest
            {
                CantReg = 1,
                PtoVta = invoice.PointOfSale,
                CbteTipo = (int)invoice.Type
            },
            FeDetReq = [detail]
        };
    }

    private static InvoiceResult MapToResult(FECAEResponse response, long number)
    {
        var cab = response.FeCabResp;
        var det = response.FeDetResp?.FirstOrDefault();
        var approved = det?.Resultado == "A";

        return new InvoiceResult
        {
            Approved = approved,
            PointOfSale = cab?.PtoVta ?? 0,
            Type = (InvoiceType)(cab?.CbteTipo ?? 0),
            InvoiceNumber = number,
            Cae = det?.CAE,
            CaeExpiration = string.IsNullOrEmpty(det?.CAEFchVto) ? null : DateTime.ParseExact(det.CAEFchVto, "yyyyMMdd", null),
            ProcessDate = string.IsNullOrEmpty(cab?.FchProceso) ? null : DateTime.ParseExact(cab.FchProceso, "yyyyMMddHHmmss", null),
            Reprocessed = cab?.Reproceso == "S",
            Errors = response.Errors?.Select(e => new InvoiceError { Code = e.Code, Message = e.Msg ?? "" }).ToList() ?? [],
            Observations = det?.Observaciones?.Select(o => new InvoiceObservation { Code = o.Code, Message = o.Msg ?? "" }).ToList() ?? [],
            Events = response.Events?.Select(e => new InvoiceEvent { Code = e.Code, Message = e.Msg ?? "" }).ToList() ?? []
        };
    }
    
    private static InvoiceDetail MapToInvoiceDetail(FECompConsResponse r)
    {
        return new InvoiceDetail
        {
            PointOfSale = r.PtoVta,
            Type = (InvoiceType)r.CbteTipo,
            Concept = (ConceptType)r.Concepto,
            CustomerDocumentType = (DocumentType)r.DocTipo,
            CustomerDocumentNumber = r.DocNro,
            NumberFrom = r.CbteDesde,
            NumberTo = r.CbteHasta,
            Date = DateTime.ParseExact(r.CbteFch!, "yyyyMMdd", null),
            TotalAmount = (decimal)r.ImpTotal,
            NetAmount = (decimal)r.ImpNeto,
            ExemptAmount = (decimal)r.ImpOpEx,
            TaxAmount = (decimal)r.ImpTrib,
            VatAmount = (decimal)r.ImpIVA,
            Currency = r.MonId == null ? Currency.Ars : new Currency(r.MonId),
            ExchangeRate = (decimal)r.MonCotiz,
            Cae = r.CodAutorizacion,
            CaeExpiration = string.IsNullOrEmpty(r.FchVto) ? null : DateTime.ParseExact(r.FchVto, "yyyyMMdd", null),
            Result = r.Resultado,
            ServiceFrom = string.IsNullOrEmpty(r.FchServDesde) ? null : DateTime.ParseExact(r.FchServDesde, "yyyyMMdd", null),
            ServiceTo = string.IsNullOrEmpty(r.FchServHasta) ? null : DateTime.ParseExact(r.FchServHasta, "yyyyMMdd", null),
            PaymentDue = string.IsNullOrEmpty(r.FchVtoPago) ? null : DateTime.ParseExact(r.FchVtoPago, "yyyyMMdd", null),
            ProcessDate = string.IsNullOrEmpty(r.FchProceso) ? null : DateTime.ParseExact(r.FchProceso, "yyyyMMddHHmmss", null),
            Vat = r.Iva?.Select(v => new VatItem
            {
                Rate = (VatRate)v.Id,
                BaseAmount = (decimal)v.BaseImp,
                Amount = (decimal)v.Importe
            }).ToList() ?? [],
            Observations = r.Observaciones?.Select(o => new InvoiceObservation
            {
                Code = o.Code,
                Message = o.Msg ?? ""
            }).ToList() ?? []
        };
    }
}