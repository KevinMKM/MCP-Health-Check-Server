using System.Net;
using System.Net.Sockets;

namespace McpHealthServer.Security;
public class DefaultSsrfPolicy : ISsrfPolicy
{
    private readonly ILogger<DefaultSsrfPolicy> _logger;

    public DefaultSsrfPolicy(ILogger<DefaultSsrfPolicy> logger)
    {
        _logger = logger;
    }

    public bool IsAllowed(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("SSRF check failed: URL is null or empty");
            return false;
        }

        // Parse URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("SSRF check failed: Invalid URL format: {Url}", url);
            return false;
        }

        // Only allow HTTP/HTTPS
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            _logger.LogWarning(
                "SSRF check failed: Unsupported scheme '{Scheme}' in URL: {Url}",
                uri.Scheme,
                url);
            return false;
        }

        // Get host (domain or IP)
        var host = uri.Host;

        // Block localhost variations
        if (IsLocalhost(host))
        {
            _logger.LogWarning(
                "SSRF blocked: Localhost detected in URL: {Url}",
                url);
            return false;
        }

        // Resolve hostname to IP addresses
        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(host);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(
                ex,
                "SSRF check failed: DNS resolution failed for host: {Host}",
                host);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SSRF check failed: Unexpected error during DNS resolution for host: {Host}",
                host);
            return false;
        }

        // Check each resolved IP address
        foreach (var address in addresses)
        {
            if (IsBlockedIpAddress(address))
            {
                _logger.LogWarning(
                    "SSRF blocked: Resolved to blocked IP {IpAddress} for URL: {Url}",
                    address,
                    url);
                return false;
            }
        }

        // All checks passed
        _logger.LogDebug("SSRF check passed for URL: {Url}", url);
        return true;
    }
    
    private static bool IsLocalhost(string host)
    {
        var normalized = host.ToLowerInvariant();

        return normalized == "localhost" ||
               normalized == "localhost.localdomain" ||
               normalized.StartsWith("localhost.", StringComparison.OrdinalIgnoreCase);
    }
    
    private bool IsBlockedIpAddress(IPAddress address)
    {
        // Check for loopback
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        // IPv4 checks
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8 (Private)
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12 (Private)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16 (Private)
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 169.254.0.0/16 (Link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            // 127.0.0.0/8 (Loopback - redundant check for clarity)
            if (bytes[0] == 127)
            {
                return true;
            }

            // 0.0.0.0/8 (Current network)
            if (bytes[0] == 0)
            {
                return true;
            }

            // 100.64.0.0/10 (Shared Address Space / Carrier-grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
            {
                return true;
            }

            // 224.0.0.0/4 (Multicast)
            if (bytes[0] >= 224 && bytes[0] <= 239)
            {
                return true;
            }

            // 240.0.0.0/4 (Reserved)
            if (bytes[0] >= 240)
            {
                return true;
            }
        }

        // IPv6 checks
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();

            // ::1 (Loopback - already checked by IsLoopback)
            // fc00::/7 (Unique local address)
            if ((bytes[0] & 0xfe) == 0xfc)
            {
                return true;
            }

            // fe80::/10 (Link-local)
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
            {
                return true;
            }

            // ff00::/8 (Multicast)
            if (bytes[0] == 0xff)
            {
                return true;
            }

            // ::/128 (Unspecified address)
            if (address.Equals(IPAddress.IPv6Any))
            {
                return true;
            }

            // ::ffff:0:0/96 (IPv4-mapped IPv6 addresses)
            // Check if first 10 bytes are 0, next 2 are 0xff
            if (bytes[10] == 0xff && bytes[11] == 0xff &&
                bytes.Take(10).All(b => b == 0))
            {
                // Extract the IPv4 part and check recursively
                var ipv4Bytes = bytes.Skip(12).Take(4).ToArray();
                var ipv4Address = new IPAddress(ipv4Bytes);
                return IsBlockedIpAddress(ipv4Address);
            }
        }

        return false;
    }
}