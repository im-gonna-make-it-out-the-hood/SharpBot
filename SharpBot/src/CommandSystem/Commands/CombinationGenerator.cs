using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using Masked.DiscordNet;

namespace SharpBot.CommandSystem.Commands;

public class CombinationGenerator : IDiscordCommand {
    public static readonly CombinationGenerator GlobalInstance = new();

    public async Task Run(SocketSlashCommand commandSocket) {
        // Defer the request to avoid time out.
        await commandSocket.DeferAsync();

        var combinationSource = ((string)commandSocket.Data.Options.ElementAt(0).Value).Split(',');
        var combinationSizeLimit = commandSocket.Data.Options.Count > 1
            ? (long)commandSocket.Data.Options.ElementAt(1).Value
            : 128;

        if (Math.Pow(combinationSource.Length, 2) >= 2_000_000) {
            _ = await commandSocket.FollowupAsync(embed: new EmbedBuilder {
                Title = "Error Too Much Depth!",
                Description =
                    $"The combination depth is TOO high, the asked depth would generate **{Math.Pow(combinationSource.Length, 2)}** combinations! This are **too** many! Reduce your depth to generate less than 2,000,000 results!",
            }.Build());
            return;
        }

        if (combinationSource.Length == 0) {
            _ = await commandSocket.FollowupAsync(embed: new EmbedBuilder() {
                Title = "No text provided.",
                Description =
                    $"The text parameter contains \'{(string)commandSocket.Data.Options.ElementAt(0).Value}\', but no text was able to be obtained from it! ",
            }.Build());
            return;
        }

        EmbedBuilder embed = new() {
            Title = "Generating Combinations...",
            Description = "The combinations are being generated, this may take a while...",
        };


        Task.Run(async () => {
            var restFollowup = await commandSocket.FollowupAsync(embed: embed.Build());

            var watch = Stopwatch.StartNew();

            var file = Path.GetTempFileName();

            var writer = File.CreateText(file);

            writer.AutoFlush = false;

            for (var i = 0; i < combinationSource.Length; i++)
                combinationSource[i] = combinationSource[i].Trim(); // Pre-process text to remove whitespaces.

            for (var i = 0; i < combinationSource.Length; i++) {
                var baseWord = combinationSource[i];
                await writer.WriteAsync(combinationSource[i]);

                for (var j = 0; j < combinationSource.Length; j++) {
                    var currentTarget = combinationSource[j];

                    if (currentTarget == baseWord) continue; // Skip is same word.

                    await writer.WriteAsync(baseWord);
                    await writer.WriteAsync(currentTarget);

                    if (baseWord.Length + currentTarget.Length > combinationSizeLimit) {
                        await writer.WriteAsync("\n// Generation skipped, too long for what was set in arguments!\n");
                        continue;
                    }

                    await writer.WriteAsync('\n');
                }

                await writer.FlushAsync(); // Flush to storage.
            }

            await writer.FlushAsync();
            await writer.DisposeAsync();
            watch.Stop();
            await Task.Delay(200);

            await restFollowup.ModifyAsync(x => x.Embed = new EmbedBuilder() {
                Title = "Combinations Generated!",
                Description =
                    $"All {Math.Pow(combinationSource.Length, 2)} possible combinations have been generated successfully in {watch.ElapsedMilliseconds}ms.",
            }.Build());


            await commandSocket.FollowupWithFileAsync(file, "combinations.txt", embed: new EmbedBuilder() {
                Title = "Here are your combinations!",
            }.Build());

            File.Delete(file); // Delete temp.
        });
    }

    public SlashCommandProperties Build() {
        SlashCommandBuilder builder = new() {
            Name = "generaatecombination",
            Description = "Generates a combination given the list of text, must be separated by commas",
        };
        builder.AddOption("text", ApplicationCommandOptionType.String,
            "The sentences used to generate the combination, must be separated by a comma.", true);

        builder.AddOption("lengthlimit", ApplicationCommandOptionType.Integer,
            "The maximum length of a combination, this is in characters.", false);
        return builder.Build();
    }
}