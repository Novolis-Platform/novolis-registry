namespace Novolis.Registry.Primitives.Updates;

/// <summary>Distribution authority for an installed application.</summary>
public enum UpdateDistributionMode
{
    /// <summary>The app is installed directly from a release asset.</summary>
    DirectGithub,

    /// <summary>The platform store owns installation and update decisions.</summary>
    StoreManaged,
}
