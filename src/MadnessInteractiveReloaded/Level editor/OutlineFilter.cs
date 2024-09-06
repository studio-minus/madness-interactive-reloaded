namespace MIR.LevelEditor;

/// <summary>
/// Things that should be outlined.
/// </summary>
public struct OutlineFilter
{
    public bool Visible, Selectable;

    public OutlineFilter(bool visible, bool selectable)
    {
        Visible = visible;
        Selectable = selectable;
    }

    public OutlineFilter(bool visibleAndSelectable)
    {
        Visible = Selectable = visibleAndSelectable;
    }

    public bool ShouldDisableRaycast => !Visible || !Selectable;
}

