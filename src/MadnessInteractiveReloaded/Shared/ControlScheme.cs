using System.Collections.Generic;
using Walgelijk;

namespace MIR;

public class ControlScheme
{
    public static ControlScheme ActiveControlScheme { get; set; } = new();

    public readonly Dictionary<GameAction, UserInput> InputMap = new()
    {
        { GameAction.Right, new(Key.D) },
        { GameAction.Left, new(Key.A) },
        { GameAction.JumpDodge, new(Key.Space) },
        { GameAction.Interact, new(Key.E) },

        { GameAction.Attack, new(MouseButton.Left) },
        { GameAction.Melee, new(MouseButton.Middle) },
        { GameAction.BlockAim, new(MouseButton.Right) },
        { GameAction.Throw, new(Key.F) },

        { GameAction.Ability1, new(Key.LeftShift) },
        { GameAction.Ability2, new(Key.Q) },
        { GameAction.Ability3, new(Key.LeftControl) },
        { GameAction.Ability4, new(Key.R) },
    };

    public bool Held(GameAction action, in InputState input)
        => InputMap.TryGetValue(action, out var userInput) && userInput.Held(input);

    public bool Released(GameAction action, in InputState input)
        => InputMap.TryGetValue(action, out var userInput) && userInput.Released(input);

    public bool Pressed(GameAction action, in InputState input)
        => InputMap.TryGetValue(action, out var userInput) && userInput.Pressed(input);
}
