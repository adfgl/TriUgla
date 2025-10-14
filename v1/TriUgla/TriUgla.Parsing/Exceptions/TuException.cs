using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Exceptions
{
    public abstract class TuException : Exception
    {
        public Token Token { get; }

        protected TuException(string message, Token token, Exception? inner = null)
            : base(message, inner)
        {
            Token = token;
        }

        public override string ToString()
        {
            return $"{GetType().Name} at line {Token.line}, col {Token.column}: {Message}";
        }
    }
}
