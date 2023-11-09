namespace EmailClient;

using System.Text;
using System.Text.RegularExpressions;
using MimeHeaders = Dictionary<string, MimeHeaderValue>;

public partial class MimeHeaderValue
{
    [GeneratedRegex("""(["\\])""")]
    public static partial Regex EscapedChars();
    public string Value { get; private set; }
    public Dictionary<string, string> ExtraValues { get; } = new();
    public MimeHeaderValue(string value)
    {
        Value = value;
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
            builder.Append("\"");
        }
        return builder.ToString();
    }
}

public abstract class MimeEntity
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

public class MimePart : MimeEntity
{
    public override string Body { get; }
    public MimePart(MimeHeaders headers, string body) : base(headers)
    {
        Body = body;
    }
}

public class MimeMultipart : MimeEntity
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
        string fallback,
        MimeHeaders headers,
        List<MimeEntity> parts,
        string? boundary = null
    ) :
        base(headers)
    {
        Fallback = fallback;
        Parts = parts;
        Boundary = boundary ?? Guid.NewGuid().ToString();
    }
}
