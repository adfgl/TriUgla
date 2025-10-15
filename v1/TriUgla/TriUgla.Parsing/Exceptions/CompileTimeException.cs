using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Exceptions
{
    public sealed class CompileTimeException : TuException
    {
        public CompileTimeException(string message, Token token, Exception? inner = null)
            : base(message, token, inner)
        {
        }

        public static CompileTimeException DivisionByZero(Token token)
        {
            return new CompileTimeException("Division by zero.", token);
        }

        public static CompileTimeException UnexpectedToken(Token token, string expected)
        {
            return new CompileTimeException(
                $"Unexpected token '{token.value}' of type {token.type}; expected {expected}.",
                token);
        }

        public static CompileTimeException Syntax(string message, Token token)
        {
            return new CompileTimeException($"Syntax error: {message}", token);
        }

        public static CompileTimeException Semantic(string message, Token token)
        {
            return new CompileTimeException($"Semantic error: {message}", token);
        }
    }
}
