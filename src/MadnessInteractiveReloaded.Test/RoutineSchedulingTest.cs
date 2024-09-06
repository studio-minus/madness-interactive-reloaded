namespace MIR.Test;

using System;
using System.Collections.Generic;
using Walgelijk;
using Xunit;

public class RoutineSchedulingTest
{
    private const float dt = 1 / 60f;
    private static float fakeTime = 0;

    public RoutineSchedulingTest()
    {
        RoutineScheduler.StopAll();
    }

    private static void RunScheduler(TimeSpan duration)
    {
        fakeTime = 0;
        while (fakeTime < duration.TotalSeconds)
        {
            RoutineScheduler.StepRoutines(dt);
            fakeTime += dt;
        }
    }

    [Fact]
    public void WaitTest()
    {
        RoutineScheduler.StopAll();
        RoutineScheduler.Start(routine());
        RunScheduler(TimeSpan.FromMinutes(2));

        static IEnumerator<IRoutineCommand> routine()
        {
            Assert.Equal(0, fakeTime, 1f);
            yield return new RoutineDelay(10);
            Assert.Equal(10, fakeTime, 1f);
            yield return new RoutineWaitUntil(static () => fakeTime > 60);
            Assert.Equal(60, fakeTime, 1f);
        }
    }

    [Fact]
    public void ExactFrameDelayTest()
    {
        RoutineScheduler.StopAll();

        RoutineScheduler.Start(routine());

        RunScheduler(TimeSpan.FromMinutes(2));

        static IEnumerator<IRoutineCommand> routine()
        {
            Assert.Equal(0, fakeTime, 4f);
            yield return new RoutineFrameDelay();
            Assert.Equal(dt, fakeTime, 4f);
            yield return new RoutineFrameDelay();
            Assert.Equal(dt * 2, fakeTime, 4f);
        }
    }

    [Fact]
    public void ConcurrentConditionalTest()
    {
        RoutineScheduler.StopAll();

        int v = 0;
        Assert.Equal(0, v);

        RoutineScheduler.Start(routineA());
        RoutineScheduler.Start(routineB());

        RunScheduler(TimeSpan.FromMinutes(2));

        IEnumerator<IRoutineCommand> routineA()
        {
            while (true)
            {
                v++;
                yield return new RoutineFrameDelay();
            }
        }

        IEnumerator<IRoutineCommand> routineB()
        {
            int target = 15;
            yield return new RoutineWaitUntil(() => v == target);
            Assert.Equal(target, v);

            target = 632;
            yield return new RoutineWaitUntil(() => v == target);
            Assert.Equal(target, v);
        }
    }

    [Fact]
    public void EndRoutineTest()
    {
        bool flag = false;

        RoutineScheduler.StopAll();
        var handle = RoutineScheduler.Start(routine());

        RunScheduler(TimeSpan.FromSeconds(5));
        //hier is de routine nog steeds bezig omdat het 10 seconden wacht
        Assert.True(RoutineScheduler.IsOngoing(handle));
        Assert.True(flag);

        RunScheduler(TimeSpan.FromSeconds(6f));
        Assert.False(RoutineScheduler.IsOngoing(handle));
        Assert.True(flag);

        IEnumerator<IRoutineCommand> routine()
        {
            Assert.False(flag);
            flag = true;
            yield return new RoutineDelay(10);
        }
    }

    [Fact]
    public void StopRoutineTest()
    {
        bool flag = false;

        RoutineScheduler.StopAll();
        var handle = RoutineScheduler.Start(routine());

        RunScheduler(TimeSpan.FromSeconds(5));
        //hier is de routine nog steeds bezig omdat het 10 seconden wacht
        Assert.True(RoutineScheduler.IsOngoing(handle));
        Assert.True(flag);

        RunScheduler(TimeSpan.FromSeconds(1f));

        RoutineScheduler.Stop(handle);
        Assert.False(RoutineScheduler.IsOngoing(handle));
        Assert.True(flag); //de routine zet het false als het zn einde bereikt maar dat kan niet want hij is stopgezet

        IEnumerator<IRoutineCommand> routine()
        {
            Assert.False(flag);
            flag = true;
            yield return new RoutineDelay(10);
            flag = false;
        }
    }
}
