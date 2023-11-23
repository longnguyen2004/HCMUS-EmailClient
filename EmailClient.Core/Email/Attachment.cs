using System.Security.Cryptography;
using System.Text;

namespace EmailClient;

public interface IAttachment {
    string FileName { get; }
    long FileSize { get; }
    string MimeType { get; }
    public Stream ToBase64();
    public Stream ToBytes();
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
        return new CryptoStream(_file.Open(FileMode.Open, FileAccess.Read, FileShare.Read), new ToBase64Transform(), CryptoStreamMode.Read);
    }
    public Stream ToBytes()
    {
        return _file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}

public class AttachmentRemote: IAttachment {
    private readonly byte[] _b64Bytes;
    public string FileName { get; }
    public long FileSize { get; }
    public string MimeType { get; }
    public AttachmentRemote(string base64, string fileName, string mimeType)
    {
        _b64Bytes = Encoding.ASCII.GetBytes(base64);
        FileName = fileName;
        FileSize = base64.Length / 4 * 3
            - (base64[^1] == '=' ? 1 : 0)
            - (base64[^2] == '=' ? 1 : 0);
        MimeType = mimeType;
    }
    public Stream ToBase64()
    {
        return new MemoryStream(_b64Bytes);
    }
    public Stream ToBytes()
    {
        return new CryptoStream(new MemoryStream(_b64Bytes), new FromBase64Transform(), CryptoStreamMode.Read);
    }
}