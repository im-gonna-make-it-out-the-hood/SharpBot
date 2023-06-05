using Discord;
using Masked.DiscordNet;
using SharpBot.CommandSystem;
using SharpBot.Start_Stages.Structs;
using Spectre.Console;

namespace SharpBot.Start_Stages;

public sealed partial class StartStage {
    public async Task<PreInit> PreInitialization(string[] arguments) {
        // Set Ctrl + C/Break Handler.

        AnsiConsole.MarkupLine(
            "[maroon][[INFO]] Increasing the [red][bold]minimum[/] and [green bold]maximum[/] worker[/] [yellow]count[/] to [green]double[/] the Environment's Core count.[/]");

        if (!ThreadPool.SetMaxThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2) ||
            !ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2))
            AnsiConsole.MarkupLine(
                "[maroon][[WARN]] [red]Failure[/] [yellow]setting[/] [green]values[/]! Expect [red bold]degraded[/] performance on [yellow bold]scanning[/]...[/]");

        Console.CancelKeyPress += async (sender, args) => await ConsoleExit(sender, args);

        AnsiConsole.MarkupLine("[maroon][[INFO]] Running [red]Pre-Initialization[/].[/]");

        // Setup commands.
        var commandContext = CommandLoader.LoadCommands(new CommandHelper());

        var token = await File.ReadAllTextAsync("./TOKEN");

        return new PreInit(commandContext, token);
    }

    private static async Task ConsoleExit(object? sender, ConsoleCancelEventArgs args) {
        // If it is testing mode, we should close normally, else just force it to not close.
        Console.WriteLine("\nThe process has received a Signal Interrupt!");

        // End the process 'gracefully'.
        Console.WriteLine($"Signal interrupt triggered by key {args.SpecialKey}");


        // HTTPClient disposal.
        Console.WriteLine("Disposing HttpClient...");
        Shared.HttpClient.Dispose();

        Console.WriteLine("Setting last Discord Client status as DND...");
        await Shared.DiscordClient.SetStatusAsync(UserStatus.DoNotDisturb);

        // Discord Client disposal
        Console.WriteLine("Logging Out and Stopping the Discord Socket Client...");
        await Shared.DiscordClient.LogoutAsync();
        Console.WriteLine("Disposing of the Discord Socket Client...");
        await Shared.DiscordClient.DisposeAsync();
    }
}