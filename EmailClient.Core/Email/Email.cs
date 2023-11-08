using System.Text.RegularExpressions;
using MCollections;

namespace EmailClient;

public partial class Email
{
    [GeneratedRegex(
        """(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])"""
    )]
    public static partial Regex EmailAddressRegex();
    public string? From { get; set; }
    public IndexedSet<string> To { get; } = new();
    public IndexedSet<string> Cc { get; } = new();
    public IndexedSet<string> Bcc { get; } = new();
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? HtmlBody { get; set; }
    public List<IAttachment> Attachments { get; } = new();
    private static void ParseEmailAddresses(string emails, ICollection<string> list)
    {
        foreach (var match in EmailAddressRegex().Matches(emails).Cast<Match>())
            list.Add(match.Value);
    }
    public Email() {}
    public Email(MimeEntity mime)
    {
        if (mime.Headers.TryGetValue("From", out var from))
        {
            List<string> dummy = new();
            ParseEmailAddresses(from.Value, dummy);
            From = dummy[0];
        }
        if (mime.Headers.TryGetValue("To", out var to))
            ParseEmailAddresses(to.Value, To);
        if (mime.Headers.TryGetValue("Cc", out var cc))
            ParseEmailAddresses(cc.Value, Cc);
        if (mime.Headers.TryGetValue("Bcc", out var bcc))
            ParseEmailAddresses(bcc.Value, Bcc);
        Subject = mime.Headers["Subject"]?.Value;

        // Regular email without multipart
        if (mime is MimePart mimePart)
        {
            Body = mimePart.Body;
        }
        else if (mime is MimeMultipart mimeMultipart)
        {
            var firstPart = mimeMultipart.Parts[0];
            if (firstPart is MimePart body && body.ContentType == "text/plain")
            {
                Body = body.Body;
            }
            else if (firstPart is MimeMultipart alternative)
            {
                foreach (var altPart in alternative.Parts.Where(part => part is MimePart).Cast<MimePart>())
                {
                    if (altPart.ContentType == "text/plain")
                        Body ??= altPart.Body;
                    else if (altPart.ContentType == "text/html")
                        HtmlBody ??= altPart.Body;
                }
            }
            foreach (var part in mimeMultipart.Parts)
            {
                if (!part.Headers.TryGetValue("Content-Disposition", out var contentDisposition))
                    continue;
                if (contentDisposition.Value == "attachment")
                {
                    Attachments.Add(
                        new AttachmentRemote(
                            part.Body,
                            contentDisposition.ExtraValues["filename"],
                            part.ContentType
                        )
                    );
                }
            }
        }
    }
}