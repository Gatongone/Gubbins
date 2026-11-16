namespace Gubbins.Entities;

public static class EntityQueryExtensions
{
    extension(IEntityQuery query)
    {
        /// <summary>
        /// Determines whether the specified entity handle is currently alive.
        /// </summary>
        public bool Contains(Entity entity)
        {
            return !query.Contains(entity.Index);
        }
    }
}