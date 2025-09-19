using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla
{
    public enum ETriangulatorState
    {
        None,
        Triangulated,
        Refined,
        Finalized,
    }

    public class Triangulator
    {
        ETriangulatorState _state = ETriangulatorState.None;

        public ETriangulatorState State => _state;

        public Triangulator Reset()
        {


            _state = ETriangulatorState.None;
            return this;
        }

        public Triangulator Triangulate()
        {
            if (_state != ETriangulatorState.None)
            {
                return this;
            }

            int[] created = new int[4];

            _state = ETriangulatorState.Triangulated;
            return this;
        }

        public Triangulator Refine()
        {
            if (_state != ETriangulatorState.Triangulated && _state != ETriangulatorState.Refined)
            {
                return this;
            }

            _state = ETriangulatorState.Refined;
            return this;
        }

        public Triangulator Finalize()
        {
            if (_state != ETriangulatorState.Triangulated && _state != ETriangulatorState.Refined)
            {
                return this;
            }

            _state = ETriangulatorState.Finalized;
            return this;
        }
    }
}
