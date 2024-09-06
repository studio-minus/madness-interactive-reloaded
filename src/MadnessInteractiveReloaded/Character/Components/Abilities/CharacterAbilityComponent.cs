using System;
using Walgelijk;

namespace MIR;

[RequiresComponents(typeof(CharacterComponent))]
public abstract class CharacterAbilityComponent : Component, IAnimationConstraintComponent
{
    public abstract string DisplayName { get; }

    public bool IsUsing = false;
    public readonly AbilitySlot Slot;
    public readonly AbilityBehaviour Behaviour;

    protected Game Game => Game.Main;
    protected Window Window => Game.Window;
    protected Scene Scene => Game.Scene;
    protected InputState Input => Game.State.Input;
    protected Time Time => Game.State.Time;
    protected AudioRenderer Audio => Game.AudioRenderer;

    public bool Initialised = false;

    protected CharacterAbilityComponent(AbilitySlot slot, AbilityBehaviour behaviour)
    {
        Slot = slot;
        Behaviour = behaviour;
    }

    public virtual void Initialise(AbilityParams a) { }
    public abstract void StartAbility(AbilityParams a);
    public abstract void UpdateAbility(AbilityParams a);
    public virtual void FixedUpdateAbility(AbilityParams a, float dt) { }
    public abstract void EndAbility(AbilityParams a);

    public virtual AnimationConstraint Constraints { get; } = AnimationConstraint.AllowAll;

    // TODO support AI
    // add a function that takes the Ai component and does the processing shit

    /// <summary>
    /// Returns true if the given ability type has a constructor that takes an <see cref="AbilitySlot"/>. False otherwise.
    /// </summary>
    public static bool OccupiesSlot(Type t)
    {
        if (!t.IsAssignableTo(typeof(CharacterAbilityComponent)))
            throw new Exception($"Given type {t} is not a {nameof(CharacterAbilityComponent)}");

        var constructors = t.GetConstructors(System.Reflection.BindingFlags.Public);

        foreach (var ctor in constructors)
        {
            var @params = ctor.GetParameters();

            if (@params.Length == 0)
                continue;

            if (@params.Length > 1)
                continue;
                //throw new Exception($"Given type {t} has an invalid constructor {ctor}");

            if (@params[0].ParameterType == typeof(AbilitySlot))
                return true;
        }

        return false;
    }
}
