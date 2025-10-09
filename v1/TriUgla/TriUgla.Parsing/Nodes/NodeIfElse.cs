using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeIfElse : INode
    {
        static (INode elif, NodeBlock elifBlock)[] s_empty = [];

        public NodeIfElse(Token token, INode @if, NodeBlock ifBlock, IEnumerable<(INode elif, NodeBlock elifBlock)>? elifs = null, NodeBlock? elseBlock = null)
        {
            Token = token;
            If = @if;
            IfBlock = ifBlock;
            ElseIfs = elifs is null ? s_empty : elifs.ToArray();
            ElseBlock = elseBlock;
        }

        public NodeIfElse(Token token, INode @if, NodeBlock ifBlock, NodeBlock? elseBlock = null) : this(token, @if, ifBlock, null, elseBlock)
        {
            
        }

        public Token Token { get; }
        public INode If { get; }
        public NodeBlock IfBlock { get; }
        public IReadOnlyList<(INode elif, NodeBlock elifBlock)> ElseIfs { get; }
        public NodeBlock? ElseBlock { get; }

        public TuValue Accept(INodeVisitor visitor) => visitor.Visit(this);
    }
}
