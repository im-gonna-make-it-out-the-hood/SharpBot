using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using SharpBot.CommandSystem.Commands;
using SharpBot.Systems.Collections;
using SharpBot.Systems.RobloxAPI.Mappings.RangeIdScanner;

namespace SharpBot.Systems.RobloxAPI.Implementations;

/// <summary>
///     This class contains the logic used to validate users in a range of IDs, and guarantee the IDs are valid and the
///     users are not banned.
/// </summary>
public class ValidateIdRange {
    /// <summary>
    ///     Batch version of the roblox API to receive a <see cref="MinimalRobloxUserContainer" />.
    /// </summary>
    /// <remarks>
    ///     This is a <see cref="HttpMethod.Post" /> endpoint! Its contents must be that of
    ///     <see cref="MinimalRobloxUserApiBatch" />
    /// </remarks>
    private const string RobloxUsersAPI = "https://users.roblox.com/v1/users";

    /// <summary>
    ///     Verifies a range of IDs using the given Thread count.
    /// </summary>
    /// <param name="threadCount"> The amount of Threads to use during the operation. </param>
    /// <param name="userIdentifiers"> The list of identifiers containing the targets. </param>
    /// <param name="autoAdjustThreadCount">The code will, when called, adjust the number of threads to efficiently process the given identifiers in <paramref name="userIdentifiers"/></param>
    /// <returns> A new <see cref="IEnumerable{RobloxMinimalUser}" /> containing data on the valid users. </returns>
    public static Task<IEnumerable<RobloxMinimalUser>> VerifyUsers(int preferredThreadCount,
        IEnumerable<long> userIdentifiers, bool autoAdjustThreadCount = true) {
        var identifiers = userIdentifiers as long[] ?? userIdentifiers.ToArray();

        // The preferable amount of Ids per thread
        byte preferableIdPerThread = 75;

        if (identifiers.Length / preferableIdPerThread < preferredThreadCount && !autoAdjustThreadCount) {
            throw new ArgumentOutOfRangeException(nameof(userIdentifiers),
                "The given list of identifiers (over 75 or lower!) must contain at least the given amount of threads!");
        }
        else if (identifiers.Length / preferableIdPerThread < preferredThreadCount && autoAdjustThreadCount) {
            preferredThreadCount = (identifiers.Length / preferableIdPerThread) + 1;
            Console.WriteLine($"Adaptive Threading has selected {preferredThreadCount} as best threading.");
        }

        List<Thread> threadContainer = new(preferredThreadCount);

        // Make sub chunks to the chunks for Threading to be easier, yes. I'm lazy.
        var subChunks = new List<long[][]>();

        while (true) {
            try {
                var x = identifiers.Chunk(preferableIdPerThread).ToArray(); // .ToList() -> Enumerate Entirely!
                subChunks = x.Chunk(x.Length / preferredThreadCount).ToList();

                if (subChunks.Count > preferredThreadCount)
                    throw new ArgumentOutOfRangeException("SUB");
                else if (subChunks.Count < preferredThreadCount)
                    throw new ArgumentOutOfRangeException("OVER");

                break;
            }
            catch (ArgumentOutOfRangeException ex) {
                if (preferableIdPerThread == 0) {
                    throw new InvalidProgramException(
                        "Could not initialize threads, too little ids for the given thread count to divide correctly!");
                }

                switch (ex.ParamName) {
                    case "SUB":
                        preferableIdPerThread++;
                        break;
                    case "OVER":
                        preferableIdPerThread--;
                        break;
                }
            }
        }

        Console.WriteLine(subChunks.Count);

        ConcurrentList<RobloxMinimalUser> minimalUserList = new();

        for (var i = 0; i < preferredThreadCount; i++) {
            var assignedWorkload = subChunks[i];

            Thread thread = new(() => {
                for (var j = 0; j < assignedWorkload.Length; j++) {
                    var selectedIdsForThread = assignedWorkload[j];
                    foreach (var chunk in selectedIdsForThread.Chunk(60)) {
                        var validUsers = GetValidUsersInCollection(chunk, false);

                        // Not required, GetValidUsersInCollection() returns a List<RobloxMinimalUser> | Multiple enumeration should NOT be an issue!
                        //validUsers = validUsers.ToList();

                        while (validUsers.Any() && !minimalUserList.TryAddRange(validUsers)) {
                            Thread.Sleep(1000); // Delay Task.
                            Console.WriteLine("ConcurrentList Semaphore Locked! Awaiting unlock...");
                        }
                    }
                }
            });

            // Random sleep, avoids all threads being equally started at the same time.
            Thread.Sleep(Random.Shared.Next(0, 100));
            thread.Start();
            threadContainer.Add(thread);
        }

        foreach (var thread in threadContainer) {
            if (Shared.ShouldLog)
                Console.WriteLine("Waiting for thread join...");
            thread.Join();
        }

        return Task.FromResult(minimalUserList.ToList().AsEnumerable());
    }

