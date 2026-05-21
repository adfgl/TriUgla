namespace TriUgla.Script.Data
{
    public abstract class ObjEntity<TId>(TId id) : Obj
     where TId : struct
    {
        public TId Id { get; } = id;
    }
}
