using System.Diagnostics;
using System.Text.Json;
using SharpBot.Start_Stages;
using SharpBot.Systems.RobloxAPI.Implementations;

namespace SharpBot;

public static class MainActivity {
    public static async Task Main(string[] args) {
        var programStages = new StartStage();

        //! |-------------------------|
        //! |     Pre-Init Code.      |     |
        //! |-------------------------|     V

        var preInitialization = await programStages.PreInitialization(args);

        //! |-------------------------|
        //! |       Init Code.        |     |
        //! |-------------------------|     V

        await programStages.Initialization(preInitialization);

        //! |-------------------------|
        //! |     Post-Init Code.     |     |
        //! |-------------------------|     V

        await programStages.PostInitialization();

        static async Task LockConsole() {
            /* Lock Main Thread to avoid exiting. */
            await Task.Delay(-1);
        }

        await LockConsole();
    }
}