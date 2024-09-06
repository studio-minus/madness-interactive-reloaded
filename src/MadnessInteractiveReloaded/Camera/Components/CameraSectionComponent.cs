using Walgelijk;

namespace MIR;

/// <summary>
/// Describes a rectangular bounds that will constrian the camera.
/// </summary>
public class CameraSectionComponent : Component
{
    public bool Enabled = true;
    public Rect Section;
    public bool HasFocus;

    public CameraSectionComponent(Rect section)
    {
        Section = section;
    }
}
