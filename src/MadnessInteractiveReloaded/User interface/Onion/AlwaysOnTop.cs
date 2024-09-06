using Walgelijk.Onion.Layout;
using Walgelijk.Onion.Controls;

namespace MIR;

/// <summary>
/// Make a node always appear on top.
/// </summary>
public readonly struct AlwaysOnTop : IConstraint
{
    public void Apply(in ControlParams p)
    {
        p.Node.AlwaysOnTop = true;
    }
}
