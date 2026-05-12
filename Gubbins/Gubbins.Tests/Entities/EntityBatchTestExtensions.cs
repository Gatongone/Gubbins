using Gubbins.Enhance;

namespace Gubbins.Entities.Tests;

internal static class EntityBatchTestExtensions
{
    extension(Batch<Entity> batch)
    {
        internal List<int> GetIndexes()
        {
            var result = new List<int>(batch.ElementCount);

            for (var segment = 0; segment < batch.SegmentCount; segment++)
            {
                var span = batch[segment];
                for (var i = 0; i < span.Length; i++)
                {
                    result.Add(span[i].Index);
                }
            }

            return result;
        }
    }
}

