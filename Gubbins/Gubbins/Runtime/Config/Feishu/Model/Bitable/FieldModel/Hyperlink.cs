using System;
using Newtonsoft.Json;

namespace Gubbins.Config;

[Serializable]
public class Hyperlink
{
    [JsonProperty("text")]
    public string Text;
    
    [JsonProperty("link")]
    public string Link;
}