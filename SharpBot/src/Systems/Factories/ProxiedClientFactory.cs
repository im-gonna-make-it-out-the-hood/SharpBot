using System.Net;
using System.Security.Authentication;

namespace SharpBot.CommandSystem.Commands;

public class ProxiedClientFactory {
    private static volatile ProxyCache _proxyCache = new(ProxyScraper.ScrapeAllProxiesAsync().Result);
    private static SemaphoreSlim semaphoreReinit = new(1);
    public static ProxyCache GetProxies() {
        return _proxyCache;
    }

    private static void RefreshProxyLocked() {
        Thread.Sleep(Random.Shared.Next(0, 1500));
        semaphoreReinit.Wait();   
        while (_proxyCache.Count == 0) {
            _proxyCache = new(ProxyScraper.ScrapeAllProxiesAsync().GetAwaiter().GetResult());
        }

        semaphoreReinit.Release();
    }

    public static void SetProxies(ProxyCache cache) {
        _proxyCache = cache;
    }

    public static HttpClient CreateProxiedClient() {
        if (_proxyCache.Count == 0) {
            RefreshProxyLocked();
        }
        
        var proxy = _proxyCache.GetProxy();

        while (proxy == null) {
            proxy = _proxyCache.GetProxy();
            Thread.Sleep(1000);
        }

        var client = new HttpClient(new HttpClientHandler {
            Proxy = proxy, UseCookies = false, UseProxy = true,
            SslProtocols = SslProtocols.None, ServerCertificateCustomValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true,
        }, true);

        client.Timeout = TimeSpan.FromSeconds(6);
        return client;
    }
    
    public static async Task<HttpClient> CreateProxiedClientAsync() {
        if (_proxyCache.Count == 0) {
            RefreshProxyLocked();
        }
        
        var proxy = await _proxyCache.GetProxyAsync();

        while (proxy == null) {
            proxy = await _proxyCache.GetProxyAsync();
            await Task.Delay(1000);
        }

        var client = new HttpClient(new HttpClientHandler {
            Proxy = proxy, UseCookies = false, UseProxy = true,
            SslProtocols = SslProtocols.None, ServerCertificateCustomValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true,
        }, true);

        client.Timeout = TimeSpan.FromSeconds(6);
        return client;
    }

    public static HttpClient CreateProxiedClient(WebProxy proxy) {
        var client = new HttpClient(new HttpClientHandler {
            Proxy = proxy, UseCookies = false, UseProxy = true,
            SslProtocols = SslProtocols.None, ServerCertificateCustomValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true,
        }, true);
        client.Timeout = TimeSpan.FromSeconds(6);
        return client;
    }
}