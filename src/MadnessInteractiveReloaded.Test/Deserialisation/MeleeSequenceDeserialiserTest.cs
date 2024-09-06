namespace MIR.Test.Deserialisation;
using System.IO;
using Walgelijk.AssetManager;
using Xunit;

[Collection("Registry collection")]
public class MeleeSequenceDeserialiserTest : global::System.IDisposable
{
    public MeleeSequenceDeserialiserTest()
    {
        if (!Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Registries.ClearAll();
        Registries.LoadAnimations();
    }

    [Fact]
    public void PositiveCaseTestSingleSided()
    {
        string content =
@$"{GameVersion.Version}

# animation_id transition_frame [hit frame array]

melee_unarmed_1		 8		[7]     
melee_unarmed_2  12         [9,5]  
 melee_unarmed_3		10   [3] 
melee_unarmed_4   -1   [4,12,5]
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var sequence = MeleeSequenceDeserialiser.Load(path);

        Assert.Equal(4, sequence.Keys.Length);

        Assert.Equal(Registries.Animations.Get("melee_unarmed_1"), sequence.Keys[0].Animation.FacingRight);
        Assert.Equal(Registries.Animations.Get("melee_unarmed_2"), sequence.Keys[1].Animation.FacingRight);
        Assert.Equal(Registries.Animations.Get("melee_unarmed_3"), sequence.Keys[2].Animation.FacingRight);
        Assert.Equal(Registries.Animations.Get("melee_unarmed_4"), sequence.Keys[3].Animation.FacingRight);

        Assert.Equal(Registries.Animations.Get("melee_unarmed_1"), sequence.Keys[0].Animation.FacingLeft);
        Assert.Equal(Registries.Animations.Get("melee_unarmed_2"), sequence.Keys[1].Animation.FacingLeft);
        Assert.Equal(Registries.Animations.Get("melee_unarmed_3"), sequence.Keys[2].Animation.FacingLeft);
        Assert.Equal(Registries.Animations.Get("melee_unarmed_4"), sequence.Keys[3].Animation.FacingLeft);

        Assert.Equal(8, sequence.Keys[0].TransitionFrame);
        Assert.Equal(12, sequence.Keys[1].TransitionFrame);
        Assert.Equal(10, sequence.Keys[2].TransitionFrame);
        Assert.Equal(-1, sequence.Keys[3].TransitionFrame);

        Assert.Equal(new[] { 7 }, sequence.Keys[0].HitFrames);
        Assert.Equal(new[] { 9, 5 }, sequence.Keys[1].HitFrames);
        Assert.Equal(new[] { 3 }, sequence.Keys[2].HitFrames);
        Assert.Equal(new[] { 4, 12, 5 }, sequence.Keys[3].HitFrames);
    }

    [Fact]
    public void PositiveCaseTestDoubleSided()
    {
        string content =
@$"{GameVersion.Version}

[melee_two_handed_blunt_R_1,melee_two_handed_blunt_L_1]		 8	   [7]     
[melee_two_handed_blunt_R_2,melee_two_handed_blunt_L_2]                        12     [9,5]  
[melee_two_handed_blunt_R_3,melee_two_handed_blunt_L_3]		                10     [3] 
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var sequence = MeleeSequenceDeserialiser.Load(path);

        Assert.Equal(3, sequence.Keys.Length);

        Assert.Equal(Registries.Animations.Get("melee_two_handed_blunt_R_1"), sequence.Keys[0].Animation.FacingRight);
        Assert.Equal(Registries.Animations.Get("melee_two_handed_blunt_R_2"), sequence.Keys[1].Animation.FacingRight);
        Assert.Equal(Registries.Animations.Get("melee_two_handed_blunt_R_3"), sequence.Keys[2].Animation.FacingRight);

        Assert.Equal(Registries.Animations.Get("melee_two_handed_blunt_L_1"), sequence.Keys[0].Animation.FacingLeft);
        Assert.Equal(Registries.Animations.Get("melee_two_handed_blunt_L_2"), sequence.Keys[1].Animation.FacingLeft);
        Assert.Equal(Registries.Animations.Get("melee_two_handed_blunt_L_3"), sequence.Keys[2].Animation.FacingLeft);

        Assert.Equal(8, sequence.Keys[0].TransitionFrame);
        Assert.Equal(12, sequence.Keys[1].TransitionFrame);
        Assert.Equal(10, sequence.Keys[2].TransitionFrame);

        Assert.Equal(new[] { 7 }, sequence.Keys[0].HitFrames);
        Assert.Equal(new[] { 9, 5 }, sequence.Keys[1].HitFrames);
        Assert.Equal(new[] { 3 }, sequence.Keys[2].HitFrames);
    }

    //    [Fact]
    //    public void InvalidPropertyTest()
    //    {
    //        string content =
    //@$"{GameVersion.Version}

    //panic 5
    //melee 13.72
    //this_does_not_exist 50.130
    //can_deflect true";

    //        var path = Path.GetTempFileName();
    //        File.WriteAllText(path, content);

    //        Assert.Throws<Exceptions.SerialisationException>(() =>
    //        {
    //            var stats = CharacterStatsDeserialiser.Load(path);

    //            Assert.Equal(5, stats.PanicIntensity);
    //            Assert.Equal(13.72f, stats.MeleeSkill);
    //            Assert.True(stats.CanDeflect);
    //        });
    //    }

    public void Dispose()
    {
        Registries.ClearAll();
    }
}
