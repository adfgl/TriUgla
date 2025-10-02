using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Exceptions
{
    public class UseOfUndeclaredVariableException : RuntimeException
    {
        public UseOfUndeclaredVariableException(Token token)
            : base($"Name '{token.value}' does not exist in current context.", token.line, token.column)
        {
        }
    }
}
