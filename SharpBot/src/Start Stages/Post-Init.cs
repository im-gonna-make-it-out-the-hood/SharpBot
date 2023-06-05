using SharpBot.CommandSystem.Commands;
using Spectre.Console;

namespace SharpBot.Start_Stages;

public sealed partial class StartStage {
    public async Task PostInitialization() {
        AnsiConsole.MarkupLine("[maroon][[INFO]] Running [yellow]Post-Initialization[/].[/]");

        // TODO: Auto Completion of certain commands which might need it.

        AnsiConsole.MarkupLine("[maroon][[PostInit/INFO]] Running Proxy [yellow]Scraper[/]...[/]");

        //await ProxiedClientFactory.GetProxies().VerifyAllProxiesAsync();
    }
}