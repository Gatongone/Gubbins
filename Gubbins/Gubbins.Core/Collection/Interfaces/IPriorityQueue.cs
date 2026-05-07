namespace Gubbins.Collection;

public interface IPriorityQueue<T> : IEnumerable<T>, IClearable
{
    int Count { get; }
    T Peak();
    void Append(T item);
    T Extract();
    bool Contains(T item);
}