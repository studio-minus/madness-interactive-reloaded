using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using MIR.LevelEditor.Objects;
using System.Diagnostics.Metrics;

namespace MIR.LevelEditor;

/// <summary>
/// For creating a menu with more context on an action/things.
/// </summary>
public struct ContextMenu
{
    public string? Title;
    public Vector2 Position;
    public IList<(string label, Action<LevelEditorComponent, Scene> action)>? Buttons;
}

