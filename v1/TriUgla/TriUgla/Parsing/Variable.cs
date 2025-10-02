using TriUgla.Parsing.Exceptions;

namespace TriUgla.Parsing
{
    public class Variable
    {
        public Token Token { get; }
        public string Name { get; }
        public EVariableType Type { get; }

        public Variable(Token symbol, EDataType dataType = EDataType.None, EVariableType type = EVariableType.Regular)
        {
            string id = symbol.value.ToString();
            if (id.Length == 0 || symbol.type != ETokenType.IdentifierLiteral)
            {
                throw new Exception("logic error");
            }

            Token = symbol;
            Name = id;
            DataType = dataType;
            Type = type;
            DataType = EDataType.None;
        }

        public EDataType DataType { get; private set; }
        public Value Value { get; private set; }
        public bool Assigned { get; private set; }

        public Variable Assign(Value newValue)
        {
            if (Assigned && Type != EVariableType.Regular)
            {
                throw new RuntimeException($"Cannot assign to constant '{Name}'.", Token.line, Token.column);
            }

            if (DataType == EDataType.None || DataType == newValue.type)
            {
                DataType = newValue.type;
                Value = newValue;
                Assigned = true;
                return this;
            }
            throw new RuntimeException($"Cannot implicitly convert type '{newValue.type}' to '{Value.type}'.", Token.line, Token.column);
        }
    }
}
