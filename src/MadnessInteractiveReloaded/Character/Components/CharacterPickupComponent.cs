using System.Numerics;
using Walgelijk;

namespace MIR;

[RequiresComponents(typeof(CharacterComponent))]
public class CharacterPickupComponent : Component
{
    public float Time;
    public float Duration = 0.4f;
    public float PickupTime = 0.4f;
    public ComponentRef<TransformComponent> Target;

    public Vector2[] LastHandPosePositions = new Vector2[2];
}
