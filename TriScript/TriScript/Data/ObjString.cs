namespace TriScript.Data
{
    public sealed class ObjString : Obj
    {
        public ObjString(string value) : base(EDataType.String)
        {
            Content = value;
        }

        public string Content { get; set; }

        public override string ToString()
        {
            return Content;
        }
    }
}
