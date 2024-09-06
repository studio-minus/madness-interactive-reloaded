using MIR.Exceptions;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace MIR.Serialisation;

public static class KeyValueDeserialiserExtensions
{
    // Convenient wrappers

    /// <summary>
    /// Register a <see cref="Vector2"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="des"></param>
    /// <param name="key"></param>
    /// <param name="handler"></param>
    /// <exception cref="SerialisationException"></exception>
    public static void RegisterVector2<T>(this KeyValueDeserialiser<T> des, in string key, Action<T, Vector2> handler) where T : class, new()
    {
        des.RegisterString(key, (t, s) =>
        {
            if (MadnessUtils.TryGetFirstSpaceIndex(s, out var splitIndex))
            {
                Vector2 v = default;
                if (float.TryParse(s.AsSpan(0, splitIndex), out v.X) && float.TryParse(s.AsSpan(splitIndex), out v.Y))
                {
                    handler(t, v);
                    return;
                }
            }
            throw new SerialisationException("Invalid Vector2 formatting. Expected x y");
        });
    }

    /// <summary>
    /// Attempt to auto register all fields of a given type via reflection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="des"></param>
    /// <exception cref="Exception"></exception>
    public static void AutoRegisterAllFields<T>(this KeyValueDeserialiser<T> des) where T : class, new()
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsNotSerialized)
                continue;

            var t = field.FieldType;

            // single values
            if (t == typeof(float))
                des.RegisterFloat(field.Name, (t, v) => field.SetValue(t, v));
            else if (t == typeof(int))
                des.RegisterInt(field.Name, (t, v) => field.SetValue(t, v));
            else if (t == typeof(string))
                des.RegisterString(field.Name, (t, v) => field.SetValue(t, v));
            else if (t == typeof(bool))
                des.RegisterBool(field.Name, (t, v) => field.SetValue(t, v));
            else if (t == typeof(Vector2))
                des.RegisterVector2(field.Name, (t, v) => field.SetValue(t, v));
            // arrays
            else if (t == typeof(float[]))
                des.RegisterFloatArray(field.Name, (t, v) => field.SetValue(t, v.ToArray()));
            else if (t == typeof(int[]))
                des.RegisterIntArray(field.Name, (t, v) => field.SetValue(t, v.ToArray()));
            else if (t == typeof(string[]))
                des.RegisterStringArray(field.Name, (t, v) => field.SetValue(t, v.ToArray()));
            else if (t == typeof(bool[]))
                des.RegisterBoolArray(field.Name, (t, v) => field.SetValue(t, v.ToArray()));
            else throw new Exception($"Could not autoregsiter field {field.Name}, invalid type. Only float, int, string, bool, and arrays of all of the above are allowed.");
        }
    }
}
