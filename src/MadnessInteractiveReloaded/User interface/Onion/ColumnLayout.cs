using Walgelijk.Onion.Layout;
using Walgelijk.Onion.Controls;

namespace MIR;

/// <summary>
/// Column style node layout.
/// </summary>
public readonly struct ColumnLayout : ILayout
{
    public readonly float[] Columns;

    public ColumnLayout(params float[] columns)
    {
        Columns = columns;
    }

    public void Apply(in ControlParams p, int index, int childId)
    {
        if (Columns == null || index < 0 || index >= Columns.Length)
            return;

        var me = p.Instance.Rects.GetInnerContentRect();

        var child = p.Tree.EnsureInstance(childId);
        child.Rects.Intermediate.Width = me.Width * Columns[index];
    }
}
