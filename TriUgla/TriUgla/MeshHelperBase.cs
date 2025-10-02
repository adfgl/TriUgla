namespace TriUgla
{
    public class MeshHelperBase
    {
        protected Mesh _mesh;
        protected int[] m_created = new int[4];

        public MeshHelperBase(Mesh mesh)
        {
            _mesh = mesh;
        }

        public Mesh Mesh
        {
            get => _mesh;
            set => _mesh = value;
        }
    }
}
