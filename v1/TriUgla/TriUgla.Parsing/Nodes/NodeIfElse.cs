using TriUgla.Parsing.Compiling;

namespace TriUgla.Parsing.Nodes
{
    public class NodeIfElse : INode
    {
        static (INode elif, NodeBlock elifBlock)[] s_empty = [];

        public NodeIfElse(INode @if, NodeBlock ifBlock, IEnumerable<(INode elif, NodeBlock elifBlock)>? elifs = null, NodeBlock? elseBlock = null)
        {
            If = @if;
            IfBlock = ifBlock;
            ElseIfs = elifs is null ? s_empty : elifs.ToArray();
            ElseBlock = elseBlock;
        }

        public NodeIfElse(INode @if, NodeBlock ifBlock, NodeBlock? elseBlock = null) : this(@if, ifBlock, null, elseBlock)
        {
            
        }

        public INode If { get; }
        public NodeBlock IfBlock { get; }
        public IReadOnlyList<(INode elif, NodeBlock elifBlock)> ElseIfs { get; }
        public NodeBlock? ElseBlock { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
