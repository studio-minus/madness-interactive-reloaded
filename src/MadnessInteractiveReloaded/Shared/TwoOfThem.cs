using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Walgelijk;

namespace MIR;

/// <summary>
/// A collection with exactly two items
/// </summary>
public class TwoOfThem<T> : IEnumerable<T>
{
    public T First;
    public T Second;

    public int Length => 2;

    public TwoOfThem(T first, T second)
    {
        First = first;
        Second = second;
    }

    public IEnumerator<T> GetEnumerator()
    {
        yield return First;
        yield return Second;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return First;
        yield return Second;
    }

    public T this[int index]
    {
        get => index switch
        {
            0 => First,
            1 => Second,
            _ => throw new IndexOutOfRangeException()
        };

        set
        {
            switch (index)
            {
                case 0:
                    First = value;
                    return;
                case 1:
                    Second = value;
                    return;
            }

            throw new IndexOutOfRangeException();
        }
    }
}
