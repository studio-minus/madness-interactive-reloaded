using System.Numerics;

namespace MIR;

/// <summary>
/// A gizmo that can be dragged around with the mouse.
/// </summary>
public interface IDraggableGizmo
{
    WeaponInstructions? Weapon { get; set; }
    void OnDrag(Vector2 delta);
}

/// <summary>
/// Gizmo for <see cref="WeaponInstructions.HoldPoints"/>.
/// </summary>
public class HoldPointGizmo : IDraggableGizmo
{
    public WeaponInstructions? Weapon { get; set; }
    public int HoldPointIndex;

    public void OnDrag(Vector2 delta)
    {
        if (Weapon == null)
            return;

        bool firstPoint = HoldPointIndex == 0;
        if (firstPoint)
            delta *= -1;

        Weapon.HoldPoints[HoldPointIndex] += delta;
    }
}

/// <summary>
/// Gizmo for <see cref="WeaponInstructions.BarrelEndPoint"/>.
/// </summary>
public class BarrelGizmo : IDraggableGizmo
{
    public WeaponInstructions? Weapon { get; set; }

    public void OnDrag(Vector2 delta)
    {
        if (Weapon == null)
            return;

        Weapon.BarrelEndPoint += delta;
    }
}

/// <summary>
/// Gizmo for <see cref="WeaponInstructions.CasingEjectionPoint"/>.
/// </summary>
public class CasingEjectionPointGizmo : IDraggableGizmo
{
    public WeaponInstructions? Weapon { get; set; }

    public void OnDrag(Vector2 delta)
    {
        if (Weapon == null)
            return;

        Weapon.CasingEjectionPoint += delta;
    }
}
