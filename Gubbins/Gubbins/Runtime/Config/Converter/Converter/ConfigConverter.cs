namespace Gubbins.Config;

internal class ConfigConverter<T> where T : IConfigConverter, new()
{
    private static T s_Instance;
    public static T GetDeserializer()
    {
        if (s_Instance == null) 
            s_Instance = new T();
        return s_Instance;
    }
}