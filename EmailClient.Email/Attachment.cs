using System.Security.Cryptography;
using System.Text;

namespace EmailClient;

public interface IAttachment {
    string MimeType { get; }
    string FileName { get; }
}

public class AttachmentLocal: IAttachment {
    private readonly FileInfo _file;
    public string FileName => _file.Name;
    public long FileSize => _file.Length;
    public string MimeType => MimeTypes.GetMimeType(_file.Name);
    public AttachmentLocal(FileInfo file)
    {
        _file = file;
    }
    public Stream ToBase64()
    {
        return new CryptoStream(_file.OpenRead(), new ToBase64Transform(), CryptoStreamMode.Read);
    }
}

public class AttachmentRemote: IAttachment {
    private readonly MemoryStream _b64Stream;
    public string FileName { get; private set; }
    public string MimeType { get; private set; }
    public AttachmentRemote(string base64, string fileName, string mimeType)
    {
        _b64Stream = new(Encoding.ASCII.GetBytes(base64));
        FileName = fileName;
        MimeType = mimeType;
    }
    public Stream ToBytes()
    {
        return new CryptoStream(_b64Stream, new FromBase64Transform(), CryptoStreamMode.Read);
    }
}