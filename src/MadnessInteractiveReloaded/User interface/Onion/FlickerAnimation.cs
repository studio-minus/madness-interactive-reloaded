using Walgelijk;
using Walgelijk.Onion.Animations;

namespace MIR;

public struct FlickerAnimation : IAnimation
{
    public void AnimateColour(ref Color color, float t)
    {
        var f = (t >= 0.99f || (float.Sin(t * float.Tau) > 0));
        color.A *= f ? 1 : 0.1f;
        color.R += f ? 0 : 0.4f;
    }

    public void AnimateRect(ref Rect rect, float t)
    {
    }

    public bool ShouldRenderText(float t)
    {
        return true;
    }
}