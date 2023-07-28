using System;
using System.Threading.Tasks;

namespace Gubbins.Config;

public partial class FeishuService
{
    public async Task<string> GetAppTokenFromWiki(string appToken)
    {
        var node = await GetWikiNodeAsync(appToken);
        if (node.ObjType != WikiObjectType.Bitable)
            throw new ArgumentException("The wiki node's object type is not a bitable type");
        return node.ObjToken;
    }

}