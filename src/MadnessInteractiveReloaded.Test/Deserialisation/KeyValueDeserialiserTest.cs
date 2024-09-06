using FluentAssertions;
using System.IO;
using System.Linq;
using Xunit;
using MIR.Serialisation;

namespace MIR.Test.Deserialisation;

public class KeyValueDeserialiserTest
{
    public class GuineaPig
    {
        public float Float;
        public int Integer;
        public bool Boolean;
        public string String = string.Empty;

        public float[]? FloatArr;
        public int[]? IntegerArr;
        public bool[]? BooleanArr;
        public string[]? StringArr;
    }

    private readonly KeyValueDeserialiser<GuineaPig> deserialiser = new(nameof(KeyValueDeserialiserTest));

    public KeyValueDeserialiserTest()
    {
        deserialiser.RegisterFloat("float_key", (p, v) => p.Float = v);
        deserialiser.RegisterInt("int_key", (p, v) => p.Integer = v);
        deserialiser.RegisterBool("bool_key", (p, v) => p.Boolean = v);
        deserialiser.RegisterString("string_key", (p, v) => p.String = v);

        deserialiser.RegisterFloatArray("floats", (p, v) => p.FloatArr = v.ToArray());
        deserialiser.RegisterIntArray("ints", (p, v) => p.IntegerArr = v.ToArray());
        deserialiser.RegisterBoolArray("bools", (p, v) => p.BooleanArr = v.ToArray());
        deserialiser.RegisterStringArray("strings", (p, v) => p.StringArr = v.ToArray());
    }

    [Fact]
    public void AutoRegisterTest()
    {
        var deserialiser = new KeyValueDeserialiser<GuineaPig>(nameof(AutoRegisterTest));
        deserialiser.AutoRegisterAllFields();

        var path = Path.GetTempFileName();
        File.WriteAllText(path,
@$"{GameVersion.Version}

Float 58.032
Integer -12
Boolean true
String Hello World!

FloatArr
    -2
    0.32
    0
    100.3

IntegerArr
    15
    -23
    144

BooleanArr
    true
    false
    false
    false
    true

StringArr
    This is entry number 1
    It is kind of sad that I can't have trailing whitespace...          
    You should watch Mob Psycho 100
"
        );

        var created = deserialiser.Deserialise(path);

        created.Should().NotBeNull();
        created.Float.Should().Be(58.032f);
        created.Integer.Should().Be(-12);
        created.Boolean.Should().Be(true);
        created.String.Should().Be("Hello World!");

        created.FloatArr.Should().Equal(new[] { -2f, 0.32f, 0, 100.3f });
        created.IntegerArr.Should().Equal(new[] { 15, -23, 144 });
        created.BooleanArr.Should().Equal(new[] { true, false, false, false, true });
        created.StringArr.Should().Equal(new[] { "This is entry number 1", "It is kind of sad that I can't have trailing whitespace...", "You should watch Mob Psycho 100" });
    }

    [Fact]
    public void PairTest()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path,
@$"{GameVersion.Version}

float_key 0.523
int_key 429
bool_key false
string_key i don't need quotes because nothing comes after me
"
        );

        var created = deserialiser.Deserialise(path);

        created.Float.Should().BeApproximately(0.523f, 0.001f);
        created.Integer.Should().Be(429);
        created.Boolean.Should().Be(false);
        created.String.Should().Be("i don't need quotes because nothing comes after me");
    }

    [Fact]
    public void ArrTest()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path,
@$"{GameVersion.Version}

floats
    0.2
    1.52
    -0.5

bools
    true

strings
    hallo ik ben een man
    ik ik ik 
    ik 
    wat de hel

ints
    4
    -3
    0
    5400
"
        );

        var created = deserialiser.Deserialise(path);

        created.FloatArr.Should().Equal(new[] { 0.2f, 1.52f, -0.5f });
        created.BooleanArr.Should().Equal(new[] { true });
        created.StringArr.Should().Equal(new[] { "hallo ik ben een man", "ik ik ik", "ik", "wat de hel" });
        created.IntegerArr.Should().Equal(new[] { 4, -3, 0, 5400 });
    }
}