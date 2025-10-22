using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriScript.Data.Units
{
    public readonly struct Unit
    {
        public string Symbol { get; }
        public double ScaleToMeter { get; } 
        public Dimension Dim { get; }

        public Unit(string symbol, double scaleToMeter, Dimension dim)
            => (Symbol, ScaleToMeter, Dim) = (symbol, scaleToMeter, dim);
    }
}
