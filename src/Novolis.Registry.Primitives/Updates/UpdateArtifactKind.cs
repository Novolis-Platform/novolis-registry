namespace Novolis.Registry.Primitives.Updates;

/// <summary>Installation or handoff form of a release artifact.</summary>
public enum UpdateArtifactKind
{
    WindowsInstaller,
    WindowsPortable,
    LinuxTarGz,
    AndroidApk,
}
