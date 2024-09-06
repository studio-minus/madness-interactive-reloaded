using Walgelijk;

namespace MIR;

public static class InputExtensions
{
    public static ControlScheme Map(this InputState _)
        => ControlScheme.ActiveControlScheme;

    public static bool ActionHeld(this InputState input, GameAction action)
        => ControlScheme.ActiveControlScheme.Held(action, input);

    public static bool ActionReleased(this InputState input, GameAction action)
        => ControlScheme.ActiveControlScheme.Released(action, input);

    public static bool ActionPressed(this InputState input, GameAction action)
        => ControlScheme.ActiveControlScheme.Pressed(action, input);

    // enum shit

    public static string GetDisplayName(this Key key)
    {
        return key switch
        {
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.D0 => "0",

            Key.GraveAccent => "~",

            Key.KeyPad1 => "Numpad 1",
            Key.KeyPad2 => "Numpad 2",
            Key.KeyPad3 => "Numpad 3",
            Key.KeyPad4 => "Numpad 4",
            Key.KeyPad5 => "Numpad 5",
            Key.KeyPad6 => "Numpad 6",
            Key.KeyPad7 => "Numpad 7",
            Key.KeyPad8 => "Numpad 8",
            Key.KeyPad9 => "Numpad 9",
            Key.KeyPad0 => "Numpad 0",

            Key.KeyPadAdd => "Numpad -",
            Key.KeyPadSubtract => "Numpad +",
            Key.KeyPadDecimal => "Numpad .",
            Key.KeyPadDivide => "Numpad /",
            Key.KeyPadEnter => "Numpad Enter",
            Key.KeyPadEqual => "Numpad =",
            Key.KeyPadMultiply => "Numpad *",

            _ => key.ToString(),
        };
    }    
    
    public static string GetDisplayName(this MouseButton mb)
    {
        return mb switch
        {
            MouseButton.Button1 => "Left mouse button",
            MouseButton.Button2 => "Right mouse button",
            MouseButton.Button3 => "Middle mouse button",
            MouseButton.Button4 => "MB 4",
            MouseButton.Button5 => "MB 5",
            MouseButton.Button6 => "MB 6",
            MouseButton.Button7 => "MB 7",
            MouseButton.Button8 => "MB 8",
            _ => mb.ToString(),
        };
    }    

    public static string GetDisplayName(this GameAction ac)
    {
        return ac switch
        {
            GameAction.JumpDodge => "Jump / Dodge",
            GameAction.BlockAim => "Block / Aim",
            _ => ac.ToString(),
        };
    }
}
