namespace EmailClient;

public class MimeIterator
{
    public static IEnumerable<MimeEntity> Get(MimeEntity root)
    {
        Stack<MimeEntity> stack = new();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is MimeMultipart multipart)
            {
                for (var i = multipart.Parts.Count - 1; i > 0; --i)
                    stack.Push(multipart.Parts[i]);
            }
            else
            {
                yield return current;
            }
        }
    }
}