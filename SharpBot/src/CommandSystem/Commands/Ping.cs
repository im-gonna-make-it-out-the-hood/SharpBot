using Discord;
using Discord.WebSocket;
using Masked.DiscordNet;

namespace SharpBot.CommandSystem.Commands;

public sealed class Ping : IDiscordCommand {
    public static readonly Ping GlobalInstance = new();

    public async Task Run(SocketSlashCommand commandSocket) {
        // Defer the request to avoid time out.
        await commandSocket.DeferAsync();

        EmbedBuilder embed = new() {
            Title = "Pong!",
            Description = $"{Shared.DiscordClient.Latency}ms",
        };

        await commandSocket.FollowupAsync(embed: embed.Build());
    }

    public SlashCommandProperties Build() {
        SlashCommandBuilder builder = new() {
            Name = "ping",
            Description = "Obtain the ping of the bot to the Discord server",
        };
        return builder.Build();
    }
}