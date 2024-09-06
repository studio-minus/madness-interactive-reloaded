namespace MIR;
using Walgelijk;

/// <summary>
/// Stores the current gamemode.
/// </summary>
public class GameModeComponent : Component
{
    public GameMode Mode;

    public GameModeComponent(GameMode mode)
    {
        Mode = mode;
    }
}
