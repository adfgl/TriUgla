using System.Runtime.CompilerServices;

namespace TriUgla;

/// <summary>
/// Error-free floating-point expansion arithmetic for robust geometric predicates.
/// Components are stored from least to most significant.
/// </summary>
public static class Expansion
{
    /// <summary>2^27 + 1, used to split an IEEE 754 double into high and low words.</summary>
    public const double SPLITTER = 134217729d;

    /// <summary>Renormalizes an expansion in place and removes zero components.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compress(List<double> expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        if (expansion.Count == 0)
        {
            return 0;
        }

        double sum = 0d;
        int write = 0;
        for (int index = 0; index < expansion.Count; index++)
        {
            TwoSum(sum, expansion[index], out double high, out double low);
            if (low != 0d)
            {
                expansion[write++] = low;
            }

            sum = high;
        }

        if (sum != 0d)
        {
            expansion[write++] = sum;
        }

        if (write < expansion.Count)
        {
            expansion.RemoveRange(write, expansion.Count - write);
        }

        return write;
    }

    public static void Negate(List<double> expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        for (int index = 0; index < expansion.Count; index++)
        {
            expansion[index] = -expansion[index];
        }
    }

    public static int Sign(List<double> expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        for (int index = expansion.Count - 1; index >= 0; index--)
        {
            if (expansion[index] != 0d)
            {
                return expansion[index] > 0d ? 1 : -1;
            }
        }

        return 0;
    }

    public static double Approximate(List<double> expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        double sum = 0d;
        for (int index = 0; index < expansion.Count; index++)
        {
            sum += expansion[index];
        }

        return sum;
    }

    public static void Add(List<double> target, List<double> addend)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(addend);
        IReadOnlyList<double> values = ReferenceEquals(target, addend)
            ? addend.ToArray()
            : addend;
        for (int index = 0; index < values.Count; index++)
        {
            Add(target, values[index]);
        }
    }

    public static void Add(List<double> expansion, double value)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        if (value == 0d)
        {
            return;
        }

        int count = expansion.Count;
        for (int index = 0; index < count; index++)
        {
            TwoSum(expansion[index], value, out value, out double low);
            expansion[index] = low;
        }

        if (value != 0d)
        {
            expansion.Add(value);
        }
    }

    public static void Mul(List<double> target, List<double> factor)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(factor);
        if (target.Count == 0 || factor.Count == 0)
        {
            target.Clear();
            return;
        }

        double[] multiplicand = target.ToArray();
        double[] factors = factor.ToArray();
        var result = new List<double>(multiplicand.Length * factors.Length * 2);
        foreach (double value in factors)
        {
            if (value == 0d)
            {
                continue;
            }

            var partial = new List<double>(multiplicand);
            Mul(partial, value);
            Add(result, partial);
        }

        target.Clear();
        target.AddRange(result);
    }

    public static void Mul(List<double> expansion, double factor)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        if (factor == 0d || expansion.Count == 0)
        {
            expansion.Clear();
            return;
        }

        if (factor == 1d)
        {
            return;
        }

        var result = new List<double>(expansion.Count * 2);
        TwoProd(expansion[0], factor, out double accumulator, out double productLow);
        AppendNonZero(result, productLow);

        for (int index = 1; index < expansion.Count; index++)
        {
            TwoProd(expansion[index], factor, out double productHigh, out productLow);
            TwoSum(accumulator, productLow, out double sum, out double sumLow);
            AppendNonZero(result, sumLow);
            FastTwoSum(productHigh, sum, out accumulator, out double carryLow);
            AppendNonZero(result, carryLow);
        }

        AppendNonZero(result, accumulator);
        expansion.Clear();
        expansion.AddRange(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TwoSum(double a, double b, out double high, out double low)
    {
        high = a + b;
        double storedB = high - a;
        low = (a - (high - storedB)) + (b - storedB);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Split(double value, out double high, out double low)
    {
        double combined = SPLITTER * value;
        double large = combined - value;
        high = combined - large;
        low = value - high;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TwoProd(double a, double b, out double high, out double low)
    {
        high = a * b;
        Split(a, out double aHigh, out double aLow);
        Split(b, out double bHigh, out double bLow);
        low = ((aHigh * bHigh - high) + aHigh * bLow + aLow * bHigh) + aLow * bLow;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void FastTwoSum(double a, double b, out double high, out double low)
    {
        high = a + b;
        low = b - (high - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AppendNonZero(List<double> expansion, double value)
    {
        if (value != 0d)
        {
            expansion.Add(value);
        }
    }
}
