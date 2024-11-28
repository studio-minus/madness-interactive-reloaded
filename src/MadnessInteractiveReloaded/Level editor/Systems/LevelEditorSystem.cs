using OpenTK.Graphics.ES20;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor;

/// <summary>
/// Logic for the <see cref="LevelEditor"/>.
/// </summary>
public class LevelEditorSystem : Walgelijk.System
{
    private Vector2 lastMousePos;
    private Vector2 mouseDelta;
    private bool doorOpen = false;
    private bool uiBlock;
    private float pixelSize;
    private float doorTimer = 0;

    public override void Update()
    {
        if (!Scene.FindAnyComponent<CameraComponent>(out var camera))
        {
            Logger.Warn("There is no CameraComponent in the scene...");
            return;
        }

        if (!Scene.FindAnyComponent<CameraMovementComponent>(out var cameraMovement))
        {
            Logger.Warn("There is no CameraMovementComponent in the scene...");
            return;
        }

        Draw.Reset();
        pixelSize = 2f * (camera.OrthographicSize * camera.PixelsPerUnit);

        uiBlock = Onion.Navigator.IsBeingUsed;
        mouseDelta = Input.WorldMousePosition - lastMousePos;
        lastMousePos = Input.WorldMousePosition;
        doorTimer += Time.DeltaTime;

        //Scene.GetSystem<DebugCameraSystem>().Speed = uiBlock ? 0 : 900;

        if (!Scene.FindAnyComponent<LevelEditorComponent>(out var editor))
        {
            Logger.Warn("Level editor system without editor component...");
            return;
        }

        ref var lvl = ref editor.Level;

        // (duston): use Pressed so we don't instantly re-enter test mode when exiting test mode
        if (Input.IsKeyPressed(Key.F5) && editor.Level != null)
            MadnessUtils.TransitionScene(game => LevelEditorTestScene.Create(Game.Main, editor.Level));

        if (Input.IsKeyReleased(Key.Home))
        {
            // TODO maybe store target in level editor somewhere
            //cameraMovement.Zoom = 1;
            //cameraMovement.Position = Vector2.Zero;
        }

        if (Input.IsKeyHeld(Key.LeftControl))
        {
            if (Input.IsKeyPressed(Key.Z))
                if (Input.IsKeyHeld(Key.LeftShift))
                    editor.Redo();
                else
                    editor.Undo();

            if (Input.IsKeyPressed(Key.Y))
                editor.Redo();
        }

        if (lvl != null)
        {
            var index = 0;
            var line = lvl.FloorLine.ToArray();
            foreach (var p1 in line)
            {
                var p2 = p1;
                if (index > 0 && index < line.Length - 1)
                    p2 = line[index + 1];

                Draw.Colour = Colors.Orange;
                Draw.Line(p1, p2, 14);
                index++;
            }
        }

        if (editor.Dirty && lvl != null)
        {
            Level.CurrentLevel = lvl;
            lvl.LevelBounds = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

            foreach (var item in lvl.Objects)
                item.Disabled = editor.Filter[item.GetType()].ShouldDisableRaycast;

            editor.SelectionManager.Selectables = lvl.Objects;
            editor.SelectionManager.UpdateOrder();

            editor.UpdateLevelBounds();
            editor.UpdateFloorLine();

            editor.Dirty = false;

            //Logger.Debug("Level editor state update");
        }

        //TODO dit is lelijk :(
        Prefabs.Editor.ExampleStaticDoorMaterial.SetUniform(DoorComponent.TimeUniform, 1f);
        Prefabs.Editor.ExampleDoorMaterial.SetUniform(DoorComponent.TimeUniform, Time.SecondsSinceLoad);
        if (doorTimer > 2)
        {
            doorTimer = 0;
            doorOpen = !doorOpen;
            Prefabs.Editor.ExampleDoorMaterial.SetUniform(DoorComponent.TimeSinceChangeUniform, Time.SecondsSinceLoad);
            Prefabs.Editor.ExampleDoorMaterial.SetUniform(DoorComponent.IsOpenUniform, doorOpen ? 1f : 0f);
        }

        if (lvl != null)
        {
            if (editor.SelectionManager.SelectedObject != null)
            {
                var selected = editor.SelectionManager.SelectedObject;

                if (Input.IsKeyPressed(Key.Delete) && selected is not Objects.PlayerSpawn)
                {
                    editor.RegisterAction();

                    lvl.Objects.Remove(selected);
                    selected.Dispose();
                    editor.SelectionManager.DeselectAll();
                    Audio.Play(Sounds.UiBad);
                    editor.Dirty = true;
                }

                if ((Input.IsKeyPressed(Key.D) && Input.IsKeyHeld(Key.LeftShift)) && selected is not Objects.PlayerSpawn)
                {
                    if (selected.Clone() is not Objects.LevelObject clone)
                        throw new Exception("Failed to duplicate level editor object");
                    else
                    {
                        editor.RegisterAction();

                        lvl.Objects.Add(clone);
                        Audio.Play(Sounds.UiConfirm);
                        editor.Dirty = true;

                        Prefabs.CreateNotification(Scene, Input.WorldMousePosition, "Duplicated!", 0.2f);

                        MadnessUtils.Delay(0.03f, () =>
                        {
                            clone.TranslationTransformation.DragAccumulation = Vector2.Zero;
                            clone.TranslationTransformation.PositionBeforeDrag = selected.GetPosition();
                            Objects.LevelObject.DraggingObject = clone;
                            editor.SelectionManager.Select(clone);
                        });
                    }
                }
            }

            for (int i = lvl.Objects.Count - 1; i >= 0; i--)
            {
                Objects.LevelObject? item = lvl.Objects[i];
                if (!editor.Filter[item.GetType()].Visible)
                    continue;

                Draw.Reset();
                Draw.BlendMode = BlendMode.AlphaBlend;
                Draw.Order = RenderOrders.UserInterface.WithOrder(-1000);
                item.ProcessInEditor(Scene, Input);
            }
        }

        if (!uiBlock)
            editor.SelectionManager.UpdateState(Input.WorldMousePosition, Input.IsButtonReleased(MouseButton.Left));

        editor.PixelSize = pixelSize;
        editor.MouseDelta = mouseDelta;
    }
}
