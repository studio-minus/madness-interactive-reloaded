namespace MIR;

/// <summary>
/// Static class that holds animations and registers them to <see cref="Registries.Animations"/>.
/// </summary>
public static class Animations
{
    public static readonly CharacterAnimation[] FistFightAnimations = {
        Registries.Animations.Get("melee_unarmed_adept_1"),
        Registries.Animations.Get("melee_unarmed_adept_2"),
    };

    public static readonly CharacterAnimation[] FistMeleeHits = {
        Registries.Animations.Get("hit_enemy_1"),
        Registries.Animations.Get("hit_enemy_2"),
    };

    public static readonly CharacterAnimation[] Dodge ={
        Registries.Animations.Get("dodge_1"),
        Registries.Animations.Get("dodge_2"),
    };

    public static readonly DoubleSidedMultiple<CharacterAnimation> Pickup = new([
        Registries.Animations.Get("pickup_1"),
    ], [
        Registries.Animations.Get("pickup_2"),
    ]);

    public static readonly CharacterAnimation[] Dancing ={
        Registries.Animations.Get("dancing_1"),
        Registries.Animations.Get("dancing_2"),
        Registries.Animations.Get("dancing_3"),
        Registries.Animations.Get("dancing_4"),
    };

    public static readonly CharacterAnimation MainMenuLoopDown = Registries.Animations.Get("main_menu_loop_gundown");
    public static readonly CharacterAnimation MainMenuLoopUp = Registries.Animations.Get("main_menu_loop_gunup");
    public static readonly CharacterAnimation MainMenuTransitionDown = Registries.Animations.Get("main_menu_transition_togundown");
    public static readonly CharacterAnimation MainMenuTransitionUp = Registries.Animations.Get("main_menu_transition_togunup");

    public static readonly CharacterAnimation CharacterCreationIdleAnimation = Registries.Animations.Get("charactercreation_idle");
    public static readonly CharacterAnimation[] CharacterCreationFaceAnimations ={
        Registries.Animations.Get("charactercreation_face_1"),
    };
    public static readonly CharacterAnimation[] CharacterCreationHeadAnimations ={
        Registries.Animations.Get("charactercreation_head_1"),
    };
    public static readonly CharacterAnimation[] CharacterCreationBodyAnimations ={
        Registries.Animations.Get("charactercreation_body_1"),
    };
    public static readonly CharacterAnimation[] CharacterCreationHandsAnimations ={
        Registries.Animations.Get("charactercreation_hands_1"),
    };

    public static readonly DoubleSidedMultiple<CharacterAnimation> DeathByHeadshot = new()
    {
        FromFront = new[] {
            Registries.Animations.Get("f_headshot_touch_for"),
            Registries.Animations.Get("f_headshot_touch_for2"),
            Registries.Animations.Get("f_headshot_touch_back"),
            Registries.Animations.Get("f_headshot_touch_back2"),
            Registries.Animations.Get("f_headshot_suffer_for"),
            Registries.Animations.Get("f_headshot_suffer_for2"),
            Registries.Animations.Get("f_headshot_suffer_back"),
            Registries.Animations.Get("f_headshot_suffer_back2"),
            Registries.Animations.Get("f_headshot_stumble_for"),
            Registries.Animations.Get("f_headshot_stumble_for2"),
            Registries.Animations.Get("f_headshot_stumble_for3"),
            Registries.Animations.Get("f_headshot_stumble_back"),
            Registries.Animations.Get("f_headshot_stumble_back2"),
            Registries.Animations.Get("f_headshot_stumble_back3"),
            Registries.Animations.Get("f_headshot_pain_for"),
            Registries.Animations.Get("f_headshot_pain_for2"),
            Registries.Animations.Get("f_headshot_pain_back"),
            Registries.Animations.Get("f_headshot_pain_back2"),
            Registries.Animations.Get("f_headshot_limp_for"),
            Registries.Animations.Get("f_headshot_limp_back"),
            Registries.Animations.Get("f_headshot_fly_back"),
            Registries.Animations.Get("f_headshot_instafall_back"),
        },
        FromBehind = new[] {
            Registries.Animations.Get("b_headshot_touch_back"),
            Registries.Animations.Get("b_headshot_stumble_for"),
        },
    };

