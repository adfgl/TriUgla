using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Exceptions
{
    public class InvalidOperatorException : RuntimeException
    {
        public InvalidOperatorException(Token op, Value left, Value right)
            : base($"Operator '{op.type}' cannot be applied to operands of type '{left.type}' and '{right.type}'.", op.line, op.column)
        {
        }

        public InvalidOperatorException(Token op, Value operand)
            : base($"Operator '{op.type}' cannot be applied to operand of type '{operand.type}'.", op.line, op.column)
        {

        }
    }
}
