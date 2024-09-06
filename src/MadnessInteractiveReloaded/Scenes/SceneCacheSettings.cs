using System.Diagnostics.CodeAnalysis;
using Walgelijk;

namespace MIR;

/// <summary>
/// Used when creating a scene to specify if a <see cref="Scene"/> should be cached or not.
/// </summary>
public struct SceneCacheSettings
{
    public SceneId? CacheId;

    [MemberNotNullWhen(true, nameof(CacheId))]
    public readonly bool ShouldCache => CacheId != null;

    public SceneCacheSettings(SceneId? cacheId)
    {
        CacheId = cacheId;
    }

    public static SceneCacheSettings NoCache => new(null);
}