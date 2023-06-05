using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Masked.DiscordNet;
using Masked.DiscordNet.Extensions;
using SharpBot.Systems.Extensions;
using SharpBot.Systems.RobloxAPI.Implementations;
using SharpBot.Systems.RobloxAPI.Mappings.RangeIdScanner;

namespace SharpBot.CommandSystem.Commands;

public class RangeIdScanner : IDiscordCommand {
    public static RangeIdScanner GlobalInstance { get; } = new();

    public async Task Run(SocketSlashCommand commandSocket) {
        await commandSocket.DeferAsync(true);

        var bottomOfIds = (long)commandSocket.Data.Options.ElementAt(0).Value;
        var topOfIds = (long)commandSocket.Data.Options.ElementAt(1).Value;

        var rangeSize = topOfIds - bottomOfIds;
        if (rangeSize <= 0) {
            await commandSocket.FollowupAsync(embed: new EmbedBuilder {
                Title = "Error!",
                Description =
                    "The bot is unable to scan the range of user ids, they are less than, or equal to zero ids to scan! Did you place the ranges incorrectly?",
            }.Build());
            return; // Preemptive return, the user gave us an invalid range of IDs.
        }

        if (rangeSize > 30000) {
            await commandSocket.FollowupAsync(embed: new EmbedBuilder {
                Title = "Too many IDs!",
                Description =
                    "The bot is unable to scan the range of user ids, they are more than 30000 ids to scan! Please reduce your range."
            }.Build());
            return;
        }

        var thread = new Thread(async () => {
            var socketSlashCommand = commandSocket;
            const int preferableIdPerThread = 75;
            var estimatedTime =
                TimeSpan.FromMilliseconds(5000 * Random.Shared.Next(5) * rangeSize);
            var restResponse = await socketSlashCommand.FollowupAsync(
                $"Scan in progress, we are scanning the range you have given us, please wait. Processing {rangeSize} ids... | This operation may take somewhere around {estimatedTime.TotalSeconds} seconds, or {estimatedTime.TotalMinutes} minutes! (Worst case scenario)");

            await Task.Delay(Random.Shared.Next(2500, 5000));
            
            const string RobloxUsersAPI_POST = "https://users.roblox.com/v1/users";
            var IdList = new List<long>((int)rangeSize);
            var semaphore_Synchronizer = new SemaphoreSlim(1);
            var minimalUserList = new List<RobloxMinimalUser>((int)rangeSize);

            var PendingRequests = new ConcurrentBag<HttpRequestMessage>();

            var watch = Stopwatch.StartNew();
            await restResponse.ModifyAsync(x => x.Content = "Pre-Generating ID List...");

            for (var i = bottomOfIds; i <= topOfIds; i++)
                IdList.Add(i);

            await restResponse.ModifyAsync(x =>
                x.Content =
                    $"ID List generated in {watch.Elapsed.TotalSeconds} seconds! | Preparing to send requests...");
            watch.Stop();

            var timeTakenOnIdGeneration = watch.Elapsed;

            await Task.Delay(Random.Shared.Next(1000, 2000));

            var maxDegreeOfParallelism = await socketSlashCommand.User.GetGuildUser().GetTaskLimit();

            await restResponse.ModifyAsync(x =>
                x.Content =
                    $"Effectuating range scan with {maxDegreeOfParallelism} threads! | Sending all requests...");

            Console.WriteLine(
                $"[Range Scanner] INFO: Calling scanner code with a parallelism of {maxDegreeOfParallelism}, auto adjust if required enabled!");
            watch.Restart();
            await ValidateIdRange.VerifyUsers(maxDegreeOfParallelism, IdList, true);
            watch.Stop();
            Console.WriteLine("[Range Scanner] INFO: Threads have completed their workload.");

            var timeTakenToRunToCompletion = watch.Elapsed;

            //FIXME: FIX SYNCHRONIZATION ISSUES! MASSIVE TODO:
            await restResponse.ModifyAsync(x =>
                x.Content =
                    $"Scan completed. Your file will be available soon, as it is being uploaded...\n\nRequest Information:\nTime Taken Generating IDs: {timeTakenOnIdGeneration.TotalMilliseconds}ms\nTime Taken obtaining data: {timeTakenToRunToCompletion.TotalMilliseconds}ms\nTotal time elapsed: {(timeTakenOnIdGeneration + timeTakenToRunToCompletion).TotalMilliseconds}ms");

            var file = Path.GetTempFileName();
            StringBuilder builder = new(minimalUserList.Count * 2 * 24);
            var userList = minimalUserList.OrderBy(x => x.UserIdentifier).ToList();
            for (var i = 0; i < minimalUserList.Count; i++)
                builder.Append(userList[i].Username).Append(" | ").Append(userList[i].UserIdentifier)
                       .Append('\n');

            await File.WriteAllTextAsync(file, builder.ToString(), Encoding.UTF8);
            builder.Clear();
            await socketSlashCommand.FollowupWithFileAsync(file, "scanned_range.txt",
                "Here is your ranged scan! **THIS IS ONLY VISIBLE FOR YOU!**", ephemeral: true);
            File.Delete(file);
        });

        thread.Start();
    }

    public SlashCommandProperties Build() {
        var builder = new SlashCommandBuilder();
        builder.WithName("rangescanner");
        builder.WithDescription("Scan for range of Roblox user ids and get usernames, and if the user is banned");
        builder.AddOption("bottom", ApplicationCommandOptionType.Integer, "The bottom of the range", true);
        builder.AddOption("top", ApplicationCommandOptionType.Integer, "The top of the range (exclusive)", true);
        return builder.Build();
    }
}