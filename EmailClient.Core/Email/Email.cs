using System.Globalization;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using MCollections;

namespace EmailClient;

public partial class Email
{
    public partial class EmailAddress : IComparable<EmailAddress>
    {
        [GeneratedRegex(
            """(?<local_part>[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?<domain>(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])"""
        )]
        private static partial Regex EmailAddressRegex();
        public string LocalPart { get; }
        public string Domain { get; }
        public EmailAddress(string address)
        {
            var match = EmailAddressRegex().Match(address);
            if (!match.Success)
                throw new ArgumentException($"Invalid email address: {address}");
            LocalPart = match.Groups["local_part"].Value;
            Domain = match.Groups["domain"].Value;
        }
        public EmailAddress(string localPart, string domain)
        {
            if (Uri.CheckHostName(domain) == UriHostNameType.Unknown)
                throw new ArgumentException("Invalid domain");
            LocalPart = localPart;
            Domain = domain;
        }
        public static void ParseEmailAddresses(string emails, ICollection<EmailAddress> list)
        {
            IdnMapping idn = new();
            foreach (var match in EmailAddressRegex().Matches(emails).Cast<Match>())
                list.Add(
                    new(
                        match.Groups["local_part"].Value,
                        idn.GetUnicode(match.Groups["domain"].Value)
                    )
                );
        }
        public override string ToString()
        {
            // NEED PROPER FORMATTING!
            return $"{LocalPart}@{Domain}";
        }
        public string ToStringPunycode()
        {
            // NEED PROPER FORMATTING!
            IdnMapping idn = new();
            return $"{LocalPart}@{idn.GetAscii(Domain)}";
        }
        public int CompareTo(EmailAddress? other)
        {
            if (other == null) return 1;
            return ToString().CompareTo(other.ToString());
        }
    }
    public string? MessageId { get; set; }
    public EmailAddress? From { get; set; }
    public IndexedSet<EmailAddress> To { get; set; } = new();
    public IndexedSet<EmailAddress> Cc { get; set; } = new();
    public IndexedSet<EmailAddress> Bcc { get; set; } = new();
    public DateTime? Date { get; set; }
    public string? Subject { get; set; }
    public string? TextBody { get {
        if (_textBodyDecoded == null)
        {
            if (_textBodyPart == null)
                return null;
            _textBodyDecoded = BodyDecoder.Decode(_textBodyPart);
        }
        return _textBodyDecoded;
    } }
    private MimePart? _textBodyPart;
    private string? _textBodyDecoded;
    public string? HtmlBody { get {
        if (_htmlBodyDecoded == null)
        {
            if (_htmlBodyPart == null)
                return null;
            _htmlBodyDecoded = BodyDecoder.Decode(_htmlBodyPart);
        }
        return _htmlBodyDecoded;
    } }
    private MimePart? _htmlBodyPart;
    private string? _htmlBodyDecoded;
    private MimeEntity? _body;
    public MimeEntity? Body
    {
        get => _body;
        set
        {
            _textBodyPart = null;
            _htmlBodyPart = null;
            _body = value;
            if (value == null)
                return;

            foreach (var entity in MimeIterator.Get(value))
            {
                if (_textBodyPart != null && _htmlBodyPart != null)
                    break;
                if (entity is MimePart part)
                {
                    if (part.ContentType == "text/plain")
                        _textBodyPart ??= part;
                    else if (part.ContentType == "text/html")
                        _htmlBodyPart ??= part;
                }
            }
        }
    }
    public IEnumerable<IAttachment> Attachments { get; private set; } = Array.Empty<IAttachment>();
    public Email() { }
    public Email(MimeEntity mime)
    {
        if (mime.Headers.TryGetValue("From", out var from))
            From = new(from.Value);
        if (mime.Headers.TryGetValue("To", out var to))
            EmailAddress.ParseEmailAddresses(to.Value, To);
        if (mime.Headers.TryGetValue("Cc", out var cc))
            EmailAddress.ParseEmailAddresses(cc.Value, Cc);
        if (mime.Headers.TryGetValue("Bcc", out var bcc))
            EmailAddress.ParseEmailAddresses(bcc.Value, Bcc);
        if (mime.Headers.TryGetValue("Date", out var date))
            Date = DateTime.Parse(date.Value);
        if (mime.Headers.TryGetValue("Subject", out var subject))
            Subject = subject.Value;
        MessageId = mime.Headers["Message-ID"].Value[1..^1];
        Body = mime;
    }
    public IEnumerable<EmailAddress> GetRecipients()
    {
        foreach (var email in To)
            yield return email;
        foreach (var email in Cc)
            yield return email;
        foreach (var email in Bcc)
            yield return email;
    }
    public MimeEntity? ToMime()
    {
        var message = Body;
        if (message == null)
            return null;
        if (From != null)
            message.Headers["From"] = new(From.ToStringPunycode());
        if (To.Count > 0)
            message.Headers["To"] = new(string.Join(", ", To.Select(email => email.ToStringPunycode())));
        if (Cc.Count > 0)
            message.Headers["Cc"] = new(string.Join(", ", Cc.Select(email => email.ToStringPunycode())));
        if (Date != null)
            message.Headers["Date"] = new(string.Format("{0:r}", Date));
        if (Subject != null)
            message.Headers["Subject"] = new(Subject);

        if (MessageId == null)
        {
            var uuid = Guid.NewGuid().ToString();
            string domain;
            if (From != null)
            {
                IdnMapping idn = new();
                domain = idn.GetAscii(From.Domain);
            }
            else
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                domain = properties.HostName;
            }
            MessageId = $"{uuid}@{domain}";
        }
        message.Headers["Message-ID"] = new($"<{MessageId}>");
        return message;
    }
}