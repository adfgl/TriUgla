using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriScript.UnitHandling
{
    public sealed class Unit
    {
        public string Symbol { get; }
        public Dim Dim { get; }
        public double ScaleToSI { get; }      // multiplicative factor to SI
        public bool AllowPrefixes { get; }    // SI prefixes allowed (not for kg, °C, etc.)
        public bool IsAffine { get; }         // e.g., °C, °F
        public double OffsetToSI { get; }     // add after scaling when converting ABSOLUTE values to SI

        public Unit(string symbol, double scaleToSI, Dim dim,
                    bool allowPrefixes = true, bool isAffine = false, double offsetToSI = 0.0)
        {
            Symbol = symbol;
            ScaleToSI = scaleToSI;
            Dim = dim;
            AllowPrefixes = allowPrefixes;
            IsAffine = isAffine;
            OffsetToSI = offsetToSI;
        }
    }
}
