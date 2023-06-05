using System.Net;
using System.Text;
using System.Text.Json;
using SharpBot.Systems.ProxyScrapers;

namespace SharpBot.CommandSystem.Commands;

internal class ProxyScraper {
    public static async Task<List<WebProxy>> ScrapeAllProxiesAsync() {
        var proxies = new List<WebProxy>();
        // StringBuilder builder = new(21); // 21 == max proxy (IP) length.

        CancellationTokenSource tokenSource = new(5000);

        try { //! Scrape FreeProxyLists
            var scrapedProxies = await FreeProxyListsScraper.GetProxiesAsync(tokenSource.Token);

            proxies.AddRange(IntoWebProxy(scrapedProxies));
        }
        catch {
            Console.WriteLine("Failure contacting https://free-proxy-list.net/");
        }

        try { //! Scrape NucleusVPN proxies.
            var scrapedProxies = (await NucleousVPNProxies.GetProxies()).proxy_list;


            foreach (var proxyClass in scrapedProxies) {
                var proxy = IntoWebProxy(proxyClass.host);
                if (proxy != null)
                    proxies.Add(proxy);
            }
        }
        catch {
            Console.WriteLine("Failure contacting https://api.nucleusvpn.com/api/proxy");
        }

        if (!tokenSource.TryReset()) tokenSource = new CancellationTokenSource(5000);
        else tokenSource.CancelAfter(5000);

        { //! Use Bongo's Proxy API.
            const string Bongo_APIURL =
                "https://bongoapi.hxhadventure.repl.co/api/scrapeProxies?username=username&key=BongoAPIKey[6004d030e68c657609160a4e049201a85e4b87459048e908337799699319dadfcb944a28b0fcae5f8c30e18ed4a3ea49fb413ccbe73f2195ec3b9ae7e15c04988752fd9ad6dd3fa00dce751be6029809c4ef68f4ee8c5af5fa50fbe978c20b5be59d50fb5356e91601c0d5b4fb316ca6e616eb11dffb56c2cf13883b6101ea60]";

            StringBuilder sb = new(32);
            try {
                var str = await Shared.HttpClient.GetStringAsync(Bongo_APIURL, tokenSource.Token);

                foreach (var proxies_ in
                         JsonSerializer.Deserialize(str, Shared.Serializers.Default.StringArrayArray)!)
                    proxies.AddRange(IntoWebProxy(proxies_));
            }
            catch (Exception ex) {
                Console.WriteLine(
                    $"Failed to obtain proxies from https://bongoapi.hxhadventure.repl.co/api/ with error {ex}.");
            }
        }

        {
            var ProxyUrlTargets = new[] {
                "https://raw.githubusercontent.com/ALIILAPRO/Proxy/main/http.txt",
                "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/http.txt",
                "https://raw.githubusercontent.com/prxchk/proxy-list/main/http.txt",
                "https://raw.githubusercontent.com/UptimerBot/proxy-list/master/proxies/http.txt",
                "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
                //"https://raw.githubusercontent.com/officialputuid/KangProxy/KangProxy/http/http.txt", //! MAY CONTAIN MALFORMED PROXIES!
                //"https://raw.githubusercontent.com/Bardiafa/Proxy-Leecher/main/proxies.txt", //! MAY CONTAIN MALFORMED PROXIES!
            };

            var allScraped = await ScrapeProxiesGeneric(ProxyUrlTargets);

            proxies.AddRange(IntoWebProxy(allScraped));
        }

        return proxies;
    }

