#if GUBBINS_ENABLED
using Godot;
using Gubbins.Enhance;

namespace Gubbins.Context;

/// <summary>
/// A serialized reference to an <see cref="IDependenciesInstaller"/> that can be used in the Godot editor.
/// </summary>
[GlobalClass, Tool]
public partial class SerializedInstaller : SerializedReference<IDependenciesInstaller>;
#endif
