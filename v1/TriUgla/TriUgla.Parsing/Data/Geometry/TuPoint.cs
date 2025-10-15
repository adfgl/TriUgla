namespace TriUgla.Parsing.Data.Geometry
{
    public class TuPoint : TuObject
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public override string ToString()
        {
            return $"[{Id}] {X}, {Y}, {Z}, {W}";
        }

        public override TuObject Clone()
        {
            return new TuPoint()
            {
                Id = Id,
                X = X,
                Y = Y,
                Z = Z,
                W = W
            };
        }
    }
}
