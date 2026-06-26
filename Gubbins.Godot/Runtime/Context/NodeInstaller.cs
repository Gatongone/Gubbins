using Godot;
using Godot.Collections;

namespace Gubbins.Context;

/// <summary>
/// A <see cref="Node"/> installer that registers dependencies from a serialized list
/// of <see cref="SerializedInstallInfo"/> entries. Attach to any Node in the scene to
/// contribute bindings to the context at install time.
/// </summary>>
[GlobalClass]
public partial class NodeInstaller  : Node, IDependenciesInstaller
{
    /// <summary>
    /// Serialized install info array.
    /// </summary>
    [Export] public Array<SerializedInstallInfo> InstallInfos;

    /// <inheritdoc/>
    public void Install(IDependenciesRegistry registry) => registry.RegisterAll(InstallInfos.Select(static item => item.ToInstallInfo()));
}