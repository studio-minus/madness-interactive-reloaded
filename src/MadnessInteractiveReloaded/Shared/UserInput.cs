using MIR.Tutorials;
using Newtonsoft.Json;
using System;
using System.Linq;
using Walgelijk;
using Walgelijk.Onion;

namespace MIR;

public struct UserInput : IEquatable<UserInput>
{
    public string[] Inputs;
    public UserInputType Type;

    [JsonIgnore]
    public bool Valid;

    private readonly Key[] keys = new Key[3];
    private readonly MouseButton[] buttons = new MouseButton[1];

    private readonly string FriendlyName;

    public UserInput(string[] names, UserInputType type)
    {
        Valid = true;

        Inputs = names;
        Type = type;

        int maxNames = type switch
        {
            UserInputType.Key => keys.Length,
            UserInputType.Button => buttons.Length,
            _ => -1,
        };

        if (maxNames == -1 || names.Length > maxNames)
        {
            Valid = false;
            return;
        }

        int i = 0;
        foreach (var n in names)
        {
            switch (type)
            {
                case UserInputType.Key:
                    if (Enum.TryParse<Key>(n, out var k))
                        keys[i++] = k;
                    else
                    {
                        Valid = false;
                        return;
                    }
                    break;
                case UserInputType.Button:
                    if (Enum.TryParse<MouseButton>(n, out var mb))
                        buttons[i++] = mb;
                    else
                    {
                        Valid = false;
                        return;
                    }
                    break;
            }
        }

        switch (Type)
        {
            case UserInputType.Key:
                Array.Resize(ref keys, Inputs.Length);
                FriendlyName = string.Join('+', keys.Select(k => k.ToDisplayString()));
                break;
            case UserInputType.Button:
                Array.Resize(ref buttons, Inputs.Length);
                FriendlyName = string.Join('+', buttons.Select(k => k.ToDisplayString()));
                break;
        }
    }

    public UserInput(params Key[] k)
    {
        Type = UserInputType.Key;
        Valid = k.Length <= keys.Length && k.Length > 0;
        Inputs = k.Select(k => k.ToString()).ToArray();
        keys = k;

        FriendlyName = string.Join('+', keys.Select(k => k.ToDisplayString()));
    }

    public UserInput(params MouseButton[] m)
    {
        Type = UserInputType.Button;
        Valid = m.Length <= buttons.Length && buttons.Length > 0;
        Inputs = m.Select(m => m.ToString()).ToArray();
        buttons = m;

        FriendlyName = string.Join('+', buttons.Select(b => b.ToDisplayString()));
    }

    public readonly bool Held(in InputState input)
    {
        if (!Valid)
            return false;

        switch (Type)
        {
            case UserInputType.Key:
                foreach (var k in keys)
                    if (!input.IsKeyHeld(k))
                        return false;
                return true;

            case UserInputType.Button:
                foreach (var b in buttons)
                    if (!input.IsButtonHeld(b))
                        return false;
                return true;

            default:
                return false;
        }
    }

    public readonly bool Pressed(in InputState input)
    {
        // For multiple inputs, "pressed" means that the final input must be pressed, while the others must be held

        if (!Valid)
            return false;

        switch (Type)
        {
            case UserInputType.Key:
                for (int i = 0; i < keys.Length - 1; i++)
                    if (!input.IsKeyHeld(keys[i]))
                        return false;

                if (!input.IsKeyPressed(keys[^1]))
                    return false;

                return true;

            case UserInputType.Button:
                for (int i = 0; i < buttons.Length - 1; i++)
                    if (!input.IsButtonHeld(buttons[i]))
                        return false;

                if (!input.IsButtonPressed(buttons[^1]))
                    return false;

                return true;

            default:
                return false;
        }
    }

    public readonly bool Released(in InputState input)
    {
        // For multiple inputs, "released" means that the final input must be released, while the others must be held

        if (!Valid)
            return false;

        switch (Type)
        {
            case UserInputType.Key:
                for (int i = 0; i < keys.Length - 1; i++)
                    if (!input.IsKeyHeld(keys[i]))
                        return false;

                if (!input.IsKeyReleased(keys[^1]))
                    return false;

                return true;

            case UserInputType.Button:
                for (int i = 0; i < buttons.Length - 1; i++)
                    if (!input.IsButtonHeld(buttons[i]))
                        return false;

                if (!input.IsButtonReleased(buttons[^1]))
                    return false;

                return true;

            default:
                return false;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is UserInput input && Equals(input);
    }

    public bool Equals(UserInput other)
    {
        return Inputs.SequenceEqual(other.Inputs) &&
               Type == other.Type;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Inputs, Type);
    }

    public enum UserInputType
    {
        Key,
        Button
    }

    public override string ToString()
    {
        return FriendlyName;
    }

    public static bool operator ==(UserInput left, UserInput right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserInput left, UserInput right)
    {
        return !(left == right);
    }
}
