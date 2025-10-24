namespace TriScript.Data
{
    public abstract class Obj
    {
        protected Obj(EDataType type)
        {
            Type = type;
        }

        public EDataType Type { get; } = EDataType.None;
    }
}
