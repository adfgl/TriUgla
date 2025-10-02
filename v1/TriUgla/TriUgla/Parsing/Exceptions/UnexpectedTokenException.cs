using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Exceptions
{
    public class UnexpectedTokenException : RuntimeException
    {
        public Token Token { get; }

        public UnexpectedTokenException(Token token, ETokenType expected)
            : base($"Unexpected token. Expected '{expected}' but got '{token.type}'.", token.line, token.column)
        {
            Token = token;
        }

        public UnexpectedTokenException(Token token)
             : base($"Unexpected token '{token.type}'.", token.line, token.column)
        {
            Token = token;
        }
    }
}
