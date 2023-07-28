using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gubbins.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gubbins.Config;

public partial class FeishuService
{
    public async Task<List<Bitable>> GetBitablesAsync(string appToken, string pageToken = null)
    {
        var accessToken = await m_AccessTokenRequest;
        var response = await HttpRequest.CreateRequest(bitable_api)
            .WithMethod(HttpMethod.Get)
            .WithAuthorization(accessToken)
            .WithPath(appToken, "tables")
            .WithQuery("page_size", 20)
            .WithQuery("page_token", pageToken)
            .SendAsync<JToken>();

        if (response.Value<int>("code") != 0)
            throw new HttpRequestException("Get bitables failed", response.Value<int>("code"), response.Value<string>("msg"));

        var bitables = JsonConvert.DeserializeObject<List<Bitable>>(response["data"]!["items"]!.ToString());
        foreach (var bitable in bitables)
        {
            bitable.m_Records = await GetRecordsAsync(appToken, bitable.TableID);
        }

        //If has more, keep reading.
        if (response["data"]!.Value<bool>("has_more"))
        {
            pageToken = response["data"]!.Value<string>("page_token");
            bitables.AddRange(await GetBitablesAsync(appToken, pageToken));
        }

        return bitables;
    }
    
    public async Task<Bitable.Record[][]> GetRecordsAsync(string appToken, string tableId, string pageToken = null)
    {
        var accessToken = await m_AccessTokenRequest;
        var response = await HttpRequest.CreateRequest(bitable_api)
            .WithMethod(HttpMethod.Get)
            .WithAuthorization(accessToken)
            .WithPath(appToken, "tables", tableId, "records")
            .WithQuery("page_token", pageToken)
            .SendAsync<JToken>();
        
        if (response.Value<int>("code") != 0)
            throw new HttpRequestException("Get records failed", response.Value<int>("code"), response.Value<string>("msg"));
        
        var records = JsonConvert.DeserializeObject<List<JToken>>(response["data"]!["items"]!.ToString())
            .Select(token => token["fields"]).ToList();
        if (response["data"]!.Value<bool>("has_more"))
        {
            pageToken = response["data"]!.Value<string>("page_token");
            records.AddRange(await GetRecordsAsync(appToken, pageToken, pageToken));
        }
        return records;
    }
}