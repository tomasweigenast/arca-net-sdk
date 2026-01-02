using ArcaNet.Authentication;
using ArcaNet.Wsfe;

namespace ArcaNet.Services;

internal class InvoiceService(string cuit, bool production = false)
{
    private readonly string _wsfeEndpoint = production ? Endpoints.WsfeProd : Endpoints.WsfeTest;

    private ServiceSoapClient CreateClient() => new(
        ServiceSoapClient.EndpointConfiguration.ServiceSoap,
        _wsfeEndpoint);

    private FEAuthRequest CreateAuth(AuthTicket ticket) => new()
    {
        Token = ticket.Token,
        Sign = ticket.Sign,
        Cuit = long.Parse(cuit)
    };

    public async Task<int> GetLastInvoiceNumberAsync(
        AuthTicket ticket,
        int ptoVta,
        int cbteTipo,
        CancellationToken ct = default)
    {
        var client = CreateClient();
        try
        {
            var response = await client.FECompUltimoAutorizadoAsync(
                CreateAuth(ticket), ptoVta, cbteTipo);
            return response.Body.FECompUltimoAutorizadoResult.CbteNro;
        }
        finally
        {
            await client.CloseAsync();
        }
    }

    public async Task<FECAEResponse> RequestCaeAsync(
        AuthTicket ticket,
        FECAERequest request,
        CancellationToken ct = default)
    {
        var client = CreateClient();
        try
        {
            var response = await client.FECAESolicitarAsync(CreateAuth(ticket), request);
            return response.Body.FECAESolicitarResult;
        }
        finally
        {
            await client.CloseAsync();
        }
    }

    public async Task<FECompConsResponse?> GetInvoiceAsync(
        AuthTicket ticket,
        int ptoVta,
        int cbteTipo,
        long cbteNro,
        CancellationToken ct = default)
    {
        var client = CreateClient();
        try
        {
            var response = await client.FECompConsultarAsync(
                CreateAuth(ticket),
                new FECompConsultaReq { PtoVta = ptoVta, CbteTipo = cbteTipo, CbteNro = cbteNro });
            return response.Body.FECompConsultarResult.ResultGet;
        }
        finally
        {
            await client.CloseAsync();
        }
    }

    public async Task<DummyResponse> HealthCheckAsync(CancellationToken ct = default)
    {
        var client = CreateClient();
        try
        {
            var response = await client.FEDummyAsync();
            return response.Body.FEDummyResult;
        }
        finally
        {
            await client.CloseAsync();
        }
    }
    
    public async Task<PtoVenta[]?> GetPointsOfSaleAsync(AuthTicket ticket, CancellationToken ct = default)
    {
        var client = CreateClient();
        try
        {
            var response = await client.FEParamGetPtosVentaAsync(CreateAuth(ticket));
            return response.Body.FEParamGetPtosVentaResult.ResultGet;
        }
        finally
        {
            await client.CloseAsync();
        }
    }
}