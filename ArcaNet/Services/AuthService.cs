using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using ArcaNet.Authentication;
using ArcaNet.Wsaa;

namespace ArcaNet.Services;

internal class AuthService
{
    public async Task<AuthTicket> GetTicketAsync(
        X509Certificate2 certificate,
        bool isProduction,
        string service = "wsfe",
        CancellationToken ct = default)
    {
        // build and sign TRA
        var tra = BuildTra(service);
        var cms = SignCms(tra, certificate);

        // call WSAA
        var endpoint = isProduction ? Endpoints.WsaaProd : Endpoints.WsaaTest;
        var client = new LoginCMSClient(LoginCMSClient.EndpointConfiguration.LoginCms, endpoint);

        try
        {
            var response = await client.loginCmsAsync(cms);
            var ticket = ParseLoginResponse(response.loginCmsReturn);

            return ticket;
        }
        finally
        {
            await client.CloseAsync();
        }
    }

    private static string BuildTra(string service)
    {
        var now = DateTime.UtcNow;
        return new XDocument(
            new XElement("loginTicketRequest",
                new XAttribute("version", "1.0"),
                new XElement("header",
                    new XElement("uniqueId", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    new XElement("generationTime", now.AddMinutes(-10).ToString("o")),
                    new XElement("expirationTime", now.AddMinutes(10).ToString("o"))),
                new XElement("service", service))
        ).ToString(SaveOptions.DisableFormatting);
    }

    private static string SignCms(string tra, X509Certificate2 cert)
    {
        var content = new ContentInfo(Encoding.UTF8.GetBytes(tra));
        var cms = new SignedCms(content);
        cms.ComputeSignature(new CmsSigner(cert));
        return Convert.ToBase64String(cms.Encode());
    }

    private static AuthTicket ParseLoginResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var header = doc.Descendants("header").First();
        var creds = doc.Descendants("credentials").First();

        return AuthTicket.Create(
            token: creds.Element("token")!.Value,
            sign: creds.Element("sign")!.Value,
            expirationTime: DateTime.Parse(header.Element("expirationTime")!.Value),
            generationTime: DateTime.Parse(header.Element("generationTime")!.Value),
            source: header.Element("source")?.Value,
            destination: header.Element("destination")?.Value,
            uniqueId: long.Parse(header.Element("uniqueId")!.Value));
    }
}