namespace MIR.Test;

using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using Walgelijk;
using Xunit;

public class PrevTest
{
    [Fact]
    public void ChangedValueTest()
    {
        var a = new PrevValue<bool>(false);

        Assert.False(a.HasChanged);
        Assert.False(a.Value);
        Assert.False(a.PreviousValue);

        a.Update();
        a.Value = true;

        Assert.True(a.HasChanged);
        Assert.True(a.Value);
        Assert.False(a.PreviousValue);

        a.Update();

        Assert.False(a.HasChanged);
        Assert.True(a.Value);
        Assert.True(a.PreviousValue);
    }
}