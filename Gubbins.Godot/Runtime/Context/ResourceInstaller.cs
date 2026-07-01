using Godot;
using Godot.Collections;

namespace Gubbins.Context;

/// <summary>
/// A <see cref="Resource"/> installer that registers dependencies from a serialized
/// list of <see cref="SerializedInstallInfo"/> entries.
/// </summary>
[GlobalClass]
public partial class ResourceInstaller : Godot.Resource, IDependenciesInstaller
{
    /// <summary>
    /// Serialized install info array.
    /// </summary>
    [Export] public Array<SerializedInstallInfo> InstallInfos;

    /// <inheritdoc/>
    public void Install(IDependenciesRegistry registry) => registry.RegisterAll(InstallInfos.Select(static item => item.ToInstallInfo()));
}