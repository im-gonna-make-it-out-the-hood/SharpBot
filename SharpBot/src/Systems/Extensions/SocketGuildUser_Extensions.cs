using Discord.WebSocket;
using Masked.DiscordNet.Extensions;

namespace SharpBot.Systems.Extensions;

public static class SocketGuildUser_Extensions {
    public static async Task<int> GetTaskLimit(this SocketGuildUser user) {
        var userGuild = user.Guild;
        if (await user.HasRole(userGuild.GetRole(1114586108710363246))) // Dev
            return 64;


        if (await user.HasRole(userGuild.GetRole(1114586046735339620))) // High
            return 32;


        if (await user.HasRole(userGuild.GetRole(1114586080197496894))) // Low
            return 8;


        // If the user has none, return 4 tasks max!
        return 64;
    }
}