namespace TriScript.Scanning
{
    public class Keywords
    {
        readonly static Dictionary<string, ETokenType> _source = new Dictionary<string, ETokenType>()
        {

        };

        public static IReadOnlyDictionary<string, ETokenType> Source => _source;
    }
}
