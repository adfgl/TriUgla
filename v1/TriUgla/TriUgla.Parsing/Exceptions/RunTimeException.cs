using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Exceptions
{
    public sealed class RuntimeException : TuException
    {
        public RuntimeException(string message, Token token, Exception? inner = null)
            : base(message, token, inner)
        {
        }

        public static RuntimeException UndefinedVariable(string name, Token token)
        {
            return new RuntimeException($"Undefined variable '{name}'.", token);
        }

        public static RuntimeException DivisionByZero(Token token)
        {
            return new RuntimeException("Division by zero.", token);
        }

        public static RuntimeException InvalidOperation(string op, string leftType, string rightType, Token token)
        {
            return new RuntimeException($"Invalid operation '{op}' between {leftType} and {rightType}.", token);
        }

        public static RuntimeException Argument(string message, Token token)
        {
            return new RuntimeException($"Invalid argument: {message}", token);
        }
    }
}
