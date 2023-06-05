using System.Net;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;
using SharpBot.Systems.ProxyScrapers;
using SharpBot.Systems.RobloxAPI.Mappings.RangeIdScanner;

namespace SharpBot;

/// <summary>
///     Class containing objects that are shared throughout the program.
/// </summary>
public static partial class Shared {
    /// <summary>
    ///     The Discord Client that communicates with the Discord API.
    /// </summary>
    public static DiscordSocketClient DiscordClient { get; } = new(new DiscordSocketConfig {
        // During development we want  V e r b o s e  logs to diagnose stuff
        LogLevel = LogSeverity.Debug,
        AlwaysDownloadUsers = true,
        GatewayIntents = GatewayIntents.AllUnprivileged,
        LogGatewayIntentWarnings = true,

        // Wait for servers in a time frame of 25 seconds, else fire READY event anyways.
        MaxWaitBetweenGuildAvailablesBeforeReady = 25000,

        // Small cache.
        MessageCacheSize = 15,
    });

    /// <summary>
    ///     HTTPClient used to make any type of HTTP requests.
    /// </summary>
    public static HttpClient HttpClient { get; } = new(new HttpClientHandler {
        SslProtocols = SslProtocols.Tls12,
        UseCookies = false,
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.All,
        AllowAutoRedirect = true,
    });

    /// <summary>
    ///     Determines if the program should give debugging console logs to the standard output (Development Logs, Discord.Net
    ///     logs don't apply!)
    /// </summary>
    public static bool ShouldLog {
        get {
#if DEBUG
            return true;
#endif
            return false;
        }
    }

    [JsonSerializable(typeof(NucleousVPNProxies))]
    [JsonSerializable(typeof(List<long>))]
    [JsonSerializable(typeof(RobloxMinimalUser))]
    [JsonSerializable(typeof(MinimalRobloxUserContainer))]
    [JsonSerializable(typeof(MinimalRobloxUserApiBatch))]
    [JsonSerializable(typeof(MinimalRobloxUserContainer_Alternative))]
    [JsonSerializable(typeof(string[][]))]
    public partial class Serializers : JsonSerializerContext { }
}