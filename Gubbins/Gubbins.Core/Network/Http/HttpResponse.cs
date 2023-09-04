/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/12-01:56:32
 * Github: https://github.com/Gatongone
 * Description: Http response.
 */

using System.Text;

namespace Gubbins.Network;

/// <summary>
/// Http response.
/// </summary>
public readonly struct HttpResponse : IDisposable
{
    /// <summary>
    /// Response data.
    /// </summary>
    public readonly byte[] Data;
    
    /// <summary>
    /// Http code.
    /// </summary>
    public readonly long Code;
    
    /// <summary>
    /// Http description with code.
    /// </summary>
    public readonly string? Description;

    /// <summary>
    /// Response stream.
    /// </summary>
    public readonly Stream Stream;
    
    /// <summary>
    /// Create a Http response
    /// </summary>
    /// <param name="data">Response data for bytes.</param>
    /// <param name="code">Http code.</param>
    /// <param name="description">Http description with code.</param>
    public HttpResponse(byte[] data, long code, string? description)
    {
        Data = data;
        Code = code;
        Stream = new MemoryStream(data);
    }

    /// <summary>
    /// Create a Http response
    /// </summary>
    /// <param name="stream">Response data stream</param>
    /// <param name="code">Http code.</param>
    /// <param name="description">Http description with code.</param>
    public HttpResponse(Stream stream, long code, string? description)
    {
        Stream = stream;
        Code = code;
        Description = description;
    }
    
    public string GetText(Encoding encoding)
    {
        using var reader = new StreamReader(Stream, encoding);
        return reader.ReadToEnd();
    }
    
    /// <summary>
    /// Get text from data.
    /// </summary>
    /// <returns>Text from data</returns>
    public string GetText() => GetText(Encoding.UTF8);
    
    /// <summary>
    /// Get text from data async.
    /// </summary>
    /// <returns>Text from data.</returns>
    public async Task<string> GetTextAsync() => await GetTextAsync(Encoding.UTF8);
    
    /// <summary>
    /// Get text from data async.
    /// </summary>
    /// <param name="encoding">Text encoding.</param>
    /// <returns>Text from data.</returns>
    public async Task<string> GetTextAsync(Encoding encoding)
    {
        using var reader = new StreamReader(Stream, encoding);
        return await reader.ReadToEndAsync();
    }
    
    /// <summary>
    /// Dispose data stream.
    /// </summary>
    public void Dispose() => Stream?.Dispose();
}