using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpBot.Systems.RobloxAPI.Mappings.RangeIdScanner;

public class MinimalRobloxUserApiBatch {
    [JsonPropertyName("userIds")] public List<long> UserIdentifiers { get; set; }

    [JsonPropertyName("excludeBannedUsers")]
    public bool ExcludeBannedUsers { get; set; }

    public override string ToString() {
        return JsonSerializer.Serialize(this, Shared.Serializers.Default.MinimalRobloxUserApiBatch);
    }
}