using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpBot.Systems.RobloxAPI.Mappings.RangeIdScanner;

public class MinimalRobloxUserContainer {
    [JsonPropertyName("data")] public List<RobloxMinimalUser>? Users { get; set; }

    public static MinimalRobloxUserContainer? FromString(string str) {
        return JsonSerializer.Deserialize(str, Shared.Serializers.Default.MinimalRobloxUserContainer);
    }
}

public class MinimalRobloxUserContainer_Alternative {
    [JsonPropertyName("userIds")] public List<RobloxMinimalUser>? Users { get; set; }

    public static MinimalRobloxUserContainer_Alternative? FromString(string str) {
        return JsonSerializer.Deserialize(str, Shared.Serializers.Default.MinimalRobloxUserContainer_Alternative);
    }

    public static implicit operator MinimalRobloxUserContainer(MinimalRobloxUserContainer_Alternative? alt) {
        return new MinimalRobloxUserContainer {
            Users = alt.Users,
        };
    }
}