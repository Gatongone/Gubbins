namespace Gubbins.Collections;

public interface ISetGroup
{
    bool InSameGroup(int left, int right);
    bool TryMerge(int left, int right);
}

public interface ISetGroup<in T>
{
    bool InSameGroup(T x, T y);
    bool TryMerge(T left, T right);
}