using System;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.Diagnostics.Snapshots;
using Crusaders30XX.ECS.Data.Save;

ShaderRuntimeOptions.ConfigureFromArgs(args);
NewGameLaunchOptions.ConfigureFromArgs(args);
if (!ShaderRuntimeOptions.ShadersEnabled)
{
    Console.WriteLine("[Launch] GPU screen effects disabled (no-shaders)");
}

if (NewGameLaunchOptions.DeleteSaveBeforeLaunch)
{
    SaveCache.DeleteSaveFilesIfPresent();
}

var appArgs = NewGameLaunchOptions.StripLaunchFlag(ShaderRuntimeOptions.StripLaunchFlags(args));

DisplaySnapshotLaunchOptions snapshotOptions = null;
if (DisplaySnapshotLaunchOptions.TryParse(appArgs, out var parsed))
{
    snapshotOptions = parsed;
}

using var game = new Crusaders30XX.Game1(snapshotOptions);
game.Run();
