using System.Text;

namespace Gubbins.Network;

public struct HttpResponse : IDisposable
{
    public readonly byte[] Data;
    public readonly long   Code;
    public readonly Stream Stream;

    public HttpResponse(byte[] data, long code)
    {
        Data = data;
        Code = code;
        Stream = new MemoryStream(data);
    }

    public HttpResponse(Stream stream, long code)
    {
        Stream = stream;
        Code = code;
    }
    
    public string GetText(Encoding encoding)
    {
        using var reader = new StreamReader(Stream, encoding);
        return reader.ReadToEnd();
    }

    public string GetText() => GetText(Encoding.UTF8);

    public async Task<string> GetTextAsync() => await GetTextAsync(Encoding.UTF8);

    public async Task<string> GetTextAsync(Encoding encoding)
    {
        using var reader = new StreamReader(Stream, encoding);
        return await reader.ReadToEndAsync();
    }

    public void Dispose() => Stream?.Dispose();
}