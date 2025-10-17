using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Runtime
{
    public class Variable
    {
        public Variable(Token id)
        {
            Identifier = id;
        }
        
        public Token Identifier { get; }
        public EVariableType Type { get; private set; }
        public TuValue Value { get; private set; } = TuValue.Nothing;
        public string Name => Identifier.value;

        public void Protect()
        {
            Type = EVariableType.Protected;
        }

        public void Unprotect()
        {
            Type = EVariableType.Normal;
        }

        public void Assign(TuValue value)
        {
            if (Type == EVariableType.Protected)
            {
                throw new RunTimeException(
                    $"Cannot assign to protected variable '{Identifier}'.",
                    Identifier);
            }

            EDataType type = Value.type;
            if (type == EDataType.Nothing || type == value.type)
            {
                Value = value;
                return;
            }

            if (type == EDataType.Float && value.type == EDataType.Integer)
            {
                Value = new TuValue(value.AsNumeric());
                return;
            }

            throw new RunTimeException(
                        $"Type mismatch assigning to '{Identifier}': " +
                        $"existing type {type}, attempted {value.type}.",
                        Identifier);
        }

        public override string ToString()
        {
            return $"{Identifier.value}";
        }
    }
}
