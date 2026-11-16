#if GUBBINS_ENABLED
using Godot;
using Gubbins.Enhance;
using Gubbins.Events;

namespace Gubbins.Pipeline;

/// <summary>
/// A serialized reference to an IEventListener, allowing for serialization and deserialization of event listeners in the Gubbins pipeline.
/// </summary>
[GlobalClass, Tool]
public partial class SerializedEventListener : SerializedReference<IEventListener>;
#endif