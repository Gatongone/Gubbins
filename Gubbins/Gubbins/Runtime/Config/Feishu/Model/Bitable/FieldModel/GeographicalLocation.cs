using Newtonsoft.Json;

namespace Gubbins.Config;

public class GeographicalLocation
{
    [JsonProperty("location")]
    public string Location;
    
    [JsonProperty("pname")]
    public string Province;
    
    [JsonProperty("cityname")]
    public string City;
    
    [JsonProperty("adname")]
    public string Area;
    
    [JsonProperty("address")]
    public string Address;
    
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("fullname")]
    public string FullAddress;
}