using System.Net;
using System.Net.Sockets;

namespace McpHealthServer.Security;

public interface ISsrfPolicy
{
    bool IsAllowed(string url);
}