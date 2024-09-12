using FluentAssertions;
using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Xunit;

namespace MIR.Test;

[Collection("Registry collection")]
public class MadnessUtilsTest
{
    public MadnessUtilsTest()
    {
        if (Assets.TryGetPackage("base", out _))
            MadnessInteractiveReloaded.PrepareResourceInitialise();
        Registries.ClearAll();
    }

    public class CharacterSpanOperations
    {
        [Fact]
        public void SpaceIndexTest()
        {
            const string testString = "\t\t         Hier is tekst :)        \t ";

            Assert.True(MadnessUtils.TryGetFirstSpaceIndex(testString, out var firstSpaceIndex));
            Assert.Equal(0, firstSpaceIndex);

            Assert.True(MadnessUtils.TryGetLastSpaceIndex(testString, out var lastSpaceIndex));
            Assert.Equal(testString.Length - 1, lastSpaceIndex);
        }

        [Fact]
        public void BoolValueFromStringTest()
        {
            Assert.True(MadnessUtils.GetValueFromString("gaming true", out bool boolValue));
            Assert.True(boolValue);

            Assert.True(MadnessUtils.GetValueFromString("propertyName false", out boolValue));
            Assert.False(boolValue);

            Assert.False(MadnessUtils.GetValueFromString("jaMaar er is geen boolean", out boolValue));
        }

        [Fact]
        public void StringValueFromStringTest()
        {
            Assert.True(MadnessUtils.GetValueFromString("gaming \"ik weet niet wat dit verwacht\"", out ReadOnlySpan<char> stringValue));
            Assert.True(stringValue.SequenceEqual("\"ik weet niet wat dit verwacht\""));

            Assert.True(MadnessUtils.GetValueFromString("okeDan Dit is een string met \n dingen \0 erin", out stringValue));
            Assert.True(stringValue.SequenceEqual("Dit is een string met \n dingen \0 erin"));

            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam", out stringValue));
        }

        [Fact]
        public void FloatValueFromStringTest()
        {
            Assert.True(MadnessUtils.GetValueFromString("gaming 232.54", out float floatValue));
            floatValue.Should().BeApproximately(232.54f, 0.01f);

            Assert.True(MadnessUtils.GetValueFromString("gaming 5", out floatValue));
            floatValue.Should().BeApproximately(5, 0.01f);

            Assert.True(MadnessUtils.GetValueFromString("broodje -40123.3942", out floatValue));
            floatValue.Should().BeApproximately(-40123.3942f, 0.01f);

            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam", out floatValue));
            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam invalid", out floatValue));
        }

        [Fact]
        public void IntValueFromStringTest()
        {
            Assert.True(MadnessUtils.GetValueFromString("gaming 502", out int intValue));
            Assert.Equal(502, intValue);

            Assert.True(MadnessUtils.GetValueFromString("broodje -10", out intValue));
            Assert.Equal(-10, intValue);

            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam invalid", out intValue));
        }

        [Fact]
        public void Vec2ValueFromStringTest()
        {
            Assert.True(MadnessUtils.GetValueFromString("gaming 30 -5.3", out Vector2 vecValue));
            Assert.True(Vector2.Distance(new Vector2(30, -5.3f), vecValue) < 0.01f);

            Assert.True(MadnessUtils.GetValueFromString("broodje -10 0", out vecValue));
            Assert.True(Vector2.Distance(new Vector2(-10, 0), vecValue) < 0.01f);

            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam niks", out vecValue));
            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam 5", out vecValue));
            Assert.False(MadnessUtils.GetValueFromString("alleenPropertyNaam 10 l", out vecValue));
        }
    }

    [Fact]
    public void NoiseTest()
    {
        for (int i = 0; i < 1024; i++)
        {
            var n = MadnessUtils.Noise2DNormalised(Utilities.RandomFloat(0, 10000f), Utilities.RandomFloat(-10000, 10000f));
            Assert.InRange(n.X, 0, 1);
            Assert.InRange(n.Y, 0, 1);
        }

        for (int i = 0; i < 1024; i++)
        {
            var n = MadnessUtils.Noise2D(Utilities.RandomFloat(0, 10000f), Utilities.RandomFloat(-10000, 10000f));
            Assert.InRange(n.X, -1, 1);
            Assert.InRange(n.Y, -1, 1);
        }
    }
}
