using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MIR;

[JsonConverter(typeof(StringEnumConverter))]
public enum GameAction
{
    None,

    Right,
    Left,
    JumpDodge,
    Interact,

    Attack,
    Melee,
    BlockAim,
    Throw,

    Ability1,
    Ability2,
    Ability3,
    Ability4
}
