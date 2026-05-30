using System;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.Diagnostics.Snapshots;

ShaderRuntimeOptions.ConfigureFromArgs(args);
if (!ShaderRuntimeOptions.ShadersEnabled)
{
    Console.WriteLine("[Launch] GPU screen effects disabled (no-shaders)");
}

var appArgs = ShaderRuntimeOptions.StripLaunchFlags(args);

DisplaySnapshotLaunchOptions snapshotOptions = null;
if (DisplaySnapshotLaunchOptions.TryParse(appArgs, out var parsed))
{
    snapshotOptions = parsed;
}

using var game = new Crusaders30XX.Game1(snapshotOptions);
game.Run();
