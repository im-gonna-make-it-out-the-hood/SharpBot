using System.Net;
using System.Net.Sockets;
using SharpBot.Systems.ProxyChecker;

namespace SharpBot.Systems.Extensions;

public static class WebProxy_Extensions {
    public static async Task<ProxyResult> VerifyAsync(this WebProxy proxy) {
        CancellationTokenSource cts = new(2000);
        Socket sock = new(SocketType.Stream, ProtocolType.Tcp);
        var host = proxy.Address.Host;
        var port = proxy.Address.Port;
        try {
            await sock.ConnectAsync(new IPEndPoint(IPAddress.Parse(proxy.Address.Host), proxy.Address.Port), cts.Token);
            //Console.WriteLine($"[Proxy Checker] Send CONNECT request to {host}:{port}");
            await sock.SendAsync("CONNECT http://google.com/ HTTP/1.1\n"u8.ToArray());

            //Console.WriteLine("[Proxy Checker] Waiting for a response from the proxy... (3 seconds)");

            var buf = new byte[1024];
            sock.ReceiveTimeout = 2000; // 5 Seconds.
            var read = sock.Receive(buf, 0, buf.Length, SocketFlags.None, out var error);

            if (read == 0 && error == SocketError.TimedOut) {
                //Console.WriteLine("[Proxy Checker] Proxy didn't respond! Cleaning up...");
                await sock.DisconnectAsync(false);
                return new ProxyResult(host, port, false);
            }

            //Console.WriteLine("[Proxy Checker] Check passed! Cleaning up...");
            await sock.DisconnectAsync(false);
            return new ProxyResult(host, port, true);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) {
            //Console.WriteLine($"[Proxy Checker] Proxy timed out! {host}:{port}");
        }
        catch (FormatException) {
            Console.WriteLine($"[Proxy Checker] The host was malformed! {host}:{port}");
        }
        catch (SocketException ex) {
            //Console.WriteLine($"[Proxy Checker] The proxy likely refused the connection! {host}:{port}");
        }
        finally {
            sock.Dispose();
        }

        return new ProxyResult(host, port, false);
    }
}