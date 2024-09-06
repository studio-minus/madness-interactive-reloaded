using FluentAssertions;
using System;
using Walgelijk;
using Xunit;

namespace MIR.Test;

public class SingleFunctionTests
{
    [Fact]
    public void FixedIntervalDistributor()
    {
        testDistributor(60, TimeSpan.FromMinutes(5), static () => 1 / 60f);
        testDistributor(25, TimeSpan.FromMinutes(2), static () => Utilities.RandomFloat(1 / 10000f, 1 / 15f));
        testDistributor(5, TimeSpan.FromMinutes(1), static () => Utilities.RandomFloat(1 / 50000f, 1 / 2f));
        testDistributor(250, TimeSpan.FromMinutes(10), static () => Utilities.RandomFloat(1 / 120f, 1 / 30f));

        static void testDistributor(float rate, TimeSpan duration, Func<float> getDt)
        {
            var i = new FixedIntervalDistributor(rate);
            TimeSpan time = TimeSpan.Zero;
            int expectedIterations = (int)(duration.TotalSeconds * i.Rate);
            int actualIterations = 0;

            while (time < duration)
            {
                var dt = getDt();
                actualIterations += i.CalculateCycleCount(dt);
                time += TimeSpan.FromSeconds(dt);
            }

            actualIterations.Should().BeCloseTo(expectedIterations, 10);
        }
    }
}
