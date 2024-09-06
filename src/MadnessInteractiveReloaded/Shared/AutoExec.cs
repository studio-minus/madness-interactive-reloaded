using System;
using System.IO;
using System.Text;
using Walgelijk;

namespace MIR;

/// <summary>
/// Static class for running a user-created config file (list of console commands and config variables).<br></br>
/// Works similarly the autoexec file in Source engine games.
/// </summary>
public static class AutoExec
{
    static string commands = default;
    static bool firstRun = false;

    /// <summary>
    /// Run the autoexec commands.
    /// </summary>
    /// <param name="useCached">If we want to reload the the autoexec file or use the 
    /// cached version from game launch.</param>
    public static void Run(bool useCached = true)
    {
        if (!firstRun || !useCached)
            Load();

        if (string.IsNullOrEmpty(commands))
            return;

        var split = commands.Split(Environment.NewLine);
        foreach (var command in split)
        {
            // (duston): don't run SetLevel commands after the first AutoExec run.
            if (firstRun && command.Contains("setlevel", StringComparison.InvariantCultureIgnoreCase))
                continue;

            CommandProcessor.Execute(command, Game.Main.Console);
        }

        firstRun = true;
    }

    private static void Load()
    {
        Logger.Debug("Loading autoexec.");
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.autoexec");

        // (duston): just use the first one it finds
        var file = files.Length > 0 ? files[0] : default;

        // (duston): no autoexec found
        if (file == default)
            return;

        if (File.Exists(file))
        {
            var autoExec = LinesReader.GetLines(file);
            var sb = new StringBuilder();
            foreach (var line in autoExec)
                sb.AppendFormat("{0}{1}", line, Environment.NewLine);

            commands = sb.ToString();
        }
    }
}
