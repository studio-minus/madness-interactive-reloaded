using FluentAssertions;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Xunit;

namespace MIR.Test.Deserialisation;

[Collection("Registry collection")]
public class ArmourDeserialiserTest : global::System.IDisposable
{
    public ArmourDeserialiserTest()
    {
        if (!Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Registries.ClearAll();
    }

    [Theory]
    [InlineData("ik heb geen hoofd", ArmourPieceType.Head, 128, -30, 51)]
    [InlineData("andere test case", ArmourPieceType.Body, 45, 0, 0)]
    [InlineData("je wilt niet weten", ArmourPieceType.Body, 15, -129, 0)]
    [InlineData("\t\twat voor naam is dit", ArmourPieceType.HeadAccessory, 0, 0, 0)]
    public void PositiveCaseTest(string name, ArmourPieceType type, int order, int xOffset, int yOffset)
    {
        string content =
$@"{GameVersion.Version}

name {name}
order {order}
type {type}

left base:textures/bodies/default/head_left.png
right base:textures/bodies/default/head_right.png
offset {xOffset} {yOffset}
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var result = ArmourDeserialiser.Load(path);

        result.Name.Should().Be(name.Trim().ToString());
        result.MenuOrder.Should().Be(order);
        result.Type.Should().Be(type);
        ((int)result.OffsetLeft.X).Should().Be(xOffset);
        ((int)result.OffsetLeft.Y).Should().Be(yOffset);
        ((int)result.OffsetRight.X).Should().Be(xOffset);
        ((int)result.OffsetRight.Y).Should().Be(yOffset);
        result.Left.Should().BeEquivalentTo(new AssetRef<Texture>("base:textures/bodies/default/head_left.png"));
        result.Right.Should().BeEquivalentTo(new AssetRef<Texture>("base:textures/bodies/default/head_right.png"));
    }

    [Fact]
    public void BrokenPartsTest()
    {
        string content =
$@"{GameVersion.Version}

name does not matter here
type Head

left base:textures/bodies/default/head_left.png
right base:textures/bodies/default/head_right.png
offset 4 -3
broken
    test54
    what12
    kameel!
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var result = ArmourDeserialiser.Load(path);

        result.BrokenKeys!.Select(b => b.ToString()).Should().Equal(["test54", "what12", "kameel!"]);
    }

    [Fact]
    public void InvalidTypeTest()
    {
        string content =
$@"{GameVersion.Version}

name Hoe is het
type IkBestaNietNooitNooitKijkMaar

left base:textures/bodies/default/head_left.png
right base:textures/bodies/default/head_right.png
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        Assert.Throws<Exceptions.SerialisationException>(() =>
        {
            var result = ArmourDeserialiser.Load(path);

            result.Name.Should().Be("Hoe is het");
            result.MenuOrder.Should().Be(default);
            result.Type.Should().Be(default);
            ((int)result.OffsetLeft.X).Should().Be(default);
            ((int)result.OffsetLeft.Y).Should().Be(default);
            ((int)result.OffsetRight.X).Should().Be(default);
            ((int)result.OffsetRight.Y).Should().Be(default);
            result.Left.Should().BeEquivalentTo(new AssetRef<Texture>("base:textures/bodies/default/head_left.png"));
            result.Right.Should().BeEquivalentTo(new AssetRef<Texture>("base:textures/bodies/default/head_right.png"));
        });
    }

    [Fact]
    public void InvalidPropertyTest()
    {
        string content =
$@"{GameVersion.Version}

name Head again
type Head
coolness 500

left base:textures/bodies/default/head_left.png
right base:textures/bodies/default/head_right.png
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        Assert.Throws<Exceptions.SerialisationException>(() =>
        {
            var result = ArmourDeserialiser.Load(path);

            result.Name.Should().Be("Head again");
            result.MenuOrder.Should().Be(default);
            result.Type.Should().Be(ArmourPieceType.Head);
            ((int)result.OffsetLeft.X).Should().Be(default);
            ((int)result.OffsetLeft.Y).Should().Be(default);
            ((int)result.OffsetRight.X).Should().Be(default);
            ((int)result.OffsetRight.Y).Should().Be(default);
            result.Left.Should().Be(@"bodies\default\head_left.png");
            result.Right.Should().Be(@"bodies\default\head_right.png");
        });
    }

    public void Dispose()
    {
        Registries.ClearAll();
    }
}