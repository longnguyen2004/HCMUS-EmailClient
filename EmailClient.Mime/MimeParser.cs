using System.Text;
using System.Text.RegularExpressions;

namespace EmailClient;

class MimeParserException : ApplicationException
{
    private readonly string _message;
    private readonly string _line;
    public MimeParserException(string message, string line)
    {
        _message = message;
        _line = line;
    }
    public override string Message { get => $"{_message} (parsing line \"{_line}\")"; }
}

partial class MimeParser
{
    [GeneratedRegex(@"^(?<key>[A-Za-z-]+?): ?(?<value>.+?)(;|$)")]
    private static partial Regex HeaderRegex();
    [GeneratedRegex(@"(?<key>[A-Za-z-]+?)=(?<value>[^\s""]+?|"".+?"")(;|$)")]
    private static partial Regex HeaderExtraDataRegex();
    private readonly Stream _stream;
    private enum MimeParserState
    {
        Headers,
        Body
    }
    public MimeParser(Stream stream)
    {
        _stream = stream;
    }
    private async Task<(MimeEntity, bool)> ParseImpl(StreamReader reader, string? boundary = null)
    {
        var state = MimeParserState.Headers;
        MimeEntity? entity = null;
        Dictionary<string, MimeHeaderValue>? headers = null;
        StringBuilder bodyBuilder = new();
        string? line;

        while (true)
        {
            line = await reader.ReadLineAsync();
            if (boundary != null)
            {
                if (line == null)
                {
                    // Uh oh, we reached the end but we're expecting a boundary
                    throw new MimeParserException("Unexpected end of stream while expecting boundary", "");
                }
                if (line.StartsWith($"--{boundary}"))
                {
                    if (entity == null)
                    {
                        if (headers != null)
                        {
                            // There is a header, so we assume that it's a regular MimePart
                            entity = new MimePart(headers, bodyBuilder.ToString());
                        }
                        else
                        {
                            throw new MimeParserException("Unexpected boundary while parsing", line);
                        }
                    }
                    return (entity, line == $"--{boundary}");
                }
            }
            if (line == null)
            {
                if (entity == null)
                {
                    if (headers != null)
                    {
                        // There is a header, so we assume that it's a regular MimePart
                        entity = new MimePart(headers, bodyBuilder.ToString());
                    }
                    else
                    {
                        throw new MimeParserException("Unexpected end of stream while parsing", "");
                    }
                }
                return (entity, false);
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
                            var nextBoundary = contentType.ExtraValues["boundary"];
                            if (line != $"--{nextBoundary}")
                                continue;
                            List<MimeEntity> parts = new();
                            bool continueParsing;
                            do
                            {
                                (var part, continueParsing) = await ParseImpl(reader, nextBoundary);
                                parts.Add(part);
                            }
                            while (continueParsing);
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
    public async Task<MimeEntity> Parse()
    {
        StreamReader reader = new(_stream);
        return (await ParseImpl(reader)).Item1;
    }
}