#if GUBBINS_ENABLED
using Godot;
using Gubbins.Enhance;

namespace Gubbins.Context;

/// <summary>
/// A serialized reference to an IContext object, allowing for easy serialization and deserialization of context objects in Godot.
/// </summary>
[GlobalClass, Tool]
public partial class SerializedContext : SerializedReference<IContext>;
#endif
