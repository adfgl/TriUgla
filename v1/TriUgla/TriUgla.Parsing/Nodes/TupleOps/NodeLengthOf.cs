using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes.TupleOps
{
    public class NodeLengthOf : NodeBase
    {
        public NodeLengthOf(Token token, NodeBase tpl) : base(token)
        {
            Tuple = tpl;
        }

        public NodeBase Tuple { get; }

        public override TuValue Evaluate(TuStack stack)
        {
            TuValue value = Tuple.Evaluate(stack);
            if (value.type == EDataType.Tuple)
            {
                return new TuValue(value.AsTuple()!.Values.Count);
            }
            throw new Exception($"Can't obtain length of value '{Tuple}'.");
        }
    }
}
