using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Bob.Networking
{
    /// <summary>
    /// Builds HTTP handlers for fetching user provided URLs without letting them reach Bob's own network.
    /// </summary>
    public static class SafeHttp
    {
        /// <summary>
        /// Creates a handler that refuses to connect to loopback, private, link-local, or otherwise internal addresses.
        /// The check runs on the resolved IP at connect time, so it also covers redirects and domains that point inward.
        /// </summary>
        /// <param name="allowAutoRedirect">Whether the handler should follow redirects on its own.</param>
        /// <returns>A handler that only connects to public addresses.</returns>
        public static SocketsHttpHandler CreateHandler(bool allowAutoRedirect = true)
        {
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = allowAutoRedirect,
                UseProxy = false,
                ConnectCallback = ConnectToPublicAddressAsync
            };
        }

        private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);

            // Connect to the exact address that was checked so a second DNS lookup can't swap it for an internal one.
            IPAddress address = addresses.FirstOrDefault(a => !IsInternal(a))
                ?? throw new HttpRequestException("Bob is not allowed to visit local or internal addresses.");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Checks if a host is localhost or an internal IP address. Domains are not resolved, use <see cref="CreateHandler"/> for that.
        /// </summary>
        /// <param name="host">The host name or IP address.</param>
        /// <returns>True if the host is obviously internal.</returns>
        public static bool IsInternalHost(string host)
        {
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IPAddress.TryParse(host.Trim('[', ']'), out IPAddress address) && IsInternal(address);
        }

        /// <summary>
        /// Checks if an IP address is not on the public internet.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <returns>True for loopback, private, link-local, shared, reserved, and multicast addresses.</returns>
        public static bool IsInternal(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = address.GetAddressBytes();

                return b[0] == 0                                   // 0.0.0.0/8 "this network"
                    || b[0] == 10                                  // 10.0.0.0/8 private (Railway internal)
                    || b[0] == 127                                 // 127.0.0.0/8 loopback
                    || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)  // 100.64.0.0/10 carrier-grade NAT
                    || (b[0] == 169 && b[1] == 254)                // 169.254.0.0/16 link-local / cloud metadata
                    || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)   // 172.16.0.0/12 private
                    || (b[0] == 192 && b[1] == 0 && b[2] == 0)     // 192.0.0.0/24 protocol assignments
                    || (b[0] == 192 && b[1] == 168)                // 192.168.0.0/16 private
                    || (b[0] == 198 && (b[1] == 18 || b[1] == 19)) // 198.18.0.0/15 benchmarking
                    || b[0] >= 224;                                // multicast and reserved
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] b = address.GetAddressBytes();

                // 64:ff9b::/96 NAT64 embeds an IPv4 address in the last 4 bytes.
                if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b && b[4..12].All(x => x == 0))
                {
                    return IsInternal(new IPAddress(b[12..]));
                }

                return IPAddress.IsLoopback(address)
                    || address.Equals(IPAddress.IPv6Any)
                    || address.IsIPv6LinkLocal
                    || address.IsIPv6SiteLocal
                    || address.IsIPv6Multicast
                    || (b[0] & 0xfe) == 0xfc; // fc00::/7 unique local
            }

            return true;
        }
    }
}
