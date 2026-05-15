namespace Gubbins.Collections;

public static class HeapExtensions
{
    public static bool TryExtract<T>(this IPriorityQueue<T> heap, out T item)
    {
        if (heap.Count <= 0)
        {
            item = default!;
            return false;
        }

        item = heap.Extract();
        return true;
    }
}