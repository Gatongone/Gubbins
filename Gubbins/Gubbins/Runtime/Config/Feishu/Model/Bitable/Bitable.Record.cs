using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gubbins.Config;

public partial class Bitable
{
    public struct Record
    {
        private readonly JToken m_Token;

        public Record(JToken token) => m_Token = token;

        public override string ToString() => m_Token.Value<string>();
        public JToken ToJToken() => m_Token;
        public int ToInt() => m_Token.Value<int>();
        public float ToFloat() => m_Token.Value<int>();
        public string[] ToStringArray() => m_Token.Value<string[]>();
        public DateTime ToDateTime() => m_Token.Value<DateTime>();
        public bool ToBool() => m_Token.Value<bool>();
        public Personnel ToPersonnel() => JsonConvert.DeserializeObject<Personnel>(m_Token.ToString());
        public Attachment ToAttachment() => JsonConvert.DeserializeObject<Attachment>(m_Token.ToString());
        public GeographicalLocation ToGeographicalLocation() => JsonConvert.DeserializeObject<GeographicalLocation>(m_Token.ToString());
        public Hyperlink ToHyperlink() => JsonConvert.DeserializeObject<Hyperlink>(m_Token.ToString());

        public T ToEnums<T>() where T : Enum => (T) ToEnums(typeof(T));

        public object ToEnums(Type enumType)
        {
            if (enumType.GetCustomAttribute<FlagsAttribute>() != null)
            {
                var array = m_Token.ToObject<string[]>();
                var flags = 0;
                foreach (var flagStr in array)
                {
                    if (string.IsNullOrEmpty(flagStr)) continue;
                    flags += (int) Enum.Parse(enumType, flagStr);
                }

                return flags;
            }

            var str = m_Token.FirstOrDefault()?.ToObject<string>();
            return string.IsNullOrEmpty(str) ? 0 : Enum.Parse(enumType, str);
        }
    }
}