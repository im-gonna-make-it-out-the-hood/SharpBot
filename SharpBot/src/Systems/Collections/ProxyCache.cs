using System.Net;
using SharpBot.Systems.Collections;
using SharpBot.Systems.Extensions;
using SharpBot.Systems.ProxyChecker;
using SharpBot.Systems.ProxyScrapers;

namespace SharpBot;

public class ProxyCache {
    public ProxyCache(List<WebProxy> proxies) {
        Proxies = new ConcurrentList<WebProxy>(proxies);
    }
    private ConcurrentList<WebProxy> Proxies { get; }

    /// <summary>
    ///     The amount of proxies available in this cache.
    /// </summary>
    public int Count => Proxies.Count;

    /// <summary>
    ///     Verifies if all the proxies are valid, will remove the ones that are NOT.
    /// </summary>
    public async Task VerifyAllProxiesAsync() {
        Dictionary<Task<ProxyResult>, WebProxy> resultList = new(Proxies.Count);
        foreach (var proxy in Proxies.ToList()) // We modify the collection, MUST clone first!
            resultList.Add(Task.Run(() => proxy.VerifyAsync()), proxy);

        while (resultList.Count > 0) {
            Console.WriteLine(resultList.Count);
            var anyCompleted = await Task.WhenAny(resultList.Keys);
            resultList.Remove(anyCompleted, out var proxy);

            if ((await anyCompleted).isSuccess) continue;

            while (!await Proxies.TryRemoveAsync(proxy)) await Task.Delay(100);
        }
    }

    /// <summary>
    ///     Removes one proxy from the list and returns it as a <see cref="WebProxy" />
    /// </summary>
    /// <returns> A Nullable WebProxy that represents the obtained proxy, will be Null if there are no proxies left. </returns>
    public WebProxy? GetProxy() {
        if (Proxies.Count == 0) return null;

        WebProxy? proxy;
        
        while (!Proxies.TryGetRandomElement(true, out proxy)) {
            Thread.Sleep(100);
        }

        return proxy;
    }

    /// <summary>
    ///     Removes one proxy from the list and returns it as a <see cref="WebProxy" />
    /// </summary>
    /// <returns> A Nullable WebProxy that represents the obtained proxy, will be Null if there are no proxies left. </returns>
    public async Task<WebProxy?> GetProxyAsync() {
        if (Proxies.Count == 0) return null;

        WebProxy? proxy;
        
        while (!Proxies.TryGetRandomElement(true, out proxy)) {
            await Task.Delay(100);
        }
        return proxy;
    }
}