using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public static class Program
{
#if DEBUG
    public static string BaseDirectory = Path.GetFullPath("../../../../");
#else
    public static string BaseDirectory => Environment.CurrentDirectory;
#endif

    private static void Main(string? mode = null, string? input = null, string? output = null)
    {
        var me = Path.Combine(Environment.CurrentDirectory, nameof(MIR) + ".dll");
        if (!File.Exists(me))
        {
            Console.Error.WriteLine("Game not found in current directory. Please start the game from the game root directory, or use a shortcut.");
            return;
        }

        switch (mode)
        {
            case "pack":
                {
                    Console.WriteLine("packing mode...");

                    if (input == null || !Directory.Exists(input))
                    {
                        Console.Error.WriteLine("{0} is not a directory, so it cannot be packed. Terminating.", input);
                        return;
                    }

                    var dir = new DirectoryInfo(input);
                    using var o = new FileStream(output ?? "base.waa", FileMode.Create);

                    AssetPackageUtils.Build("base", dir, o, new AssetPreprocessor());

                    Console.WriteLine("... done :)");
                    return;
                }
        }

        TextureLoader.Decoders.Insert(0, new PngDecoder());

#if DEBUG
        Harmony.DEBUG = true;
        var _ = new MadnessInteractiveReloaded();
#else
        try
        {
            var _ = new MadnessInteractiveReloaded();
        }
        catch (System.Exception e)
        {
            if (MadnessInteractiveReloaded.Game == null)
                return;

            string crashID = NameGenerator.Shared.GenerateName(5).ToString();

            string presentationPath = Path.GetTempPath() + $"mir_{crashID}.html";
            string componentDumpPath = Path.GetTempPath() + $"mir_{crashID}_components.md";
            string systemDumpPath = Path.GetTempPath() + $"mir_{crashID}_systems.md";
            string logDumpPath = Path.GetTempPath() + $"mir_{crashID}.log";

            var w = new StreamWriter(presentationPath, false);
            w.Write(@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Madness Interactive Reloaded has crashed</title>
    <style>
        html,body{
            font-family: sans-serif;
            background: black;
            color: white;
        }
        a{
            color: red !important;
        }
    </style>
</head>
<body>
");
            w.WriteLine("<h1>Madness Interactive Reloaded has crashed with crash ID {0}</h1>", crashID);
            //TODO hardware info
            w.WriteLine("<div style='margin-bottom: 1em'>");
            w.WriteLine("\t<div><b>Game build: </b>{0}</div>", System.Reflection.Assembly.GetAssembly(typeof(MIR.MadnessUtils)));
            w.WriteLine("\t<div><b>Engine build: </b>{0}</div>", System.Reflection.Assembly.GetAssembly(typeof(Walgelijk.Game)));
            w.WriteLine("</div>");

            if (ModLoader.Mods.Any())
            {
                w.WriteLine("<div style='margin-bottom: 1em'>");
                w.WriteLine("<div><b>Mods</b></div><ul>");
                foreach (var mod in ModLoader.Mods)
                    w.WriteLine("<li style=\"color: red\"><code>{0}</code></li>", mod.Id);
                w.WriteLine("</ul></div>");
            }

            w.WriteLine("<div style='margin-bottom: 1em'>");
            w.WriteLine("\t<div><a target='blank' href='{0}'>Component dump path</a></div>", componentDumpPath);
            w.WriteLine("\t<div><a target='blank' href='{0}'>System dump path</a></div>", systemDumpPath);
            w.WriteLine("\t<div><a target='blank' href='{0}'>Log path</a></div>", logDumpPath);
            w.WriteLine("</div>");

            w.WriteLine("<div style='margin-bottom: 1em'>");
            w.WriteLine("<h4>Unhandled exception occurred</h4>");
            w.WriteLine("<code>{0}: {1}</code>", e.GetType(), e.Message);
            w.WriteLine("</div>");

            w.WriteLine("<div style='margin-bottom: 1em'>");
            w.WriteLine("<h4>Stack trace<h4>");
            w.WriteLine("<code><pre>{0}</pre></code>", e.StackTrace);
            w.WriteLine("</div>");

            w.Write(@"
</body>
</html>
");

            w.Close();
            w.Dispose();

            WriteComponents(componentDumpPath);
            WriteSystems(systemDumpPath);
            WriteLog(logDumpPath);

            MadnessUtils.OpenExplorer($"\"{presentationPath}\"");

            return;
        }
    }

    private static void WriteLog(string path)
    {
        // Add a disk logger to the ILogger thing

        //var impl = Logger.Implementations.FirstOrDefault(a => a is DiskLogger);
        //if (impl == null || impl is not DiskLogger diskLogger)
        //{
        //    File.WriteAllText(path, "Disk logger could not be found. No log was recorded. This is catastrophic.");
        //    return;
        //}
        //try
        //{
        //    File.Copy(diskLogger.TargetPath, path);
        //}
        //catch (Exception e)
        //{
        //    File.WriteAllText(path, "Log could not be copied to target location: " + e);
        //    return;
        //}
    }

    private static void WriteComponents(string path)
    {
        var w = new StreamWriter(path, false);
        w.WriteLine("# Components");
        w.WriteLine();
        w.WriteLine("```");
        foreach (var entity in MadnessInteractiveReloaded.Game!.Scene.GetAllEntities())
        {
            w.WriteLine("{0}", entity);
            foreach (var component in MadnessInteractiveReloaded.Game.Scene.GetAllComponentsFrom(entity))
            {
                w.WriteLine("\t{0}", component.GetType().Name);
                WriteFieldsProperties(component, w);
            }
            w.WriteLine();
        }
        w.WriteLine("```");
        w.WriteLine();
        w.Dispose();
    }

    private static void WriteSystems(string path)
    {
        var w = new StreamWriter(path, false);
        w.WriteLine("# Systems");
        w.WriteLine();
        w.WriteLine("```");
        foreach (var system in MadnessInteractiveReloaded.Game!.Scene.GetSystems())
        {
            w.Write(system);
            if (!system.Enabled)
                w.WriteLine(" (disabled)");

            WriteFieldsProperties(system, w);
        }
        w.WriteLine("```");
        w.WriteLine();
        w.Dispose();
    }

    private static void WriteFieldsProperties<T>(T obj, StreamWriter w) where T : notnull
    {
        var t = obj.GetType();

        var fields = t.GetFields();
        var props = t.GetProperties();

        foreach (var item in fields)
        {
            var value = item.GetValue(obj);
            w.WriteLine("\t\tf_{0}: {1}", item.Name, value);
        }

        foreach (var item in props)
        {
            var value = item.GetValue(obj);
            w.WriteLine("\t\tp_{0}: {1}", item.Name, value);
        }
#endif
    }
}
