using HarmonyLib;

namespace MIR;

/// <summary>
/// The interface that a mod has to implement somewhere to be able to communicate with the mod loader and such
/// </summary>
public interface IModEntry
{
    /// <summary>
    /// Called immediately after this mod is loaded. Beware that some resources might not yet be present.
    /// </summary>
    /// <param name="mod">Your mod instance</param>
    /// <param name="harmony">Your Harmony instance</param>
    public void OnLoad(Mod mod, Harmony harmony);

    /// <summary>
    /// Called when everything is ready.
    /// </summary>
    public void OnReady();

    /// <summary>
    /// Called when the game closes and this mod is unloaded.
    /// </summary>
    public void OnUnload();
}