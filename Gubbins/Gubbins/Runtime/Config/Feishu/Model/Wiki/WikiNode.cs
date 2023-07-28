using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Gubbins.Config;

public struct WikiNode
{
    [JsonProperty("space_id")]
    public string SpaceID;

    [JsonProperty("node_token")]
    public string NodeToken;

    [JsonProperty("obj_token")]
    public string ObjToken;

    [JsonProperty("obj_type")]
    [JsonConverter(typeof(StringEnumConverter),typeof(CamelCaseNamingStrategy))]
    public WikiObjectType ObjType;

    [JsonProperty("parent_node_token")]
    public string ParentNodeToken;

    [JsonProperty("node_type")]
    [JsonConverter(typeof(StringEnumConverter),typeof(CamelCaseNamingStrategy))]
    public WikiNodeType NodeType;

    [JsonProperty("origin_node_token")]
    public string OriginNodeToken;

    [JsonProperty("title")]
    public string Title;

    [JsonProperty("obj_create_time")]
    public string ObjCreateTime;

    [JsonProperty("obj_edit_time")]
    public string ObjEditTime;

    [JsonProperty("creator")] 
    public string Creator;

    [JsonProperty("owner")] 
    public string Owner;
}
