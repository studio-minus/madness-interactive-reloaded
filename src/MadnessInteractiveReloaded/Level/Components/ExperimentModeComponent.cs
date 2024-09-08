using System;
using System.Numerics;
using Walgelijk;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// Data for <see cref="ExperimentModeSystem"/>.
/// </summary>
public class ExperimentModeComponent : Component
{
    public bool IsEditMode = false;
    public Tab CurrentTab = Tab.Characters;
    public readonly SelectionManager<IExperimentSelectable> SelectionManager = new();

    public float TimeSinceMenuOpened;
    public string SelectedFaction = "aahw";
    public bool DrawPropertyMenu = true;
    public ExperimentPlacableObject? CurrentlyPlacing;
    public ContextMenu? ContextMenuInstance;
    public bool AutoSpawnSettingsOpen = false;
    public bool ImprobabilityDisksOpen = false;
    public string WeaponFilter = string.Empty;
    public string MusicFilter = string.Empty;
    public PlayerTarget PlayerCameraTarget = new();
    public FreeMoveTarget FreeMoveCameraTarget = new();

    public void CopySceneToSelectionManager(Scene scene)
    {
        SelectionManager.Selectables.Clear();

        foreach (var item in scene.GetAllComponentsOfType<WeaponComponent>())
            SelectionManager.Selectables.Add(new ExperimentSelectableWeapon(item));

        foreach (var item in scene.GetAllComponentsOfType<CharacterComponent>())
            SelectionManager.Selectables.Add(new ExperimentSelectableCharacter(item));

        SelectionManager.UpdateOrder();
    }

    public readonly struct ContextMenu
    {
        public readonly Vector2 ScreenPosition;
        public readonly Action ProcessUi;

        public ContextMenu(Vector2 screenPosition, Action processUi)
        {
            ScreenPosition = screenPosition;
            ProcessUi = processUi;
        }
    }

    public enum Tab
    {
        Characters,
        Weapons,
        Music,
        GameRules,
        Settings
    }

    public interface IExperimentSelectable : ISelectable
    {
        public Entity Entity { get; }
        public Rect BoundingBox { get; }
        public string Name { get; }
        public void ProcessUi(Scene scene, ExperimentModeComponent exp);
        public int UiHeight { get; }
        public void Translate(Scene scene, Vector2 offset);
        public void Rotate(Scene scene, float th);
        public void OnDrop(Scene scene);

        bool ISelectable.ContainsPoint(Vector2 p) => BoundingBox.ContainsPoint(p);
    }
}
