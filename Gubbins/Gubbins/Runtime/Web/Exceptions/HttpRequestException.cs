using System;

namespace Gubbins.Web;

public class HttpRequestException : Exception
{
    public HttpRequestException(int code, string error) : base($"Http request failed. Code: {code}. Error: {error}") { }
    public HttpRequestException(string msg, int code, string error) : base($"{msg}. Code: {code}. Error: {error}") { }
}