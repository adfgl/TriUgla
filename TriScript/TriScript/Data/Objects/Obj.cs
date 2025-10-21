namespace TriScript.Data.Objects
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
