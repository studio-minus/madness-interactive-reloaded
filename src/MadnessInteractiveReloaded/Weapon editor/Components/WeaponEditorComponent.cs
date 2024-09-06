using System.IO;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Singleton component for running the <see cref="WeaponEditorSystem"/>.
/// </summary>
public class WeaponEditorComponent : Component
{
    public const float TopbarHeight = 32;
    public const float SidebarWidth = 300;
    public const float SidebarTabWidth = 48;
    public const float CurveEditorHeight = 270;
    public const float TallCurveEditorHeight = 600;

    /// <summary>
    /// A macro value for common height for most of the controls in this UI to use.
    /// </summary>
    public const int ControlHeight = 32;

    /// <summary>
    /// A macro value for common width for most of the controls in this UI to use.
    /// </summary>
    public const int ControlSpacing = 4;

    /// <summary>
    /// Information about the weapon file being edited.
    /// </summary>
    public FileInfo? File;

    /// <summary>
    /// The weapon being edited.
    /// </summary>
    public WeaponInstructions? Weapon;

    /// <summary>
    /// Which editor tab we're on.
    /// </summary>
    public PropertyTab CurrentPropertyTab;

    /// <summary>
    /// Which curve tab we're on.
    /// </summary>
    public CurveTab CurrentCurveTab;

    /// <summary>
    /// Is the curve editor expanded?
    /// </summary>
    public bool ExpandedCurveEditor;

    public AnimatedWeaponPart? CurrentSelectedAnimatedPart;

    public enum CurveTab
    {
        None,
        Translation,
        Visibility
    }

    public enum PropertyTab
    {
        None,
        BasicInfo,
        WeaponDamage,

        FirearmSettings,
        MeleeSettings,

        SoundSettings,
        AnimatedParts,
        HoldPointsEditor
    }

    public static Texture GetTabIcon(PropertyTab tab)
    {
        return tab switch
        {
            PropertyTab.BasicInfo => Assets.Load<Texture>("textures/ui/weaponeditor/settings.png"),
            PropertyTab.WeaponDamage => Assets.Load<Texture>("textures/ui/weaponeditor/medical-cross.png"),
            PropertyTab.FirearmSettings => Assets.Load<Texture>("textures/ui/weaponeditor/gun.png"),
            PropertyTab.MeleeSettings => Assets.Load<Texture>("textures/ui/weaponeditor/swords.png"),
            PropertyTab.SoundSettings => Assets.Load<Texture>("textures/ui/weaponeditor/volume.png"),
            PropertyTab.AnimatedParts => Assets.Load<Texture>("textures/ui/weaponeditor/keyframe-align-vertical.png"),
            PropertyTab.HoldPointsEditor => Assets.Load<Texture>("textures/ui/weaponeditor/hand-stop.png"),
            _ => Texture.ErrorTexture,
        };
    }
}

