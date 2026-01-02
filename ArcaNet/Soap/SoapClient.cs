using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace ArcaNet.Soap;

/// <summary>
/// SOAP client implementation using HttpClient
/// </summary>
internal sealed class SoapClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public SoapClient(HttpClient? httpClient = null, bool useLegacyTls = false)
    {
        _ownsHttpClient = httpClient == null;

        if (httpClient != null)
            _httpClient = httpClient;
        else
        {
            var handler = new SocketsHttpHandler
            {
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = useLegacyTls
                        ? SslProtocols.Tls12 | SslProtocols.Tls13
                        : SslProtocols.Tls13 | SslProtocols.Tls12
                }
            };
            _httpClient = new HttpClient(handler);
        }
    }

    /// <inheritdoc />
    public async Task<string> CallAsync(
        string endpoint,
        string action,
        string body,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/xml")
        };

        if (!string.IsNullOrEmpty(action))
        {
            request.Headers.Add("SOAPAction", action);
        }

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"SOAP request failed: {response.StatusCode} - {responseBody}"
            );
        }

        return responseBody;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}