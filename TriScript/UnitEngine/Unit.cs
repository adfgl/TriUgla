using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitEngine
{

    public readonly struct Unit
    {
        public string Symbol { get; }
        public double ScaleToMeter { get; } // factor to meters
        public Dimension Dim { get; }

        public Unit(string symbol, double scaleToMeter, Dimension dim)
        {
            Symbol = symbol;
            ScaleToMeter = scaleToMeter;
            Dim = dim;
        }

        public override string ToString() => Symbol;
    }
}
