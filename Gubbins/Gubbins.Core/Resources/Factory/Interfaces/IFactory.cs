namespace Gubbins.Resources;

public interface IFactory<T>
{
    public T Spawn();
}