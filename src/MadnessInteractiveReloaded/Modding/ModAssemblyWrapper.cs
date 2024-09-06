using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace MIR;

public class ModAssemblyWrapper
{
    private readonly Mod mod;
    private readonly Assembly assembly;

    public readonly IModEntry ModEntry;

    public ModAssemblyWrapper(Mod mod, Assembly assembly)
    {
        this.mod = mod;
        this.assembly = assembly;
        foreach (var item in assembly.GetTypes())
        {
            if (item.IsAssignableTo(typeof(IModEntry)))
            {
                if (ModEntry != null)
                {
                    mod.Errors.Add(new Exception($"There can only be one {nameof(IModEntry)} implementation!"));
                    return;
                }

                var ctors = item.GetConstructors();

                if (ctors.Length != 1)
                {
                    mod.Errors.Add(new Exception($"{nameof(IModEntry)} needs to have exactly 1 constructor!"));
                    return;
                }

                var validCtor = ctors.FirstOrDefault(static c => c.GetParameters().Length == 0);

                if (validCtor == null)
                {
                    mod.Errors.Add(new Exception($"{nameof(IModEntry)} constructor cannot have any parameters!"));
                    return;
                }

                var activated = Activator.CreateInstance(item, true);

                if (activated is IModEntry t)
                    ModEntry = t;
                else
                {
                    mod.Errors.Add(new Exception($"Failed to instantiate {nameof(IModEntry)} implementation!"));
                    return;
                }
            }
        }

        if (ModEntry == null)
        {
            mod.Errors.Add(new Exception($"No {nameof(IModEntry)} implementation could be found!"));
            return;
        }

        ModEntry.OnLoad(mod, new Harmony(mod.Id));
    }
}
