using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Walgelijk;
namespace MIR;

public class Registry<T> : IRegistry<string, T> where T : class
{
    private readonly ConcurrentDictionary<string, T> dict = new();
    private readonly bool disposeOnRemove;

    public Registry(bool disposeOnRemove = true)
    {
        this.disposeOnRemove = disposeOnRemove;
    }

    public int Count => dict.Count;

    public void Clear()
    {
        if (disposeOnRemove)
        {
            foreach (var item in dict.Values)
            {
                if (item is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Error disposing item in registry: {e}");
                    }
                }
            }
        }
        dict.Clear();
    }

    public T Get(string key) 
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
            
        if (dict.TryGetValue(key, out var value))
            return value;
            
        throw new KeyNotFoundException($"Key '{key}' not found in registry");
    }

    public IEnumerable<string> GetAllKeys() => dict.Keys;

    public IEnumerable<T> GetAllValues() => dict.Values;

    public string GetRandomKey() => Utilities.PickRandom(dict.Keys);

    public T GetRandomValue() => Utilities.PickRandom(dict.Values);

    public bool Has(string key) => key != null && dict.ContainsKey(key);

    public bool TryGet(string key, [NotNullWhen(true)] out T? v) => dict.TryGetValue(key, out v);

    public bool TryGetKeyFor(T value, [NotNullWhen(true)] out string? key)
    {
        key = null;
        foreach (var item in dict)
            if (item.Value.Equals(value))
            {
                key = item.Key;
                return true;
            }
        return false;
    }

    public void Unregister(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (dict.TryRemove(key, out var value) && disposeOnRemove && value is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error($"Error disposing value for key '{key}': {e}");
            }
        }
    }

    public void Register(string key, T val)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (val == null)
            throw new ArgumentNullException(nameof(val));
            
        if (dict.TryGetValue(key, out var oldValue))
        {
            if (disposeOnRemove && oldValue is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Error($"Error disposing old value for key '{key}': {e}");
                }
            }
                
            if (!dict.TryUpdate(key, val, oldValue))
            {
                throw new InvalidOperationException($"Concurrent modification detected while updating key '{key}'");
            }
            Logger.Log($"Registry {GetType().Name} replaced value at key '{key}'");
        }
        else if (!dict.TryAdd(key, val))
        {
            throw new InvalidOperationException($"Failed to add value at key '{key}' - concurrent modification detected");
        }
    }

    public T this[string key] => Get(key);
}
