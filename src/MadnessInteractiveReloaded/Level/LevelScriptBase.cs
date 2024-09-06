using Walgelijk;

namespace MIR;

public abstract class LevelScriptBase
{
    public Game Game => Walgelijk.Game.Main;
    public Scene Scene => Game.Scene;
    public InputState Input => Game.State.Input;
    public Time Time => Game.State.Time;
    public AudioRenderer Audio => Game.AudioRenderer;
    public Window Window => Game.Window;
    public IGraphics Graphics => Window.Graphics;
    public DebugDraw DebugDraw => Scene.Game.DebugDraw;
}
