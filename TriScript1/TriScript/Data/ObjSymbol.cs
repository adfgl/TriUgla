namespace TriScript.Data
{
    public class ObjSymbol : Obj
    {
        public ObjSymbol(string value) : base(EDataType.Symbol)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}
