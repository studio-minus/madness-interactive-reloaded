using Walgelijk;

namespace MIR;

/// <summary>
/// For enemies spawning out of vertical doors.
/// </summary>
public class ExitDoorSystem : Walgelijk.System
{
    public override void Update()
    {
        foreach (var e in Scene.GetAllComponentsOfType<ExitDoorComponent>())
        {
            var ch = Scene.GetComponentFrom<CharacterComponent>(e.Entity);

            var t = e.CurrentTime / e.DurationSeconds;

            ch.Positioning.GlobalCenter = Utilities.Lerp(e.Start, e.End, Easings.Quad.Out(t));
            ch.Positioning.GlobalTarget = e.End with { Y = 0 };

            if (e.CurrentTime >= e.DurationSeconds)
            {
                Scene.DetachComponent<ExitDoorComponent>(e.Entity);
                ch.Tint = Colors.White;
                ch.NeedsLookUpdate = true;
            }
            else
            {
                ch.Tint = Utilities.Lerp(Colors.Black, Colors.White, Easings.Quad.Out(t));
                e.CurrentTime += Time.DeltaTime;
                ch.NeedsLookUpdate = true;
            }
        }
    }
}