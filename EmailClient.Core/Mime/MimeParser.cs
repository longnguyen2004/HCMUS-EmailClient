using System.Text;
using System.Text.RegularExpressions;

namespace EmailClient;

public class MimeParserException : ApplicationException
{
    private readonly string _message;
    private readonly string? _line;
    public MimeParserException(string message, string? line)
    {
        _message = message;
        _line = line;
    }
    public override string Message { get => $"{_message} (parsing line \"{_line}\")"; }
}

public partial class MimeParser
{
    [GeneratedRegex(@"^(?<key>[A-Za-z-]+?): ?(?<value>.+?)(;|$)")]
    private static partial Regex HeaderRegex();
    [GeneratedRegex(@"(?<key>[A-Za-z-]+?)=(?<value>[^\s""]+?|"".+?"")(;|$)")]
    private static partial Regex HeaderExtraDataRegex();
    private readonly StreamReader _reader;
    private enum MimeParserState
    {
        Headers,
        Body
    }
    public MimeParser(StreamReader reader)
    {
        _reader = reader;
    }
    private async Task<(MimeEntity?, string?)> ParseImpl(StreamReader reader, HashSet<string?> endOfParse)
    {
        var state = MimeParserState.Headers;
        MimeEntity? entity = null;
        Dictionary<string, MimeHeaderValue>? headers = null;
        StringBuilder bodyBuilder = new();
        string? line;

        while (true)
        {
            line = await reader.ReadLineAsync();
            if (endOfParse.Contains(line))
            {
                if (state == MimeParserState.Headers)
                {
                    // We're still in the header parsing state, throw an exception
                    throw new MimeParserException("Unexpected end of parse line while parsing header", line);
                }
                // headers isn't null, and entity isn't created => assume not multipart
                if (headers != null)
                    entity ??= new MimePart(headers, bodyBuilder.ToString());
                return (entity, line);
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
                                continue;
                            List<MimeEntity> parts = new();
                            string? lastLine;
                            do
                            {
                                (var part, lastLine) = await ParseImpl(
                                    reader,
                                    new() {
                                        boundaryContinue,
                                        boundaryStop
                                    }
                                );
                                if (part != null)
                                    parts.Add(part);
                            }
                            while (lastLine != boundaryStop);
                            entity = new MimeMultipart(headers, parts);
                        }
                        else
                        {
                            if (contentType.Value.StartsWith("text"))
                                bodyBuilder.AppendLine(line);
                            else
                                bodyBuilder.Append(line);
                        }
                        break;
                    }
            }
        }
    }
    public async Task<MimeEntity?> Parse(string? endParseLine = null)
    {
        return (await ParseImpl(_reader, new() {
            endParseLine
        })).Item1;
    }
}