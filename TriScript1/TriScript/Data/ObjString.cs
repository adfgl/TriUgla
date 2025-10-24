namespace TriScript.Data
{
    public sealed class ObjString : Obj
    {
        public ObjString(string content) : base(EDataType.String)
        {
            Content = String.IsNullOrEmpty(content) ? String.Empty : content;
        }

        public string Content { get; set; }

        public override string ToString()
        {
            return Content;
        }
    }
}
