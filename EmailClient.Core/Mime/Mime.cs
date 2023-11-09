namespace EmailClient;

using System.Text;
using System.Text.RegularExpressions;
using MimeHeaders = Dictionary<string, MimeHeaderValue>;

public partial class MimeHeaderValue
{
    [GeneratedRegex("""(["\\])""")]
    public static partial Regex EscapedChars();
    public string Value { get; }
    public Dictionary<string, string> ExtraValues { get; }
    public MimeHeaderValue(string value, Dictionary<string, string>? extraValues = null)
    {
        Value = value;
        ExtraValues = extraValues ?? new();
    }
    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(Value);
        foreach (var (extraKey, extraValue) in ExtraValues)
        {
            builder.Append("; ");
            builder.Append(extraKey);
            builder.Append("=\"");
            builder.Append(EscapedChars().Replace(extraValue, "\\$1"));
            builder.Append('"');
        }
        return builder.ToString();
    }
}

public abstract partial class MimeEntity
{
    public MimeHeaders Headers { get; }
    public abstract string Body { get; }
    public string ContentType { get => Headers["Content-Type"].Value; }
    protected MimeEntity(MimeHeaders headers)
    {
        Headers = headers;
    }
    public override string ToString()
    {
        StringBuilder builder = new();
        foreach (var (key, value) in Headers)
        {
            builder.Append(key);
            builder.Append(": ");
            builder.AppendLine(value.ToString());
        }
        builder.AppendLine();
        builder.AppendLine(Body);
        return builder.ToString();
    }
}

public partial class MimePart : MimeEntity
{
    public override string Body { get; }
    public MimePart(MimeHeaders headers, string body) : base(headers)
    {
        Body = body;
    }
}

public partial class MimeMultipart : MimeEntity
{
    public string Fallback { get; }
    public string Boundary { get; }
    public List<MimeEntity> Parts { get; } = new();
    public override string Body
    {
        get
        {
            StringBuilder builder = new();
            foreach (var part in Parts)
            {
                builder.Append("--");
                builder.AppendLine(Boundary);
                builder.AppendLine(part.ToString());
            }
            builder.Append("--");
            builder.Append(Boundary);
            builder.AppendLine("--");
            return builder.ToString();
        }
    }
    public MimeMultipart(
        MimeHeaders headers,
        List<MimeEntity> parts,
        string fallback,
        string multipartType = "mixed",
        string? boundary = null
    ) :
        base(headers)
    {
        Fallback = fallback;
        Parts = parts;
        Boundary = boundary ?? Guid.NewGuid().ToString();
        Headers["Content-Type"] = new($"multipart/{multipartType}", new(){
            {"boundary", Boundary}
        });
    }
}

public partial class MimeAttachment : MimeEntity
{
    public IAttachment Attachment { get; }
    public override string Body
    {
        get
        {
            using StreamReader reader = new(Attachment.ToBase64());
            return reader.ReadToEnd();
        }
    }
    public MimeAttachment(IAttachment attachment) :
        base(new())
    {
        Attachment = attachment;
        Headers["Content-Type"] = new(
            attachment.MimeType, new(){
                {"name", attachment.FileName}
            }
        );
        Headers["Content-Disposition"] = new(
            "attachment", new(){
                {"filename", attachment.FileName}
            }
        );
        Headers["Content-Transfer-Encoding"] = new("base64");
    }
}