using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public struct Circler
    {
        readonly List<Triangle> _triangles;
        readonly int _start, _vertex;
        int _current;

        public Circler(List<Triangle> triangles, int triangleIndex, int vertex)
        {
            _triangles = triangles;
            _vertex = vertex;
            _start = triangleIndex;
            _current = triangleIndex;
        }

        public Circler(List<Triangle> triangles, Node vtx) : this(triangles, vtx.Triangle, vtx.Index)
        {

        }

        public int Current => _current;
        public int Vertex => _vertex;

        public bool Next()
        {
            Triangle t = _triangles[_current];
            t = t.Orient(t.IndexOf(_vertex));
            int next = t.adj0;
            if (next == _start || next == -1)
            {
                return false;
            }
            _current = next;
            return true;
        }
    }
}
