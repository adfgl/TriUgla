namespace TriUgla.Script.Data.Collections
{
    public sealed class ObjRange(
        Value start,
        Value end,
        Value step) : Obj, IObjEnumerable
    {
        public Value Start { get; } = start;
        public Value End { get; } = end;
        public Value Step { get; } = step;

        public override DataKind Kind => DataKind.Range;

        public IEnumerable<Value> Enumerate()
        {
            double start = Start.AsDouble();
            double end = End.AsDouble();
            double step = Step.AsDouble();

            if (step == 0)
                throw new InvalidOperationException("Range step cannot be zero.");

            if (step > 0)
            {
                for (double x = start; x <= end; x += step)
                    yield return Number(x);
            }
            else
            {
                for (double x = start; x >= end; x += step)
                    yield return Number(x);
            }
        }

        static Value Number(double value)
        {
            double rounded = Math.Round(value);

            if (Math.Abs(value - rounded) < 1e-12)
                return new Value((int)rounded);

            return new Value(value);
        }

        public override string ToString()
            => $"{{{Start}:{End}:{Step}}}";
    }
}
