namespace TriUgla.Script.Parsing
{
    public sealed class LoopTrack
    {
        public int Depth { get; private set; }
        public bool IsInsideLoop => Depth > 0;

        public void Enter() => Depth++;

        public void Exit()
        {
            if (Depth == 0)
                throw new InvalidOperationException("Loop depth underflow.");

            Depth--;
        }
    }
}
