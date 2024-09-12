namespace MIR.Test.Deserialisation;

using FluentAssertions;
using MIR.Exceptions;
using System.IO;
using System.Text;
using Walgelijk;
using Walgelijk.AssetManager;
using Xunit;

[Collection("Registry collection")]
public class HandArmourDeserialiserTest : global::System.IDisposable
{
    public HandArmourDeserialiserTest()
    {
        if (Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Registries.ClearAll();

        Resources.BasePath = "resources";
        Resources.SetBasePathForType<IReadableTexture>("textures");
        Resources.SetBasePathForType<Texture>("textures");
        Resources.RegisterType(typeof(Texture), static path =>
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();
            return new Texture(1, 1, false, false);
        });
    }

    public struct HandTextures
    {
        public string FistFront;
        public string FistBack;

        public string HoldPistolFront;
        public string HoldPistolBack;

        public string HoldUndersideFront;
        public string HoldUndersideBack;

        public string HoldRifleFront;
        public string HoldRifleBack;

        public string OpenFront;
        public string OpenBack;

        public string PointFront;
        public string PointBack;
    }

    [Fact]
    public void PositiveCaseTest()
    {
        Load("Default", -1, new HandTextures
        {
            FistFront = "textures/bodies/default/hand_fist.png",
            FistBack = "textures/bodies/default/hand_fist_back.png",

            HoldPistolFront = "textures/bodies/default/hand_pistol.png",
            HoldPistolBack = "textures/bodies/default/hand_pistol_back.png",

            HoldUndersideFront = "textures/bodies/default/hand_underside.png",
            HoldUndersideBack = "textures/bodies/default/hand_underside_back.png",

            HoldRifleFront = "textures/bodies/default/hand_rifle.png",
            HoldRifleBack = "textures/bodies/default/hand_rifle_back.png",

            OpenFront = "textures/bodies/default/hand_open.png",
            OpenBack = "textures/bodies/default/hand_open_back.png",

            PointFront = "textures/bodies/default/hand_point.png",
            PointBack = "textures/bodies/default/hand_point_back.png",
        });

        Load("\tStang stang saNT", 5423, new HandTextures
        {
            FistFront = "textures/bodies/default/hand_fist.png",
            FistBack = "textures/bodies/default/hand_fist_back.png",

            HoldPistolFront = "textures/bodies/default/hand_pistol.png",
            HoldPistolBack = "textures/bodies/default/hand_pistol_back.png",

            HoldUndersideFront = "textures/bodies/default/hand_underside.png",
            HoldUndersideBack = "textures/bodies/default/hand_underside_back.png",

            HoldRifleFront = "textures/bodies/default/hand_rifle.png",
            HoldRifleBack = "textures/bodies/default/hand_rifle_back.png",

            OpenFront = "textures/bodies/default/hand_open.png",
            OpenBack = "textures/bodies/default/hand_open_back.png",

            PointFront = "textures/bodies/default/hand_point.png",
            PointBack = "textures/bodies/default/hand_point_back.png",
        });
    }