    public static readonly DoubleSidedMultiple<CharacterAnimation> DeathByThroatshot = new()
    {
        FromFront = new[] {
            Registries.Animations.Get("f_throatshot_touch_back"),
            Registries.Animations.Get("f_throatshot_touch_for"),
            Registries.Animations.Get("f_throatshot_suffer_back"),
            Registries.Animations.Get("f_throatshot_stumble_back"),
            Registries.Animations.Get("f_throatshot_stumble_back2"),
            Registries.Animations.Get("f_throatshot_pain_for"),
            Registries.Animations.Get("f_throatshot_pain_for2"),
            Registries.Animations.Get("f_throatshot_limp_for"),
        },
        FromBehind = new[] {
            Registries.Animations.Get("b_throatshot_stumble_for"),
            Registries.Animations.Get("b_throatshot_stumble_back"),
        },
    };

    public static readonly DoubleSidedMultiple<CharacterAnimation> DeathByBodyshot = new()
    {
        FromFront = new[] {
            Registries.Animations.Get("f_bodyshot_touch_for"),
            Registries.Animations.Get("f_bodyshot_touch_for2"),
            Registries.Animations.Get("f_bodyshot_suffer_back"),
            Registries.Animations.Get("f_bodyshot_stumble_back"),
            Registries.Animations.Get("f_bodyshot_pain_for"),
        },
        FromBehind = new[] {
            Registries.Animations.Get("b_bodyshot_pain_for"),
            Registries.Animations.Get("b_bodyshot_stumble_for"),
        },
    };

    public static readonly DoubleSidedMultiple<CharacterAnimation> DeathByLegshot = new()
    {
        FromFront = new[] {
            Registries.Animations.Get("f_legshot_pain_back"),
            Registries.Animations.Get("f_legshot_suffer_for")
        },
        FromBehind = new[] {
            Registries.Animations.Get("b_legshot_suffer_back")
        },
    };

    public static readonly DoubleSidedMultiple<CharacterAnimation> Throw = new()
    {
        FromFront = new[] { Registries.Animations.Get("throw_left") },
        FromBehind = new[] { Registries.Animations.Get("throw_right") },
    };

    public static CharacterAnimation? GetStationaryJumpDodge(AgilitySkillLevel agility)
        => agility switch
        {
            AgilitySkillLevel.Master => Registries.Animations.Get("jump_master_stationary"),
            _ => null,
        };

    public static CharacterAnimation? GetBackwardsJumpDodge(AgilitySkillLevel agility)
        => agility switch
        {
            AgilitySkillLevel.Novice => Registries.Animations.Get("jump_novice_backward"),
            AgilitySkillLevel.Adept => Registries.Animations.Get("jump_adept_backward"),
            AgilitySkillLevel.Master => Registries.Animations.Get("jump_master_backward"),
            _ => null,
        };

    public static CharacterAnimation? GetForwardsJumpDodge(AgilitySkillLevel agility)
        => agility switch
        {
            AgilitySkillLevel.Novice => Registries.Animations.Get("jump_novice_forward"),
            AgilitySkillLevel.Adept => Registries.Animations.Get("jump_adept_forward"),
            AgilitySkillLevel.Master => Registries.Animations.Get("jump_master_forward"),
            _ => null,
        };

    public static readonly CharacterAnimation SpawnFromSky = Registries.Animations.Get("spawn_jump");

    public static readonly CharacterAnimation Scared1 = Registries.Animations.Get("scared_1");
    public static readonly CharacterAnimation Scared2 = Registries.Animations.Get("scared_2");
}

