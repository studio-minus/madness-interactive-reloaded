namespace MIR.Test.Deserialisation;

using FluentAssertions;
using System.IO;
using Walgelijk;
using Walgelijk.AssetManager;
using Xunit;

[Collection("Registry collection")]
public class CharacterLookDeserialiserTest
{
    private readonly ArmourPiece fake_body = new() { Name = nameof(fake_body), Type = ArmourPieceType.Body };
    private readonly ArmourPiece fake_body_accessory_a = new() { Name = nameof(fake_body_accessory_a), Type = ArmourPieceType.BodyAccessory };
    private readonly ArmourPiece fake_body_accessory_b = new() { Name = nameof(fake_body_accessory_b), Type = ArmourPieceType.BodyAccessory };

    private readonly ArmourPiece fake_head = new() { Name = nameof(fake_head), Type = ArmourPieceType.Head };
    private readonly ArmourPiece fake_head_accessory_a = new() { Name = nameof(fake_head_accessory_a), Type = ArmourPieceType.HeadAccessory };
    private readonly ArmourPiece fake_head_accessory_b = new() { Name = nameof(fake_head_accessory_b), Type = ArmourPieceType.HeadAccessory };
    private readonly ArmourPiece fake_head_accessory_c = new() { Name = nameof(fake_head_accessory_c), Type = ArmourPieceType.HeadAccessory };

    public CharacterLookDeserialiserTest()
    {
        if (!Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Registries.ClearAll();

        Registries.Armour.Body.Register(nameof(fake_body), fake_body);
        Registries.Armour.BodyAccessory.Register(nameof(fake_body_accessory_a), fake_body_accessory_a);
        Registries.Armour.BodyAccessory.Register(nameof(fake_body_accessory_b), fake_body_accessory_b);

        Registries.Armour.Head.Register(nameof(fake_head), fake_head);
        Registries.Armour.HeadAccessory.Register(nameof(fake_head_accessory_a), fake_head_accessory_a);
        Registries.Armour.HeadAccessory.Register(nameof(fake_head_accessory_b), fake_head_accessory_b);
        Registries.Armour.HeadAccessory.Register(nameof(fake_head_accessory_c), fake_head_accessory_c);
    }

    [Fact]
    public void BackAndForthTest()
    {
        var path = Path.GetTempFileName();

        const string colour = "#6BD33AA3";

        Registries.Armour.Body.Has(nameof(fake_body)).Should().BeTrue();
        Registries.Armour.BodyAccessory.Has(nameof(fake_body_accessory_a)).Should().BeTrue();
        Registries.Armour.BodyAccessory.Has(nameof(fake_body_accessory_b)).Should().BeTrue();

        Registries.Armour.Head.Has(nameof(fake_head)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_a)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_b)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_c)).Should().BeTrue();

        string content =
$@"{GameVersion.Version}

body
   {nameof(fake_body)}
   {nameof(fake_body_accessory_a)}
   {nameof(fake_body_accessory_b)}

head
   {nameof(fake_head)}
   {nameof(fake_head_accessory_a)}
   {nameof(fake_head_accessory_b)}
   {nameof(fake_head_accessory_c)}

blood {colour}
";

        var look = new CharacterLook()
        {
            Body = Registries.Armour.Body.Get(nameof(fake_body)),
            BodyLayer1 = Registries.Armour.BodyAccessory.Get(nameof(fake_body_accessory_a)),
            BodyLayer2 = Registries.Armour.BodyAccessory.Get(nameof(fake_body_accessory_b)),

            Head = Registries.Armour.Head.Get(nameof(fake_head)),
            HeadLayer1 = Registries.Armour.HeadAccessory.Get(nameof(fake_head_accessory_a)),
            HeadLayer2 = Registries.Armour.HeadAccessory.Get(nameof(fake_head_accessory_b)),
            HeadLayer3 = Registries.Armour.HeadAccessory.Get(nameof(fake_head_accessory_c)),

            BloodColour = new Color(colour)
        };
        CharacterLookDeserialiser.Save(look, path);

        File.ReadAllText(path).Trim().Normalize().ReplaceLineEndings().Should().Be(content.Trim().Normalize().ReplaceLineEndings());
    }

    [Fact]
    public void PositiveCaseTest()
    {
        const string colour = "#00ffa3";

        Registries.Armour.Body.Has(nameof(fake_body)).Should().BeTrue();
        Registries.Armour.BodyAccessory.Has(nameof(fake_body_accessory_a)).Should().BeTrue();
        Registries.Armour.BodyAccessory.Has(nameof(fake_body_accessory_b)).Should().BeTrue();

        Registries.Armour.Head.Has(nameof(fake_head)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_a)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_b)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_c)).Should().BeTrue();

        string content =
$@"{GameVersion.Version}

body
   {nameof(fake_body)}
   {nameof(fake_body_accessory_a)}
   {nameof(fake_body_accessory_b)}

head
   {nameof(fake_head)}
   {nameof(fake_head_accessory_a)}
   {nameof(fake_head_accessory_b)}
   {nameof(fake_head_accessory_c)}

blood {colour}
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        var look = CharacterLookDeserialiser.Load(path);

        look.Body.Should().BeSameAs(fake_body);
        look.BodyLayer1.Should().BeSameAs(fake_body_accessory_a);
        look.BodyLayer2.Should().BeSameAs(fake_body_accessory_b);

        look.Head.Should().BeSameAs(fake_head);
        look.HeadLayer1.Should().BeSameAs(fake_head_accessory_a);
        look.HeadLayer2.Should().BeSameAs(fake_head_accessory_b);
        look.HeadLayer3.Should().BeSameAs(fake_head_accessory_c);

        look.BloodColour.Should().Be(new Walgelijk.Color(colour));
    }

    [Fact]
    public void InvalidPropertyTest()
    {
        Registries.Armour.Body.Has(nameof(fake_body)).Should().BeTrue();
        Registries.Armour.BodyAccessory.Has(nameof(fake_body_accessory_a)).Should().BeTrue();
        Registries.Armour.BodyAccessory.Has(nameof(fake_body_accessory_b)).Should().BeTrue();

        Registries.Armour.Head.Has(nameof(fake_head)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_a)).Should().BeTrue();
        Registries.Armour.HeadAccessory.Has(nameof(fake_head_accessory_b)).Should().BeTrue();

        string content =
$@"{GameVersion.Version}

body
   {nameof(fake_body)}
   {nameof(fake_body_accessory_a)}
   {nameof(fake_body_accessory_b)}

head
   {nameof(fake_head)}
   {nameof(fake_head_accessory_a)}

coolness 500
blood #ff0000
";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);

        Assert.Throws<Exceptions.SerialisationException>(() =>
        {
            var look = CharacterLookDeserialiser.Load(path);

            look.Body.Should().BeSameAs(fake_body);
            look.BodyLayer1.Should().BeSameAs(fake_body_accessory_a);
            look.BodyLayer2.Should().BeSameAs(fake_body_accessory_b);

            look.Head.Should().BeSameAs(fake_head);
            look.HeadLayer1.Should().BeSameAs(fake_head_accessory_a);

            look.HeadLayer2.Should().BeNull();
            look.HeadLayer3.Should().BeNull();

            look.BloodColour.Should().Be(Colors.Red);
        });
    }
}
