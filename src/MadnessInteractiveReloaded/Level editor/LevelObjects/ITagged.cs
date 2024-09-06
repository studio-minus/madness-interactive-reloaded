using Walgelijk;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Define level objects that have an associated <see cref="Tag"/>.
/// </summary>
public interface ITagged
{
    public Tag? Tag { get; set; }
}