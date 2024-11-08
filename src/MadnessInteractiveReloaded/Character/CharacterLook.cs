using System;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// What the character looks like.
/// Their clothes, the color of their blood (like yellow for Soldats).
/// </summary>
public class CharacterLook
{
    public const int HeadLayerCount = 4;
    public const int BodyLayerCount = 3;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name = "Untitled";

    public ArmourPiece Head;
    public ArmourPiece Body;

    public ArmourPiece? HeadLayer1;
    public ArmourPiece? HeadLayer2;
    public ArmourPiece? HeadLayer3;

    public ArmourPiece? BodyLayer1;
    public ArmourPiece? BodyLayer2;

    public HandArmourPiece? Hands;

    /// <summary>
    /// Feet texture. Falls back to default if null.
    /// </summary>
    public AssetRef<Texture>? Feet;

    /// <summary>
    /// Inner flesh textures. Falls back to default if null.
    /// </summary>
    public AssetRef<Texture>? HeadFlesh, BodyFlesh;  
    
    /// <summary>
    /// Inner gore textures. Falls back to default if null.
    /// </summary>
    public AssetRef<Texture>? HeadGore, BodyGore;

    /// <summary>
    /// Protective stats have no influence if true
    /// </summary>
    public bool Cosmetic = false;

    public Color BloodColour;

    public bool Jitter;

    public CharacterLook()
    {
        Head = new ArmourPiece();
        Body = new ArmourPiece();
    }

    public CharacterLook(CharacterLook toCopy)
    {
        toCopy.CopyTo(this);
    }

    public void CopyTo(CharacterLook target)
    {
        target.Head = Head;
        target.Body = Body;

        target.HeadLayer1 = HeadLayer1;
        target.HeadLayer2 = HeadLayer2;
        target.HeadLayer3 = HeadLayer3;

        target.BodyLayer1 = BodyLayer1;
        target.BodyLayer2 = BodyLayer2;

        target.HeadFlesh = HeadFlesh;
        target.BodyFlesh = BodyFlesh; 
        target.HeadGore = HeadGore;
        target.BodyGore = BodyGore;

        target.Hands = Hands;
        target.Feet = Feet;
        target.Cosmetic = Cosmetic;

        target.BloodColour = BloodColour;
        target.Jitter = Jitter;
    }

    public override string ToString() => Name;

    public ArmourPiece? GetHeadLayer(int index) => index switch
    {
        -1 => Head,
        0 => HeadLayer1,
        1 => HeadLayer2,
        2 => HeadLayer3,
        _ => null,
    };

    public ArmourPiece? GetBodyLayer(int index) => index switch
    {
        -1 => Body,
        0 => BodyLayer1,
        1 => BodyLayer2,
        _ => null,
    };

    /// <summary>
    /// Set the head layer for the given index, -1 being the base skin layer<br></br>
    /// <paramref name="piece"/> can be null if and only if <paramref name="index"/> is greater than -1. 
    /// An exception will be thrown if <paramref name="piece"/> is null and <paramref name="index"/> is -1
    /// </summary>
    /// <exception cref="Exception">
    /// </exception>
    public void SetHeadLayer(int index, ArmourPiece? piece)
    {
        switch (index)
        {
            case -1:
                if (piece == null)
                    throw new Exception("You can't assign null to the main head layer");
                else
                    Head = piece;
                break;
            case 0:
                HeadLayer1 = piece;
                break;
            case 1:
                HeadLayer2 = piece;
                break;
            case 2:
                HeadLayer3 = piece;
                break;
            default:
                throw new Exception("Attempt to set invalid head layer");
        }
    }

    /// <summary>
    /// Set the body layer for the given index, -1 being the base skin layer<br></br>
    /// <paramref name="piece"/> can be null if and only if <paramref name="index"/> is greater than -1. 
    /// An exception will be thrown if <paramref name="piece"/> is null and <paramref name="index"/> is -1
    /// </summary>
    /// <exception cref="Exception">
    /// </exception>
    public void SetBodyLayer(int index, ArmourPiece? piece)
    {
        switch (index)
        {
            case -1:
                if (piece == null)
                    throw new Exception("You can't assign null to the main body layer");
                Body = piece;
                break;
            case 0:
                BodyLayer1 = piece;
                break;
            case 1:
                BodyLayer2 = piece;
                break;
            default:
                throw new Exception("Attempt to set invalid body layer");
        }
    }

    /// <summary>
    /// Return the combined chance of head armour bullet deflection
    /// </summary>
    /// <returns></returns>
    public float GetHeadDeflectChance()
    {
        if (Cosmetic) 
            return 0;

        float a = 0;

        a = float.Max(a, Head.DeflectChance);
        a = float.Max(a, HeadLayer1?.DeflectChance ?? 0);
        a = float.Max(a, HeadLayer2?.DeflectChance ?? 0);
        a = float.Max(a, HeadLayer3?.DeflectChance ?? 0);

        return a;
    }

    /// <summary>
    /// Return the combined chance of body armour bullet deflection
    /// </summary>
    /// <returns></returns>
    public float GetBodyDeflectChance()
    {
        if (Cosmetic) 
            return 0;

        float a = 0;

        a = float.Max(a, Body.DeflectChance);
        a = float.Max(a, BodyLayer1?.DeflectChance ?? 0);
        a = float.Max(a, BodyLayer2?.DeflectChance ?? 0);

        return a;
    }
}
