using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// For something that can be placed in experiment mode.
/// </summary>
public struct ExperimentPlacableObject
{
    public Action<Scene, Vector2> SpawnFunction;
    public IReadableTexture DraggingTexture;

    public ExperimentPlacableObject(Action<Scene, Vector2> spawnFunction, IReadableTexture draggingTexture)
    {
        SpawnFunction = spawnFunction;
        DraggingTexture = draggingTexture;
    }
}