    /// <summary>
    ///     Obtains all the valid users that are held in the collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="IEnumerable{RobloxMinimalUser}" /> containing data on all the valid users of the given
    ///     <see cref="IEnumerable{long}" />
    /// </returns>
    /// <remarks>
    ///     This method will not allow the usage of more than <b> 60 </b> ids in a SINGLE call!<br />This will also use
    ///     the <see cref="ProxiedClientFactory" /> to create HttpClients with Proxies.
    /// </remarks>
    public static IEnumerable<RobloxMinimalUser> GetValidUsersInCollection(
        IEnumerable<long> userIdentifiers,
        bool excludeBannedUsers) {
        const byte MaximumAllowedIDsPerCall = 60; // Bytes, I'm a cheapo, three bytes for free.

        var identifiers = userIdentifiers.ToList();

        if (identifiers.Count > MaximumAllowedIDsPerCall)
            throw new ArgumentOutOfRangeException(nameof(userIdentifiers),
                $"The amount identifiers must be less than {MaximumAllowedIDsPerCall}, but more than 0! This collection contains {identifiers.Count} IDs!");

        var batch = new MinimalRobloxUserApiBatch {
            ExcludeBannedUsers = excludeBannedUsers, UserIdentifiers = identifiers,
        };
        var batchAsStr = batch.ToString();

        // Marks whether or not the HTTP Request to Roblox's API succeeded.
        var succeededRequest = false;

        // The proxied HttpClient used to maintain connections to the remote party.
        var proxiedClient = ProxiedClientFactory.CreateProxiedClient();

        // The list of users to return to the caller.
        var userList = new List<RobloxMinimalUser>();

        // Whether or not the Proxied Client should be reinitialized after a loop.
        var reInitProxiedClient = false;

        // The attempt counter for ratelimited proxies.
        var ratelimitAttempt = 0;

        var apiUri = new Uri(RobloxUsersAPI);

        while (!succeededRequest) {
            if (reInitProxiedClient) {
                proxiedClient.Dispose();
                proxiedClient = ProxiedClientFactory.CreateProxiedClient();
                reInitProxiedClient = false;
            }

            var httpReqMessage = new HttpRequestMessage {
                Method = HttpMethod.Post,
                RequestUri = apiUri,
                Content = new StringContent(batchAsStr, Encoding.UTF8, "application/json"),
                Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            };

            try {
                var response = proxiedClient.Send(httpReqMessage);

                // The string of the response.
                var stringResponse = string.Empty;

                if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                    reInitProxiedClient = true;
                    succeededRequest = false;
                    continue;
                }

                if (!response.IsSuccessStatusCode) {
                    stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    if (Shared.ShouldLog)
                        Console.WriteLine($"Error registered in request: {stringResponse}");
                    throw new HttpRequestException(
                        $"An error occurred processing the request! Request failed with HttpCode {response.StatusCode} ({response.StatusCode.ToString()}) | (Server Response Content) => {stringResponse}");
                }

                stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                var users = MinimalRobloxUserContainer.FromString(stringResponse);

                if (users == null) {
                    if (Shared.ShouldLog)
                        Console.WriteLine("Nothing to parse, error!");
                    throw new JsonException(
                        $"Parse failed from {nameof(String)} => {nameof(MinimalRobloxUserContainer)}) -> (JSON) => {stringResponse}");
                }

                if (users.Users == null) {
                    Console.WriteLine("Bad parse! Attempting alternative parse...");

                    users = MinimalRobloxUserContainer_Alternative.FromString(stringResponse);

                    if (users.Users == null) { // Alternative data structure was invalid too, ignore this attempt.
                        if (Shared.ShouldLog) {
                            Console.WriteLine("There were no valid users registered! Skipping...");
                            Console.WriteLine($"Server response: {stringResponse}");
                            Console.WriteLine(response.RequestMessage.Content.ReadAsStringAsync().GetAwaiter()
                                                      .GetResult());
                            succeededRequest = false;
                            reInitProxiedClient = true;
                            continue;
                        }
                    }
                }

                userList.AddRange(users.Users);

                succeededRequest = true;
            }
            catch (TaskCanceledException) { // The request timed out, funny.
                if (Shared.ShouldLog)
                    Console.WriteLine("Request timeout.");
                reInitProxiedClient = true;
            }
            catch (InvalidOperationException invalidOpEx) {
                if (Shared.ShouldLog)
                    Console.WriteLine($"This should never happen, but if it does, have an error! {invalidOpEx}");
                reInitProxiedClient = true;
            }
            catch (Exception ex) {
                reInitProxiedClient = true;
                if (!Shared.ShouldLog)
                    continue;

                Console.WriteLine("Generic Error handler:tm: -> Error reason below!");

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.Write("ERROR: ");

                Console.WriteLine(ex);
            }
        }

