using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gubbins.Config;

public partial class Bitable
{
    public class Field
    {
        [JsonProperty("type")]
        public FieldType FieldType;
    
        [JsonProperty("field_id")]
        public string ID;
    
        [JsonProperty("field_name")]
        public string Name;
    
        [JsonProperty("property")]
        public JObject Property;

        private readonly Bitable m_Bitable;
        
        public Record this[int row] => m_Bitable[Name][row];
        
        internal Field(Bitable bitable)
        {
            m_Bitable = bitable;
        }
    }
    public enum FieldType : int
    {
        MultipleText = 1,
        Numbers = 2,
        SingleChoice = 3,
        MultipleChoice = 4,
        Date = 5,
        Checkbox = 7,
        Personnel = 11,
        PhoneNumber = 13,
        Hyperlink = 15,
        Attachment = 17,
        UnidirectionalAssociation = 18,
        FindReferences = 19,
        Formula = 20,
        BidirectionalAssociation = 21,
        GeographicalLocation = 22,
        CreationTime = 1001,
        LastUpdateTime = 1002,
        Creator = 1003,
        ModifiedBy = 1004,
        AutomaticNumbering = 1005
    }

}