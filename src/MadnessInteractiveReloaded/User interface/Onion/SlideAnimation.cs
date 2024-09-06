using Walgelijk;
using System.Numerics;
using Walgelijk.Onion.Animations;

namespace MIR;

/// <summary>
/// Animate and slide a node.
/// </summary>
public readonly struct SlideAnimation : IAnimation
{
    public readonly Vector2 Direction;

    public SlideAnimation(Vector2 direction)
    {
        Direction = direction;
    }

    public void AnimateAlpha(ref float alpha, float t)
    {
    }

    public void AnimateColour(ref Color color, float t)
    {
    }

    public void AnimateRect(ref Rect rect, float t)
    {
        var a = rect.Translate(Direction * rect.GetSize());
        rect = Utilities.Lerp(in a, in rect, IAnimation.GetProgress(t));
    }

    public Matrix3x2 GetTransform(float t)
    {
        return Matrix3x2.Identity;
    }

    public bool ShouldRenderText(float t)
    {
        return true;
    }
}