        return userList;
    }

    /// <summary>
    ///     Obtains all the valid users that are held in the collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="IEnumerable{RobloxMinimalUser}" /> containing data on all the valid users of the given
    ///     <see cref="IEnumerable{long}" />
    /// </returns>
    /// <remarks>
    ///     This method will not allow the usage of more than <b> 60 </b> ids in a SINGLE call!<br />This will also use
    ///     the <see cref="ProxiedClientFactory" /> to create HttpClients with Proxies.
    /// </remarks>
    public static async Task<IEnumerable<RobloxMinimalUser>> GetValidUsersInCollectionAsync(
        IEnumerable<long> userIdentifiers,
        bool excludeBannedUsers) {
        const byte MaximumAllowedIDsPerCall = 60; // Bytes, I'm a cheapo, three bytes for free.

        var identifiers = userIdentifiers.ToList();

        if (identifiers.Count > MaximumAllowedIDsPerCall)
            throw new ArgumentOutOfRangeException(nameof(userIdentifiers),
                $"The amount identifiers must be less than {MaximumAllowedIDsPerCall}, but more than 0! This collection contains {identifiers.Count} IDs!");

        var batch = new MinimalRobloxUserApiBatch {
            ExcludeBannedUsers = excludeBannedUsers, UserIdentifiers = identifiers,
        };
        var batchAsStr = batch.ToString();

        // Marks whether or not the HTTP Request to Roblox's API succeeded.
        var succeededRequest = false;

        // The proxied HttpClient used to maintain connections to the remote party.
        var proxiedClient = await ProxiedClientFactory.CreateProxiedClientAsync();

        // The list of users to return to the caller.
        var userList = new List<RobloxMinimalUser>();

        // Whether or not the Proxied Client should be reinitialized after a loop.
        var reInitProxiedClient = false;

        // The attempt counter for ratelimited proxies.
        var ratelimitAttempt = 0;

        while (!succeededRequest) {
            if (reInitProxiedClient) {
                proxiedClient.Dispose();
                proxiedClient = await ProxiedClientFactory.CreateProxiedClientAsync();
                reInitProxiedClient = false;
            }

            var httpReqMessage = new HttpRequestMessage {
                Method = HttpMethod.Post,
                RequestUri = new Uri(RobloxUsersAPI),
                Content = new StringContent(batchAsStr, Encoding.UTF8, "application/json"),
                Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            };

            try {
                var response = await proxiedClient.SendAsync(httpReqMessage);

                // The string of the response.
                var stringResponse = string.Empty;

                if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                    Console.WriteLine("Rate limited on this proxy! Waiting some seconds to attempt again...");
                    await Task.Delay(5000 * (1 + ratelimitAttempt));
                    ratelimitAttempt++;
                    reInitProxiedClient = false;
                    if (ratelimitAttempt == 3) {
                        reInitProxiedClient = true;
                        ratelimitAttempt = 0;
                    }

                    succeededRequest = false;
                    continue;
                }

                if (!response.IsSuccessStatusCode) {
                    stringResponse = await response.Content.ReadAsStringAsync();

                    if (Shared.ShouldLog)
                        Console.WriteLine($"Error registered in request: {stringResponse}");
                    throw new HttpRequestException(
                        $"An error occurred processing the request! Request failed with HttpCode {response.StatusCode} ({response.StatusCode.ToString()}) | (Server Response Content) => {stringResponse}");
                }

                stringResponse = await response.Content.ReadAsStringAsync();

                var users = MinimalRobloxUserContainer.FromString(stringResponse);

                if (users == null) {
                    if (Shared.ShouldLog)
                        Console.WriteLine("Nothing to parse, error!");
                    throw new JsonException(
                        $"Parse failed from {nameof(String)} => {nameof(MinimalRobloxUserContainer)}) -> (JSON) => {stringResponse}");
                }

                if (users.Users == null) {
                    Console.WriteLine("Bad parse! Attempting alternative parse...");

                    users = MinimalRobloxUserContainer_Alternative.FromString(stringResponse);

                    if (users.Users == null) { // Alternative data structure was invalid too, ignore this attempt.
                        if (Shared.ShouldLog) {
                            Console.WriteLine("There were no valid users registered! Skipping...");
                            Console.WriteLine($"Server response: {stringResponse}");
                            Console.WriteLine(await response.RequestMessage.Content.ReadAsStringAsync());
                            succeededRequest = false;
                            reInitProxiedClient = true;
                            continue;
                        }
                    }
                }

                userList.AddRange(users.Users);

                succeededRequest = true;
            }
            catch (TaskCanceledException) { // The request timed out, funny.
                if (Shared.ShouldLog)
                    Console.WriteLine("Request timeout.");
                reInitProxiedClient = true;
            }
            catch (InvalidOperationException invalidOpEx) {
                if (Shared.ShouldLog)
                    Console.WriteLine($"This should never happen, but if it does, have an error! {invalidOpEx}");
                reInitProxiedClient = true;
            }
            catch (Exception ex) when (Shared.ShouldLog) {
                Console.WriteLine("Generic Error handler:tm: -> Error reason below!");

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.Write("ERROR: ");

                Console.WriteLine(ex);

                reInitProxiedClient = true;
            }
        }

        return userList;
    }
}