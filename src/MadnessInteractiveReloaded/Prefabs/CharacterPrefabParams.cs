using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public struct CharacterPrefabParams
{
    public string Name;
    public Vector2 Bottom;

    public Faction Faction;
    public CharacterLook Look;
    public CharacterStats Stats;

    public Tag Tag;
    public float? ScaleOverride;
    public AssetRef<Texture>? HeadFleshTexture, BodyFleshTexture;
}
