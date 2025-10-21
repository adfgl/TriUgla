namespace TriScript.Data.Objects
{
    public sealed class ObjString : Obj
    {
        public ObjString(string value) : base(EDataType.String)
        {
            Content = value;
        }

        public string Content { get; }

        public override string ToString()
        {
            return Content;
        }
    }
}
