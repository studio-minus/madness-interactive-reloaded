using MIR.LevelEditor.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Walgelijk;

namespace MIR.LevelEditor;

/// <summary>
/// Manages the LevelEditor.
/// </summary>
public class LevelEditorComponent : Component
{
    /// <summary>
    /// What objects are we selecting?
    /// </summary>
    public readonly SelectionManager<LevelObject> SelectionManager = new();

    /// <summary>
    /// The level we're editing.
    /// </summary>
    public Level? Level;

    /// <summary>
    /// If the LevelEditor data needs an update.
    /// </summary>
    public bool Dirty = false;

    /// <summary>
    /// The LevelEditor context menu.
    /// </summary>
    public ContextMenu? ContextMenu;

    /// <summary>
    /// What direction the mouse is going.
    /// </summary>
    public Vector2 MouseDelta;

    /// <summary>
    /// The pixel size from zooming.
    /// </summary>
    public float PixelSize;

    /// <summary>
    /// The level's file name.
    /// </summary>
    public string? FileName;

    /// <summary>
    /// A cache of the looks registry's keys.
    /// </summary>
    public string[] Looks;

    /// <summary>
    /// A cache of the stats registry's keys.
    /// </summary>
    public string[] Stats;

    /// <summary>
    /// A cache of the faction registry's keys.
    /// </summary>
    public string[] Factions;

    /// <summary>
    /// A cache of the animation registry's keys.
    /// </summary>
    public string[] Animations;

    /// <summary>
    /// A cache of the weapon registry's keys.
    /// </summary>
    public string[] Weapons;

    public string[] GameSystemOptionNames;
    public string[] GameSystemOptions;

    /// <summary>
    /// Levels from the registry that we can edit.
    /// </summary>
    public string[] LevelIds;

    public readonly Dictionary<Type, OutlineFilter> Filter = [];

    public OutlinerMode OutlinerMode;
    public bool LevelSettingsMenuOpen;
    public bool WeaponSpawnMenuOpen;
    public bool AutospawnMenuOpen;

    /// <summary>
    /// The Undo history.
    /// </summary>
    private readonly Stack<string> undoStack = new();

    /// <summary>
    /// The Redo history.
    /// </summary>
    private readonly Stack<string> redoStack = new();

    public LevelEditorComponent()
    {
        Looks = Registries.Looks.GetAllKeys().ToArray();
        Stats = Registries.Stats.GetAllKeys().ToArray();
        Animations = Registries.Animations.GetAllKeys().ToArray();
        Weapons = Registries.Weapons.GetAllKeys().ToArray();
        LevelIds = Registries.Levels.GetAllKeys().ToArray();
        Factions = Registries.Factions.GetAllKeys().ToArray();

        foreach (var item in typeof(Objects.LevelObject).Assembly.GetTypes().Where(static t => !t.IsAbstract && t.IsAssignableTo(typeof(Objects.LevelObject))))
            if (item != null)
                Filter.Add(item, new OutlineFilter(true));

        var asm = global::System.Reflection.Assembly.GetEntryAssembly();
        if (asm != null)
        {
            var allTypes = asm.GetTypes().Where(t => t.IsAssignableTo(typeof(Walgelijk.System)) && !string.IsNullOrWhiteSpace(t.FullName));

            GameSystemOptionNames = allTypes.Select(s => s.Name ?? throw new Exception("Name for GameSystemOptionNames was null")).ToArray();
            GameSystemOptions = allTypes.Select(s => s.FullName ?? throw new Exception("AssemblyQualifiedName for GameSystemOptions was null")).ToArray();
        }
        else
        {
            GameSystemOptionNames = [];
            GameSystemOptions = [];
        }
    }

    /// <summary>
    /// We did something that can be un-done or re-done.
    /// Add it to the history.
    /// </summary>
    public void RegisterAction()
    {
        try
        {
            var json = JsonConvert.SerializeObject(Level, LevelDeserialiser.SerializerSettings);
            undoStack.Push(json);
            redoStack.Clear();
            SelectionManager.DeselectAll();
            Dirty = true;
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    /// <summary>
    /// Undo the last thing we did.
    /// </summary>
    public void Undo()
    {
        if (undoStack.TryPop(out var json))
        {
            Level = JsonConvert.DeserializeObject<Level>(json, LevelDeserialiser.SerializerSettings);
            if (Level != null)
                foreach (var item in Level.Objects)
                    item.Editor = this;
            redoStack.Push(json);
            SelectionManager.DeselectAll();
            Dirty = true;
        }
    }

    /// <summary>
    /// We un-did something but we want
    /// that thing to be done again.
    /// </summary>
    public void Redo()
    {
        if (redoStack.TryPop(out var json))
        {
            Level = JsonConvert.DeserializeObject<Level>(json, LevelDeserialiser.SerializerSettings);
            if (Level != null)
                foreach (var item in Level.Objects)
                    item.Editor = this;
            undoStack.Push(json);
            Dirty = true;
        }
    }

    /// <summary>
    /// Re-calculate the floor line.
    /// </summary>
    public void UpdateFloorLine()
    {
        if (Level != null)
            FloorlineCalculator.Calculate(Level);
    }

    /// <summary>
    /// Check if the level bounds need to be grown or shrunk to fit the level's objects.
    /// </summary>
    public void UpdateLevelBounds()
    {
        if (Level == null)
            return;

        Level.LevelBounds = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        foreach (var item in Level.Objects)
        {
            var b = item.GetBounds();
            if (b.HasValue)
                Level.LevelBounds = Level.LevelBounds.StretchToContain(b.Value);
        }
    }

    /// <summary>
    /// Export a level to a file.
    /// Exports to a JSON file.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="Exception"></exception>
    public void ExportLevel(string path)
    {
        if (Level == null)
            throw new Exception("Level is null");
        LevelDeserialiser.Save(Level, path);
        Level.Id  = Path.GetFileNameWithoutExtension(path);
        FileName = path;
    }

    /// <summary>
    /// Load a level file, takes JSON.
    /// </summary>
    /// <param name="path"></param>
    public void ImportLevel(string path)
    {
        undoStack.Clear();
        redoStack.Clear();

        var fileName = Path.GetFileName(path);
        Logger.Log($"Imported level \"{fileName}\"");
        FileName = path;

        var lvl = LevelDeserialiser.Load(File.OpenRead(path), Path.GetFileNameWithoutExtension(path));
        Level = lvl;
        Dirty = true;

        foreach (var item in Level.Objects)
            item.Editor = this;

        //ensure there is 1 player spawn
        if (!Level.Objects.OfType<Objects.PlayerSpawn>().Any())
            Level.Objects.Add(new Objects.PlayerSpawn(this));
        else if (Level.Objects.OfType<Objects.PlayerSpawn>().Count() != 1)
        {
            Level.Objects.RemoveAll(static o => o is Objects.PlayerSpawn);
            Level.Objects.Add(new Objects.PlayerSpawn(this));
        }
    }
}
