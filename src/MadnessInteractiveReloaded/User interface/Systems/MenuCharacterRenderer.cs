using Walgelijk;

namespace MIR;

/// <summary>
/// Helps to draw a character neatly onto a separate render target (usually a render texture)
/// </summary>
public class MenuCharacterRenderer
{
    private readonly MenuCharacterDrawTask characterDrawTask = new();
    private readonly TargetRenderTask setTarget = new(null!);
    private readonly TargetRenderTask resetTarget = new(null!);

    public bool HorizontalFlip = false;

    public void Render(Window window, RenderTarget target, CharacterComponent character)
    {
        characterDrawTask.Target = target;
        characterDrawTask.HorizontalFlip = HorizontalFlip;
        setTarget.Target = target;
        resetTarget.Target = window.RenderTarget;

        var bottom = character.BaseRenderOrder.WithOrder(int.MinValue);
        var top = character.BaseRenderOrder.WithOrder(int.MaxValue);

        window.RenderQueue.Add(setTarget, bottom);
        window.RenderQueue.Add(characterDrawTask, bottom);
        window.RenderQueue.Add(resetTarget, top);
    }
}
