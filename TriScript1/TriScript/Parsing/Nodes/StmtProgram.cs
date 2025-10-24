using TriScript.Scanning;

namespace TriScript.Parsing.Nodes
{
    public sealed class StmtProgram : Stmt
    {
        public StmtProgram(Token token, StmtBlock block) : base(token)
        {
            Block = block;
        }

        public StmtBlock Block { get; }

        public override bool Accept<T>(INodeVisitor<T> visitor, out T? result) where T : default
        {
            return visitor.Visit(this, out result);
        }
    }
}
