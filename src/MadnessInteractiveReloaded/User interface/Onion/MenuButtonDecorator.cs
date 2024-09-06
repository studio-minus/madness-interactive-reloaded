using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Decorators;

namespace MIR;

/// <summary>
/// Decorate menu buttons.
/// </summary>
public struct MenuButtonDecorator : IDecorator
{
    public static float LastHeight = -1;

    public void RenderAfter(in ControlParams p)
    {
        if (p.Instance.IsHover)
            LastHeight = p.Instance.Rects.ComputedGlobal.GetCenter().Y;
    }

    public void RenderBefore(in ControlParams p)
    {
    }
}
