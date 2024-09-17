using Walgelijk;

namespace MIR;
//🎈
public static class InputEnumExtensions
{
    public static string ToDisplayString(this Key key)
    {
        return key switch
        {
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            _ => key.ToString(),
        };
    }

    public static string ToDisplayString(this MouseButton btn)
    {
        return btn switch
        {
            MouseButton.Button1 or MouseButton.Left => "LMB",
            MouseButton.Button2 or MouseButton.Right => "RMB",

            MouseButton.Button3 => "Mouse 3",
            MouseButton.Button4 => "Mouse 4",
            MouseButton.Button5 => "Mouse 5",
            MouseButton.Button6 => "Mouse 6",
            MouseButton.Button7 => "Mouse 7",
            MouseButton.Button8 => "Mouse 8",
            _ => btn.ToString(),
        };
    }
}