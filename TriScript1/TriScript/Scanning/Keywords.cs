namespace TriScript.Scanning
{
    public class Keywords
    {
        readonly static Dictionary<string, ETokenType> _source = new Dictionary<string, ETokenType>()
        {
            { "print", ETokenType.Print },
            { "is", ETokenType.Equal },
            { "not", ETokenType.Not },
            { "or", ETokenType.Or },
            { "and", ETokenType.And }
        };

        public static IReadOnlyDictionary<string, ETokenType> Source => _source;
    }
}
