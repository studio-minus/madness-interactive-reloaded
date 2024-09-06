using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR; //👩👩

public class AuditorFireSystem : Walgelijk.System
{
    public override void Update()
    {
        Draw.Reset();
        foreach (var fire in Scene.GetAllComponentsOfType<AuditorFireComponent>())
        {
            if (!fire.Character.TryGet(Scene, out var character) || !character.IsAlive)
            {
                Scene.RemoveEntity(fire.Entity);
                return;
            }

            const float scaling = 2.7f; // scaling value hapazardly chosen by Koof
            const int columns = 6;
            const int rows = 4;
            Draw.Order = character.BaseRenderOrder.OffsetOrder(-500);

            {
                var head = fire.HeadFlipbook.Get(Scene);
                var headTransform = Scene.GetComponentFrom<TransformComponent>(character.Positioning.Head.Entity);
                var offset = new Vector2(-5, 5); // I don't know what determines this value, just eyeball it.

                Draw.Material = head.Material;
                Draw.Texture = fire.HeadTexture.Value;

                var headSize = new Vector2(Draw.Texture.Size.X / columns, Draw.Texture.Size.Y / rows) * scaling;
                //offset.X *= character.Positioning.FlipScaling;
                var headRect = new Rect(
                    headTransform.GetBounds().GetCenter(),
                    headSize);

                Draw.Colour = Colors.Black.WithAlpha(0.5f);
                Draw.TransformMatrix =
                    Matrix3x2.CreateScale(character.Positioning.FlipScaling, 1, headRect.GetCenter())
                    * Matrix3x2.CreateRotation(character.Positioning.Head.GlobalRotation * Utilities.DegToRad, headRect.GetCenter());
                Draw.Quad(headRect.Translate(offset));
            }

            {
                var body = fire.BodyFlipbook.Get(Scene);
                var bodyTransform = Scene.GetComponentFrom<TransformComponent>(character.Positioning.Body.Entity);
                var offset = new Vector2(-20, 0); // I don't know what determines this value, just eyeball it.

                Draw.Material = body.Material;
                Draw.Texture = fire.BodyTexture.Value;

                var bodySize = new Vector2(Draw.Texture.Size.X / columns, Draw.Texture.Size.Y / rows) * scaling;
                var bodyRect = new Rect(
                    bodyTransform.GetBounds().GetCenter(),
                    bodySize);

                Draw.Colour = Colors.Black.WithAlpha(0.5f);
                Draw.TransformMatrix =
                    Matrix3x2.CreateScale(character.Positioning.FlipScaling, 1, bodyRect.GetCenter())
                    * Matrix3x2.CreateRotation(character.Positioning.Body.GlobalRotation * Utilities.DegToRad, bodyRect.GetCenter());
                Draw.Quad(bodyRect.Translate(offset));
            }
        }
    }
}

public class AuditorFireComponent : Component
{
    public ComponentRef<CharacterComponent> Character;
    public ComponentRef<FlipbookComponent> HeadFlipbook, BodyFlipbook;
    public AssetRef<Texture> HeadTexture, BodyTexture;
}

public class CharacterAbilitySystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var ability in Scene.GetAllComponentsOfType<CharacterAbilityComponent>())
        {
            var character = Scene.GetComponentFrom<CharacterComponent>(ability.Entity);
            ProcessAbility(ability, character);
        }
    }

    public override void FixedUpdate()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var ability in Scene.GetAllComponentsOfType<CharacterAbilityComponent>())
        {
            var character = Scene.GetComponentFrom<CharacterComponent>(ability.Entity);

            ability.FixedUpdateAbility(new(Scene, character), Time.FixedInterval);
        }
    }

    private void ProcessAbility(CharacterAbilityComponent ability, CharacterComponent character)
    {
        if (!ability.Initialised)
        {
            ability.Initialise(new(Scene, character));
            ability.Initialised = true;
        }

        if (!character.IsAlive)
        {
            if (ability.IsUsing)
                ability.EndAbility(new(Scene, character));
            ability.IsUsing = false;
            return;
        }

        var action = ability.Slot.AsAction();

        if (!ability.IsUsing && Input.ActionPressed(action))
            ability.StartAbility(new(Scene, character));

        // TODO support AI
        switch (ability.Behaviour)
        {
            case AbilityBehaviour.Always:
                ability.IsUsing = true;
                break;  
            case AbilityBehaviour.Hold:
                ability.IsUsing = Input.ActionHeld(action);
                break;
            case AbilityBehaviour.Toggle:
                if (Input.ActionPressed(action))
                    ability.IsUsing = !ability.IsUsing;
                break;
        }

        ability.UpdateAbility(new(Scene, character));

        if (Input.ActionReleased(action))
            ability.EndAbility(new(Scene, character));
    }
}
