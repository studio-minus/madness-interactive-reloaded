using MIR.LevelEditor.Objects;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Debug demo scenes.
/// </summary>
public static class DemoScenes
{
    public static Scene LoadCharacterCustomisationDemo(Game game)
    {
        game.AudioRenderer.StopAll();

        var level = new Level();

        level.LevelBounds = new Rect(-2000, -30, 2000, 1000);

        level.FloorLine = new()
        {
            new Vector2(-512, 0),
            new Vector2(512, 0)
        };

        level.Objects.Add(new PlayerSpawn(null) { Position = new Vector2(0, 128) });
        level.Objects.Add(new RectWall(null, new Rect(-1000, 0, -512, 1000)));
        level.Objects.Add(new RectWall(null, new Rect(512, 0, 1000, 1000)));

        var scene = SceneUtils.PrepareGameScene(game, GameMode.Unknown, true, level);
        scene.RemoveSystem<PlayerUISystem>();
        scene.AddSystem(new CharacterDemoScene());
        return scene;
    }

    public class CharacterDemoScene : Walgelijk.System
    {
        public CharacterLook intermediateLook = new CharacterLook(UserData.Instances.PlayerLook);

        public override void FixedUpdate()
        {
            if (Utilities.RandomFloat() > 0.9f) 
                intermediateLook.Head = Registries.Armour.Head.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.HeadLayer1 = Registries.Armour.HeadAccessory.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.HeadLayer2 = Registries.Armour.HeadAccessory.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.HeadLayer3 = Registries.Armour.HeadAccessory.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.Body = Registries.Armour.Body.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.BodyLayer1 = Registries.Armour.BodyAccessory.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.BodyLayer2 = Registries.Armour.BodyAccessory.GetRandomValue();

            if (Utilities.RandomFloat() > 0.8f) 
                intermediateLook.Hands = Registries.Armour.HandArmour.GetRandomValue();

            foreach (var item in Scene.GetAllComponentsOfType<CharacterComponent>())
            {
                item.Look = intermediateLook;
                item.NeedsLookUpdate = true;
            }
        }
    }
}