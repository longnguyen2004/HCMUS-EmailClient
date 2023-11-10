using System.Text;
using System.Text.RegularExpressions;

namespace EmailClient;

public class MimeParserException : ApplicationException
{
    private readonly string? _line;
    public MimeParserException(string message, string? line) :
        base(message)
    {
        _line = line;
    }
    public override string Message { get => $"{base.Message} (parsing line \"{_line}\")"; }
}

public partial class MimeParser
{
    [GeneratedRegex(@"^(?<key>[A-Za-z-]+?): ?(?<value>.+?)(;|$)")]
    private static partial Regex HeaderRegex();
    [GeneratedRegex(@"(?<key>[A-Za-z-]+?)=(?<value>[^\s""]+?|"".+?"")(;|$)")]
    private static partial Regex HeaderExtraDataRegex();
    private enum MimeParserState
    {
        Headers,
        Body
    }
    private static async Task<(MimeEntity?, string?)> ParseImpl(Stream stream, HashSet<string?> endOfParse)
    {
        var state = MimeParserState.Headers;
        MimeEntity? entity = null;
        Dictionary<string, MimeHeaderValue>? headers = null;
        StringBuilder bodyBuilder = new();
        var buffer = new byte[1024];
        var len = 0;

        while (true)
        {
            try
            {
                await stream.ReadExactlyAsync(buffer, len, 1);
                if (buffer[len++] != 0x0a) continue;
            }
            catch (EndOfStreamException)
            {
            }

            string? line = null;
            if (len > 0)
            {
                if (state == MimeParserState.Headers)
                {
                    line = Encoding.ASCII.GetString(buffer, 0, len);
                }
                else
                {
                    if (headers!["Content-Type"].ExtraValues.TryGetValue("charset", out var charset))
                        line = Encoding.GetEncoding(charset!).GetString(buffer, 0, len);
                    else
                        line = Encoding.ASCII.GetString(buffer, 0, len);
                }
                line = line.ReplaceLineEndings("");
                len = 0;
            }
            if (endOfParse.Contains(line))
            {
                if (state == MimeParserState.Headers)
                {
                    // We're still in the header parsing state, throw an exception
                    throw new MimeParserException("Unexpected end of parse line while parsing header", line);
                }
                // headers isn't null, and entity isn't created => assume not multipart
                if (headers != null)
                {
                    string body;
                    var hasEncoding = headers.TryGetValue("Content-Transfer-Encoding", out var encoding);
                    if (hasEncoding)
                    {
                        // I really hope no one uses binary MIME...
                        if (encoding.Value == "7bit" || encoding.Value == "8bit")
                            body = bodyBuilder.ToString();
                        else
                            body = bodyBuilder.ToString().ReplaceLineEndings("");
                    }
                    else
                    {
                        body = bodyBuilder.ToString();
                    }
                    if (
                        headers.TryGetValue("Content-Disposition", out var disposition)
                        && disposition.Value == "attachment"
                    )
                    {
                    }
                    else if (hasEncoding)
                    {
                        if (encoding.Value == "base64")
                            body = Encoding.GetEncoding(
                                headers!["Content-Type"].ExtraValues["charset"]
                            ).GetString(Convert.FromBase64String(body));
                    }
                    entity ??= new MimePart(headers, body);
                    return (entity, line);
                }
            }
            if (line == null)
            {
                // We reached the end without encountering the end of parsing line
                throw new MimeParserException("Unexpected end of stream while parsing", null);
            }
            switch (state)
            {
                case MimeParserState.Headers:
                    {
                        if (line == string.Empty)
                        {
                            if (headers == null)
                                throw new MimeParserException("Expected header, got blank line", string.Empty);
                            state = MimeParserState.Body;
                            break;
                        }
                        headers ??= new();
                        var match = HeaderRegex().Match(line);
                        if (!match.Success)
                            throw new MimeParserException("Invalid header format", line);
                        MimeHeaderValue value = new(match.Groups["value"].Value);
                        foreach (var extraMatch in HeaderExtraDataRegex().Matches(line).Cast<Match>())
                        {
                            value.ExtraValues.Add(
                                extraMatch.Groups["key"].Value,
                                extraMatch.Groups["value"].Value.Replace("\"", "")
                            );
                        }
                        headers.Add(match.Groups["key"].Value, value);
                        break;
                    }
                case MimeParserState.Body:
                    {
                        var contentType = headers!["Content-Type"];
                        if (contentType.Value.StartsWith("multipart"))
                        {
                            var boundary = contentType.ExtraValues["boundary"];
                            var boundaryContinue = $"--{boundary}";
                            var boundaryStop = $"--{boundary}--";
                            if (line != boundaryContinue)
                            {
                                bodyBuilder.AppendLine(line);
                                continue;
                            }
                            List<MimeEntity> parts = new();
                            string? lastLine;
                            do
                            {
                                (var part, lastLine) = await ParseImpl(
                                    stream,
                                    new() {
                                        boundaryContinue,
                                        boundaryStop
                                    }
                                );
                                if (part != null)
                                    parts.Add(part);
                            }
                            while (lastLine != boundaryStop);
                            entity = new MimeMultipart(
                                headers, parts, bodyBuilder.ToString(),
                                contentType.Value.Replace("multipart/", ""), boundary
                            );
                        }
                        else
                        {
                            bodyBuilder.AppendLine(line);
                        }
                        break;
                    }
            }
        }
    }
    public static async Task<MimeEntity?> Parse(Stream stream, string? endParseLine = null)
    {
        return (await ParseImpl(stream, new() {
            endParseLine
        })).Item1;
    }
}