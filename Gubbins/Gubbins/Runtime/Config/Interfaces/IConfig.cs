public interface IConfig<out T>
{
    T Source { get; }
}