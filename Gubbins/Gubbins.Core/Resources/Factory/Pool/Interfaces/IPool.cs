namespace Gubbins.Resources;

public interface IPool<T> : IFactory<T>
{
    void Recycle(T instance);
}