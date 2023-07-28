using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gubbins.Web;
using Newtonsoft.Json.Linq;

namespace Gubbins.Config;

public partial class FeishuService
{
    private readonly AccessTokenType m_TokenType;
    private readonly string m_AppID;
    private readonly string m_Secret;
    private string m_AccessToken;
    private readonly Stopwatch m_Watch = new();
    private int m_ExpireTime = int.MaxValue;
    
    private Task<string> m_AccessTokenRequest
    {
        get
        {
            if (string.IsNullOrEmpty(m_AccessToken) || m_Watch.Elapsed.Seconds >= m_ExpireTime)
            {
                if (!string.IsNullOrEmpty(m_AppID) && !string.IsNullOrEmpty(m_Secret))
                    return RequireToken(m_TokenType, m_AppID, m_Secret);
                throw new ArgumentException("Token expire, please rebuild.");
            }

            return Task.FromResult(m_AccessToken);
        }
    }

    public FeishuService(string appId, string secret, AccessTokenType tokenType = AccessTokenType.Tenant)
        => (m_AppID, m_Secret, m_TokenType) = (appId, secret, tokenType);

    private async Task<string> RequireToken(AccessTokenType tokenType, string appId, string secret)
    {
        var tokenKey = tokenType.ToPath();
        var response = await HttpRequest.CreateRequest(feishu_api)
            .WithMethod(HttpMethod.Post)
            .WithContent(HttpContentType.utf8_json)
            .WithPath("auth", "v3", tokenKey, "internal")
            .WithBody("app_id", appId)
            .WithBody("app_secret", secret)
            .SendAsync<JToken>();

        if (response.Value<int>("code") != 0)
            throw new HttpRequestException("Require token failed", response.Value<int>("code"), response.Value<string>("msg"));

        m_Watch.Restart();
        m_ExpireTime = response.Value<int>("expire");
        m_AccessToken = response.Value<string>(tokenKey);
        return m_AccessToken;
    }
}