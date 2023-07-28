using System;
using Gubbins.Web;

namespace Gubbins.Config;

internal static class FesihuExtensions
{
    internal static string ToPath(this AccessTokenType tokenType)
    {
        switch (tokenType)
        {
            case AccessTokenType.User: return "access_token";
            case AccessTokenType.App: return "app_access_token";
            case AccessTokenType.Tenant: return "tenant_access_token";
            default: throw new ArgumentException("Unknown token type: " + tokenType);
        }
    }
    internal static HttpRequest WithAuthorization(this HttpRequest request, string token) => request.WithHeader("Authorization", "Bearer " + token);
}