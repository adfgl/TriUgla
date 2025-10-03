using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Error
    {
        public Error(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }

        public int Line { get; set; }   
        public int Column { get; set; }
        public string Message { get; set; } = string.Empty;

        public static Error InvalidOperator(Value left, Token op, Value right)
        {
            return new Error(op.line, op.column, $"Operator '{op.type}' cannot be applied to operands of type '{left.type}' and '{right.type}'.");
        }

        public static Error InvalidOperator(Token op, Value operand)
        {
            return new Error(op.line, op.column, $"Operator '{op.type}' cannot be applied to operand of type '{operand.type}'.");
        }
    }
}
