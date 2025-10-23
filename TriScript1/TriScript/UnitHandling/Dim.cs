namespace TriScript.UnitHandling
{
    public readonly struct Dim : IEquatable<Dim>
    {
        /// <summary>Length (L)</summary>
        public readonly int l;

        /// <summary>Mass (M)</summary>
        public readonly int m;

        /// <summary>Time (T)</summary>
        public readonly int t;

        /// <summary>Electric current (I)</summary>
        public readonly int i;

        /// <summary>Temperature (Θ)</summary>
        public readonly int temp;

        /// <summary>Amount of substance (N)</summary>
        public readonly int n;

        /// <summary>Luminous intensity (J)</summary>
        public readonly int j;

        public Dim(int l, int m, int t, int i, int temp, int n, int j)
        {
            this.l = l;
            this.m = m;
            this.t = t;
            this.i = i;
            this.temp = temp;
            this.n = n;
            this.j = j;
        }

        #region BASE
        public static readonly Dim None = new(0, 0, 0, 0, 0, 0, 0);
        public static readonly Dim Length = new(1, 0, 0, 0, 0, 0, 0);
        public static readonly Dim Mass = new(0, 1, 0, 0, 0, 0, 0);
        public static readonly Dim Time = new(0, 0, 1, 0, 0, 0, 0);
        public static readonly Dim Current = new(0, 0, 0, 1, 0, 0, 0);
        public static readonly Dim Temperature = new(0, 0, 0, 0, 1, 0, 0);
        public static readonly Dim Substance = new(0, 0, 0, 0, 0, 1, 0);
        public static readonly Dim Luminosity = new(0, 0, 0, 0, 0, 0, 1);
        #endregion BASE

        #region MECHANICS
        public static readonly Dim Area = new(2, 0, 0, 0, 0, 0, 0);     // L²
        public static readonly Dim Volume = new(3, 0, 0, 0, 0, 0, 0);     // L³
        public static readonly Dim Velocity = new(1, 0, -1, 0, 0, 0, 0);    // L T⁻¹
        public static readonly Dim Acceleration = new(1, 0, -2, 0, 0, 0, 0);    // L T⁻²
        public static readonly Dim Force = new(1, 1, -2, 0, 0, 0, 0);    // M L T⁻²
        public static readonly Dim Pressure = new(-1, 1, -2, 0, 0, 0, 0);   // M L⁻¹ T⁻²
        public static readonly Dim Energy = new(2, 1, -2, 0, 0, 0, 0);    // M L² T⁻²
        public static readonly Dim Power = new(2, 1, -3, 0, 0, 0, 0);    // M L² T⁻³
        public static readonly Dim Momentum = new(1, 1, -1, 0, 0, 0, 0);    // M L T⁻¹
        public static readonly Dim AngularMomentum = new(2, 1, -1, 0, 0, 0, 0);    // M L² T⁻¹
        public static readonly Dim Density = new(-3, 1, 0, 0, 0, 0, 0);    // M L⁻³
        public static readonly Dim DynamicViscosity = new(-1, 1, -1, 0, 0, 0, 0);   // M L⁻¹ T⁻¹
        public static readonly Dim KinematicViscosity = new(2, 0, -1, 0, 0, 0, 0);    // L² T⁻¹
        public static readonly Dim SurfaceTension = new(0, 1, -2, 0, 0, 0, 0);    // M T⁻²
        public static readonly Dim Frequency = new(0, 0, -1, 0, 0, 0, 0);    // T⁻¹
        public static readonly Dim Diffusion = new(2, 0, -1, 0, 0, 0, 0);    // L² T⁻¹
        #endregion MECHANICS

        #region ELECTROMAGNETISM
        public static readonly Dim Charge = new(0, 0, 1, 1, 0, 0, 0);     // I T
        public static readonly Dim Voltage = new(2, 1, -3, -1, 0, 0, 0);   // M L² T⁻³ I⁻¹
        public static readonly Dim Capacitance = new(-2, -1, 4, 2, 0, 0, 0);   // M⁻¹ L⁻² T⁴ I²
        public static readonly Dim Resistance = new(2, 1, -3, -2, 0, 0, 0);   // M L² T⁻³ I⁻²
        public static readonly Dim Conductance = new(-2, -1, 3, 2, 0, 0, 0);   // M⁻¹ L⁻² T³ I²
        public static readonly Dim Inductance = new(2, 1, -2, -2, 0, 0, 0);   // M L² T⁻² I⁻²
        public static readonly Dim MagneticFlux = new(2, 1, -2, -1, 0, 0, 0);   // M L² T⁻² I⁻¹
        public static readonly Dim MagneticFluxDensity = new(0, 1, -2, -1, 0, 0, 0);  // M T⁻² I⁻¹
        #endregion ELECTROMAGNETISM

        #region THERMODYNAMICS
        public static readonly Dim HeatCapacity = new(2, 1, -2, 0, -1, 0, 0);   // M L² T⁻² Θ⁻¹
        public static readonly Dim ThermalConductivity = new(1, 1, -3, 0, -1, 0, 0);  // M L T⁻³ Θ⁻¹
        public static readonly Dim SpecificHeat = new(2, 0, -2, 0, -1, 0, 0);   // L² T⁻² Θ⁻¹
        public static readonly Dim Entropy = new(2, 1, -2, 0, -1, 0, 0);   // M L² T⁻² Θ⁻¹
        public static readonly Dim ThermalDiffusivity = new(2, 0, -1, 0, 0, 0, 0);    // L² T⁻¹
        #endregion THERMODYNAMICS

        #region FLUID_MECHANICS
        public static readonly Dim VolumetricFlowRate = new(3, 0, -1, 0, 0, 0, 0); // m^3/s  -> L^3 T^-1
        public static readonly Dim MassFlowRate = new(0, 1, -1, 0, 0, 0, 0); // kg/s   -> M T^-1
        public static readonly Dim SpecificWeight = new(-2, 1, -2, 0, 0, 0, 0); // N/m^3 -> M L^-2 T^-2
        public static readonly Dim Head = new(1, 0, 0, 0, 0, 0, 0);   // m     -> L
        public static readonly Dim Vorticity = new(0, 0, -1, 0, 0, 0, 0);  // s^-1  -> T^-1
        public static readonly Dim StrainRate = new(0, 0, -1, 0, 0, 0, 0);  // s^-1  -> T^-1
        public static readonly Dim Compressibility = new(1, -1, 2, 0, 0, 0, 0);  // Pa^-1 -> L M^-1 T^2
        public static readonly Dim BulkModulus = new(-1, 1, -2, 0, 0, 0, 0); // Pa    -> M L^-1 T^-2
        public static readonly Dim DarcyFlux = new(1, 0, -1, 0, 0, 0, 0);  // m/s   -> L T^-1 (a.k.a. specific discharge)
        #endregion FLUID_MECHANICS

        #region OPTICS_PHOTOMETRY
        public static readonly Dim LuminousFlux = new(0, 0, 0, 0, 0, 0, 1);   // lm    -> J
        public static readonly Dim Illuminance = new(-2, 0, 0, 0, 0, 0, 1);  // lx    -> J L^-2
        public static readonly Dim Luminance = new(-2, 0, 0, 0, 0, 0, 1);  // cd/m^2-> J L^-2
        public static readonly Dim LuminousEnergy = new(0, 0, 1, 0, 0, 0, 1);   // lm·s  -> J T
        public static readonly Dim LuminousExposure = new(-2, 0, 1, 0, 0, 0, 1);  // lx·s  -> J L^-2 T
        public static readonly Dim LuminousEfficacy = new(-2, -1, 3, 0, 0, 0, 1); // lm/W  -> M^-1 L^-2 T^3 J
        public static readonly Dim OpticalPower = new(-1, 0, 0, 0, 0, 0, 0);  // diopter -> L^-1
        #endregion OPTICS_PHOTOMETRY

        #region CHEMISTRY
        public static readonly Dim MolarConcentration = new(-3, 0, 0, 0, 0, 1, 0);  // mol/m^3 -> N L^-3
        public static readonly Dim MassConcentration = new(-3, 1, 0, 0, 0, 0, 0);  // kg/m^3  -> M L^-3
        public static readonly Dim Molality = new(0, -1, 0, 0, 0, 1, 0);  // mol/kg  -> N M^-1
        public static readonly Dim MolarMass = new(0, 1, 0, 0, 0, -1, 0);  // kg/mol  -> M N^-1
        public static readonly Dim MolarVolume = new(3, 0, 0, 0, 0, -1, 0);  // m^3/mol -> L^3 N^-1
        public static readonly Dim MolarEnergy = new(2, 1, -2, 0, 0, -1, 0); // J/mol   -> M L^2 T^-2 N^-1
        public static readonly Dim MolarEntropy = new(2, 1, -2, 0, -1, -1, 0);// J/(mol·K)-> M L^2 T^-2 Θ^-1 N^-1
        public static readonly Dim CatalyticActivity = new(0, 0, -1, 0, 0, 1, 0);  // kat     -> N T^-1
        public static readonly Dim CatActivityConc = new(-3, 0, -1, 0, 0, 1, 0); // kat/m^3 -> N L^-3 T^-1
        public static readonly Dim OsmoticPressure = new(-1, 1, -2, 0, 0, 0, 0); // Pa      -> M L^-1 T^-2
        #endregion CHEMISTRY

        public static Dim Pow(in Dim d, int p)
            => new(
                d.l * p, d.m * p, d.t * p, d.i * p, d.temp * p, d.n * p, d.j * p);

        public static Dim Sum(in Dim a, in Dim b)
            => new(
                a.l + b.l, a.m + b.m, a.t + b.t, a.i + b.i,
                a.temp + b.temp, a.n + b.n, a.j + b.j);

        public static Dim Div(in Dim a, in Dim b)
            => new(
                a.l - b.l, a.m - b.m, a.t - b.t, a.i - b.i,
                a.temp - b.temp, a.n - b.n, a.j - b.j);

        public bool Equals(Dim other)
            => l == other.l && m == other.m && t == other.t &&
               i == other.i && temp == other.temp &&
               n == other.n && j == other.j;

        public override bool Equals(object? obj)
            => obj is Dim d && Equals(d);

        public override int GetHashCode()
            => HashCode.Combine(l, m, t, i, temp, n, j);

        public override string ToString()
        {
            List<(string, int)> num = new List<(string, int)>();
            List<(string, int)> den = new List<(string, int)>();

            void addIfNonZero(string symbol, int power)
            {
                if (power > 0) num.Add((symbol, power));
                else if (power < 0) den.Add((symbol, power));
            }

            addIfNonZero("L", l);
            addIfNonZero("M", m);
            addIfNonZero("T", t);
            addIfNonZero("I", i);
            addIfNonZero("Θ", temp);
            addIfNonZero("N", n);
            addIfNonZero("J", j);

            // sort by smaller absolute power first
            num.Sort((a, b) 
                => Math.Abs(a.Item2).CompareTo(Math.Abs(b.Item2)));

            den.Sort((a, b) 
                => Math.Abs(a.Item2).CompareTo(Math.Abs(b.Item2)));

            string Format((string symbol, int power) p)
                => p.power == 1 || p.power == -1 ? p.symbol : $"{p.symbol}^{Math.Abs(p.power)}";

            string numStr = num.Count == 0 ? "1" : string.Join("·", num.Select(Format));
            string denStr = den.Count == 0 ? "" : string.Join("·", den.Select(Format));

            return den.Count == 0 ? numStr : $"{numStr} / ({denStr})";
        }
    }
}
