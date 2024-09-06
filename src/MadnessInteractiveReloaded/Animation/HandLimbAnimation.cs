namespace MIR;

/// <summary>
/// A <see cref="LimbAnimation"/> for a hand
/// </summary>
public class HandLimbAnimation : LimbAnimation
{
    public (float timeInSeconds, HandLook? look)[]? HandLooks = null;

    public HandLook? GetHandLookForTime(float time)
    {
        if (HandLooks == null)
            return null;
        for (int i = HandLooks.Length - 1; i >= 0; i--)
        {
            (float timeInSeconds, HandLook? look) = HandLooks[i];
            if (timeInSeconds <= time)
                return look;
        }
        return null;
    }
}

