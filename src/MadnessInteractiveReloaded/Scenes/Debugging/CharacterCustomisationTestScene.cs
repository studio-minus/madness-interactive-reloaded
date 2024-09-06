using MIR.LevelEditor.Objects;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// Debug scene for character customisation.
/// </summary>
public static class CharacterCreationTestScene
{
    public static Scene Create(Game game)
    {
        game.AudioRenderer.StopAll();

        var level = new Level();
        level.LevelBounds = new Rect(-512, 0, 512, 512);

        level.FloorLine = new()
        {
            new Vector2(-512, 0),
            new Vector2(512, 0)
        };

        //level.Objects.Add(new LevelObjects.CameraSection(null!, new Rect(-2000, 0, 2000, 1024)));
        level.Objects.Add(new PlayerSpawn(null!) { Position = new Vector2(0, 128) });
        level.Objects.Add(new MovementBlocker(null!, new Rect(-1000, 0, -512, 1000)));
        level.Objects.Add(new MovementBlocker(null!, new Rect(512, 0, 1000, 1000)));

        level.LevelBounds = new Rect(-2000, -30, 2000, 1000);

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, true, level);

        if (!MadnessUtils.FindPlayer(scene, out _, out var character))
            throw new System.Exception("wtf where is the player");

        scene.AddSystem(new CharacterCustomisationTestSystem());

        scene.AttachComponent(scene.CreateEntity(), new CharTestComponent(character.Look));
        scene.RemoveSystem<PlayerUISystem>();

        return scene;
    }

    public class CharTestComponent : Component
    {
        public CharacterLook Look;
        public Tab CurrentTab;

        public CharTestComponent(CharacterLook look)
        {
            Look = look;
        }
    }

    public enum Tab
    {
        HeadL0,
        HeadL1,
        HeadL2,
        HeadL3,

        BodyL0,
        BodyL1,
        BodyL2,

        Gloves
    }

    public static object? GetRegistryForTab(Tab tab)
    {
        return tab switch
        {
            Tab.HeadL0 
                => Registries.Armour.Head,

            Tab.HeadL1 or Tab.HeadL2 or Tab.HeadL3 
                => Registries.Armour.HeadAccessory,

            Tab.BodyL0 
                => Registries.Armour.Body,

            Tab.BodyL1 or Tab.BodyL2 
                => Registries.Armour.BodyAccessory,

            Tab.Gloves 
                => Registries.Armour.HandArmour,

            _ => null,
        };
    }

    public static void SetPiece(CharacterLook look, Tab tab, ArmourPiece? piece)
    {
        switch (tab)
        {
            case Tab.HeadL0:
                look.SetHeadLayer(-1, piece);
                break;
            case Tab.HeadL1:
                look.SetHeadLayer(0, piece);
                break;
            case Tab.HeadL2:
                look.SetHeadLayer(1, piece);
                break;
            case Tab.HeadL3:
                look.SetHeadLayer(2, piece);
                break;
            case Tab.BodyL0:
                look.SetBodyLayer(-1, piece);
                break;
            case Tab.BodyL1:
                look.SetBodyLayer(0, piece);
                break;
            case Tab.BodyL2:
                look.SetBodyLayer(1, piece);
                break;
        }
    }

    public static bool IsSelected(CharacterLook look, Tab tab, object? piece)
    {
        return tab switch
        {
            Tab.HeadL0 => look.Head == piece,
            Tab.HeadL1 => look.HeadLayer1 == piece,
            Tab.HeadL2 => look.HeadLayer2 == piece,
            Tab.HeadL3 => look.HeadLayer3 == piece,
            Tab.BodyL0 => look.Body == piece,
            Tab.BodyL1 => look.BodyLayer1 == piece,
            Tab.BodyL2 => look.BodyLayer2 == piece,
            Tab.Gloves => look.Hands == piece,
            _ => false,
        };
    }

    public class CharacterCustomisationTestSystem : Walgelijk.System
    {
        public override void Update()
        {
            if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
                return;
            if (!Scene.FindAnyComponent<CharTestComponent>(out var charTest))
                return;
            var look = character.Look;
            Ui.Layout.Width(300).FitHeight(false).VerticalLayout();
            Ui.StartScrollView();
            {
                var r = GetRegistryForTab(charTest.CurrentTab);
                if (r != null)
                {
                    if (charTest.CurrentTab is not (Tab.HeadL0 or Tab.BodyL0))
                    {
                        Ui.Layout.FitWidth(false).Height(32);
                        if (Ui.ClickButton("None"))
                        {
                            SetPiece(look, charTest.CurrentTab, null);
                            character.NeedsLookUpdate = true;
                        }
                    }

                    if (r is Registry<ArmourPiece> rr)
                    {
                        foreach (var key in rr.GetAllKeys())
                        {
                            var piece = rr.Get(key);

                            if (IsSelected(look, charTest.CurrentTab, piece))
                                Ui.Theme.OutlineColour(Colors.Red).OutlineWidth(4).Once();
                            Ui.Layout.FitWidth(false).Height(32);
                            if (Ui.ClickButton(piece.Name, identity: key.GetHashCode()))
                            {
                                SetPiece(look, charTest.CurrentTab, piece);
                                character.NeedsLookUpdate = true;
                            }
                        }
                    }
                    else if (r is Registry<HandArmourPiece> har)
                    {
                        foreach (var key in har.GetAllKeys())
                        {
                            var piece = har.Get(key);

                            if (IsSelected(look, charTest.CurrentTab, piece))
                                Ui.Theme.OutlineColour(Colors.Red).OutlineWidth(4).Once();
                            Ui.Layout.FitWidth(false).Height(32);
                            if (Ui.ClickButton(piece.Name, identity: key.GetHashCode()))
                            {
                                look.Hands = piece;
                                character.NeedsLookUpdate = true;
                            }
                        }
                    }
                }
            }
            Ui.End();

            Ui.Layout.Move(300, 0).FitHeight().Width(48).VerticalLayout();
            Ui.StartScrollView();
            {
                foreach (Tab v in Enum.GetValues<Tab>())
                {
                    Ui.Layout.FitWidth(false).Height(32);
                    if (Ui.ClickButton(Enum.GetName(v) ?? "???", identity: (int)v))
                        charTest.CurrentTab = v;
                }
            }
            Ui.End();
        }
    }
}
