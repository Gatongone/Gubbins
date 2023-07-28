using System;
using Newtonsoft.Json;

namespace Gubbins.Config;

[Serializable]
public struct Attachment
{
    [JsonProperty("file_token")]
    public string FileToken;
    
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("type")]
    public string Type;
    
    [JsonProperty("url")]
    public string Url;
    
    [JsonProperty("tmp_url")]
    public string TMP_Url;
    
    [JsonProperty("size")]
    public int Size;
}
