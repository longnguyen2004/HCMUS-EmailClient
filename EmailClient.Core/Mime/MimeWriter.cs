using System.Text;

namespace EmailClient;

public partial class MimeHeaderValue
{
    public async Task WriteToAsync(Stream stream, CancellationToken token = default)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(Value), token);
        foreach (var (extraKey, extraValue) in ExtraValues)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes("; "), token);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(extraKey), token);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("=\""), token);
            await stream.WriteAsync(
                Encoding.ASCII.GetBytes(EscapedChars().Replace(extraValue, "\\$1")),
                token
            );
            await stream.WriteAsync(Encoding.ASCII.GetBytes("\""), token);
        }
    }
}
public abstract partial class MimeEntity
{
    protected abstract Task WriteBodyAsync(Stream stream, CancellationToken token);
    public async Task WriteToAsync(Stream stream, CancellationToken token = default)
    {
        foreach (var (key, value) in Headers)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(key), token);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(": "), token);
            await value.WriteToAsync(stream, token);
            await stream.WriteAsync(StreamHelper.NewLine, token);
        }
        await stream.WriteAsync(StreamHelper.NewLine, token);
        await WriteBodyAsync(stream, token);
    }
}

public partial class MimePart
{
    protected override async Task WriteBodyAsync(Stream stream, CancellationToken token)
    {
        var charset = "ascii";
        // is 8 bit, need to change encoding
        if (Headers.TryGetValue("Content-Transfer-Encoding", out var encoding)
            && encoding.Value == "8bit"
        )
        {
            // This should definitely be available, or else we'll have no clue how to write
            charset = Headers["Content-Type"].ExtraValues["charset"];
        }
        await stream.WriteAsync(Encoding.GetEncoding(charset).GetBytes(Body), token);
    }
}

public partial class MimeMultipart
{
    protected override async Task WriteBodyAsync(Stream stream, CancellationToken token)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(Fallback), token);
        await stream.WriteAsync(StreamHelper.NewLine, token);
        var boundary = Encoding.ASCII.GetBytes($"--{Boundary}\r\n");
        var boundaryStop = Encoding.ASCII.GetBytes($"--{Boundary}--\r\n");
        foreach (var part in Parts)
        {
            await stream.WriteAsync(boundary, token);
            await part.WriteToAsync(stream, token);
        }
        await stream.WriteAsync(boundaryStop, token);
    }
}

public partial class MimeAttachment
{
    protected override async Task WriteBodyAsync(Stream stream, CancellationToken token)
    {
        using var b64Stream = Attachment.ToBase64();
        var buffer = new byte[72];
        try
        {
            int len;
            while ((len = await b64Stream.ReadAsync(buffer, token)) != 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, len), token);
                await stream.WriteAsync(StreamHelper.NewLine, token);
            }
        }
        catch (EndOfStreamException)
        {
        }
    }
}