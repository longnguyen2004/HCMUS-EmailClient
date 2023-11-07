namespace EmailClient;

using System.Diagnostics.CodeAnalysis;
using MimeHeaders = Dictionary<string, MimeHeaderValue>;

public class MimeHeaderValue
{
    public string Value { get; private set; }
    public Dictionary<string, string> ExtraValues { get; } = new();
    public MimeHeaderValue(string value)
    {
        Value = value;
    }
}

public class MimeEntity
{
    public MimeHeaders Headers { get; protected set; }
    protected MimeEntity() {}
}

public class MimePart: MimeEntity
{
    public string Body { get; private set; }
    public MimePart(MimeHeaders headers, string body)
    {
        Headers = headers;
        Body = body;
    }
}

public class MimeMultipart: MimeEntity
{
    public List<MimeEntity> Parts { get; } = new();
    public MimeMultipart(MimeHeaders headers, List<MimeEntity> parts)
    {
        Headers = headers;
        Parts = parts;
    }
}