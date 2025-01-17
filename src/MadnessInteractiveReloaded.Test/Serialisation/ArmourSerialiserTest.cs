using FluentAssertions;
using System.IO;
using System.Numerics;
using Walgelijk.AssetManager;
using Xunit;

namespace MIR.Test.Serialisation;

[Collection("Registry collection")]
public class ArmourSerialiserTest : global::System.IDisposable
{
    public ArmourSerialiserTest()
    {
        try
        {
            Assets.RegisterPackage("resources/base.waa");
        }
        catch { }

        if (Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        else
            throw new System.Exception("Tests can't continue because the base assets cannot be found");
        Registries.ClearAll();
    }

    [Theory]
    [InlineData("One", ArmourPieceType.Head, 128, -30, 51)]
    [InlineData("Two", ArmourPieceType.Body, 45, 0, 0)]
    [InlineData("i am a hat", ArmourPieceType.HeadAccessory, 3, -10, 3)]
    [InlineData("\t\ti am a hat with tabs", ArmourPieceType.HeadAccessory, 10, 1000, -1000)]
    public void PositiveSaveTest(string name, ArmourPieceType type, int order, int xOffset, int yOffset)
    {
        var testPiece = new ArmourPiece
        {
            Name = name,
            MenuOrder = order,
            Type = type,
            OffsetLeft = new Vector2(xOffset, yOffset),
            OffsetRight = new Vector2(xOffset, yOffset),
            Left = new("textures/bodies/default/head_left.png"),
            Right = new("textures/bodies/default/head_right.png"),
            BrokenKeys = ["broken_agent_glasses1_eyes", "broken_agent_glasses2_eyes", "broken_agent_glasses3_eyes"]
        };

        var path = Path.GetTempFileName() + ".armor";
        Assert.True(ArmourDeserialiser.Save(testPiece, path));

        var loaded = ArmourDeserialiser.LoadFromPath(path);
        Assert.NotNull(loaded);
        loaded.Name.Should().Be(name.Trim().ToString());
        loaded.MenuOrder.Should().Be(order);
        loaded.Type.Should().Be(type);
        ((int)loaded.OffsetLeft.X).Should().Be(xOffset);
        ((int)loaded.OffsetLeft.Y).Should().Be(yOffset);
        ((int)loaded.OffsetRight.X).Should().Be(xOffset);
        ((int)loaded.OffsetRight.Y).Should().Be(yOffset);
        Assert.Equal(loaded.Left.Id, new GlobalAssetId("textures/bodies/default/head_left.png"));
        Assert.Equal(loaded.Right.Id, new GlobalAssetId("textures/bodies/default/head_right.png"));
        loaded.BrokenKeys.Should().BeEquivalentTo(testPiece.BrokenKeys);
    }

    [Fact]
    public void InvalidSaveTest()
    {
        var armourPiece = new ArmourPiece
        {
            Name = "i am an armour name",
            Type = ArmourPieceType.BodyAccessory,
            OffsetLeft = new Vector2(-100, 20000),
        };

        var path = Path.GetTempFileName() + ".armor";

        Assert.Throws<Exceptions.SerialisationException>(() =>
        {
            Assert.True(ArmourDeserialiser.Save(armourPiece, path));
            var loaded = ArmourDeserialiser.LoadFromPath(path);
            Assert.NotNull(loaded);

            loaded.Name.Should().Be(armourPiece.Name);
            loaded.MenuOrder.Should().Be(default);
            loaded.Type.Should().Be(armourPiece.Type);
            loaded.OffsetLeft.Should().Be(armourPiece.OffsetLeft);
            loaded.OffsetRight.Should().Be(armourPiece.OffsetRight);
        });
    }

    public void Dispose()
    {
        Registries.ClearAll();
    }
}