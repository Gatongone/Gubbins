using System.Diagnostics;
using Gubbins.Config;
using Gubbins.Web;
using Newtonsoft.Json;


var config = new MyConfig();
await config.Refresh();
foreach (var item in config.Source)
{
    Console.WriteLine(item);
}

public class Player
{
    public string Name;
    public int HP;
}

[Serializable]
public class TestObject
{
    public override string ToString()
    {
        return $"{nameof(String)}: {String}\n{nameof(Int)}: {Int}\n{nameof(Float)}: {Float}\n{nameof(Enum)}: {Enum}\n{nameof(Flags)}: {Flags}\n{nameof(Object)}: {Object}\n{nameof(Array)}: {Array}";
    }
    
    [JsonProperty("String")]
    public string String;
    [JsonProperty("Int")]
    public string Int;
    [JsonProperty("Float")]
    public string Float;
    [JsonProperty("Enum")]
    public TestEnum Enum;
    [JsonProperty("Flags")]
    public TestFlags Flags;
    [JsonProperty("Object")]
    public Player Object;
    [JsonProperty("Array")]
    public Player[] Array;
}

public enum TestEnum
{
    Enum1,
    Enum2,
    Enum3,
}

public enum TestFlags
{
    Flags1,
    Flags2,
    Flags3,
}

public class MyConfig : IConfig<List<TestObject>>
{
    private const string appid = "";
    private const string secret = "";

    private static readonly FeishuRemote.FeishuHandle s_DefaultHandle = new()
    {
        AppToken = "Ll5cbevhdaQqV4s4ViwcyrpLnEg",
        TokenType = FeishuRemote.TokenType.Tenant | FeishuRemote.TokenType.Base,
        AppID = "cli_a4353e93a333500c",
        Secret = "2NaAQAscXd8GE8a29IVItblRxAhZPfE0",
        TableID = "tblQmaycG1wMSAjk",
        ViewID = "vewB1Oq4hZ"
    };
    
    private readonly IRemote m_Remote = new FeishuRemote(s_DefaultHandle);

    private List<TestObject> m_Source;
    public List<TestObject> Source => m_Source;

    public void Save() => m_Remote.Save(Source);

    public async Task Refresh() => m_Source = await m_Remote.Read<List<TestObject>>();
}