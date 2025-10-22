using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriScript.Data.Units
{
    public sealed class UnitBinding
    {
        public Dimension Dim { get; set; } = Dimension.None;   // e.g., L^2
        public double SiValue { get; set; } = double.NaN;      // canonical numeric in SI (m^l)
        public UnitEval Preferred { get; set; }                // last requested display unit
    }
}
