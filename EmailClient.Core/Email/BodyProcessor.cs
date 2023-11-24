using System.Security.Cryptography;
using System.Text;

namespace EmailClient;

public class BodyProcessor
{
    public static string DecodeBase64(string b64String, string charset)
    {
        return Encoding.GetEncoding(charset).GetString(
            Convert.FromBase64String(b64String.ReplaceLineEndings(""))
        );
    }
    public static string DecodeQuotedPrintable(string quotedPrintable, string charset)
    {
        using MemoryStream stream = new();
        foreach (var line in quotedPrintable.Split("\r\n"))
        {
            var i = 0;
            var nextLine = true;
            while (i < line.Length)
            {
                var nextChar = line[i++];
                // Not = sign, append and continue
                if (nextChar != '=')
                {
                    stream.WriteByte((byte)nextChar);
                    continue;
                }
                // = sign at end, is a line continuation
                if (i == line.Length)
                {
                    nextLine = false;
                    continue;
                }
                // Decode the hex value
                var hexVal = line.Substring(i, 2);
                stream.WriteByte(Convert.ToByte(hexVal));
                i += 2;
            }
            if (nextLine)
                stream.Write("\r\n"u8);
        }
        return Encoding.GetEncoding(charset).GetString(stream.ToArray());
    }
    public static string EncodeBase64(string body, string charset = "utf-8")
    {
        StringBuilder builder = new();
        using MemoryStream stream = new(Encoding.GetEncoding(charset).GetBytes(body));
        using CryptoStream b64stream = new(stream, new ToBase64Transform(), CryptoStreamMode.Read);
        var buffer = new byte[72];
        int read;
        while ((read = b64stream.Read(buffer)) != 0)
            builder.AppendLine(Encoding.ASCII.GetString(buffer, 0, read));
        return builder.ToString();
    }
    public static string Decode(MimePart part)
    {
        // No transfer encoding, no need to do anything
        if (!part.Headers.TryGetValue("Content-Transfer-Encoding", out var transfer))
            return part.Body;
        var charset = "ascii";
        if (part.Headers.TryGetValue("Content-Type", out var contentType))
            if (contentType.ExtraValues.TryGetValue("charset", out var newCharset))
                charset = newCharset;
        return transfer.Value switch
        {
            "7bit" or "8bit" => part.Body,
            "base64" => DecodeBase64(part.Body, charset),
            "quoted-printable" => DecodeQuotedPrintable(part.Body, charset),
            _ => throw new ApplicationException($"Unknown transfer encoding: {transfer.Value}"),
        };
    }
}