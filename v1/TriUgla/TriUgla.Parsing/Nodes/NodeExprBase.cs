using System.Linq.Expressions;
using TriUgla.Parsing.Data;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Expressions;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public abstract class NodeExprBase : NodeBase
    {
        protected NodeExprBase(Token token) : base(token)
        {
        }
    }
}
