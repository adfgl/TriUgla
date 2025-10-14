using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Exceptions
{
    public sealed class CompiletimeException : TuException
    {
        public CompiletimeException(string message, Token token, Exception? inner = null)
            : base(message, token, inner)
        {
        }

        public static CompiletimeException UnexpectedToken(Token token, string expected)
        {
            return new CompiletimeException(
                $"Unexpected token '{token.value}' of type {token.type}; expected {expected}.",
                token);
        }

        public static CompiletimeException Syntax(string message, Token token)
        {
            return new CompiletimeException($"Syntax error: {message}", token);
        }

        public static CompiletimeException Semantic(string message, Token token)
        {
            return new CompiletimeException($"Semantic error: {message}", token);
        }
    }
}
