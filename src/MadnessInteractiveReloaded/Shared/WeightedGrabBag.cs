using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MIR;

public class WeightedGrabBag<T> : IEnumerable<T> where T : notnull
{
    private readonly List<Entry> entries = [];
    private readonly Random rand = new();

    public WeightedGrabBag(Random rand)
    {
        this.rand = rand;
    }

    public WeightedGrabBag() { }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in entries)
            yield return item.Value;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var item in entries)
            yield return item.Value;
    }

    public void Add(T value, float weight)
    {
        if (entries.Any(v => v.Value.Equals(value)))
            return;

        entries.Add(new Entry { Value = value, Weight = weight });
    }

    public void Remove(T value)
    {
        entries.RemoveAll(v => v.Value.Equals(value));
    }

    public T Grab()
    {
        if (entries.Count == 0)
            throw new Exception("Can't grab out of an empty bag");

        float totalWeight = entries.Sum(static v => v.Weight);
        float threshold = float.Lerp(0, totalWeight, rand.NextSingle());

        foreach (var item in entries.OrderBy(a => rand.Next()))
        {
            if (item.Weight < threshold)
                return item.Value;
            threshold -= item.Weight;
        }
        return entries[rand.Next(0, entries.Count)].Value;
    }

    private struct Entry
    {
        public T Value;
        public float Weight;
    }
}
