using Walgelijk;

namespace MIR;

/// <summary>
/// Things required for posing hands.
/// </summary>
public readonly struct HandPoseParams
{
    public readonly Scene Scene;
    public readonly CharacterComponent Character;
    public readonly float DeltaTime;
    public readonly WeaponComponent? Equipped;
    public readonly TransformComponent? EquippedTransform;

    public HandPoseParams(Scene scene, CharacterComponent character, float deltaTime, WeaponComponent? equipped)
    {
        Scene = scene;
        Character = character;
        DeltaTime = deltaTime;
        Equipped = equipped;

        if (equipped != null)
            scene.TryGetComponentFrom(equipped.Entity, out EquippedTransform);
        else
            EquippedTransform = null;
    }
}
