using System;
using Newtonsoft.Json;

namespace Gubbins.Config;

[Serializable]
public struct Personnel
{
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("id")]
    public string ID;

    [JsonProperty("en_name")]
    public string EN_Name;
    
    [JsonProperty("email")]
    public string Email;
}