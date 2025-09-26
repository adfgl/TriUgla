namespace TriUgla
{
    public class Quality
    {
        public double MaxArea { get; set; } = -1;

        public bool ShouldRefine()
        {
            return MaxArea > 0;
        }
    }
}
