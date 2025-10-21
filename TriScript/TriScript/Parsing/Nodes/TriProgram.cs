using TriScript.Parsing.Nodes.Statements;

namespace TriScript.Parsing.Nodes
{
    public class TriProgram : Stmt
    {
        public TriProgram(StmtBlock block)
        {
            Block = block;
        }

        public StmtBlock Block { get; }

        public override void Evaluate(Executor ex)
        {
            Block.Evaluate(ex);
        }
    }
}
