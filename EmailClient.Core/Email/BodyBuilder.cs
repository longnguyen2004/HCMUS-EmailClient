using System.Text;

namespace EmailClient;

public class BodyBuilder
{
    public string TextBody { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public ICollection<IAttachment> Attachments { get; set; } = new List<IAttachment>();

    public BodyBuilder()
    {

    }
    public MimeEntity GetMessageBody()
    {
        static MimePart createBodyMime(string body, string contentType)
        {
            Dictionary<string, MimeHeaderValue> header = new()
            {
                {
                    "Content-Type",
                    new(contentType, new(){
                        {"charset", "UTF-8"}
                    })
                }
            };
            var utf8Bytes = Encoding.UTF8.GetBytes(body);
            // Not pure ASCII, need encoding
            if (utf8Bytes.Length != body.Length)
            {
                body = Convert.ToBase64String(utf8Bytes);
                header["Content-Transfer-Encoding"] = new("base64");
            }
            return new MimePart(header, body);

        }
        MimeEntity message;
        var textPart = createBodyMime(TextBody ?? "", "text/plain");
        if (HtmlBody != null)
        {
            var htmlPart = createBodyMime(HtmlBody, "text/html");
            message = new MimeMultipart(
                new(),
                new(){ textPart, htmlPart },
                "",
                "alternative"
            );
        }
        else
        {
            message = textPart;
        }
        if (Attachments.Count > 0)
        {
            var withAttachments = new MimeMultipart(
                new(),
                new(){ message },
                "This is a multi-part message in MIME format."
            );
            foreach (var attachment in Attachments)
                withAttachments.Parts.Add(new MimeAttachment(attachment));
            message = withAttachments;
        }
        message.Headers["MIME-Version"] = new("1.0");
        return message;
    }
}