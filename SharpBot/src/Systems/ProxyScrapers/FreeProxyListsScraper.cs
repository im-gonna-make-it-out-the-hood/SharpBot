using HtmlAgilityPack;

namespace SharpBot.Systems.ProxyScrapers;

public class FreeProxyListsScraper {
    private const string PROXIES_XPATH = "//div[@class=\"modal-body\"]/textarea";
    private static readonly HtmlWeb web = new();

    public static async Task<string[]> GetProxiesAsync(CancellationToken token = default) {
        var doc = await web.LoadFromWebAsync("https://free-proxy-list.net/", token);

        var node = doc.DocumentNode.SelectSingleNode(PROXIES_XPATH);

        var proxies = node.InnerText;

        string GetProxyAddressesFromString(string str) {
            var
                val = str.AsSpan()[
                    75..(str.Length -
                         1)]; // Remove the "Free proxies from free-proxy-list.net" and Update date header, leaving only us ONLY with the proxies, Using Span<char> to be efficient and not splitting on every line lmao.
            return val.ToString();
        }

        return GetProxyAddressesFromString(proxies).Split('\n');
    }
}