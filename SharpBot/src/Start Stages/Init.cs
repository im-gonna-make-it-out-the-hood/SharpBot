using Discord;
using SharpBot.Start_Stages.Structs;
using Spectre.Console;

namespace SharpBot.Start_Stages;

public sealed partial class StartStage {
    /// <summary>
    ///     The servers where the developers are in (Used mainly for development lmao).
    /// </summary>
    private static readonly ulong[] devServers = { 928820917495291965, };

    public async Task Initialization(PreInit preInitialization) {
        AnsiConsole.MarkupLine("[maroon][[Init/INFO]] Running [green]Initialization[/].[/]");

        // Register listeners to events.
        AnsiConsole.MarkupLine("[maroon][[Init/INFO]] [yellow italic bold]Registering event listeners[/]...[/]");

        // Load commands.
        Shared.DiscordClient.SlashCommandExecuted += preInitialization.GetCommands().GetSlashCommandHandler();
        AnsiConsole.MarkupLine(
            "[maroon][[Init/INFO]] [yellow italic bold]Registered listener for event [red bold underline]SlashCommandExecuted[/][/]...[/]");

        Shared.DiscordClient.Ready += () => {
            Shared.DiscordClient.SetStatusAsync(UserStatus.DoNotDisturb); // Booting
            AnsiConsole.MarkupLine(
                "[maroon][[Event/Ready]][/] [maroon]Received [green underline]Ready[/] from the [yellow underline bold]WebSocket[/] connection![/]");

            Shared.DiscordClient.SetStatusAsync(UserStatus.Online); // Finished boot.
            return Task.CompletedTask;
        };
        AnsiConsole.MarkupLine(
            "[maroon][[Init/INFO]] [yellow italic bold]Registered listener for event [red bold underline]SlashCommandExecuted[/][/]...[/]");

        Shared.DiscordClient.Log += log => {
            AnsiConsole.MarkupLine(
                $"[maroon][[Event/Log]][/] [red bold underline]{log.ToString().EscapeMarkup()}[/]");
            return Task.CompletedTask;
        };
        AnsiConsole.MarkupLine(
            "[maroon][[Init/INFO]] [yellow italic bold]Registered listener for event [red bold underline]Log[/][/]...[/]");


        // Start Bot
        AnsiConsole.MarkupLine(
            "[maroon][[Init/INFO]] [yellow italic bold]Logging in[/] with [red bold underline]token[/]...[/]");
        await Shared.DiscordClient.LoginAsync(TokenType.Bot, preInitialization.ReadToken());

        AnsiConsole.MarkupLine("[maroon][[Init/INFO]] [yellow italic bold]Starting Up[/]...[/]");
        await Shared.DiscordClient.StartAsync();


        // The bot has not fired the Ready event.
        while (Shared.DiscordClient.Status != UserStatus.Online) await Task.Delay(50);

        Shared.DiscordClient.GuildAvailable += async guild => {
            if (devServers.Contains(guild.Id)) {
                AnsiConsole.MarkupLine(
                    $"[maroon][[Event/GuildAvailable Info]] [green]Found Development Server[/] with id [yellow]{guild.Id}[/]. Sending remote [green bold]command builder[/][/]");
                await preInitialization.GetCommands().SubmitCommandBuilder(guild);
            }
        };
    }
}