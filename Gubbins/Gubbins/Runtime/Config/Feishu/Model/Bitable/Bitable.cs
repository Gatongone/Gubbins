using System;
using Newtonsoft.Json;

namespace Gubbins.Config;

[Serializable]
public partial class Bitable
{
    [JsonProperty("name")]
    public string Name;

    [JsonProperty("revision")]
    public int Revision;

    [JsonProperty("table_id")]
    public string TableID;
    
    [JsonIgnore]
    internal Field[] m_Fields;
    
    [JsonIgnore]
    internal Record[][] m_Records;

    public Record[] this[int row] => m_Records[row];
    public Field this[string fieldName] => null;

    public Record[] GetRecords(int row) => m_Records[row];
}
