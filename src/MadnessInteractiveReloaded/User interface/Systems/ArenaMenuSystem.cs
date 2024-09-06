using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class ArenaMenuSystem : Walgelijk.System
{
    private static readonly MenuCharacterRenderer menuCharacterRenderer = new();

    private Rect playerDrawRect;
    private RenderTexture playerDrawTarget = new(1024,1024, flags: RenderTargetFlags.None);

    public override void Update()
    {
        float v = float.Max(float.Min(Window.Width - 400, Window.Height), Window.Width * 0.333f);
        float s = (Window.Width - v) * 0.5f - Ui.Theme.Base.Padding * 2;
        playerDrawRect = new Rect(
            new Vector2(Window.Width * 0.5f, Window.Height - v * 0.5f), 
            new Vector2(v)
        );

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Texture = playerDrawTarget;
        Draw.Quad(playerDrawRect);

        ProcessPlayerCharacter();

        Ui.Layout.Size(s, 40).StickRight().StickBottom();
        if (Ui.Button("Enter arena"))
        {
            MadnessUtils.TransitionScene(ArenaScene.Create);
        }
    }

    private void ProcessPlayerCharacter()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
        {
            Level.CurrentLevel = null;
            player = Prefabs.CreatePlayer(Scene, default);
            character = Scene.GetComponentFrom<CharacterComponent>(player.Entity);
            character.AimTargetPosition = new Vector2(1000, 0);
        }

        if (Input.IsKeyReleased(Key.F5))
#if DEBUG
            character.NeedsLookUpdate = true;
#endif

            if (character.Positioning.IsFlipped)
            {
                character.Positioning.IsFlipped = false;
                character.NeedsLookUpdate = true;
            }

        character.Positioning.ShouldFeetFollowBody = false;
        character.Positioning.GlobalTarget = default;

        if (player != null)
            player.RespondToUserInput = false;

        if (!character.IsPlayingAnimation)
        {
            var next = Animations.CharacterCreationIdleAnimation;
            character.PlayAnimation(next, .5f);
        }
    }


    public override void Render()
    {
        if (!MadnessUtils.FindPlayer(Scene, out _, out var character))
            return;

        menuCharacterRenderer.HorizontalFlip = false;
        menuCharacterRenderer.Render(Window, playerDrawTarget, character);
    }
}
