using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Exceptions
{
    public class VariableAlreadyDeclaredException : RuntimeException
    {
        public VariableAlreadyDeclaredException(Token identifier, bool declaredInThis)
            : base($"A local variable or function named '{identifier.value}' is already defined in {(declaredInThis ? "this" : "enclosing")} scope.", identifier.line, identifier.column)
        {
        }
    }
}
