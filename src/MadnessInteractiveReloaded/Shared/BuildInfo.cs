namespace MIR;

/// <summary>
/// Information about the build, such as what OS we are building on.
/// </summary>
public static class BuildInfo
{
    public const string Runtime =
#if WINDOWS
    "Windows";
#elif LINUX
    "Linux";
#else
    "Unknown OS";
#endif
}

