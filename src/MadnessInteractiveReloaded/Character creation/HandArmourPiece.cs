using System;
using System.Runtime.ExceptionServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// Armour for hands. Gloves and stuff.
/// </summary>
public class HandArmourPiece : ICharacterCustomisationItem
{
    public string Name = string.Empty;
    public int MenuOrder = 0;

    public DoubleSided<AssetRef<Texture>> Fist;
    public DoubleSided<AssetRef<Texture>> HoldPistol;
    public DoubleSided<AssetRef<Texture>> HoldUnderside;
    public DoubleSided<AssetRef<Texture>> HoldRifle;
    public DoubleSided<AssetRef<Texture>> Open;
    public DoubleSided<AssetRef<Texture>> Point;
    public DoubleSided<AssetRef<Texture>> HoldStock;

    public static ArmourPieceType Type => ArmourPieceType.Hand;
    public static CharacterCreationCategory Category => CharacterCreationCategory.Hands;

    /// <summary>
    /// Get the textures assigned to the given <see cref="HandLook"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ref DoubleSided<AssetRef<Texture>> GetByLook(HandLook handLook)
    {
        switch (handLook)
        {
            case HandLook.Fist: return ref Fist;
            case HandLook.HoldPistol: return ref HoldPistol;
            case HandLook.HoldUnderside: return ref HoldUnderside;
            case HandLook.HoldRifle: return ref HoldRifle;
            case HandLook.Point: return ref Point;
            case HandLook.Open: return ref Open;
            case HandLook.HoldStock: return ref HoldStock;
            default: throw new ArgumentOutOfRangeException(nameof(handLook), "given parameter should be an enum value");
        }
    }

    string ICharacterCustomisationItem.DisplayName => Name;

    bool ICharacterCustomisationItem.Hidden => false;

    IReadableTexture ICharacterCustomisationItem.Texture
    {
        get
        {
            var index = int.Abs((int)(Game.Main.State.Time * 0.8f)) % 4;
            HandLook look = index switch
            {
                1 => HandLook.Open,
                2 => HandLook.Fist,
                3 => HandLook.HoldUnderside,
                _ => HandLook.Point,
            };
            return GetByLook(look).FacingRight.Value;
        }
    }

    int ICharacterCustomisationItem.Order => MenuOrder;
}
