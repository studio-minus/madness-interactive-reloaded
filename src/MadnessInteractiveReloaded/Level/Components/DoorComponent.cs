using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

public class DoorComponent : Component, IDisposable
{
    public static readonly float AnimationTime = 0.4f; // if you want to change this you have to do that in door.frag as well

    // door.frag shader uniform names
    public const string TimeUniform = "time";
    public const string TimeSinceChangeUniform = "timeSinceChange";
    public const string IsOpenUniform = "isOpen";

    /// <summary>
    /// Is the door open? 
    /// <br></br><i>
    /// This is not necessarily a read only variable, but I really don't recommend writing to it.
    /// You should use <see cref="Open(Scene)"/> and <see cref="Close(Scene)"/> because they also modify <see cref="IsBusyWithAnimation "/>
    /// </i>
    /// </summary>
    public bool IsOpen;

    /// <summary>
    /// Material used to draw the door
    /// </summary>
    public Material Material;

    /// <summary>
    /// Is this door busy opening or closing? 
    /// </summary>
    public bool IsBusyWithAnimation = false;

    /// <summary>
    /// Door properties
    /// </summary>
    public LevelEditor.DoorProperties Properties;

    public static float PortalOpeningDistance = 800;

    public DoorComponent(Material material, in LevelEditor.DoorProperties properties)
    {
        Material = material;
        Properties = properties;
        Material.SetUniform("mainTex", properties.EffectiveTexture);

        Properties.FacingDirection = Vector2.Normalize((Properties.BottomLeft + Properties.BottomRight) / 2 - Properties.SpawnPoint);
        if (float.IsNaN(properties.FacingDirection.X) || float.IsNaN(properties.FacingDirection.Y))
            Properties.FacingDirection = new Vector2(0, -1); // if the door is facing the camera its facing vector will be 0,0, which gets normalised to NaN,NaN, but its probably better if it points down
    }

    public void Open(Scene scene)
    {
        if (IsOpen)
            return;

        IsOpen = true;
        Material.SetUniform(IsOpenUniform, 1f);
        Material.SetUniform(TimeSinceChangeUniform, scene.Game.State.Time.SecondsSinceLoad);
        IsBusyWithAnimation = true;
        MadnessUtils.DelayPausable(AnimationTime, () => IsBusyWithAnimation = false);
        scene.Game.AudioRenderer.PlayOnce(Sounds.DoorOpen, 0.25f);
    }

    public void Close(Scene scene)
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        Material.SetUniform(IsOpenUniform, 0f);
        Material.SetUniform(TimeSinceChangeUniform, scene.Game.State.Time.SecondsSinceLoad);
        IsBusyWithAnimation = true;
        MadnessUtils.DelayPausable(AnimationTime, () => IsBusyWithAnimation = false);
        scene.Game.AudioRenderer.PlayOnce(Sounds.DoorClose, 0.25f);
    }

    public void Dispose()
    {
        DoorMaterialPool.Instance.ReturnToPool(Material);
    }
}
