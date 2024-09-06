using System.Collections.Generic;
using Walgelijk;

namespace MIR;

/// <summary>
/// Component for the <see cref="GameLoadingSystem"/>.
/// </summary>
public class GameLoadingComponent : Component
{
    /// <summary>
    /// The loading text.
    /// </summary>
    public readonly List<string> DisplayedText = new();

    /// <summary>
    /// How far into loading the game are we?
    /// </summary>
    public float Progress = 0;
    
    /// <summary>
    /// Used for flashing the screen.
    /// </summary>
    public float FlashTime;
}
