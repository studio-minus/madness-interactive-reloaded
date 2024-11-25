using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Walgelijk;
using Walgelijk.Localisation;

namespace MIR;

/// <summary>
/// The game configuration.<br></br>
/// Graphics settings, audio settings, language, etcs.
/// </summary>
public class Settings
{
    /// <summary>
    /// General common settings.
    /// </summary>
    public GeneralSettings General = new();

    /// <summary>
    /// Video specific settings.
    /// </summary>
    public VideoSettings Video = new();

    /// <summary>
    /// Audio levels and settings.
    /// </summary>
    public AudioSettings Audio = new();

    /// <summary>
    /// Input mapping
    /// </summary>
    public ControlScheme Controls = new();

    private readonly SemaphoreSlim saveLock = new(1);

    public class GeneralSettings
    {
        public string Language = "en-GB";
        public float Screenshake = 1;
    }

    public class VideoSettings
    {
        public int FramerateCap = 300;
        public bool Vsync = false;
        public WindowType WindowType = WindowType.Normal;
        public bool StampRagdolls = true;
    }

    public class AudioSettings
    {
        public float MasterVolume = 0.5f;
        public float MusicVolume = 1;
        public float SfxVolume = 1;
        public float UiVolume = 1;
    }

    public class InputSettings
    {
        public float MasterVolume = 0.5f;
        public float MusicVolume = 1;
        public float SfxVolume = 1;
        public float UiVolume = 1;
    }

    /// <summary>
    /// Apply the settings and save them to disk.
    /// </summary>
    public void Apply() => Apply(Game.Main);

    /// <summary>
    /// Apply the settings and save them to disk.
    /// </summary>
    /// <param name="game"></param>
    public void Apply(Game game)
    {
        game.Window.VSync = Video.Vsync;
        game.UpdateRate = Video.FramerateCap;
        if (game.Window.WindowType != Video.WindowType)
            game.Window.WindowType = Video.WindowType;

        game.AudioRenderer.Volume = Audio.MasterVolume;
        AudioTracks.Music.Volume = Audio.MusicVolume;
        AudioTracks.SoundEffects.Volume = Audio.SfxVolume;
        AudioTracks.UserInterface.Volume = Audio.UiVolume;
        ControlScheme.ActiveControlScheme = Controls ?? new();
        //game.Console.ToggleKey = ControlScheme.ActiveControlScheme.

        if (!Registries.Languages.TryGet(General.Language, out Localisation.CurrentLanguage))
            Logger.Warn($"Language read from settings \"{General.Language}\" is invalid, so no langauge could be set. Choices are {string.Join(", ", Registries.Languages.GetAllKeys())}");

        Task.Run(Save);
    }

    public void Save()
    {
        saveLock.Wait();
        try
        {
            WriteToFile(UserData.Paths.Settings, UserData.Instances.Settings);
        }
        finally
        {
            saveLock.Release();
        }
    }

    /// <summary>
    /// Try to load the settings from disk.
    /// </summary>
    public static bool TryReadFromFile(string path, ref Settings s)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path);
            var read = JsonConvert.DeserializeObject<Settings>(json);
            if (read != null)
            {
                s = read;
                foreach (var k in s.Controls.InputMap.Keys)
                {
                    var old = s.Controls.InputMap[k];
                    s.Controls.InputMap[k] = new UserInput(old.Inputs, old.Type);
                }
            }
        }
        catch (System.Exception e)
        {
            Logger.Error(e.ToString());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Save the settings to disk.
    /// </summary>
    public static void WriteToFile(string path, in Settings s)
    {
        try
        {
            var json = JsonConvert.SerializeObject(s, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Logger.Error(e.ToString());
        }
    }
}
