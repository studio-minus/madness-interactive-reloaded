using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Draws the black bars around the level to obscure things
/// </summary>
public class LevelBorderSystem : Walgelijk.System
{
    // TODO the Window.WorldBounds lag behind 1 frame
    public override void Render()
    {
        var lvl = Level.CurrentLevel;
        if (lvl == null)
            return;

        if (!Scene.FindAnyComponent<GameModeComponent>(out var c))
            return;

        if (MadnessUtils.IsCutscenePlaying(Scene))
            return;

        Draw.Reset();
        Draw.Colour = Colors.Black;
        Draw.Order = RenderOrders.BackgroundInFront.OffsetOrder(1000);

        var leftQuad = (lvl.LevelBounds with { MaxX = Window.WorldBounds.MinX }).SortComponents();
        var rightQuad = (lvl.LevelBounds with { MinX = Window.WorldBounds.MaxX }).SortComponents();

        var topQuad = (lvl.LevelBounds with { MinY = Window.WorldBounds.MaxY }).SortComponents();
        var bottomQuad = (lvl.LevelBounds with { MaxY = Window.WorldBounds.MinY }).SortComponents();

        Draw.Quad(leftQuad);
        Draw.Quad(rightQuad);
        Draw.Quad(topQuad);
        Draw.Quad(bottomQuad);
    }
}
