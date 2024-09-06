using System.IO;
using Walgelijk.AssetManager;
using Xunit;

namespace MIR.Test.Deserialisation;

[Collection("Registry collection")]
public class CharacterStatsDeserialiserTest : global::System.IDisposable
{
    public CharacterStatsDeserialiserTest()
    {
        if (!Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Registries.ClearAll();
    }

    [Fact]
    public void PositiveCaseTest()
    {
        string content =
@$"{GameVersion.Version}

aiming_randomness 0.55
shooting_timeout 0.75
recoil_handling 0.85
dodge 0.35
panic 0.52
melee 0.423
accurate_shot_chance 0.457
can_deflect true
body_health 0.513
head_health 13.23
sword_seq sword.seq
unarmed_seq unarmed.seq
two_handed_seq doublo.seq
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var stats = CharacterStatsDeserialiser.Load(path);

        Assert.Equal(0.55f, stats.AimingRandomness);
        Assert.Equal(0.75f, stats.ShootingTimeout);
        Assert.Equal(0.85f, stats.RecoilHandlingAbility);
        Assert.Equal(0.35f, stats.DodgeAbility);
        Assert.Equal(0.52f, stats.PanicIntensity);
        Assert.Equal(0.423f, stats.MeleeSkill);
        Assert.Equal(0.457f, stats.AccurateShotChance);
        Assert.True(stats.CanDeflect);
        Assert.Equal(0.513f, stats.BodyHealth);
        Assert.Equal(13.23f, stats.HeadHealth);

        Assert.Equal("unarmed.seq", stats.UnarmedSeq);
        Assert.Equal("sword.seq", stats.SwordSeq);
        Assert.Equal("doublo.seq", stats.TwoHandedSeq);
    }

    [Fact]
    public void WhitespaceTest()
    {
        string content = $"{GameVersion.Version}\naiming_randomness   \t    1032.23";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var stats = CharacterStatsDeserialiser.Load(path);

        Assert.Equal(1032.23f, stats.AimingRandomness);
    }

    [Fact]
    public void InvalidPropertyTest()
    {
        string content =
@$"{GameVersion.Version}

panic 5
melee 13.72
this_does_not_exist 50.130
can_deflect true";

        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        Assert.Throws<Exceptions.SerialisationException>(() =>
        {
            var stats = CharacterStatsDeserialiser.Load(path);

            Assert.Equal(5, stats.PanicIntensity);
            Assert.Equal(13.72f, stats.MeleeSkill);
            Assert.True(stats.CanDeflect);
        });
    }

    public void Dispose()
    {
        Registries.ClearAll();
    }
}
