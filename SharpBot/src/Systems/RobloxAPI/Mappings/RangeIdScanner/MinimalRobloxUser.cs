using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpBot.Systems.RobloxAPI.Mappings.RangeIdScanner;

public class RobloxMinimalUser {
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("name")] public string Username { get; set; }
    [JsonPropertyName("id")] public long UserIdentifier { get; set; }
    [JsonPropertyName("hasVerifiedBadge")] public bool isVerifiedByRoblox { get; set; }

    public static RobloxMinimalUser FromString(string str) {
        return JsonSerializer.Deserialize(str, Shared.Serializers.Default.RobloxMinimalUser);
    }
}