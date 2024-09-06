using Walgelijk;

namespace MIR;

public class KonamiCodeDetector
{
    public Key[] Sequence =
    {
        Key.Up,
        Key.Up,
        Key.Down,
        Key.Down,
        Key.Left,
        Key.Right,
        Key.Left,
        Key.Right,
        Key.B,
        Key.A,
    };

    private int index;

    private float timer = 0;
    private const float maxInterval = 0.9f;

    public bool Detect(in GameState state)
    {
        var input = state.Input;
        var time = state.Time;

        timer += time.DeltaTimeUnscaled;

        if (timer > maxInterval)
            index = 0;

        if (input.IsKeyPressed(Sequence[index]))
        {
            timer = 0;
            index++;

            if (index == Sequence.Length)
            {
                timer = maxInterval;
                return true;
            }
        }

        return false;
    }
}