    private static void Load(string name, int order, HandTextures data)
    {
        var b = new StringBuilder(GameVersion.Version.ToString());
        b.AppendLine();
        b.AppendFormat("name {0}\n", name);
        b.AppendFormat("order {0}\n", order);

        b.AppendLine();
        b.AppendLine("Fist");
        b.AppendFormat("\tfront {0}\n", data.FistFront);
        b.AppendFormat("\tback {0}\n", data.FistBack);

        b.AppendLine();
        b.AppendLine("HoldPistol");
        b.AppendFormat("\tfront {0}\n", data.HoldPistolFront);
        b.AppendFormat("\tback {0}\n", data.HoldPistolBack);

        b.AppendLine();
        b.AppendLine("HoldUnderside");
        b.AppendFormat("\tfront {0}\n", data.HoldUndersideFront);
        b.AppendFormat("\tback {0}\n", data.HoldUndersideBack);

        b.AppendLine();
        b.AppendLine("HoldRifle");
        b.AppendFormat("\tfront {0}\n", data.HoldRifleFront);
        b.AppendFormat("\tback {0}\n", data.HoldRifleBack);

        b.AppendLine();
        b.AppendLine("Open");
        b.AppendFormat("\tfront {0}\n", data.OpenFront);
        b.AppendFormat("\tback {0}\n", data.OpenBack);

        b.AppendLine();
        b.AppendLine("Point");
        b.AppendFormat("\tfront {0}\n", data.PointFront);
        b.AppendFormat("\tback {0}\n", data.PointBack);

        var p = Path.GetTempFileName();
        File.WriteAllText(p, b.ToString());

        var result = HandArmourDeserialiser.Load(p);
        result.Should().NotBeNull();
        result.Name.Should().Be(name.Trim());
        result.MenuOrder.Should().Be(order);

        sameFile(result.Fist.FacingRight, data.FistFront);
        sameFile(result.Fist.FacingLeft, data.FistBack);

        sameFile(result.HoldPistol.FacingRight, data.HoldPistolFront);
        sameFile(result.HoldPistol.FacingLeft, data.HoldPistolBack);

        sameFile((result.HoldUnderside.FacingRight), data.HoldUndersideFront);
        sameFile((result.HoldUnderside.FacingLeft), data.HoldUndersideBack);

        sameFile((result.HoldRifle.FacingRight), data.HoldRifleFront);
        sameFile((result.HoldRifle.FacingLeft), data.HoldRifleBack);

        sameFile((result.Open.FacingRight), data.OpenFront);
        sameFile((result.Open.FacingLeft), data.OpenBack);

        sameFile((result.Point.FacingRight), data.PointFront);
        sameFile((result.Point.FacingLeft), data.PointBack);

        static void sameFile(AssetRef<Texture> id, string expected)
        {
            id.IsValid.Should().BeTrue();
            expected.Should().NotBeNullOrWhiteSpace();

            id.Id.Should().BeEquivalentTo(new GlobalAssetId(expected));
            _= id.Value;
        }
    }

    [Fact()]
    public void InvalidPathTest()
    {
        Assert.ThrowsAny<global::System.Exception>(static () =>
        {
            Load("Default", -1, new HandTextures
            {
                FistFront = "textures/bodies/default/hand_fist.png",
                FistBack = "textures/bodies/default/hand-dwdwdw_back.png",

                HoldPistolFront = "textures/wrg.png",
                HoldPistolBack = "textures/bodies/d/hand_pistol_back.png",

                HoldUndersideFront = "textures/bodies/default/hand_underside.png",
                HoldUndersideBack = "textures/bodies/w/hand_underside_back.png",

                HoldRifleFront = "textures/bodies/default/hand_rifle.png",
                HoldRifleBack = "textures/wdwdwd/default/hand_rifle_back.png",

                OpenFront = "textures/bodies/default/hand_open.png",
                OpenBack = "textures/bodies5464565default/hand_open_back.png",

                PointFront = "textures/bodies/43344/hand_point.png",
                PointBack = "textures/bodies/default/hand_point_back.png",
            });
        });
    }

    [Fact]
    public void UnknownValueTest()
    {
        Assert.ThrowsAny<global::System.Exception>(static () =>
        {
            Load("raak", -1, new HandTextures
            {
                FistFront = "textures/bodies/default/hand_fist.png",
                FistBack = "textures/bodies/default/hand_fist_back.png",

                HoldPistolFront = "textures/bodies/default/hand_pistol.png",
                HoldPistolBack = "textures/bodies/default/hand_pistol_back.png",

                HoldUndersideFront = "textures/bodies/default/hand_underside.png",
                HoldUndersideBack = "textures/bodies/default/hand_underside_back.png",

                HoldRifleFront = "textures/bodies/default/hand_rifle.png",
                HoldRifleBack = "textures/bodies/default/hand_rifle_back.png",

                PointFront = "textures/bodies/default/hand_point.png",
                PointBack = "textures/bodies/default/hand_point_back.png",
            });
        });
    }

    public void Dispose()
    {
        Registries.ClearAll();
    }
}