namespace EmailClient;

using MimeHeaders = Dictionary<string, MimeHeaderValue>;

class MimeHeaderValue
{
    public string Value { get; private set; }
    public Dictionary<string, string> ExtraValues { get; } = new();
    public MimeHeaderValue(string value)
    {
        Value = value;
    }
}

class MimeEntity
{
    public MimeHeaders Headers { get; protected set; }
    protected MimeEntity() {}
}

class MimePart: MimeEntity
{
    public string Body { get; private set; }
    public MimePart(MimeHeaders headers, string body)
    {
        Headers = headers;
        Body = body;
    }
}

class MimeMultipart: MimeEntity
{
    public List<MimeEntity> Parts { get; } = new();
    public MimeMultipart(MimeHeaders headers, List<MimeEntity> parts)
    {
        Headers = headers;
        Parts = parts;
    }
}