    private static async Task<string[]> ScrapeProxiesGeneric(string[] targets) {
        var proxyList = new List<string>(7000); // We scrape normally like 15K proxies or less.

        List<Task<HttpResponseMessage>> tasks = new(targets.Length);

        for (var i = 0; i < targets.Length; i++) {
            var target = targets[i];
            tasks.Add(Task.Run(async () => {
                try {
                    return await Shared.HttpClient.GetAsync(target);
                }
                catch (Exception ex) {
                    throw new Exception($"[Proxy Scraper] Failed to connect to {target}", ex);
                }
            }));
        }

        if (Shared.ShouldLog)
            Console.WriteLine("[Proxy Scraper] Downloading all proxy lists...");
        await Task.WhenAll(tasks);

        foreach (var task in tasks) {
            var response = await task;
            if (response.IsSuccessStatusCode) {
                var proxies = await response.Content.ReadAsStringAsync();
                var strArr = Array.Empty<string>();
                if (proxies.Contains("\r\n"))
                    strArr = proxies.Split("\r\n");
                else if (proxies.Contains('\n')) strArr = proxies.Split('\n');

                foreach (var str in strArr)
                    if (!string.IsNullOrEmpty(str) && str.Length <= 21) {
                        // Avoid "" characters && Size >21 due to 255.255.255.255:65535's length, it shouldn't be larger than that, else we KNOW its a malformed proxy, and we should ignore it!
                        proxyList.Add(str);
                    } else {
                        if (Shared.ShouldLog)
                            Console.WriteLine($"[Proxy Scraper] Found malformed proxy on host {response.RequestMessage.RequestUri.OriginalString}.");
                    }

                if (Shared.ShouldLog)
                    Console.WriteLine(
                        $"[Proxy Scraper] Received {response.StatusCode} from server for {response.RequestMessage.RequestUri.OriginalString}. Obtained {strArr.Length} proxies");
            }
            else {
                if (Shared.ShouldLog)
                    Console.WriteLine(
                        $"[Proxy Scraper] Request failure! We have failed to establish a connection to {response.RequestMessage.RequestUri.OriginalString}! Server reported the following status code: {response.StatusCode}");
            }
        }

        if (Shared.ShouldLog)
            Console.WriteLine($"[Proxy Scraper] Total proxies downloaded: {proxyList.Count}.");

        if (Shared.ShouldLog)
            Console.WriteLine("[Proxy Scraper] Removing possible duplicates...");


        HashSet<string> hashSet = new(proxyList);

        var foundDupes = proxyList.Count - hashSet.Count;
        var processedProxies = proxyList.Count;

        if (Shared.ShouldLog)
            Console.WriteLine(
                $"[Proxy Scraper] Processed proxies! Removed {foundDupes} duplicates out of {processedProxies} proxies.");
        return hashSet.ToArray();
    }


    private static WebProxy? IntoWebProxy(string proxy) {
        if (string.IsNullOrEmpty(proxy)) return null;

        const int proxyHostStart = 0;
        var proxyHostEnd = proxy.IndexOf(':');

        var proxyPortStart = proxyHostEnd + 1;
        var proxyPortEnd = proxy.Length;

        var proxyHost = proxy.AsSpan().Slice(proxyHostStart, proxyHostEnd).ToString();
        var proxyPort = proxy.AsSpan().Slice(proxyPortStart, proxyPortEnd - proxyPortStart).ToString();

        if (!int.TryParse(proxyPort, out var port)) return null;

        return new WebProxy(proxyHost, port);
    }


    private static List<WebProxy> IntoWebProxy(string[] proxies) {
        var proxy_ret = new List<WebProxy>(proxies.Length);
        const int proxyHostStart = 0;
        foreach (var proxy in proxies) {
            if (string.IsNullOrEmpty(proxy) || proxy.Length > 21) continue;

            var proxyHostEnd = proxy.IndexOf(':');

            if (proxyHostEnd == -1) continue; // Continue if there is no ':' character

            var proxyPortStart = proxyHostEnd + 1;
            var proxyPortEnd = proxy.Length;

            var proxyHost = proxy.AsSpan().Slice(proxyHostStart, proxyHostEnd).ToString();
            var proxyPort = proxy.AsSpan().Slice(proxyPortStart, proxyPortEnd - proxyPortStart).ToString();

            if (!int.TryParse(proxyPort, out var port)) continue; // Broken proxy structure.

            proxy_ret.Add(new WebProxy(proxyHost, port));
        }

        return proxy_ret;
    }
}