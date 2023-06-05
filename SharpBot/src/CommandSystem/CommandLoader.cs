using Masked.DiscordNet;
using SharpBot.CommandSystem.Commands;

namespace SharpBot.CommandSystem;

internal static class CommandLoader {
    public static CommandHelper LoadCommands(CommandHelper cmdHelper) {
        cmdHelper.AddToCommandList(Ping.GlobalInstance);
        cmdHelper.AddToCommandList(RangeIdScanner.GlobalInstance);
        return cmdHelper;
    }
}