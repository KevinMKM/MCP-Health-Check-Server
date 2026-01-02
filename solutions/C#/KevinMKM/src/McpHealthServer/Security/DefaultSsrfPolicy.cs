using System.Net;

namespace McpHealthServer.Security;

public class DefaultSsrfPolicy : ISsrfPolicy
{
    public async Task ValidateAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Invalid URL");

        var ips = await Dns.GetHostAddressesAsync(uri.Host);

        foreach (var ip in ips)
        {
            if (IPAddress.IsLoopback(ip))
                throw new InvalidOperationException("Loopback blocked");

            if (ip.GetAddressBytes()[0] is 10 or 127 or 192)
                throw new InvalidOperationException("Private range blocked");
        }
    }
}