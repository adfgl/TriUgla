
namespace TriScript.Data.Objects
{
    public sealed class ObjMatrix : Obj
    {
        readonly int _rows, _cols;

        public ObjMatrix(int rows, int cols) : base(EDataType.Matrix)
        {
            _rows = rows;
            _cols = cols;
        }

        public int Rows => _rows;
        public int Columns => _cols;
    }
}
