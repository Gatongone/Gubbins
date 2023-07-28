using System.Threading.Tasks;

public interface IRemote
{
    Task Save<T>(T source) where T : new();
    Task<T> Read<T>() where T : new();
}