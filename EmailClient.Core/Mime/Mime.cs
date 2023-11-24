namespace EmailClient;

using System.Text;
using System.Text.RegularExpressions;
using MimeHeaders = Dictionary<string, MimeHeaderValue>;

public partial class MimeHeaderValue
{
    [GeneratedRegex("""(["\\])""")]
    public static partial Regex EscapedChars();
    [GeneratedRegex("""=\?(?<charset>[A-Za-z0-9-]+)\?(?<encoding>Q|q|B|b)\?(?<text>.+?)\?=""")]
    public static partial Regex EncodedWordRegex();
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

    public static string FromEncodedWord(string input)
    {
        return EncodedWordRegex().Replace(input, (match) => {
            var charset = match.Groups["charset"].Value!;
            var encoding = match.Groups["encoding"].Value!;
            var text = match.Groups["text"].Value!;
            return encoding[0] switch
            {
                'Q' or 'q' => BodyProcessor.DecodeQuotedPrintable(text, charset),
                'B' or 'b' => BodyProcessor.DecodeBase64(text, charset),
                _ => throw new ApplicationException($"Unknown encoding format {encoding[0]}, expected Q or B"),
            };
        });
    }
    public static string ToEncodedWord(string input)
    {
        List<string> encodedFragments = new();
        // Chunk length arbitrarily chosen
        var chunkLength = 72;
        for (int i = 0; i < input.Length; i += chunkLength)
        {
            var base64 = BodyProcessor.EncodeBase64(input.Substring(i, chunkLength));
            encodedFragments.Add($"=?utf-8?B?{base64}?=");
        }
        return string.Join(" \r\n", encodedFragments);
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