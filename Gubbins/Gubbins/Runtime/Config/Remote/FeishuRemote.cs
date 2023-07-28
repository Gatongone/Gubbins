using System.Threading.Tasks;

namespace Gubbins.Config;

public class FeishuRemote : IRemote
{
    public readonly FeishuService m_Service;
    
    //public FeishuRemote()
    
    public Task Save<T>(T source) where T : new()
    {
        return null;
    }

    public async Task<T> Read<T>() where T : new()
    {
        return default;
    }
}