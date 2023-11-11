using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EmailClient.Database;

public class EmailConverter: ValueConverter<Email, byte[]>
{
    private static readonly Func<Email, byte[]> fromEmail = v => {
        using MemoryStream stream = new();
        v.ToMime().WriteToAsync(stream).Wait();
        return stream.ToArray();
    };
    private static readonly Func<byte[], Email> toEmail = v => {
        using MemoryStream stream = new(v);
        var mimeTask = MimeParser.Parse(stream);
        mimeTask.Wait();
        return new Email(mimeTask.Result);
    };
    public EmailConverter():
        base(
            v => fromEmail(v),
            v => toEmail(v)
        )
    {
    }
}
