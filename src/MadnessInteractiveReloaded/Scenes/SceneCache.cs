using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Walgelijk;

namespace MIR;

/// <summary>
/// Caches scenes that get loaded once so if you load them again later, they don't need to be completely re-instantiated.
/// </summary>
//public static class SceneCache
//{
//    private static readonly Dictionary<string, Scene> scenes = new();

//    public static void Register(string name, Scene scene)
//    {
//        Logger.Debug("Scene cached: " + name);
//        scenes.Add(name, scene);
//    }

//    public static void Unload(string name)
//    {
//        if (scenes.Remove(name, out var scene))
//            scene.Dispose();
//    }

//    public static bool TryGet(string name, [NotNullWhen(true)] out Scene? scene) => scenes.TryGetValue(name, out scene);
//}
