using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Describes a piece of armour.
/// </summary>
public class ArmourPiece : ICharacterCustomisationItem
{
    /// <summary>
    /// The user-facing name of this armour piece.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Where on the body this armour piece goes.
    /// </summary>
    public ArmourPieceType Type;

    /// <summary>
    /// What category does the armour fall into when
    /// creating a character?
    /// </summary>
    public CharacterCreationCategory Category;

    /// <summary>
    /// The order it will appear in the menus.
    /// </summary>
    public int MenuOrder = 0;

    /// <summary>
    /// The texture for when the armour piece is facing right.
    /// </summary>
    public AssetRef<Texture> Right = default;

    /// <summary>
    /// The texture for when the armour piece is facing left.
    /// </summary>
    public AssetRef<Texture> Left = default;

    /// <summary>
    /// The positional offset when facing left.
    /// </summary>
    public Vector2 OffsetLeft;

    /// <summary>
    /// The positional offset when facing right.
    /// </summary>
    public Vector2 OffsetRight;

    /// <summary>
    /// Whether this armour piece is visible in the customisation menu
    /// </summary>
    public bool Hidden = false;

    /// <summary>
    /// If the armour piece can removed.
    /// </summary>
    public bool Detachable = false;

    /// <summary>
    /// Sprites for showing damage on the armour.
    /// </summary>
    public string[]? BrokenKeys = null;

    /// <summary>
    /// The scale at which this armour piece should be rendered. If set to 1 (the default), each pixel will represent one in-game unit.
    /// </summary>
    public float TextureScale = 1;

    /// <summary>
    /// The chance of this armour piece deflecting incoming bullets
    /// </summary>
    public float DeflectChance = 0;

    string ICharacterCustomisationItem.DisplayName => Name;

    bool ICharacterCustomisationItem.Hidden => Hidden;

    IReadableTexture ICharacterCustomisationItem.Texture => Right.Value;

    int ICharacterCustomisationItem.Order => MenuOrder;

    public override string ToString() => Name;

    /// <summary>
    /// Try to get a broken version of this armour piece.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the texture was found.</returns>
    public bool TryGetBrokenReplacement([NotNullWhen(true)] out string? key)
    {
        if (BrokenKeys != null && BrokenKeys.Length > 0)
        {
            key = Utilities.PickRandom(BrokenKeys);
            return true;
        }
        key = null;
        return false;
    }
}
