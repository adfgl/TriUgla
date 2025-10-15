using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Exceptions
{
    public sealed class RunTimeException : TuException
    {
        public RunTimeException(string message, Token token, Exception? inner = null)
            : base(message, token, inner)
        {
        }

        public static RunTimeException UndefinedVariable(string name, Token token)
        {
            return new RunTimeException($"Undefined variable '{name}'.", token);
        }

        public static RunTimeException DivisionByZero(Token token)
        {
            return new RunTimeException("Division by zero.", token);
        }

        public static RunTimeException InvalidOperation(string op, string leftType, string rightType, Token token)
        {
            return new RunTimeException($"Invalid operation '{op}' between {leftType} and {rightType}.", token);
        }

        public static RunTimeException Argument(string message, Token token)
        {
            return new RunTimeException($"Invalid argument: {message}", token);
        }
    }
}
