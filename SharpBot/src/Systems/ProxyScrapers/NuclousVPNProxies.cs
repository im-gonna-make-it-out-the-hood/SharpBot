using System.Text.Json;

namespace SharpBot.Systems.ProxyScrapers;

public class NucleousVPNProxies {
    public Proxy[] proxy_list { get; set; }

    public static async Task<NucleousVPNProxies> GetProxies() {
        return JsonSerializer.Deserialize(
            await Shared.HttpClient.GetStringAsync("https://api.nucleusvpn.com/api/proxy"),
            Shared.Serializers.Default.NucleousVPNProxies);
    }
}

public class Proxy {
    public string host { get; set; }
    public string country { get; set; }
    public int quality { get; set; }
}