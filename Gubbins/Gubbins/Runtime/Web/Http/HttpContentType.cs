namespace Gubbins.Web;

public struct HttpContentType
{
    public readonly string content;
    private HttpContentType(string content) => this.content = content;
    public static HttpContentType json = new HttpContentType("application/json");
    public static HttpContentType utf8_json = new HttpContentType("application/json; charset=utf-8");
}
