namespace TriScript.Data
{
    public class Variable
    {
        public Variable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public Value Value { get; set; } = Value.Nothing;
    }
}
