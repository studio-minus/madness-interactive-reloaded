using System.Collections.Generic;
using Walgelijk;

namespace MIR;

public class ImprobabilityDisk
{
    public readonly string DisplayName;
    public readonly IReadableTexture Texture;
    public readonly string Description;
    public HashSet<string> IncompatibleWith = [];

    /// <summary>
    /// Used to write the relevant abilities on the disk description screen
    /// </summary>
    public AbilityDescriptor[] AbilityDescriptors = [];

    public bool Enabled = false;

    public ImprobabilityDisk(string displayName, IReadableTexture texture, string description)
    {
        DisplayName = displayName;
        Texture = texture;
        Description = description;
    }

    public virtual void Apply(Scene scene, CharacterComponent character) { }

    public override string ToString() => DisplayName;
}

public struct AbilityDescriptor
{
    public string Name;
    public string Description;
}