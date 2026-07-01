using System.Buffers;
using Godot;
using Gubbins.Context;
using Gubbins.Enhance;

namespace Gubbins.Entities;

/// <summary>
/// A <see cref="Node3D"/> that creates and manages an entity from the node's transform and specified
/// components. Once per frame <see cref="EntityTransformSystem"/> scatters the entity's transform-component
/// columns back onto this node (see <see cref="ITransformBinding.ApplyTransform"/>), driven by
/// <see cref="EntityTransformSync"/>. This is the 3D adapter; see <see cref="EntityAdapter2D"/> for 2D.
/// </summary>
/// <remarks>
/// The Godot port of the Unity <c>EntityAdapter3D</c> MonoBehaviour. All the shared registry/scatter logic
/// lives in <see cref="EntityTransformSystem"/>; this type only supplies how a <see cref="Node3D"/> seeds
/// and writes back its own transform.
/// </remarks>
[GlobalClass]
public partial class EntityAdapter3D : Node3D, ITransformBinding
{
    /// <summary>The context resolving <see cref="IEntityCommand"/>/<see cref="IEntityQuery"/>.</summary>
    [Export] public SerializedReference<IContext> Context;

    /// <summary>The component types this entity is constructed from.</summary>
    [Export] public ComponentSet Components;

    /// <summary>The entity index.</summary>
    private int m_Index;

    /// <summary>The entity version.</summary>
    private uint m_Version;

    /// <summary>The command used to create the entity, cached so it can be deleted on exit.</summary>
    private IEntityCommand m_Command;

    /// <summary>The query used to read this entity's components back during the per-frame transform sync.</summary>
    private IEntityQuery m_Query;

    /// <summary>The transform variants this entity carries, cached for the writeback scatter.</summary>
    private TransformVariants m_Variants;

    /// <summary>This adapter's slot in the registry, or -1 when unregistered.</summary>
    private int m_TransformSlot = -1;

    int ITransformBinding.TransformSlot
    {
        get => m_TransformSlot;
        set => m_TransformSlot = value;
    }

    int ITransformBinding.EntityIndex => m_Index;

    IEntityQuery ITransformBinding.Query => m_Query;

    void ITransformBinding.ApplyTransform(in TransformSnapshot snapshot) => TransformWriteback.Apply(this, snapshot);

    /// <summary>
    /// Configures this adapter from code before it enters the tree, for spawning entities at runtime
    /// (tests/benchmarks) instead of via the inspector. Transform components are reseeded from the node.
    /// </summary>
    /// <param name="context">The context resolving <see cref="IEntityCommand"/>/<see cref="IEntityQuery"/>.</param>
    /// <param name="components">The component instances for the entity.</param>
    public void Configure(IContext context, params IComponent[] components)
    {
        Context ??= new SerializedReference<IContext>();
        Context.Value = context;
        Components ??= new ComponentSet();
        Components.SetComponents(components);
    }

    /// <summary>
    /// Godot ready callback. Initializes the entity and registers it in the ECS world.
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        var context = Context?.Value ?? throw new ArgumentException("Context is not assigned.");

        m_Command = context.Resolve<IEntityCommand>() ?? throw new ArgumentException("No IEntityCommand registered in the context.");
        m_Query   = context.Resolve<IEntityQuery>();

        var count = Components?.ComponentCount ?? 0;
        var types = ArrayPool<Type>.Shared.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var type = Components.TypeAt(i);
                types[i] = type;
                m_Variants |= EntityTransformSystem.Classify(type);
            }

            // Rebuild the payload from the current set so we never depend on a stale buffer; its byte
            // layout mirrors the type order above.
            var payload = Components?.BuildPayload() ?? [];
            var span    = types.AsSpan(0, count);

            // Seed the built-in transform components from this node's transform so the entity starts in
            // sync with the scene; other components keep their (zeroed / configured) values.
            EntityTransformSystem.Seed(payload, span, Position, RotationDegrees, Quaternion, Scale);

            var entity = m_Command.Insert(payload, span);
            m_Index   = entity.Index;
            m_Version = entity.Version;

            EntityTransformSystem.Register(this, entity, m_Query, m_Variants);
        }
        finally
        {
            ArrayPool<Type>.Shared.Return(types);
        }
    }

    /// <summary>
    /// Godot tree-exit callback. Deletes the entity and removes its registration.
    /// </summary>
    public override void _ExitTree()
    {
        base._ExitTree();

        if (m_TransformSlot < 0)
        {
            return;
        }

        m_Command?.Delete(m_Index);
        EntityTransformSystem.Unregister(this);
    }
}
