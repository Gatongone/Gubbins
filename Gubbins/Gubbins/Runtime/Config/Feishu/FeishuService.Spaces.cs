using System.Threading.Tasks;
using Gubbins.Web;
using Newtonsoft.Json.Linq;

namespace Gubbins.Config;

public partial class FeishuService
{
    public async Task<WikiNode> GetWikiNodeAsync(string wikiToken)
    {
        var accessToken = await m_AccessTokenRequest;
        var response = await HttpRequest.CreateRequest(wiki_api)
            .WithMethod(HttpMethod.Get)
            .WithContent(HttpContentType.utf8_json)
            .WithAuthorization(accessToken)
            .WithPath("get_node")
            .WithQuery("token", wikiToken)
            .SendAsync<JToken>();

        if (response.Value<int>("code") != 0)
            throw new HttpRequestException("Get wiki node failed", response.Value<int>("code"), response.Value<string>("msg"));
        return response["data"]!.Value<WikiNode>("node");
    }

}