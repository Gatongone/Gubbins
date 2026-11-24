namespace Gubbins.Entities;

/// <summary>
/// Tag component that marks an entity as disabled.
/// </summary>
/// <remarks>
/// <para>
/// A disabled entity keeps its <see cref="Entity"/> handle, its components and its place in
/// <see cref="EntityRepository.Count"/>; it only leaves the archetype that carried its component set and joins the
/// same set plus this tag. Queries skip archetypes that carry <see cref="Disabled"/>, so a disabled entity is invisible
/// to <c>Query</c> until it is enabled again, which moves it back.
/// </para>
/// <para>
/// The transition is a structural change: it is a move between chunks, not a flag flip. Do not enable or disable an
/// entity from inside a query iteration over the chunks it belongs to.
/// </para>
/// </remarks>
/// <seealso cref="EntityRepository.SetEnabled(int, bool)"/>
public readonly struct Disabled;
