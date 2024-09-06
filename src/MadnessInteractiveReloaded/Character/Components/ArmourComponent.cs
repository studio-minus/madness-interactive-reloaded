namespace MIR;
using Walgelijk;

/// <summary>
/// Used for <see cref="ArmourPiece"/> that can be ejected.
/// </summary>
public class ArmourComponent : Component
{
    public readonly ArmourPiece? Piece;
    
    /// <summary>
    /// Is the armour still on the character?
    /// </summary>
    public bool Ejected = false;

    public ArmourComponent(ArmourPiece? piece)
    {
        Piece = piece;
    }
}
