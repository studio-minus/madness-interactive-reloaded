using Walgelijk;
//👩
namespace MIR;

public readonly struct AbilityParams
{
    public readonly Scene Scene;
    public readonly CharacterComponent Character;

    public readonly ComponentRef<AiComponent> Ai;
    public readonly ComponentRef<PlayerComponent> Player;

    public readonly InputState Input => Scene.Game.State.Input;
    public readonly Time Time => Scene.Game.State.Time;

    public AbilityParams(Scene scene, CharacterComponent character)
    {
        Scene = scene;
        Character = character;
        Ai = new(character.Entity);
        Player = new(character.Entity);
    }
}
